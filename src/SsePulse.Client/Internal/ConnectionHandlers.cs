namespace SsePulse.Client.Internal;

internal class ConnectionHandlers
{
    public Func<ValueTask> OnConnectionEstablished { get; set; } = () => ValueTask.CompletedTask;
    public Func<ValueTask> OnConnectionClosed { get; set; } = () => ValueTask.CompletedTask;
    public Func<Exception, ValueTask> OnConnectionLost { get; set; } = _ => ValueTask.CompletedTask;
}

