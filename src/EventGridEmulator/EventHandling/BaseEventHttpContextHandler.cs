using System.Text.Json;
using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.EventHandling;

internal abstract class BaseEventHttpContextHandler<TEvent>
{
    private readonly HttpClient _httpClient;
    private readonly ISubscriberCancellationTokenRegistry _cancellationTokenRegistry;
    private readonly IOptionsMonitor<TopicOptions> _options;
    private readonly TopicSubscribers<TEvent> _eventQueue;
    private readonly ILogger _logger;

    protected BaseEventHttpContextHandler(
        IHttpClientFactory httpClientFactory,
        ISubscriberCancellationTokenRegistry cancellationTokenRegistry,
        IOptionsMonitor<TopicOptions> options,
        TopicSubscribers<TEvent> eventQueue,
        ILogger logger)
    {
        this._httpClient = httpClientFactory.CreateClient(SubscriberConstants.HttpClientName);
        this._cancellationTokenRegistry = cancellationTokenRegistry;
        this._options = options;
        this._eventQueue = eventQueue;
        this._logger = logger;
    }

    public async Task HandleAsync(HttpContext context, string topic)
    {
        var result = await this.HandleInternalAsync(context, topic, batch: true);
        await result.ExecuteAsync(context);
    }

    public async Task HandleAsync(HttpContext context, string topic, bool batch)
    {
        var result = await this.HandleInternalAsync(context, topic, batch);
        await result.ExecuteAsync(context);
    }

    private async Task<IResult> HandleInternalAsync(HttpContext context, string topic, bool batch)
    {
        TEvent[]? events = null;
        if (batch)
        {
            events = await EventsSerializer.DeserializeEventsAsync<TEvent>(context);
        }
        else
        {
            var ev = await EventsSerializer.DeserializeEventAsync<TEvent>(context);
            if (ev is not null)
            {
                events = [ev];
            }
        }

        if (events == null)
        {
            return Results.BadRequest();
        }

        var hasSubscribers = false;
        var pushSubscriber = this._options.CurrentValue.GetPushSubscribers(topic);
        foreach (var subscriber in pushSubscriber)
        {
            hasSubscribers = true;
            var cancellationToken = this._cancellationTokenRegistry.Get(topic, subscriber.Uri);
            this.EnhanceEventData(events, topic);
            _ = this.SendEventsToSubscriberFireAndForget(topic, subscriber.Uri, events, cancellationToken);

            if (this._logger.IsEnabled(LogLevel.Information))
            {
                this._logger.LogInformation("Event from topic '{Topic}' sent to '{SubscriberUri}' with payload '{Events}'", topic, subscriber.Uri, EventsSerializer.SerializeEventsForDebugPurposes(events));
            }
        }

        foreach (var subscriber in this._options.CurrentValue.GetPullSubscribers(topic))
        {
            hasSubscribers = true;
            this._eventQueue.AddEvent(topic, subscriber.SubscriptionName, events);

            if (this._logger.IsEnabled(LogLevel.Information))
            {
                this._logger.LogInformation("Event from topic '{Topic}' enqueued for subscriber '{SubscriberName}' with payload '{Events}'", topic, subscriber.SubscriptionName, EventsSerializer.SerializeEventsForDebugPurposes(events));
            }
        }

        if (!hasSubscribers)
        {
            this._logger.LogWarning("No subscriber for topic '{Topic}'. Payload: '{Events}'", topic, EventsSerializer.SerializeEventsForDebugPurposes(events));
        }

        return Results.Ok();
    }

    private async Task SendEventsToSubscriberFireAndForget(string topic, string subscriber, TEvent[] events, CancellationToken cancellationToken)
    {
        var info = new SubscriberRequestInfo(topic, subscriber);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, subscriber);
            request.Content = JsonContent.Create(events);
            request.Options.Set(SubscriberRequestInfo.HttpOptionKey, info);

            using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // This happens when the application stops or when the subscriber was removed from the configuration at runtime
            info.LogRequestCompletelyCanceled(this._logger);
        }
        catch (HttpRequestException ex) when (ex.InnerException is OperationCanceledException)
        {
            // This happens when each individual retry has timed out, but we already took care of logging in our retry handler
        }
        catch (Exception ex)
        {
            info.LogRequestFailed(this._logger, ex);
        }
    }

    protected virtual void EnhanceEventData(IEnumerable<TEvent> events, string topicName)
    {
    }
}