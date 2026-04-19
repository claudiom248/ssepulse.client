namespace SsePulse.Client.Common.Extensions;

internal static class ExceptionExtensions
{
    public static TException? FindInner<TException>(this Exception ex) where TException : Exception
    {
        Exception? innerException = ex.InnerException;
        while (innerException is not null)
        {
            if (innerException.GetType() == typeof(TException))
            {
                return (TException)innerException;
            }
            innerException = innerException.InnerException;
        }
        return null;
    }
}