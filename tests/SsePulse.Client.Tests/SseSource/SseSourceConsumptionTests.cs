using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SsePulse.Client.Common.NamingPolicies;
using SsePulse.Client.Core;
using SsePulse.Client.Core.Abstractions;
using SsePulse.Client.Core.Attributes;
using SsePulse.Client.Core.Configurations;
using SsePulse.Client.Core.Internal;
using SsePulse.Client.Tests.Mocks;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceConsumptionTests : SseSourceTestBase
{
    [Fact]
    public async Task StartConsumeAsync_StringHandler_InvokesOnMatch()
    {
        string? received = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "test", Data = "hello" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
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
        await using Core.SseSource source = CreateSource(client);
        source.On<TestEventData>(d => received = d);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal("ok", received?.Message);
    }

    [Fact]
    public async Task StartConsumeAsync_CustomTypedHandler_InvokesCorrectly()
    {
        TestEventData? received = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
            { EventType = "custom", Data = "{\"Message\":\"ok\"}" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        source.On<TestEventData>("custom", d => received = d);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal("ok", received?.Message);
    }

    [Fact]
    public async Task StartConsumeAsync_MultipleEvents_DispatchesAll()
    {
        int count = 0;
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" },
            new SseEvent { EventType = "e", Data = "2" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        source.On("e", _ => count++);

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task StartConsumeAsync_NoHandler_DoesNotThrow()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "unknown", Data = "ignore" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        Exception? ex = await Record.ExceptionAsync(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.Null(ex);
    }
    [Fact]
    public async Task StartConsumeAsync_MultipleHandlersPerEventType_DispatchesAll()
    {
        // ARRANGE
        bool handler1Called = false;
        bool handler2Called = false;
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        
        // ACT
        source.On("e", _ => handler1Called = true);
        source.On("e", _ => handler2Called = true);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        
        // ASSERT
        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }
    
    [Fact]
    public async Task StartConsumeAsync_HandlerError_InvokesOnError()
    {
        Exception? error = null;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
        source.On("e", _ => throw new Exception("fail"));
        source.OnError = ex => error = ex;

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task StartConsumeAsync_MidStreamCrash_InvokesOnConnectionLost()
    {
        Exception? capturedError = null;
        string? firstMessage = null;

        using HttpClient client = new(new SseCrashHandler(failImmediately: false))
        {
            BaseAddress = new Uri("https://example.com")
        };
        await using Core.SseSource source = new(client, new SseSourceOptions { Path = "/sse" });

        source.On("message", data => firstMessage = data);
        source.OnConnectionLost = ex => capturedError = ex;

        await Assert.ThrowsAsync<IOException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.Equal("healthy", firstMessage);
        Assert.NotNull(capturedError);
        Assert.False(source.IsConnected);
    }

    [Fact]
    public async Task StartConsumeAsync_AfterAllRetriesFail_SetsTcsException()
    {
        using HttpClient client = new(new SseCrashHandler(failImmediately: true));
        client.BaseAddress = new Uri("https://example.com");
        await using Core.SseSource source = new(client, new SseSourceOptions { Path = "/sse" });

        _ = Task.Run(async () => await source.StartConsumeAsync(
            new CancellationTokenSource(DefaultCancellationTokenDelay).Token));

        await Assert.ThrowsAsync<HttpRequestException>(async () => await source.Completion);
    }
    
    [Fact]
    public async Task StartConsumeAsync_Establishment_InvokesCallback()
    {
        bool called = false;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);
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
        await using Core.SseSource source = CreateSource(client);
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
        await using Core.SseSource source = CreateSource(client);
        source.OnConnectionClosed = () => called = true;

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.True(called);
    }

    [Fact]
    public async Task StartConsumeAsync_WithNoMutators_StreamWorksNormally()
    {
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" });
        source.On("e", _ => { });

        Exception? ex = await Record.ExceptionAsync(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
        Assert.Null(ex);
    }

    [Fact]
    public async Task StartConsumeAsync_WithSingleMutator_AppliesMutations()
    {
        bool mutatorApplied = false;
        MockRequestMutator mutator = new(req =>
        {
            mutatorApplied = true;
            req.Headers.Add("X-Custom-Header", "mutator-value");
            return Task.CompletedTask;
        });

        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" }, [mutator]);
        source.On("e", _ => { });

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.True(mutatorApplied);
    }

    [Fact]
    public async Task StartConsumeAsync_WithMultipleMutators_AppliesInSequence()
    {
        List<int> callOrder = [];

        MockRequestMutator mutator1 = new(_ =>
        {
            callOrder.Add(1);
            return Task.CompletedTask;
        });
        MockRequestMutator mutator2 = new(_ =>
        {
            callOrder.Add(2);
            return Task.CompletedTask;
        });
        MockRequestMutator mutator3 = new(_ =>
        {
            callOrder.Add(3);
            return Task.CompletedTask;
        });

        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" },
            [mutator1, mutator2, mutator3]);
        source.On("e", _ => { });

        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        Assert.Equal([1, 2, 3], callOrder);
    }

    [Fact]
    public async Task StartConsumeAsync_WhenFirstMutatorThrows_ThrowsImmediately()
    {
        MockRequestMutator failingMutator1 = new(_ => throw new InvalidOperationException("Mutator 1 failed"));
        MockRequestMutator failingMutator2 = new(_ => throw new ArgumentException("Mutator 2 failed"));

        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" },
            [failingMutator1, failingMutator2]);
        source.On("e", _ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));
    }
    
    [Fact]
    public async Task StartConsumeAsync_WithLastEventIdMutator_UpdatesInternalState()
    {
        // ARRANGE
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { Id = "123", EventType = "e", Data = "1" },
            new SseEvent { Id = "456", EventType = "e", Data = "2" });
        MockHttpMessageHandler handler = new(sse);
        using HttpClient client = MockSseHelpers.CreateHttpClientWithHandler(handler);

        InMemoryLastEventIdStore inMemoryLastEventIdStore = new();
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions { Path = "/sse" },
            [new LastEventIdRequestMutator(inMemoryLastEventIdStore, NullLogger<Core.SseSource>.Instance)],
            inMemoryLastEventIdStore);
        source.On("e", _ => { });

        // ACT
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        await source.StopAsync();
        source.Reset();
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("456", handler.LastEventIdSent);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_WithFactory_DispatchesEventsFromProducedManager()
    {
        // ARRANGE
        MockHandler handler = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
        {
            EventType = "SimpleAlert",
            Data = "alert from factory"
        });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(() => handler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("alert from factory", handler.LastMessage);
    }

    [Theory]
    [InlineData("stock_updated", NameCasePolicy.SnakeCase)]
    [InlineData("stock-updated", NameCasePolicy.KebabCase)]
    [InlineData("stockUpdated", NameCasePolicy.CamelCase)]
    public async Task StartConsumeAsync_Bind_MapsCamelToConfiguredNameCases(string eventType, NameCasePolicy policy)
    {
        // ARRANGE
        MockHandler handler = new();
        StockData stock = new("MSFT", 400.0m);
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
        {
            EventType = eventType,
            Data = JsonSerializer.Serialize(stock)
        });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions
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
        MockHandler handler = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
        {
            EventType = "SimpleAlert",
            Data = "System Overload"
        });

        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(handler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);
        
        // ASSERT
        Assert.Equal("System Overload", handler.LastMessage);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_WithInterfaceTypedArgument_ResolvesConcreteHandlers()
    {
        // ARRANGE
        MockHandler concreteHandler = new();
        ISseEventsManager manager = concreteHandler;
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
        {
            EventType = "SimpleAlert",
            Data = "dispatched via interface"
        });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(manager);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("dispatched via interface", concreteHandler.LastMessage);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_WithTypeParameter_RegistersHandlersFromConcreteType()
    {
        // ARRANGE
        // ThrowWhenEventHandlerNotFound = true: if Bind<MockHandler>() fails to register any handler,
        // HandlerNotFoundException is thrown and ex will not be null.
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "SimpleAlert", Data = "test" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client, new SseSourceOptions
        {
            Path = "/sse",
            MaxDegreeOfParallelism = 1,
            ThrowWhenNoEventHandlerFound = true
        });

        // ACT
        source.Bind<MockHandler>();
        Exception? ex = await Record.ExceptionAsync(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));

        // ASSERT
        Assert.Null(ex);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_WithMapEventNameAttribute_UsesCustomEventName()
    {
        // ARRANGE
        MockHandlerWithCustomEventName handler = new();
        string sse = MockSseHelpers.BuildSseStream(new SseEvent
        {
            EventType = "alert-received",
            Data = "custom alert"
        });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(handler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("custom alert", handler.LastMessage);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_MultipleManagers_DispatchesToEachManager()
    {
        // ARRANGE
        MockAlertHandler alertHandler = new();
        MockStockHandler stockHandler = new();
        StockData stock = new("AAPL", 200.0m);
        string sse = MockSseHelpers.BuildSseStream(
            new SseEvent { EventType = "SimpleAlert", Data = "critical" },
            new SseEvent { EventType = "StockUpdated", Data = JsonSerializer.Serialize(stock) });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(alertHandler);
        source.Bind(stockHandler);
        await source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token);

        // ASSERT
        Assert.Equal("critical", alertHandler.LastMessage);
        Assert.NotNull(stockHandler.LastStock);
        Assert.Equal("AAPL", stockHandler.LastStock.Symbol);
        Assert.Equal(200.0m, stockHandler.LastStock.Price);
    }

    [Fact]
    public async Task StartConsumeAsync_Bind_ManagerWithNoHandlers_DoesNotThrow()
    {
        // ARRANGE
        string sse = MockSseHelpers.BuildSseStream(new SseEvent { EventType = "e", Data = "1" });
        using HttpClient client = MockSseHelpers.CreateHttpClientWithSseStream(sse);
        await using Core.SseSource source = CreateSource(client);

        // ACT
        source.Bind(new EmptyManager());
        Exception? ex = await Record.ExceptionAsync(() =>
            source.StartConsumeAsync(new CancellationTokenSource(DefaultCancellationTokenDelay).Token));

        // ASSERT
        Assert.Null(ex);
    }

    private class MockHandler : ISseEventsManager
    {
        public StockData? LastStock { get; private set; }
        public string? LastMessage { get; private set; }

        public void OnStockUpdated(StockData data) => LastStock = data;
        public void OnSimpleAlert(string message) => LastMessage = message;
    }

    private class MockHandlerWithCustomEventName : ISseEventsManager
    {
        public string? LastMessage { get; private set; }

        [MapEventName(EventName = "alert-received")]
        public void OnSimpleAlert(string message) => LastMessage = message;
    }

    private class MockAlertHandler : ISseEventsManager
    {
        public string? LastMessage { get; private set; }

        public void OnSimpleAlert(string message) => LastMessage = message;
    }

    private class MockStockHandler : ISseEventsManager
    {
        public StockData? LastStock { get; private set; }

        public void OnStockUpdated(StockData data) => LastStock = data;
    }

    private class EmptyManager : ISseEventsManager { }

    private class StockData(string symbol, decimal price)
    {
        public string Symbol { get; } = symbol;
        public decimal Price { get; } = price;

        public void Deconstruct(out string Symbol, out decimal Price)
        {
            Symbol = this.Symbol;
            Price = this.Price;
        }
    }
}