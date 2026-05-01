using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Configurations;

namespace SsePulse.Client.Core;

/// <summary>
/// Persists the last event ID to a file so that the SSE connection can be resumed after a
/// process restart.
/// </summary>
/// <remarks>
/// <para>
/// The file is read once during construction to restore the ID from a previous run.
/// Writes are controlled by the <see cref="FlushMode"/> option:
/// </para>
/// <list type="bullet">
///   <item><see cref="FlushMode.EverySet"/> (default) — safest; writes on every received event ID.</item>
///   <item><see cref="FlushMode.AfterCount"/> — writes every N events; reduces I/O at the cost of
///     potentially losing a few IDs on an unexpected crash.</item>
///   <item><see cref="FlushMode.AfterInterval"/> — writes on a timer; lowest I/O but the most
///     events may be lost on crash. The pending write is always flushed on dispose.</item>
/// </list>
/// </remarks>
public sealed class FileLastEventIdStore : ILastEventIdStore, IDisposable
{
    private readonly string _filePath;
    private readonly FlushMode _flushMode;
    private readonly int _flushAfterCount;
    
    private readonly object _fileLock = new();
    private volatile string? _lastEventId;
    private volatile bool _pendingFlush;
    private int _count;
    private bool _disposed;
    private Timer? _flushTimer;
    
    // ReSharper disable once NotAccessedField.Local
    private readonly ILogger<FileLastEventIdStore> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLastEventIdStore"/> using the supplied options.
    /// If the file already contains a value it is loaded immediately so that the previous session's
    /// last event ID is available before any <see cref="Set"/> call.
    /// </summary>
    /// <param name="options">Options that control the file path and flush behavior.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/> when omitted.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="FileLastEventIdStoreOptions.FilePath"/> is null or whitespace,
    /// <see cref="FileLastEventIdStoreOptions.FlushAfterCount"/> is not greater than zero, or
    /// <see cref="FileLastEventIdStoreOptions.FlushInterval"/> is not greater than <see cref="TimeSpan.Zero"/>.
    /// </exception>
    public FileLastEventIdStore(FileLastEventIdStoreOptions options, ILogger<FileLastEventIdStore>? logger = null)
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
        _lastEventId = TryReadFromFile();

        if (options.FlushMode == FlushMode.AfterInterval)
        {
            _flushTimer = new Timer(
                _ => FlushIfPending(),
                state: null,
                dueTime: options.FlushInterval,
                period: options.FlushInterval);
        }
        
        _logger = logger ?? NullLogger<FileLastEventIdStore>.Instance;
    }

    /// <inheritdoc/>
    public string? LastEventId => _lastEventId;

    /// <inheritdoc/>
    public void Set(string eventId)
    {
        _lastEventId = eventId;
        switch (_flushMode)
        {
            case FlushMode.EverySet:
                WriteToFile(eventId);
                break;
            case FlushMode.AfterCount:
                if (++_count % _flushAfterCount == 0)
                {
                    WriteToFile(eventId);
                    _pendingFlush = false;
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

    /// <summary>
    /// Flushes any pending writes to disk
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _flushTimer?.Dispose();
        _flushTimer = null;
        FlushIfPending();
    }

    private void FlushIfPending()
    {
        if (!_pendingFlush)
        {
            return;
        }

        string? id = _lastEventId;
        if (id is null)
        {
            return;
        }

        _pendingFlush = false;
        WriteToFile(id);
    }

    private void WriteToFile(string eventId)
    {
        lock (_fileLock)
        {
            string temp = _filePath + ".tmp";
            File.WriteAllText(temp, eventId);

#if NETSTANDARD2_0
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
#endif
            File.Move(temp, _filePath
#if !NETSTANDARD2_0
                , overwrite: true
#endif
            );
        }
    }

    private string? TryReadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string content = File.ReadAllText(_filePath).Trim();
            return string.IsNullOrEmpty(content) ? null : content;
        }
        catch (IOException)
        {
            return null;
        }
    }
}