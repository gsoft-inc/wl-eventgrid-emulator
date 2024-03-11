using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;

namespace EventGridEmulator.Tests;

public class EmulatorValidationTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string ExpectedTopic = "orders";

    public EmulatorValidationTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForEventGridEvent()
    {
        // Define the handler to intercept the message
        var message = string.Empty;
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(this.ExpectedTopic, "https://localhost/orders-webhook")
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
            data: new DataModel(some: "data"));
        var response = await publisher.SendEventAsync(eventGridEvent);

        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        var receivedTopic = @event.Single()["topic"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{this.ExpectedTopic}", receivedTopic);
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForCloudEvent()
    {
        // Define the handler to intercept the message
        var message = string.Empty;
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(this.ExpectedTopic, "https://localhost/orders-webhook")
            .Build();

        // Create the EventGrid publisher
        var publisher = new PublisherBuilder(subscriber)
            .WithEndpoint(new Uri("https://localhost/orders/api/events"))
            .Build();

        // Create and send an event to EventGrid
        var cloudEvent = new CloudEvent("foo", "bar", new DataModel(some: "data"));
        var response = await publisher.SendEventAsync(cloudEvent);

        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        var receivedTopic = @event.Single()["source"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{this.ExpectedTopic}", receivedTopic);
    }

    private sealed class DataModel
    {
        public DataModel(string some)
        {
            this.Some = some;
        }

        public string Some { get; init; }
    }
}