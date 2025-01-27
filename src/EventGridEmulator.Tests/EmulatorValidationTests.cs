using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;
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
        var receivedTopic = @event.Single()["topic"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{ExpectedTopic}", receivedTopic);
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTripForCloudEvent()
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
        var cloudEvent = new CloudEvent("foo", "bar", new DataModel(some: "data"));
        var response = await publisher.SendEventAsync(cloudEvent);

        // Assert that the message was successfully sent
        Assert.Equal(200, response.Status);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        var receivedTopic = @event.Single()["source"].Deserialize<string>();
        Assert.Equal("data", result?.Some);
        Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{ExpectedTopic}", receivedTopic);
    }

    [Fact]
    public async Task CanFilterEvents()
    {
        var messages = new List<string>();
        HttpMessageHandler handler = new TestHandler(messages.Add);
        const string subscription = "https://localhost:5000";
        var eventTypes = new string[] { "CustomerUpdated", "CustomerCreated" };
        var invalidEventType = "CustomerDeleted";

        using var subscriber = new FactoryClientBuilder(handler)
            .WithTopic(ExpectedTopic, subscription)
            .WithFilter(new Filter { Subscription = subscription, IncludedEventTypes = eventTypes })
            .Build();

        var publisher = new PublisherBuilder(subscriber)
            .WithEndpoint(new Uri("https://localhost/orders/api/events"))
            .Build();

        var sendEventTypes = new string[] { eventTypes[0], invalidEventType, eventTypes[1] };
        foreach (var eventType in sendEventTypes)
        {
            var cloudEvent = new CloudEvent("foo", eventType, new DataModel(some: "data"));
            var eventGridEvent = new EventGridEvent(
                subject: "foo",
                eventType: eventType,
                dataVersion: "1.0",
                data: new DataModel(some: "data")
            );

            var cloudResponse = await publisher.SendEventAsync(cloudEvent);
            var eventGridResponse = await publisher.SendEventAsync(eventGridEvent);
            Assert.Equal(200, cloudResponse.Status);
            Assert.Equal(200, eventGridResponse.Status);
        }

        Assert.Equal(4, messages.Count); // 2 cloud events + 2 event grid events
        foreach (var message in messages)
        {
            var events =
                JsonSerializer.Deserialize<JsonObject[]>(message)
                ?? throw new NullReferenceException("Message cannot be deserialized");
            var @event = events.Single();
            var receivedData = @event["data"].Deserialize<DataModel>();
            var receivedTopic = (@event["source"] ?? @event["topic"]).Deserialize<string>();
            var receivedType = (@event["type"] ?? @event["eventType"]).Deserialize<string>();
            Assert.Equal("data", receivedData?.Some);
            Assert.Equal($"{SubscriberConstants.DefaultTopicValue}{ExpectedTopic}", receivedTopic);
            Assert.Contains(receivedType, eventTypes);
        }
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
