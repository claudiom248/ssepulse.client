using System.Net.Http;
using Microsoft.Extensions.Logging;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceLoggingTests
{
    private const int DefaultCancellationTokenDelay = 500;

    private static readonly HttpClient DefaultClient = new()
    {
        BaseAddress = new Uri("https://example.com")
    };

    private static readonly SseSourceOptions DefaultOptions = new()
    {
        Path = "/sse",
        MaxDegreeOfParallelism = 1
    };

    [Fact]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        // ARRANGE
        SseSourceOptions options = new() { Path = "/events" };
        using HttpClient client = new();

        // ACT
        using Core.SseSource source = new(client, options);

        // ASSERT
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Constructor_WithLogger_AcceptsLogger()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        using HttpClient client = new();
        SseSourceOptions options = new() { Path = "/events" };

        // ACT
        using Core.SseSource source = new(client, options, logger);

        // ASSERT
        Assert.False(source.IsConnected);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task StartConsumeAsync_LogsStartInformation()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "Starting SSE consumption"));
    }

    [Fact]
    public async Task StartConsumeAsync_ConnectionEstablished_LogsInformation()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "SSE connection established"));
    }

    [Fact]
    public async Task StartConsumeAsync_ConnectionClosed_LogsInformation()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "SSE connection closed gracefully"));
    }

    [Fact]
    public async Task StartConsumeAsync_NoHandler_LogsError()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "unknown", Data = "test" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(
            client, 
            new SseSourceOptions
            {
                ThrowWhenEventHandlerNotFound = true
            }, 
            logger);

        // ACT & ASSERT
        await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "No handler found"));
        Assert.True(logger.HasLog(LogLevel.Error, "unknown"));
    }

    [Fact]
    public async Task StartConsumeAsync_HandlerThrows_LogsError()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => throw new InvalidOperationException("Handler error"));
        source.OnError = _ => { }; // Suppress default error handler

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Error, "Error occurred while handling event",
            typeof(InvalidOperationException)));
    }

    [Fact]
    public async Task StartConsumeAsync_HttpError_LogsError()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: true))
        {
            BaseAddress = new Uri("https://example.com")
        };
        using Core.SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);

        // ACT & ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Error while establishing a connection with SSE endpoint",
            typeof(HttpRequestException)));
    }

    [Fact]
    public async Task StartConsumeAsync_ConnectionLost_LogsError()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: false));
        client.BaseAddress = new Uri("https://example.com");
        using Core.SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);
        source.On("message", _ => { });

        // ACT & ASSERT
        await Assert.ThrowsAsync<IOException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Connection lost", typeof(IOException)));
    }

    [Fact]
    public async Task StartConsumeAsync_ExceptionDuringConsumption_LogsError()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: true))
        {
            BaseAddress = new Uri("https://example.com")
        };
        using Core.SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);

        // ACT & ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Exception occurred during SSE consumption"));
    }

    [Fact]
    public async Task StopAsync_LogsInformation()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" },
            new SseEvent { EventType = "e", Data = "3" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", async _ => await Task.Delay(5000)); // Handler lento per permettere lo stop

        // ACT
        Task consumeTask = source.StartConsumeAsync(CancellationToken.None);
        await Task.Delay(100); // Aspetta che la connessione sia stabilita
#if NET8_0_OR_GREATER
        await source.StopAsync();
#else
        source.Stop();
#endif        
        await consumeTask;

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "Stopping SSE consumption"));
    }

    [Fact]
    public async Task StartConsumeAsync_Canceled_LogsInformation()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationToken(true));
        
        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "SSE consumption canceled"));
    }

    [Fact]
    public async Task StartConsumeAsync_StreamOpened_LogsDebug()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Debug, "SSE stream opened successfully"));
    }

    [Fact]
    public async Task StartConsumeAsync_WithLastEventIdMutator_LogsDebug()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { Id = "123", EventType = "e", Data = "1" },
            new SseEvent { Id = "456", EventType = "e", Data = "2" });
        MockHttpMessageHandler handler = new(sse);
        using HttpClient client = MockSseHelpers.CreateHttpClientWithHandler(handler);
        InMemoryLastEventIdStore inMemoryLastEventIdStore = new();
        using Core.SseSource source = new(client, DefaultOptions,
            [new LastEventIdRequestMutator(inMemoryLastEventIdStore, logger)], inMemoryLastEventIdStore, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
#if NET8_0_OR_GREATER
        await source.StopAsync();
#else
        source.Stop();
#endif
        source.Reset();
        logger.Clear();
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Debug, "Resuming SSE stream from Last-Event-ID"));
        Assert.True(logger.HasLog(LogLevel.Debug, "456"));
    }

    [Fact]
    public async Task StartConsumeAsync_MultipleEvents_LogsCorrectCount()
    {
        // ARRANGE
        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" },
            new SseEvent { EventType = "e", Data = "3" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.CountLogs(LogLevel.Information) >= 3);
        Assert.Equal(1, logger.CountLogs(LogLevel.Debug)); // Stream opened
    }

    [Fact]
    public async Task StartConsumeAsync_WhenMutatorThrows_LogsErrorCorrectly()
    {
        // ARRANGE
        MockRequestMutator failingMutator = new(_ =>
            throw new InvalidOperationException("Mutator error"));

        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, DefaultOptions, [failingMutator], null, logger);
        source.On("e", _ => { });

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Error while applying request mutator"));
        Assert.True(logger.HasLog(LogLevel.Error, "Mutator error", typeof(InvalidOperationException)));
    }

    [Fact]
    public async Task StartConsumeAsync_WhenFirstMutatorThrows_DoesNotApplySubsequentMutators()
    {
        // ARRANGE
        List<int> callOrder = [];

        MockRequestMutator mutator1 = new(_ =>
        {
            callOrder.Add(1);
            throw new InvalidOperationException("Error 1");
        });

        MockRequestMutator mutator2 = new(_ =>
        {
            callOrder.Add(2);
            return Task.CompletedTask;
        });

        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, new SseSourceOptions
        {
            Path = "/sse"
        }, [mutator1, mutator2], null, logger);
        source.On("e", _ => { });

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.Equal([1], callOrder);
        Assert.True(logger.HasLog(LogLevel.Error, "Error while applying request mutator"));
        Assert.True(logger.HasLog(LogLevel.Error, "Error 1", typeof(InvalidOperationException)));
    }

    [Fact]
    public async Task StartConsumeAsync_WithSuccessfulMutators_DoesNotLogErrors()
    {
        // ARRANGE
        MockRequestMutator successMutator = new(req =>
        {
            req.Headers.Add("X-Custom", "value");
            return Task.CompletedTask;
        });

        MockLogger<Core.SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        using Core.SseSource source = new(client, new SseSourceOptions
        {
            Path = "/sse"
        }, [successMutator], null, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        int errorLogCount = logger.CountLogs(LogLevel.Error);
        Assert.Equal(0, errorLogCount);
    }
}