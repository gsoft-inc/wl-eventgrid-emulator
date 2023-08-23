using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;

namespace EventGridEmulator.Tests;

public class EmulatorValidationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EmulatorValidationTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTrip()
    {
        // Define the handler to intercept the message
        string message = string.Empty;
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic("orders", "https://localhost/orders-webhook")
            .Build();

        // Create the EventGrid publisher
        var publisher = new PublisherBuilder(subscriber)
            .WithEndpoint(new Uri("https://localhost/orders/api/events"))
            .Build();

        // Create and send an event to EventGrid
        var eventGridEvent = new EventGridEvent(
            subject: "foo", 
            eventType: "bar", 
            dataVersion: "1.0", 
            data: new DataModel
            {
                Some = "data",
            });
        var response = await publisher.SendEventAsync(eventGridEvent);
        
        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        Assert.Equal("data", result.Some);
    }

    public struct DataModel
    {
        public string Some { get; set; }
    }
}