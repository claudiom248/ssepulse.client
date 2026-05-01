namespace SsePulse.Client.Core.Configurations;

/// <summary>
/// Controls how often <see cref="FileLastEventIdStore"/> writes the last event ID to disk.
/// </summary>
public enum FlushMode
{
    /// <summary>
    /// The last event ID is written to disk on every call to <c>Set</c>.
    /// Safest option — no data loss on crash — but generates the most I/O.
    /// </summary>
    EverySet,

    /// <summary>
    /// The last event ID is written to disk after a configurable number of <c>Set</c> calls.
    /// Reduces I/O at the cost of potentially losing the IDs received since the last flush if the
    /// process exits unexpectedly. Configure the threshold via
    /// <see cref="FileLastEventIdStoreOptions.FlushAfterCount"/>.
    /// </summary>
    AfterCount,

    /// <summary>
    /// The last event ID is written to disk on a repeating timer, regardless of how many
    /// events have been received. Configure the period via
    /// <see cref="FileLastEventIdStoreOptions.FlushInterval"/>.
    /// </summary>
    AfterInterval
}

