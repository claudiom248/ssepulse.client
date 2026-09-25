namespace SsePulse.Client;

/// <summary>
/// Controls what the source does when an event handler throws.
/// In both cases the exception is logged and passed to the <c>OnError</c> callback.
/// </summary>
public enum HandlerFailureBehavior
{
    /// <summary>
    /// The failed event is skipped: its event ID is stored like the ones of the events that succeeded and the
    /// source keeps processing the following events. This is the default.
    /// </summary>
    SkipAndAdvance,

    /// <summary>
    /// The source stops and its <c>Completion</c> task faults with the exception thrown by the handler. The event ID of
    /// the failed event is not stored, so the event is delivered again when the source is restarted (at-least-once).
    /// </summary>
    StopSource
}
