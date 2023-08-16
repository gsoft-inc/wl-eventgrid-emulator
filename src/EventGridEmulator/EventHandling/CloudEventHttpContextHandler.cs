using Azure.Messaging;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal sealed class CloudEventHttpContextHandler : BaseEventHttpContextHander<CloudEvent>, ICloudEventHttpContextHandler
{
    public CloudEventHttpContextHandler(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        ILogger<CloudEventHttpContextHandler> logger)
        : base(httpClientFactory, cancellationTokenRegistry, options, logger)
    {
    }
}