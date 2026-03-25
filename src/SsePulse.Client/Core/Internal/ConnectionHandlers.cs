namespace SsePulse.Client.Core.Internal;

internal class ConnectionHandlers
{
    public Action OnConnectionEstablished { get; set; } = () => { };
    public Action OnConnectionClosed { get; set; } = () => { };
    public Action<Exception> OnConnectionLost { get; set; } = _ => { };
}

