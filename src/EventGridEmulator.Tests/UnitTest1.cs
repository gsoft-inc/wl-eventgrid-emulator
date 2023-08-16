using Azure;
using Azure.Core.Pipeline;
using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;

namespace EventGridEmulator.Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper _testOutputHelper;

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        this._testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Test1()
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

        await using var factory = new CustomWebApplicationFactory(services =>
        {
            services.Configure<TopicOptions>(x => x.Topics = topics);
        });

        var httpClient = factory.CreateClient();

        var client = new EventGridPublisherClient(new Uri("https://localhost/licenses/api/events"), new AzureKeyCredential("noop"), new EventGridPublisherClientOptions
        {
            Transport = new HttpClientTransport(httpClient),
        });

        var data = new Dictionary<string, string>
        {
            ["some"] = "data",
        };

        await client.SendEventAsync(new EventGridEvent(subject: "foo", eventType: "bar", dataVersion: "1.0", data: data));
    }
}