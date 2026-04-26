using System.Net.ServerSentEvents;
using SsePulse.Client.EventHandlers;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.Tests;

public class SseEventDataHandlerTests
{
    [Fact]
    public void Invoke_WithCamelCaseJsonProperties_DeserializesToPascalCaseProperties()
    {
        // ARRANGE
        TestMessage? receivedData = null;
        
        SseEventDataHandler<TestMessage> handler = new(data => receivedData = data, SerializationOptions.DefaultJsonSerializerOptions);
        SseItem<string> jsonItem = new("{\"userName\":\"Jane\",\"messageId\":456}", "test-event");

        // ACT
        handler.Invoke(jsonItem);

        // ASSERT
        Assert.NotNull(receivedData);
        Assert.Equal("Jane", receivedData.UserName);
        Assert.Equal(456, receivedData.MessageId);
    }
}