using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Network;

namespace EventGridEmulator.Tests;

public class EmulatorValidationTests
{
    private const string ExpectedTopic = "orders";

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForEventGridEvent()
    {
        // Define the handler to intercept the message
        var message = string.Empty;
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(ExpectedTopic, "https://localhost/orders-webhook")
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
        var receivedSource = @event.Single()["topic"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{ExpectedTopic}", receivedSource);
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForCloudEventWithEmptySource()
    {
        // Define the handler to intercept the message
        var message = string.Empty;
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(ExpectedTopic, "https://localhost/orders-webhook")
            .Build();

        // Create the EventGrid publisher
        var publisher = new PublisherBuilder(subscriber)
            .WithEndpoint(new Uri("https://localhost/orders/api/events"))
            .Build();

        // Create and send an event to EventGrid
        var cloudEvent = new CloudEvent("", "bar", new DataModel(some: "data"));
        var response = await publisher.SendEventAsync(cloudEvent);

        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        var receivedSource = @event.Single()["source"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{ExpectedTopic}", receivedSource);
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForCloudEventPreserveOriginalSource()
    {
        // Define the handler to intercept the message
        var message = string.Empty;
        var originalSource = "SOME_SOURCE_VALUE";
        HttpMessageHandler handler = new TestHandler(requestAction => message = requestAction);

        // Create the EventGrid subscriber
        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(ExpectedTopic, "https://localhost/orders-webhook")
            .Build();

        // Create the EventGrid publisher
        var publisher = new PublisherBuilder(subscriber)
            .WithEndpoint(new Uri("https://localhost/orders/api/events"))
            .Build();

        // Create and send an event to EventGrid
        var cloudEvent = new CloudEvent(originalSource, "bar", new DataModel(some: "data"));
        var response = await publisher.SendEventAsync(cloudEvent);

        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        var receivedSource = @event.Single()["source"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal(originalSource, receivedSource);
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