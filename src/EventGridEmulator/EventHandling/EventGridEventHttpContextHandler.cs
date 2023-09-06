using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal sealed class EventGridEventHttpContextHandler : BaseEventHttpContextHandler<EventGridEvent>, IEventGridEventHttpContextHandler
{
    public EventGridEventHttpContextHandler(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        ILogger<EventGridEventHttpContextHandler> logger)
        : base(httpClientFactory, cancellationTokenRegistry, options, logger)
    {
    }

    protected override void EnhanceEventData(IEnumerable<EventGridEvent> eventGridEvents, string topicName)
    {
        foreach (var @event in eventGridEvents)
        {
            @event.Topic = $"{SubscriberConstants.DefaultTopicValue}/{topicName}";
        }
    }
}