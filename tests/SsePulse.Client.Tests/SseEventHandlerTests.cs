using System.Net.ServerSentEvents;
using SsePulse.Client.EventHandlers;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.Tests;

public class TestMessage
{
    public string UserName { get; set; } = string.Empty; 
    public int MessageId { get; set; }
}

public class SseEventHandlerTests
{
    [Fact]
    public void Invoke_WithCamelCaseJsonProperties_DeserializesToPascalCaseProperties()
    {
        // ARRANGE
        SseItem<TestMessage>? receivedItem = null;
        SseEventHandler<TestMessage> handler = new(item => receivedItem = item, SerializationOptions.DefaultJsonSerializerOptions);
        SseItem<string> jsonItem = new("{\"userName\":\"John\",\"messageId\":123}", "test-event");

        // ACT
        handler.Invoke(jsonItem);

        // ARRANGE
        Assert.NotNull(receivedItem);
        Assert.Equal("John", receivedItem.Value.Data.UserName);
        Assert.Equal(123, receivedItem.Value.Data.MessageId);
    }

    [Fact]
    public void Invoke_PreservesEventType()
    {
        // ARRANGE
        SseItem<TestMessage>? receivedItem = null;
        SseEventHandler<TestMessage> handler = new(item => receivedItem = item, SerializationOptions.DefaultJsonSerializerOptions);
        SseItem<string> jsonItem = new("{\"userName\":\"John\",\"messageId\":123}", "user-message");

        // Act
        handler.Invoke(jsonItem);

        // ASSERT
        Assert.NotNull(receivedItem);
        Assert.Equal("user-message", receivedItem.Value.EventType);
    }
}
