using Azure.Core.Pipeline;
using Azure;
using EventGridEmulator.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;

namespace EventGridEmulator.Tests;

public sealed class PullModelEventGridClientTests
{
    [Fact]
    public async Task CanSendReceiveEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var client = await CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);

        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);
        Assert.Equal(("source", "type"), (ev.Event.Source, ev.Event.Type));
        var deserializedData = ev.Event.Data!.ToObjectFromJson<EventData>();
        Assert.Equal(data, deserializedData);
    }

    [Fact]
    public async Task CanSendReceiveAcknowledgeEvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var client = await CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);

        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);

        var acknowledgeResult = await client.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions([ev.BrokerProperties.LockToken]));
        Assert.Single(acknowledgeResult.Value.SucceededLockTokens);
        
        // Assert queue is emtpy
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TaskCanceledException>(() => client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, cancellationToken: cts.Token));
    }
    
    [Fact]
    public async Task CanSendReceiveReleaseEventsWithInvalidLockTokens()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var client = await CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);

        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);

        var releaseResult = await client.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions([ev.BrokerProperties.LockToken, "abcd", "efgh"]));
        Assert.Single(releaseResult.Value.SucceededLockTokens);
        Assert.Equal(2, releaseResult.Value.FailedLockTokens.Count);

        events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        Assert.Single(events.Value.Value);
    }
    
    [Fact]
    public async Task CanSendReceiveRejectvents()
    {
        var topicName = "customers";
        var eventSubscriptionName = "CustomSubscription";

        var client = await CreateTestEventGridClient(topicName, eventSubscriptionName);

        var data = new EventData("CustomId");
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);

        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);

        var rejectResult = await client.RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions([ev.BrokerProperties.LockToken]));
        Assert.Single(rejectResult.Value.SucceededLockTokens);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<TaskCanceledException>(() => client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, cancellationToken: cts.Token));
    }

    private sealed record EventData(string Id);

    private sealed class CustomWebApplicationFactory(Action<IServiceCollection> configureServices) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureTestServices(configureServices);
    }
    
    private static async Task<EventGridClient> CreateTestEventGridClient(string topicName, string eventSubscriptionName)
    {
        var factory = new CustomWebApplicationFactory(options =>
        {
            options.Configure<TopicOptions>(topicOptions =>
            {
                topicOptions.Topics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [topicName] = [$"pull://{eventSubscriptionName}"],
                };
            });
        });
        
        var httpClientHandler = factory.Server.CreateHandler();

        var client = new EventGridClient(new Uri("https://localhost"), new AzureKeyCredential("noop"), new EventGridClientOptions
        {
            Transport = new HttpClientTransport(httpClientHandler),
        });
        return client;
    }
}
