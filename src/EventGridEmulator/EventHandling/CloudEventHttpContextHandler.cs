using Azure.Messaging;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal sealed class CloudEventHttpContextHandler : BaseEventHttpContextHandler<CloudEvent>, ICloudEventHttpContextHandler
{
    public CloudEventHttpContextHandler(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        TopicSubscribers<CloudEvent> queue,
        ILogger<CloudEventHttpContextHandler> logger)
        : base(httpClientFactory, cancellationTokenRegistry, options, queue, logger)
    {
    }

    protected override void EnhanceEventData(IEnumerable<CloudEvent> cloudEvents, string topicName)
    {
        // foreach (var @event in cloudEvents)
        // {
        //     @event.Source = $"{SubscriberConstants.DefaultTopicValue}{topicName}";
        // }
    }
}