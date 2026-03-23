using Microsoft.Extensions.Logging;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests;

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

    // --- Gruppo: Logger Initialization (2 tests) ---

    [Fact]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        using HttpClient client = new();
        SseSourceOptions options = new() { Path = "/events" };
        using SseSource source = new(client, options);
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Constructor_WithLogger_AcceptsLogger()
    {
        MockLogger<SseSource> logger = new();
        using HttpClient client = new();
        SseSourceOptions options = new() { Path = "/events" };
        using SseSource source = new(client, options, logger);
        Assert.False(source.IsConnected);
        Assert.Empty(logger.Logs);
    }

    // --- Gruppo: Connection Lifecycle Logging (3 tests) ---

    [Fact]
    public async Task StartConsumeAsync_LogsStartInformation()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
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
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
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
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "SSE connection closed gracefully"));
    }

    // --- Gruppo: Error Logging (3 tests) ---

    [Fact]
    public async Task StartConsumeAsync_NoHandler_LogsWarning()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "unknown", Data = "test" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);

        // ACT & ASSERT
        await Assert.ThrowsAsync<HandlerNotFoundException>(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));        
        Assert.True(logger.HasLog(LogLevel.Warning, "No handler found"));
        Assert.True(logger.HasLog(LogLevel.Warning, "unknown"));
    }

    [Fact]
    public async Task StartConsumeAsync_HandlerThrows_LogsError()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => throw new InvalidOperationException("Handler error"));
        source.OnError = _ => { }; // Suppress default error handler

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Error, "Error occurred while handling event", typeof(InvalidOperationException)));
    }

    [Fact]
    public async Task StartConsumeAsync_HttpError_LogsError()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: true))
        {
            BaseAddress = new Uri("https://example.com")
        };
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);
        
        // ACT & ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Error while establishing a connection with SSE endpoint", typeof(HttpRequestException)));
    }

    // --- Gruppo: Connection Lost Logging (2 tests) ---

    [Fact]
    public async Task StartConsumeAsync_ConnectionLost_LogsError()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: false));
        client.BaseAddress = new Uri("https://example.com");
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);
        source.On("message", _ => { });
        
        // ACT & ASSERT
        await Assert.ThrowsAsync<IOException>(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Connection lost", typeof(IOException)));
    }

    [Fact]
    public async Task StartConsumeAsync_ExceptionDuringConsumption_LogsError()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        using HttpClient client = new(new SseCrashHandler(failImmediately: true))
        {
            BaseAddress = new Uri("https://example.com")
        };
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" }, logger);

        // ACT & ASSERT
        await Assert.ThrowsAsync<HttpRequestException>(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.True(logger.HasLog(LogLevel.Error, "Exception occurred during SSE consumption"));
    }

    // --- Gruppo: Stop Logging (1 test) ---

    [Fact]
    public async Task StopAsync_LogsInformation()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" },
            new SseEvent { EventType = "e", Data = "3" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", async _ => await Task.Delay(5000)); // Handler lento per permettere lo stop

        // ACT
        Task consumeTask = source.StartConsumeAsync(CancellationToken.None);
        await Task.Delay(100); // Aspetta che la connessione sia stabilita
        await source.StopAsync();
        await consumeTask;

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Information, "Stopping SSE consumption"));
    }

    // --- Gruppo: Cancellation Logging (1 test) ---

    [Fact]
    public async Task StartConsumeAsync_Canceled_LogsInformation()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });
        
        // ACT & ASSERT
        await Assert.ThrowsAsync<OperationCanceledException>(() => source.StartConsumeAsync(new CancellationToken(true)));
        Assert.True(logger.HasLog(LogLevel.Information, "SSE consumption canceled"));
    }

    // --- Gruppo: Debug Level Logging (2 tests) ---

    [Fact]
    public async Task StartConsumeAsync_StreamOpened_LogsDebug()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Debug, "SSE stream opened successfully"));
    }

    [Fact]
    public async Task StartConsumeAsync_WithLastEventId_LogsDebug()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { Id = "123", EventType = "e", Data = "1" },
            new SseEvent { Id = "456", EventType = "e", Data = "2" });
        MockHttpMessageHandler handler = new(sse);
        using HttpClient client = MockSseHelpers.CreateHttpClientWithHandler(handler);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        await source.StopAsync();
        source.Reset();
        logger.Clear();
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.True(logger.HasLog(LogLevel.Debug, "Resuming SSE stream from Last-Event-ID"));
        Assert.True(logger.HasLog(LogLevel.Debug, "456"));
    }

    // --- Gruppo: Multiple Events Logging (1 test) ---

    [Fact]
    public async Task StartConsumeAsync_MultipleEvents_LogsCorrectCount()
    {
        // ARRANGE
        MockLogger<SseSource> logger = new();
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" },
            new SseEvent { EventType = "e", Data = "3" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = new(client, DefaultOptions, logger);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(10).Token);

        // ASSERT
        // Dovrebbe loggare: Starting, StreamOpened, ConnectionEstablished, ConnectionClosed, Canceled
        Assert.True(logger.CountLogs(LogLevel.Information) >= 3);
        Assert.Equal(1, logger.CountLogs(LogLevel.Debug)); // Stream opened
    }
}
