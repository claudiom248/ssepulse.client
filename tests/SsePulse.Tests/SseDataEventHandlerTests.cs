using System.Net.ServerSentEvents;

namespace SsePulse.Tests;

public class SseDataEventHandlerTests
{
    [Fact]
    public void WithCamelCaseJson_DeserializesToPascalCaseProperties()
    {
        // Arrange
        TestMessage? receivedData = null;
        SseDataEventHandler<TestMessage> handler = new(data => receivedData = data);
        SseItem<string> jsonItem = new("{\"userName\":\"Jane\",\"messageId\":456}", "test-event");

        // Act
        handler.Invoke(jsonItem);

        // Assert
        Assert.NotNull(receivedData);
        Assert.Equal("Jane", receivedData.UserName);
        Assert.Equal(456, receivedData.MessageId);
    }
}
