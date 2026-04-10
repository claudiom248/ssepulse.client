namespace SsePulse.Client.Core;

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