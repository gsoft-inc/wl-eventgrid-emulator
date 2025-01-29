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
        TopicSubscribers<EventGridEvent> queue,
        ILogger<EventGridEventHttpContextHandler> logger)
        : base(httpClientFactory, cancellationTokenRegistry, options, queue, logger)
    {
    }

    protected override void EnhanceEventData(IEnumerable<EventGridEvent> eventGridEvents, string topicName)
    {
        foreach (var @event in eventGridEvents)
        {
            @event.Topic = $"{SubscriberConstants.DefaultTopicValue}{topicName}";
        }
    }

    protected override EventGridEvent[] FilterEvents(EventGridEvent[] events, Filter filter)
    {
        return filter.IncludedEventTypes == null
            ? events
            : events.Where(e => filter.IncludedEventTypes.Contains(e.EventType)).ToArray();
    }
}
