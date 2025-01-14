using Azure;
using Azure.Core.Pipeline;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Namespaces;
using EventGridEmulator.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace EventGridEmulator.Tests;

public sealed class PullModelEventGridClientTests
{
    [Fact]
    public async Task SupportNonBatchEvent()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var (publisher, receiver) = await this.CreateTestEventGridClient(topicName, eventSubscriptionName);
        var data = new EventData("CustomId");
        _ = await publisher.SendEventAsync(new CloudEvent("source", "type", data));

        var events = await receiver.ReceiveAsync();

        var ev = Assert.Single(events.Value.Details);
        Assert.Equal(("source", "type"), (ev.Event.Source, ev.Event.Type));
        var deserializedData = ev.Event.Data!.ToObjectFromJson<EventData>();
        Assert.Equal(data, deserializedData);
    }

    [Fact]
    public async Task CanSendReceiveEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var (publisher, receiver) = await this.CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await publisher.SendEventsAsync([new CloudEvent("source", "type", data)]);

        var events = await receiver.ReceiveAsync();
        var ev = Assert.Single(events.Value.Details);
        Assert.Equal(("source", "type"), (ev.Event.Source, ev.Event.Type));
        var deserializedData = ev.Event.Data!.ToObjectFromJson<EventData>();
        Assert.Equal(data, deserializedData);
    }

    [Fact]
    public async Task CanSendReceiveAcknowledgeEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var (publisher, receiver) = await this.CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await publisher.SendEventsAsync([new CloudEvent("source", "type", data)]);

        var events = await receiver.ReceiveAsync();
        var ev = Assert.Single(events.Value.Details);

        var acknowledgeResult = await receiver.AcknowledgeAsync([ev.BrokerProperties.LockToken]);
        Assert.Single(acknowledgeResult.Value.SucceededLockTokens);

        // Assert queue is empty
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TaskCanceledException>(() => receiver.ReceiveAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task CanSendReceiveReleaseEventsWithInvalidLockTokens()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var (publisher, receiver) = await this.CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await publisher.SendEventsAsync([new CloudEvent("source", "type", data)]);

        var events = await receiver.ReceiveAsync();
        var ev = Assert.Single(events.Value.Details);

        var releaseResult = await receiver.ReleaseAsync([ev.BrokerProperties.LockToken, "abcd", "efgh"]);
        Assert.Single(releaseResult.Value.SucceededLockTokens);
        Assert.Equal(2, releaseResult.Value.FailedLockTokens.Count);

        events = await receiver.ReceiveAsync();
        Assert.Single(events.Value.Details);
    }

    [Fact]
    public async Task CanSendReceiveRejectEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var (publisher, receiver) = await this.CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await publisher.SendEventsAsync([new CloudEvent("source", "type", data)]);

        var events = await receiver.ReceiveAsync();
        var ev = Assert.Single(events.Value.Details);

        var rejectResult = await receiver.RejectAsync([ev.BrokerProperties.LockToken]);
        Assert.Single(rejectResult.Value.SucceededLockTokens);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TaskCanceledException>(() => receiver.ReceiveAsync(cancellationToken: cts.Token));
    }

    [Fact]
    public async Task CanFilterEventTypes()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";
        var filter = new Filter() { IncludedEventTypes = ["CustomerUpdated", "CustomerCreated"] };

        var (publisher, receiver) = await this.CreateTestEventGridClient(
            topicName,
            eventSubscriptionName,
            filter
        );

        var data = new EventData("CustomId1");
        _ = await publisher.SendEventsAsync(
            [
                new CloudEvent("source", "CustomerCreated", data),
                new CloudEvent("source", "CustomerDeleted", data),
                new CloudEvent("source", "CustomerUpdated", data),
            ]
        );

        for (var i = 0; i < 2; i++)
        {
            var events = await receiver.ReceiveAsync();
            var ev = Assert.Single(events.Value.Details);
            Assert.Equal("source", ev.Event.Source);
            Assert.Contains(ev.Event.Type, filter.IncludedEventTypes);
        }
    }

    private async Task<(EventGridPublisherClient, EventGridReceiverClient)> CreateTestEventGridClient(
        string topicName,
        string eventSubscriptionName,
        Filter? eventTypes = null
    )
    {
        var factory = new CustomWebApplicationFactory(options =>
        {
            options.Configure<TopicOptions>(topicOptions =>
            {
                topicOptions.Topics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [topicName] = [$"pull://{eventSubscriptionName}"],
                };
                if (eventTypes != null)
                {
                    topicOptions.Filters = new(StringComparer.OrdinalIgnoreCase)
                    {
                        [eventSubscriptionName] = eventTypes
                    };
                }
            });
        });

        var httpClientHandler = factory.Server.CreateHandler();

        var publisherClient = new EventGridPublisherClient(new Uri($"https://localhost/{topicName}/api/events"), new AzureKeyCredential("noop"), new EventGridPublisherClientOptions
        {
            Transport = new HttpClientTransport(httpClientHandler),
        });

        var receiverClient = new EventGridReceiverClient(new Uri($"https://localhost/"), topicName, eventSubscriptionName, new AzureKeyCredential("noop"), new EventGridReceiverClientOptions
        {
            Transport = new HttpClientTransport(httpClientHandler),
        });

        return (publisherClient, receiverClient);
    }

    private sealed record EventData(string Id);

    private sealed class CustomWebApplicationFactory(Action<IServiceCollection> configureServices) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureTestServices(configureServices);
    }
}
