using Azure.Messaging.EventGrid;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal sealed class EventGridEventHttpContextHandler : BaseEventHttpContextHander<EventGridEvent>, IEventGridEventHttpContextHandler
{
    public EventGridEventHttpContextHandler(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        ILogger<EventGridEventHttpContextHandler> logger)
        : base(httpClientFactory, cancellationTokenRegistry, options, logger)
    {
    }
}