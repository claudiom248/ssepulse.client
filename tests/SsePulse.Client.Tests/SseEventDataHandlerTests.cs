using System.Net.ServerSentEvents;
using SsePulse.Client.Internal;
using SsePulse.Client.Serialization;

namespace SsePulse.Client.Tests;

public class SseEventDataHandlerTests
{
    [Fact]
    public async Task InvokeAsync_WithCamelCaseJsonProperties_DeserializesToPascalCaseProperties()
    {
        // ARRANGE
        TestMessage? receivedData = null;
        
        SseEventDataHandler<TestMessage> handler = new(data => receivedData = data, SerializationOptions.DefaultJsonSerializerOptions);
        SseItem<string> jsonItem = new("{\"userName\":\"Jane\",\"messageId\":456}", "test-event");

        // ACT
        await handler.InvokeAsync(jsonItem, CancellationToken.None);

        // ASSERT
        Assert.NotNull(receivedData);
        Assert.Equal("Jane", receivedData.UserName);
        Assert.Equal(456, receivedData.MessageId);
    }
}