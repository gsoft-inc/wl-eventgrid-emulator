using Azure.Core.Pipeline;
using Azure;
using EventGridEmulator.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace EventGridEmulator.Tests;

public sealed class EventGridClientTests
{
    // TODO support :release (ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions(new[] { lockToken }), cancellationToken: cancellationToken))
    // TODO support :reject (RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions(new[] { lockToken }), cancellationToken))
    // TODO Update readme
    [Fact]
    public async Task CanSendAndReceiveEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        await using var factory = new CustomWebApplicationFactory(options =>
        {
            options.Configure<TopicOptions>(options =>
            {
                options.Topics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [topicName] = [$"pull://{eventSubscriptionName}"],
                };
            });
        });
        using var httpClientHandler = factory.Server.CreateHandler();

        var client = new EventGridClient(new Uri("https://localhost"), new AzureKeyCredential("noop"), new EventGridClientOptions
        {
            Transport = new HttpClientTransport(httpClientHandler),
        });

        var data = new EventData("CustomId");
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);

        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);
        Assert.Equal(("source", "type"), (ev.Event.Source, ev.Event.Type));
        var deserializedData = ev.Event.Data!.ToObjectFromJson<EventData>();
        Assert.Equal(data, deserializedData);

        var result = await client.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions([ev.BrokerProperties.LockToken, "abcd", "efgh"]));

        Assert.Single(result.Value.SucceededLockTokens);
        Assert.Equal(2, result.Value.FailedLockTokens.Count);
    }

    private sealed record EventData(string Id);

    private sealed class CustomWebApplicationFactory(Action<IServiceCollection> configureServices) : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureServices = configureServices;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureTestServices(services => this._configureServices(services));
    }
}
