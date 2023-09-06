using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Http;
using Polly;

namespace EventGridEmulator.Network;

/// <summary>
/// HTTP message handler that emulates the Event Grid retry policy.
/// </summary>
internal sealed class SubscriberPolicyHttpMessageHandler : PolicyHttpMessageHandler
{
    private readonly ILogger<SubscriberPolicyHttpMessageHandler> _logger;

    public SubscriberPolicyHttpMessageHandler(ILogger<SubscriberPolicyHttpMessageHandler> logger)
        : base(GetRetryPolicy())
    {
        this._logger = logger;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(IsRetriableHttpResponseMessage)
        .WaitAndRetryAsync(JitteredEventGridRetrySchedule());

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "We don't need secure randomness here")]
    private static IEnumerable<TimeSpan> JitteredEventGridRetrySchedule()
    {
        foreach (var delay in SubscriberConstants.EventGridRetrySchedule)
        {
            // From the docs: "Event Grid adds a small randomization to all retry steps [...]"
            // https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-schedule
            var jitter = Random.Shared.Next(0, 3000);
            yield return delay + TimeSpan.FromMilliseconds(jitter);
        }
    }

    private static bool IsRetriableHttpResponseMessage(HttpResponseMessage response)
    {
        return !SubscriberConstants.NonRetriableStatusCodes.Contains(response.StatusCode);
    }

    protected override async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
    {
        if (!request.Options.TryGetValue(SubscriberRequestInfo.HttpOptionKey, out var info))
        {
            // This shouldn't happen as we set the topic name on the request before sending it
            throw new InvalidOperationException("Subscriber request info should have been set on the request.");
        }

        info.IncrementRetryCount();
        info.LogRequestStarted(this._logger);

        // https://learn.microsoft.com/en-us/azure/event-grid/receive-events#message-headers
        const string deliveryCountHeaderName = "Aeg-Delivery-Count";
        _ = request.Headers.Remove(deliveryCountHeaderName);
        request.Headers.TryAddWithoutValidation(deliveryCountHeaderName, info.RetryCount.ToString(CultureInfo.InvariantCulture));

        TrySetWithoutValidationIfNotExists(request.Headers, "Aeg-Event-Type", "Notification");
        TrySetWithoutValidationIfNotExists(request.Headers, "Aeg-Subscription-Name", "EventGridEmulator");
        TrySetWithoutValidationIfNotExists(request.Headers, "Aeg-Metadata-Version", "1");
        TrySetWithoutValidationIfNotExists(request.Headers, "Aeg-Data-Version", "1.0");

        // Event Grid waits 30 seconds for message delivery (for each delivery attempt)
        // https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-schedule
        using var localCts = new CancellationTokenSource();
        using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, cancellationToken);
        localCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            var response = await base.SendCoreAsync(request, context, globalCts.Token);
            info.LogRequestEnded(this._logger, response.StatusCode);
            return response;
        }
        catch (HttpRequestException ex)
        {
            this._logger.LogDebug(ex, null);
            info.LogRequestFailed(this._logger, ex.StatusCode);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            if (localCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                info.LogRequestSingleRetryCanceled(this._logger);

                // Individual retry attempt timed out, but the whole operation isn't canceled yet
                // We throw this exception because it's going to be handled by our Polly retry policy
                throw new HttpRequestException(ex.Message, ex, HttpStatusCode.RequestTimeout);
            }

            info.LogRequestCompletelyCanceled(this._logger);

            throw;
        }
    }

    private static void TrySetWithoutValidationIfNotExists(HttpHeaders headers, string name, string value)
    {
        if (!headers.Contains(name))
        {
            headers.TryAddWithoutValidation(name, value);
        }
    }
}