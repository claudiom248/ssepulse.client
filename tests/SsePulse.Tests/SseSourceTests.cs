using System.Text.Json;
using SsePulse.Common;

namespace SsePulse.Tests;

public class SseSourceTests
{
    private const int DefaultCancellationTokenDelay = 500;

    private static readonly HttpClient DefaultClient = new()
    {
        BaseAddress = new Uri("https://example.com")
    };
    private static readonly SseSourceOptions DefaultOptions = new() { Path = "/sse", MaxDegreeOfParallelism = 1 };

    private static SseSource CreateSource(HttpClient? client = null, SseSourceOptions? options = null) =>
        new(client ?? DefaultClient, options ?? DefaultOptions);

    // --- Gruppo: Constructor and Initialization (3 tests) ---

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        using HttpClient client = new();
        SseSourceOptions options = new() { Path = "/events" };
        using SseSource source = new(client, options);
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void IsConnected_Initially_ReturnsFalse()
    {
        using SseSource source = CreateSource();
        Assert.False(source.IsConnected);
    }

    [Fact]
    public void Completion_Initially_ReturnsNotCompletedTask()
    {
        using SseSource source = CreateSource();
        Task completion = source.Completion;
        Assert.NotNull(completion);
        Assert.False(completion.IsCompleted);
    }

    // --- Gruppo: Handler Registration (4 tests) ---

    [Fact]
    public void On_StringHandler_ReturnsChainableInstance()
    {
        using SseSource source = CreateSource();
        SseSource result = source.On("test", _ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericType_UsesTypeNameAsEventName()
    {
        using SseSource source = CreateSource();
        SseSource result = source.On<TestEventData>(_ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericWithCustomName_ReturnsChainableInstance()
    {
        using SseSource source = CreateSource();
        SseSource result = source.On<TestEventData>("custom", _ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_Chaining_WorksCorrectly()
    {
        using SseSource source = CreateSource();
        SseSource result = source.On("e1", _ => { }).On("e2", _ => { });
        Assert.Same(source, result);
    }

    // --- Gruppo: Event Handling and Dispatching (6 tests) ---

    [Fact]
    public async Task StartConsumeAsync_StringHandler_InvokesOnMatch()
    {
        string? received = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "test", Data = "hello" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On("test", d => received = d);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal("hello", received);
    }

    [Fact]
    public async Task StartConsumeAsync_TypedHandler_DeserializesJson()
    {
        TestEventData? received = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
            { EventType = "TestEventData", Data = "{\"Message\":\"ok\"}" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On<TestEventData>(d => received = d);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal("ok", received?.Message);
    }

    [Fact]
    public async Task StartConsumeAsync_CustomTypedHandler_InvokesCorrectly()
    {
        TestEventData? received = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "custom", Data = "{\"Message\":\"ok\"}" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On<TestEventData>("custom", d => received = d);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal("ok", received?.Message);
    }

    [Fact]
    public async Task StartConsumeAsync_MultipleEvents_DispatchesAll()
    {
        int count = 0;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On("e", _ => count++);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task StartConsumeAsync_NoHandler_DoesNotThrow()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "unknown", Data = "ignore" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        Exception? ex =
            await Record.ExceptionAsync(() => source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.Null(ex);
    }

    [Fact]
    public async Task StartConsumeAsync_HandlerError_InvokesOnError()
    {
        Exception? error = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On("e", _ => throw new Exception("fail"));
        source.OnError = ex => error = ex;

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.NotNull(error);
    }

    // --- Gruppo: Last-Event-ID (1 test) ---

    [Fact]
    public async Task StartConsumeAsync_WithId_UpdatesInternalState()
    {
        // ARRANGE
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { Id = "123", EventType = "e", Data = "1" },
            new SseEvent { Id = "456", EventType = "e", Data = "2" });
        MockHttpMessageHandler handler = new(sse);
        using HttpClient client = MockSseHelpers.CreateHttpClientWithHandler(handler);
        await using SseSource source = CreateSource(client);
        source.On("e", _ => { });
        
        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        await source.StopAsync();
        source.Reset();
        await source.StartConsumeAsync(new CancellationTokenSource(10000).Token);

        // ASSERT
        Assert.Equal("456", handler.LastEventIdSent);
    }

    // --- Gruppo: Connection Lifecycle (3 tests) ---

    [Fact]
    public async Task StartConsumeAsync_Establishment_InvokesCallback()
    {
        bool called = false;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.OnConnectionEstablished = () => called = true;
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.True(called);
    }

    [Fact]
    public async Task StartConsumeAsync_Running_SetsIsConnectedTrue()
    {
        bool? connected = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.On("e", _ => connected = source.IsConnected);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.True(connected);
    }

    [Fact]
    public async Task StartConsumeAsync_Closing_InvokesCallback()
    {
        bool called = false;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        source.OnConnectionLost = _ => called = true;
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.True(called);
    }

    // --- Gruppo: Control Flow and Reset (3 tests) ---

    [Fact]
    public async Task StartConsumeAsync_Twice_ThrowsInvalidOperationException()
    {
        await using SseSource source = CreateSource();
        Task task = source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StartConsumeAsync(default));
    }

    [Fact]
    public void Reset_WhileRunning_ThrowsInvalidOperationException()
    {
        using SseSource source = CreateSource();
        Assert.Throws<InvalidOperationException>(() => source.Reset());
    }

    [Fact]
    public async Task Reset_AfterCompletion_ReinitializesSource()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        source.Reset(); // Deve funzionare dopo il completamento
        Assert.False(source.IsConnected);
    }


    [Fact]
    public void OnConnectionEstablished_Setter_UpdatesValue()
    {
        SseSource source = CreateSource();
        source.OnConnectionEstablished = () => { };
        Assert.NotNull(source.OnConnectionEstablished);
    }

    [Fact]
    public void OnConnectionClosed_Setter_UpdatesValue()
    {
        SseSource source = CreateSource();
        source.OnConnectionClosed = () => { };
        Assert.NotNull(source.OnConnectionClosed);
    }

    [Fact]
    public void OnConnectionLost_Setter_UpdatesValue()
    {
        SseSource source = CreateSource();
        source.OnConnectionLost = _ => { };
        Assert.NotNull(source.OnConnectionLost);
    }

    [Fact]
    public void OnError_Setter_UpdatesValue()
    {
        SseSource source = CreateSource();
        source.OnError = _ => { };
        Assert.NotNull(source.OnError);
    }
    
    [Fact]
    public void Dispose_MultipleTimes_IsIdempotent()
    {
        SseSource source = CreateSource();
        source.Dispose();
        source.Dispose();
    }

    [Fact]
    public void Dispose_On_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public void Dispose_GenericOn_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.On<TestEventData>(_ => { }));
    }

    [Fact]
    public void Dispose_Reset_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.Reset());
    }

    [Fact]
    public void Dispose_Setter_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.OnConnectionEstablished = () => { });
    }

    [Fact]
    public async Task Dispose_Start_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StartConsumeAsync(CancellationToken.None));
    }

    // --- Gruppo: Stop and Async Dispose (6 tests) ---

    [Fact]
    public async Task DisposeAsync_NewInstance_CompletesSuccessfully()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_IsIdempotent()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
        await source.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => source.On("t", _ => { }));
    }

    [Fact]
    public async Task DisposeAsync_CancelsTcs()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
        Assert.True(source.Completion.IsCompleted);
    }

    [Fact]
    public async Task StopAsync_NotStarted_ThrowsInvalidOperationException()
    {
        await using SseSource source = CreateSource();
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.StopAsync());
    }

    [Fact]
    public async Task StopAsync_AfterDisposeAsync_ThrowsObjectDisposedException()
    {
        SseSource source = CreateSource();
        await source.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => source.StopAsync());
    }
    
    // --- Gruppo: 
    [Fact]
    public async Task StartConsumeAsync_InitialConnectionFail_InvokesOnConnectionLost()
    {
        // ARRANGE: Crash immediato alla GET
        using HttpClient client = new(new SseCrashHandler(failImmediately: true));
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(2000).Token);

        // ASSERT
        Assert.False(source.IsConnected);
        Assert.True(source.Completion.Exception is not null);
    }

    [Fact]
    public async Task StartConsumeAsync_MidStreamCrash_InvokesOnConnectionLost()
    {
        // ARRANGE: Connessione OK, ma crash durante la lettura
        Exception? capturedError = null;
        string? firstMessage = null;
        
        using HttpClient client = new(new SseCrashHandler(failImmediately: false))
        {
            BaseAddress = new Uri("https://example.com")
        };
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" });
        
        source.On("message", data => firstMessage = data);
        source.OnConnectionLost = ex => capturedError = ex;

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("healthy", firstMessage); // Il primo evento è passato
        Assert.NotNull(capturedError);         // Poi è arrivato il crash
        Assert.IsType<IOException>(capturedError);
        Assert.False(source.IsConnected);      // Lo stato deve essere tornato a 0
    }

    [Fact]
    public async Task StartConsumeAsync_AfterAllRetriesFail_SetsTcsException()
    {
        // ARRANGE: Configura retry minimi per non far durare il test una vita
        using HttpClient client = new(new SseCrashHandler(failImmediately: true));
        client.BaseAddress = new Uri("https://example.com");
        // Nota: Assicurati che Helpers.RunWithRetryAsync usi i parametri che gli passi
        await using SseSource source = new(client, new SseSourceOptions { Path = "/sse" });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT: Il Task Completion deve riflettere il fallimento
        await Assert.ThrowsAsync<HttpRequestException>(async () => await source.Completion);
    }
    
    // --- Gruppo: Reflection Binding (3 tests) ---

    [Fact]
    public void Bind_ReturnsChainableInstance()
    {
        using SseSource source = CreateSource();
        MockHandler handler = new();
        SseSource result = source.Bind(handler);
        Assert.Same(source, result);
    }

    [Theory]
    [InlineData("stock_updated", NameCasePolicy.SnakeCase)]
    [InlineData("stock-updated", NameCasePolicy.KebabCase)]
    [InlineData("stockUpdated", NameCasePolicy.CamelCase)]
    public async Task StartConsumeAsync_Bind_MapsCamelToConfiguredNameCases(string eventType, NameCasePolicy policy)
    {
        // ARRANGE
        MockHandler handler = new MockHandler();
        StockData stock = new StockData("MSFT", 400.0m);
        string sse = MockSseHelpers.BuildSseStream(new SseEvent 
        { 
            EventType = eventType, 
            Data = JsonSerializer.Serialize(stock)
        });
        
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client, new SseSourceOptions
        {
            DefaultEventNameCasePolicy = policy
        });
        
        // ACT
        source.Bind(handler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.NotNull(handler.LastStock);
        Assert.Equal("MSFT", handler.LastStock.Symbol);
        Assert.Equal(400.0m, handler.LastStock.Price);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_HandlesRawStringsCorrectly()
    {
        // ARRANGE
        MockHandler handler = new MockHandler();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent 
        { 
            EventType = "SimpleAlert", 
            Data = "System Overload" 
        });
        
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using SseSource source = CreateSource(client);
        
        // ACT
        source.Bind(handler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("System Overload", handler.LastMessage);
    }

    private class TestEventData
    {
        public string Message { get; set; } = "";
    }
    
    private class MockHandler : ISseEventsManager
    {
        public StockData? LastStock { get; private set; }
        public string? LastMessage { get; private set; }

        public void OnStockUpdated(StockData data) => LastStock = data;

        public void OnSimpleAlert(string message) => LastMessage = message;
    }

    private record StockData(string Symbol, decimal Price);
}