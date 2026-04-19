namespace SsePulse.Client.Core;

/// <summary>
/// Thrown when the SSE server closes the response stream prematurely (connection abort).
/// When <see cref="SsePulse.Client.Core.Configurations.SseSourceOptions.RestartOnConnectionAbort"/> is <see langword="true"/>,
/// the connection loop restarts automatically instead of propagating this exception.
/// </summary>
public sealed class ResponseAbortedException : Exception
{
#if NET8_0_OR_GREATER
    internal ResponseAbortedException(HttpIOException ioEx) : base(ioEx.Message, ioEx)
    {
            
    }
#endif

    internal ResponseAbortedException(IOException ioEx) : base(ioEx.Message, ioEx)
    {
            
    }
}