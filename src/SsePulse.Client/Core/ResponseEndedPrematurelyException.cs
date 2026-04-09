namespace SsePulse.Client.Core;

public sealed class ResponseEndedPrematurelyException : Exception
{
#if NET8_0_OR_GREATER
    internal ResponseEndedPrematurelyException(HttpIOException ioEx) : base(ioEx.Message, ioEx)
    {
            
    }
#endif

    internal ResponseEndedPrematurelyException(IOException ioEx) : base(ioEx.Message, ioEx)
    {
            
    }
}