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
        // event is put in the queue
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);
        
        // we are asking the queue to send an event back to consumer 
        var events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        var ev = Assert.Single(events.Value.Value);
        Assert.Equal(("source", "type"), (ev.Event.Source, ev.Event.Type));
        var deserializedData = ev.Event.Data!.ToObjectFromJson<EventData>();
        Assert.Equal(data, deserializedData);

        var releaseResult = await client.ReleaseCloudEventsAsync(topicName, eventSubscriptionName, new ReleaseOptions([ev.BrokerProperties.LockToken, "abcd", "efgh"]));
        Assert.Single(releaseResult.Value.SucceededLockTokens);
        Assert.Equal(2, releaseResult.Value.FailedLockTokens.Count);
        
        // acknowledge/release/reject --> we receive an event, if application crashes, then the queue tells you that event gives you 10 mins
        // if we don't renotify the eventgrid that we are done, they will be made available again for the next runner/consumer.
        // acknowledge: fini de process and we can delete the message.
        
        // Message and locktokens --> the locktoken about the 10 minutes access time on a specific message. Then when we send back to EventGrid
        // we must pass back locktokens. --> Failed locktokens ---> there should be no idea of timeout in emulator.
        // acknowledge --> delete message
        // release --> make message available. --> pass the list of the tokens again. Only release the locktokens to not be deleted. Delete
        // reject --> move the batch of messages to the DLQ (in our case, we would be mimicking the behavior of acknowledge)
        events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        ev = Assert.Single(events.Value.Value);
        
        var acknowledgeResult = await client.AcknowledgeCloudEventsAsync(topicName, eventSubscriptionName, new AcknowledgeOptions([ev.BrokerProperties.LockToken]));
        Assert.Single(acknowledgeResult.Value.SucceededLockTokens);

        // TODO suggestion: support max wait time for Receiving Cloud Events
        await AssertQueueIsEmpty();
        
        _ = await client.PublishCloudEventsAsync(topicName, [new CloudEvent("source", "type", data)]);
        events = await client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName);
        
        ev = Assert.Single(events.Value.Value);
        var rejectResult = await client.RejectCloudEventsAsync(topicName, eventSubscriptionName, new RejectOptions([ev.BrokerProperties.LockToken]));
        Assert.Single(rejectResult.Value.SucceededLockTokens);
        
        await AssertQueueIsEmpty();

        async Task AssertQueueIsEmpty()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); 
            await Assert.ThrowsAsync<TaskCanceledException>(() => client.ReceiveCloudEventsAsync(topicName, eventSubscriptionName, cancellationToken: cts.Token));
        }
    }

    private sealed record EventData(string Id);

    private sealed class CustomWebApplicationFactory(Action<IServiceCollection> configureServices) : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection> _configureServices = configureServices;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureTestServices(services => this._configureServices(services));
    }
}
