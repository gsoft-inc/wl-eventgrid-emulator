using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Channels;
using LockToken = string;
using SubscriberName = string;
using TopicName = string;

namespace EventGridEmulator.EventHandling;

internal sealed class TopicSubscribers<T>
{
    // For each topics, we create a list of subscribers.
    // In subscription data, they contain the items in queue. Waiting for acknowledge, release, reject
    private readonly ConcurrentDictionary<TopicName, ConcurrentDictionary<SubscriberName, SubscriptionData>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public void AddEvent(TopicName topicName, SubscriberName subscriptionName, T[] events)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        foreach (var item in events)
        {
            subscription.AddItem(item);
        }
    }

    public ValueTask<(T Item, LockToken LockToken)> GetEventAsync(TopicName topicName, SubscriberName subscriptionName, CancellationToken cancellationToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.GetItemAsync(cancellationToken);
    }

    public bool TryDeleteEvent(TopicName topicName, SubscriberName subscriptionName, LockToken lockToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.RemoveItem(lockToken);
    }

    public bool TryReleaseEvent(TopicName topicName, SubscriberName subscriptionName, LockToken lockToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.ReleaseItem(lockToken);
    }

    private SubscriptionData GetSubscriptionInfo(TopicName topicName, SubscriberName subscriptionName)
    {
        var subscriptions = this._subscriptions.GetOrAdd(topicName, _ => new(StringComparer.OrdinalIgnoreCase));
        return subscriptions.GetOrAdd(subscriptionName, _ => new());
    }

    private sealed class SubscriptionData
    {
        private readonly Channel<T> _queue;
        private readonly ConcurrentDictionary<LockToken, T> _inFlightItems;

        private long _lockToken;

        public SubscriptionData()
        {
            this._queue = Channel.CreateUnbounded<T>();
            this._inFlightItems = new ConcurrentDictionary<LockToken, T>();
        }

        public void AddItem(T item) => this._queue.Writer.TryWrite(item); // TryWrite always succeeds with Unbounded channels

        public async ValueTask<(T Item, LockToken LockToken)> GetItemAsync(CancellationToken cancellationToken)
        {
            var item = await this._queue.Reader.ReadAsync(cancellationToken);
            var token = "token-" + Interlocked.Increment(ref this._lockToken).ToString(CultureInfo.InvariantCulture);
            this._inFlightItems.TryAdd(token, item);
            return (item, token);
        }

        public bool RemoveItem(LockToken lockToken)
        {
            return this._inFlightItems.TryRemove(lockToken, out _);
        }

        // Azure EventGrid does not guarantee the ordering of events, the behavior of our emulator will be to enqueue the items back.
        public bool ReleaseItem(LockToken lockToken)
        {
            if (this._inFlightItems.TryRemove(lockToken, out var item))
            {
                this.AddItem(item!);
                return true;
            }

            return false;
        }
    }
}
