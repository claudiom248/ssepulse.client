using SsePulse.Client.Core.Abstractions;

namespace SsePulse.Client.Tests.SseSource;

public class SseSourceHandlerRegistrationTests : SseSourceTestBase
{
    [Fact]
    public void On_StringHandler_ReturnsChainableInstance()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.On("test", _ => { });

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericType_UsesTypeNameAsEventName()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.On<TestEventData>(_ => { });

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void On_GenericWithCustomName_ReturnsChainableInstance()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.On<TestEventData>("custom", _ => { });

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void On_Chaining_WorksCorrectly()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.On("e1", _ => { }).On("e2", _ => { });

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void Bind_WithInstance_ReturnsChainableInstance()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();
        MockHandler handler = new();

        // ACT
        Core.SseSource result = source.Bind(handler);

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void Bind_WithFactory_ReturnsChainableInstance()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.Bind(() => new MockHandler());

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void Bind_WithFactory_InvokesFactory()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();
        bool factoryInvoked = false;

        // ACT
        source.Bind(() =>
        {
            factoryInvoked = true;
            return new MockHandler();
        });

        // ASSERT
        Assert.True(factoryInvoked);
    }

    [Fact]
    public void Bind_WithTypeParameter_ReturnsChainableInstance()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        Core.SseSource result = source.Bind<MockHandler>();

        // ASSERT
        Assert.Same(source, result);
    }

    [Fact]
    public void OnConnectionEstablished_Setter_UpdatesValue()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        source.OnConnectionEstablished = () => { };

        // ASSERT
        Assert.NotNull(source.OnConnectionEstablished);
    }

    [Fact]
    public void OnConnectionClosed_Setter_UpdatesValue()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        source.OnConnectionClosed = () => { };

        // ASSERT
        Assert.NotNull(source.OnConnectionClosed);
    }

    [Fact]
    public void OnConnectionLost_Setter_UpdatesValue()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        source.OnConnectionLost = _ => { };

        // ASSERT
        Assert.NotNull(source.OnConnectionLost);
    }

    [Fact]
    public void OnError_Setter_UpdatesValue()
    {
        // ARRANGE
        using Core.SseSource source = CreateSource();

        // ACT
        source.OnError = _ => { };

        // ASSERT
        Assert.NotNull(source.OnError);
    }

    private class MockHandler : ISseEventsManager
    {
        public void OnSimpleAlert(string message) { }
    }
}

