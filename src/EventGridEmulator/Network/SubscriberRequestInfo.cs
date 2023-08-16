using System.Globalization;
using System.Net;

namespace EventGridEmulator.Network;

internal sealed class SubscriberRequestInfo
{
    public static readonly HttpRequestOptionsKey<SubscriberRequestInfo> HttpOptionKey = new HttpRequestOptionsKey<SubscriberRequestInfo>("info");

    private static int _infoCounter;

    private readonly string _id;
    private readonly string _topic;
    private readonly string _subscriber;
    private readonly int _maxRetryCount;
    private int _retryCount;

    public SubscriberRequestInfo(string topic, string subscriber)
    {
        this._id = Interlocked.Increment(ref _infoCounter).ToString(CultureInfo.InvariantCulture).PadLeft(4, '0');
        this._topic = topic;
        this._subscriber = subscriber;
        this._maxRetryCount = SubscriberConstants.EventGridRetrySchedule.Length;

        // Event Grid retry count starts at zero (the first attempt isn't actually a "retry")
        this._retryCount = -1;
    }

    public int RetryCount => this._retryCount < 0 ? 0 : this._retryCount;

    private bool IsLastRetry => this.RetryCount >= this._maxRetryCount;

    private int UserFriendlyRetryCount => this.RetryCount + 1;

    private int UserFriendlyMaxRetryCount => this._maxRetryCount + 1;

    public void IncrementRetryCount()
    {
        this._retryCount++;
    }

    public void LogRequestStarted(ILogger logger)
    {
        logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) ...", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount);
    }

    public void LogRequestEnded(ILogger logger, HttpStatusCode statusCode)
    {
        if (IsSuccessStatusCode(statusCode))
        {
            this.LogRequestSuccessfullyEnded(logger, statusCode);
        }
        else
        {
            this.LogRequestFailed(logger, statusCode);
        }
    }

    private void LogRequestSuccessfullyEnded(ILogger logger, HttpStatusCode statusCode)
    {
        logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) succeeded and returned {HttpStatusCode}", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, (int)statusCode);
    }

    public void LogRequestFailed(ILogger logger, HttpStatusCode? statusCode)
    {
        if (statusCode.HasValue)
        {
            var isNonRetriableStatusCode = SubscriberConstants.NonRetriableStatusCodes.Contains(statusCode.Value);
            if (isNonRetriableStatusCode || this.IsLastRetry)
            {
                logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) returned {HttpStatusCode}, this was the last attempt", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, (int)statusCode.Value);
            }
            else
            {
                var nextRetryDelay = SubscriberConstants.EventGridRetrySchedule[this.RetryCount];
                logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) returned {HttpStatusCode}, retrying in ~{RetryDelay:c}", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, (int)statusCode.Value, nextRetryDelay);
            }
        }
        else
        {
            if (this.IsLastRetry)
            {
                logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) failed, this was the last attempt", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount);
            }
            else
            {
                var nextRetryDelay = SubscriberConstants.EventGridRetrySchedule[this.RetryCount];
                logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) failed, retrying in ~{RetryDelay:c}", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, nextRetryDelay);
            }
        }
    }

    public void LogRequestSingleRetryCanceled(ILogger logger) => this.LogRequestCanceled(logger, isGlobalCancellation: false);

    public void LogRequestCompletelyCanceled(ILogger logger) => this.LogRequestCanceled(logger, isGlobalCancellation: true);

    private void LogRequestCanceled(ILogger logger, bool isGlobalCancellation)
    {
        if (isGlobalCancellation)
        {
            logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) was canceled", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount);
        }
        else if (this.IsLastRetry)
        {
            logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) timed out, this was the last attempt", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount);
        }
        else
        {
            var nextRetryDelay = SubscriberConstants.EventGridRetrySchedule[this.RetryCount];
            logger.LogInformation("Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) timed out, retrying in ~{RetryDelay:c}", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, nextRetryDelay);
        }
    }

    public void LogRequestFailed(ILogger logger, Exception unhandledException)
    {
        logger.LogInformation(unhandledException, "Sending event {Id} from topic {Topic} to subscriber {Subscriber} (attempt {RetryCount}/{MaxRetryCount}) failed for an unexpected reason: {Reason}", this._id, this._topic, this._subscriber, this.UserFriendlyRetryCount, this.UserFriendlyMaxRetryCount, unhandledException.Message);
    }

    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode <= 299;
    }
}