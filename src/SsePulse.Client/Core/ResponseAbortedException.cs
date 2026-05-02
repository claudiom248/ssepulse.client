namespace SsePulse.Client.Core;

/// <summary>
/// Thrown when the SSE server closes the response stream prematurely (connection abort).
/// When <see cref="SsePulse.Client.Core.Configurations.SseSourceOptions.RestartOnConnectionAbort"/> is <see langword="true"/>,
/// the connection loop restarts automatically instead of propagating this exception.
/// </summary>
public sealed class ResponseAbortedException : Exception
{
    internal ResponseAbortedException(Exception ex) : base(ex.Message, ex)
    {
            
    }
}