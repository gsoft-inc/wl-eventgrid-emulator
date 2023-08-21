using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Azure;
using Azure.Core.Pipeline;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;
using FluentAssertions;

namespace EventGridEmulator.Tests;

public class EmulatorValidationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EmulatorValidationTests(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task ValidateTopicsCreation()
    {
        const string urlOrdersWebhook1 = "https://localhost/orders-webhook-200";
        const string urlOrdersWebhook2 = "https://localhost/orders-webhook-404"; // should be retried
        const string urlOrdersWebhook3 = "https://localhost/orders-webhook-400"; // won't be retried
        const string urlCustomersWebhook1 = "https://localhost/customers-webhook-500";

        var topics = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            // Adding invalid values for "orders" topic (they will be removed) as well as valid unique values
            ["orders"] = new[] { null!, string.Empty, "invalid_url", urlOrdersWebhook1, urlOrdersWebhook2, urlOrdersWebhook3 },

            // These "customers" topic subscribers should be merged together, resulting in a single webhook url
            ["customers"] = new[] { urlCustomersWebhook1, urlCustomersWebhook1 },
        };

        await using var factory = new CustomWebApplicationFactory(services => { services.Configure<TopicOptions>(x => x.Topics = topics); });

        var httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task ValidatePublishAndSubscribeRoundTrip()
    {
        // Define the handler to intercept the message
        string message = string.Empty;
        HttpMessageHandler handler = new YoloHandler(requestAction => message = requestAction);

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
        response.Status.Should().Be(200);

        // Assert that the message was successfully received
        var @event = JsonSerializer.Deserialize<JsonObject[]>(message) ?? throw new NullReferenceException("Message cannot be deserialized");
        var result = @event.Single()["data"].Deserialize<DataModel>();
        result.Some.Should().Be("data");
    }

    public struct DataModel
    {
        public string Some { get; set; }
    }
}