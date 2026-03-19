using System.Net.ServerSentEvents;
using SsePulse.EventHandlers;

namespace SsePulse.Tests;

public class TestMessage
{
    public string UserName { get; set; } = string.Empty;  // PascalCase in C#
    public int MessageId { get; set; }
}

public class SseEventHandlerTests
{
    [Fact]
    public void WithCamelCaseJson_DeserializesToPascalCaseProperties()
    {
        // Arrange
        SseItem<TestMessage>? receivedItem = null;
        SseEventHandler<TestMessage> handler = new(item => receivedItem = item);
        SseItem<string> jsonItem = new("{\"userName\":\"John\",\"messageId\":123}", "test-event");

        // Act
        handler.Invoke(jsonItem);

        // Assert
        Assert.NotNull(receivedItem);
        Assert.Equal("John", receivedItem.Value.Data.UserName);
        Assert.Equal(123, receivedItem.Value.Data.MessageId);
    }

    [Fact]
    public void PreservesEventType()
    {
        // Arrange
        SseItem<TestMessage>? receivedItem = null;
        SseEventHandler<TestMessage> handler = new(item => receivedItem = item);
        SseItem<string> jsonItem = new("{\"userName\":\"John\",\"messageId\":123}", "user-message");

        // Act
        handler.Invoke(jsonItem);

        // Assert
        Assert.NotNull(receivedItem);
        Assert.Equal("user-message", receivedItem.Value.EventType);
    }
}
