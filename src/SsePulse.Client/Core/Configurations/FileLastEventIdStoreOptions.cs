namespace SsePulse.Client.Core.Configurations;

/// <summary>
/// Options for <see cref="FileLastEventIdStore"/>.
/// </summary>
public sealed class FileLastEventIdStoreOptions
{
    /// <summary>
    /// Gets or sets the path of the file used to persist the last event ID.
    /// The directory must already exist; the file is created automatically if it does not exist yet.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strategy that controls how often the last event ID is written to disk.
    /// Defaults to <see cref="FlushMode.EverySet"/>.
    /// </summary>
    public FlushMode FlushMode { get; set; } = FlushMode.EverySet;

    /// <summary>
    /// Gets or sets the number of <c>Set</c> calls that must accumulate before the last event ID
    /// is flushed to disk when <see cref="FlushMode"/> is <see cref="FlushMode.AfterCount"/>.
    /// Must be greater than zero. Defaults to <c>10</c>.
    /// </summary>
    public int FlushAfterCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the interval between automatic flushes to disk when
    /// <see cref="FlushMode"/> is <see cref="FlushMode.AfterInterval"/>.
    /// Must be greater than <see cref="TimeSpan.Zero"/>. Defaults to <c>10 seconds</c>.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);
}

