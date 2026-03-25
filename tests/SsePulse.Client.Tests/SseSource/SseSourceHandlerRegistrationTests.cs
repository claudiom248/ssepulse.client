using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceHandlerRegistrationTests : SseSourceTestBase
{
    // --- Gruppo: .On() overloads (4 tests) ---

    [Fact]
    public void On_StringHandler_ReturnsChainableInstance()
    {
        using Core.SseSource source = CreateSource();
        Core.SseSource result = source.On("test", _ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericType_UsesTypeNameAsEventName()
    {
        using Core.SseSource source = CreateSource();
        Core.SseSource result = source.On<TestEventData>(_ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericWithCustomName_ReturnsChainableInstance()
    {
        using Core.SseSource source = CreateSource();
        Core.SseSource result = source.On<TestEventData>("custom", _ => { });
        Assert.Same(source, result);
    }

    [Fact]
    public void On_Chaining_WorksCorrectly()
    {
        using Core.SseSource source = CreateSource();
        Core.SseSource result = source.On("e1", _ => { }).On("e2", _ => { });
        Assert.Same(source, result);
    }

    // --- Gruppo: Bind (1 test) ---

    [Fact]
    public void Bind_ReturnsChainableInstance()
    {
        using Core.SseSource source = CreateSource();
        MockHandler handler = new();
        Core.SseSource result = source.Bind(handler);
        Assert.Same(source, result);
    }

    // --- Gruppo: Callback setter (4 tests) ---

    [Fact]
    public void OnConnectionEstablished_Setter_UpdatesValue()
    {
        using Core.SseSource source = CreateSource();
        source.OnConnectionEstablished = () => { };
        Assert.NotNull(source.OnConnectionEstablished);
    }

    [Fact]
    public void OnConnectionClosed_Setter_UpdatesValue()
    {
        using Core.SseSource source = CreateSource();
        source.OnConnectionClosed = () => { };
        Assert.NotNull(source.OnConnectionClosed);
    }

    [Fact]
    public void OnConnectionLost_Setter_UpdatesValue()
    {
        using Core.SseSource source = CreateSource();
        source.OnConnectionLost = _ => { };
        Assert.NotNull(source.OnConnectionLost);
    }

    [Fact]
    public void OnError_Setter_UpdatesValue()
    {
        using Core.SseSource source = CreateSource();
        source.OnError = _ => { };
        Assert.NotNull(source.OnError);
    }

    private class MockHandler : ISseEventsManager
    {
        public void OnSimpleAlert(string message) { }
    }
}

