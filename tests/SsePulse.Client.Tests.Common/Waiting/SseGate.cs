namespace SsePulse.Client.Tests.Common;

public sealed class SseGate
{
    private readonly TaskCompletionSource _opened = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsOpen => _opened.Task.IsCompleted;

    public void Open() => _opened.TrySetResult();

    public Task WaitAsync(CancellationToken cancellationToken = default) => _opened.Task.WaitAsync(cancellationToken);
}
