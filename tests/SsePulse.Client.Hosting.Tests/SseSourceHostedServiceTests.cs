using Microsoft.Extensions.Logging;
using NSubstitute;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Tests.Common;

namespace SsePulse.Client.Hosting.Tests;

public class SseSourceHostedServiceTests
{
    [Fact]
    public async Task StartAsync_StartSseSource()
    {
        // ARRANGE
        MockLogger<SseSourceHostedService> logger = new();
        ISseSourceControl? sseSourceControlMock = Substitute.For<ISseSourceControl>();
        SseSourceHostedService hostedService = new(sseSourceControlMock, logger);
        CancellationToken cancellationToken = CancellationToken.None;

        // ACT
        _ = hostedService.StartAsync(cancellationToken);
        await hostedService.ExecuteTask!;

        // ASSERT
        await sseSourceControlMock.Received(1)
            .StartConsumeAsync(Arg.Any<CancellationToken>());
        logger.HasLog(LogLevel.Information, "Start consuming from SseSource...");
        logger.HasLog(LogLevel.Information, "Finished consuming from SseSource...");
    }
    
    [Fact]
    public async Task StartAsync_WhenExceptionIsThrown_LogsException()
    {
        // ARRANGE
        MockLogger<SseSourceHostedService> logger = new();
        ISseSourceControl? sseSourceControlMock = Substitute.For<ISseSourceControl>();
        sseSourceControlMock.StartConsumeAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException()));
        SseSourceHostedService hostedService = new(sseSourceControlMock, logger);
        CancellationToken cancellationToken = CancellationToken.None;

        // ACT
        _ = hostedService.StartAsync(cancellationToken);
        Exception? ex = await Record.ExceptionAsync(() => hostedService.ExecuteTask!);

        // ASSERT
        await sseSourceControlMock.Received(1)
            .StartConsumeAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(ex);
        logger.HasLog(LogLevel.Error, "Error while consuming from SseSource", typeof(InvalidOperationException));
        logger.HasLog(LogLevel.Information, "Finished consuming from SseSource...");
    }
    
    [Fact]
    public async Task StopAsync_WhenServiceIsRunning_ExecutionCompletes()
    {
        // ARRANGE
        MockLogger<SseSourceHostedService> logger = new();
        ISseSourceControl sseSourceControlMock = Substitute.For<ISseSourceControl>();
        TaskCompletionSource<bool> consuming = new(TaskCreationOptions.RunContinuationsAsynchronously);

        sseSourceControlMock
            .StartConsumeAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                consuming.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.ArgAt<CancellationToken>(0));
            });

        SseSourceHostedService hostedService = new(sseSourceControlMock, logger);

        // ACT
        await hostedService.StartAsync(CancellationToken.None);
        await consuming.Task; // Ensure the background loop has actually been entered before stopping
        await hostedService.StopAsync(CancellationToken.None);

        // ASSERT
        await sseSourceControlMock.Received(1)
            .StartConsumeAsync(Arg.Any<CancellationToken>());
        Assert.True(hostedService.ExecuteTask!.IsCompleted);
    }
}