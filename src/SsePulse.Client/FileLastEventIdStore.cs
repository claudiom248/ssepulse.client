using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SsePulse.Client;

/// <summary>
/// Persists the last event ID to a file so that the SSE connection can be resumed after a
/// process restart.
/// <br/><br/>
/// <b>DOCS:</b> <see href="https://claudiom248.github.io/ssepulse.client/docs/last-event-id.html"/>
/// </summary>
/// <remarks>
/// <para>
/// The file is read lazily by the first call to <see cref="GetLastEventIdAsync"/> to restore the ID from a
/// previous run; the constructor performs no I/O.
/// Writes are controlled by the <see cref="FlushMode"/> option:
/// </para>
/// <list type="bullet">
///   <item><see cref="FlushMode.EverySet"/> (default) — safest; writes on every received event ID.</item>
///   <item><see cref="FlushMode.AfterCount"/> — writes every N events; reduces I/O at the cost of
///     potentially losing a few IDs on an unexpected crash. The pending writes is always flushed on dispose.</item>
///   <item><see cref="FlushMode.AfterInterval"/> — writes on a timer; lowest I/O but the most
///     events may be lost on a crash. The pending write is always flushed on disposal.</item>
/// </list>
/// </remarks>
public sealed class FileLastEventIdStore : ILastEventIdStore, IDisposable, IAsyncDisposable
{
    private readonly string _filePath;
    private readonly FlushMode _flushMode;
    private readonly int _flushAfterCount;

    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private volatile string? _lastEventId;
    private volatile bool _pendingFlush;
    private int _loaded;
    private int _count;
    private int _disposed;
    private ITimer? _flushTimer;

    private readonly ILogger<FileLastEventIdStore> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLastEventIdStore"/> using the supplied options.
    /// The file is not touched until the first call to <see cref="GetLastEventIdAsync"/> or <see cref="SetLastEventIdAsync"/>.
    /// </summary>
    /// <param name="options">Options that control the file path and flush behavior.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    /// <param name="timeProvider">Time provider used by the interval flush timer. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="FileLastEventIdStoreOptions.FilePath"/> is null or whitespace,
    /// <see cref="FileLastEventIdStoreOptions.FlushAfterCount"/> is not greater than zero, or
    /// <see cref="FileLastEventIdStoreOptions.FlushInterval"/> is not greater than <see cref="TimeSpan.Zero"/>.
    /// </exception>
    public FileLastEventIdStore(
        FileLastEventIdStoreOptions options,
        ILogger<FileLastEventIdStore>? logger = null,
        TimeProvider? timeProvider = null)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            throw new ArgumentException("FilePath must not be null or whitespace.", nameof(options));
        }

        if (options.FlushMode == FlushMode.AfterCount && options.FlushAfterCount <= 0)
        {
            throw new ArgumentException("FlushAfterCount must be greater than zero.", nameof(options));
        }

        if (options.FlushMode == FlushMode.AfterInterval && options.FlushInterval <= TimeSpan.Zero)
        {
            throw new ArgumentException("FlushInterval must be greater than TimeSpan.Zero.", nameof(options));
        }

        _filePath = options.FilePath;
        _flushMode = options.FlushMode;
        _flushAfterCount = options.FlushAfterCount;
        _logger = logger ?? NullLogger<FileLastEventIdStore>.Instance;

        if (options.FlushMode == FlushMode.AfterInterval)
        {
            _flushTimer = (timeProvider ?? TimeProvider.System).CreateTimer(
                _ => _ = FlushIfPendingAsync().AsTask(),
                state: null,
                dueTime: options.FlushInterval,
                period: options.FlushInterval);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<string?> GetLastEventIdAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _loaded) == 0)
        {
            await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_loaded == 0)
                {
                    (bool success, string? persisted) = await TryReadFromFileAsync(cancellationToken).ConfigureAwait(false);
                    _lastEventId ??= persisted;
                    if (success)
                    {
                        Volatile.Write(ref _loaded, 1);
                    }
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }

        return _lastEventId;
    }

    /// <inheritdoc/>
    public async ValueTask SetLastEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        _lastEventId = eventId;
        switch (_flushMode)
        {
            case FlushMode.EverySet:
                await WriteToFileAsync(cancellationToken).ConfigureAwait(false);
                break;
            case FlushMode.AfterCount:
                if (Interlocked.Increment(ref _count) % _flushAfterCount == 0)
                {
                    _pendingFlush = false;
                    await WriteToFileAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _pendingFlush = true;
                }
                break;
            case FlushMode.AfterInterval:
                _pendingFlush = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _flushTimer?.Dispose();
        _flushTimer = null;
        FlushIfPendingAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        if (_flushTimer is not null)
        {
            await _flushTimer.DisposeAsync().ConfigureAwait(false);
            _flushTimer = null;
        }

        await FlushIfPendingAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Flushes any pending writes to disk
    /// </summary>
    private async ValueTask FlushIfPendingAsync()
    {
        if (!_pendingFlush)
        {
            return;
        }

        _pendingFlush = false;
        await WriteToFileAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async ValueTask WriteToFileAsync(CancellationToken cancellationToken)
    {
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string? eventId = _lastEventId;
            if (eventId is null)
            {
                return;
            }

            string temp = _filePath + ".tmp";
            await File.WriteAllTextAsync(temp, eventId, cancellationToken).ConfigureAwait(false);
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _pendingFlush = true;
            _logger.LogError(ex, "Failed to persist the last event ID to '{FilePath}'", _filePath);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async ValueTask<(bool Success, string? Value)> TryReadFromFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return (true, null);
            }

            string content = (await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false)).Trim();
            return (true, string.IsNullOrEmpty(content) ? null : content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to read the last event ID from '{FilePath}'", _filePath);
            return (false, null);
        }
    }
}
