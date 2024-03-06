using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Channels;

namespace EventGridEmulator.EventHandling;

internal sealed class TopicSubscribers<T>
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionData>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public void AddEvent(string topicName, string subscriptionName, T[] events)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        foreach (var item in events)
        {
            subscription.AddItem(item);
        }
    }

    public ValueTask<(T Item, string LockToken)> GetEventAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.GetItemAsync(cancellationToken);
    }

    public bool TryDeleteEvent(string topicName, string subscriptionName, string lockToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.RemoveItem(lockToken);
    }

    public bool TryReleaseEvent(string topicName, string subscriptionName, string lockToken)
    {
        var subscription = this.GetSubscriptionInfo(topicName, subscriptionName);
        return subscription.ReleaseItem(lockToken);
    }

    private SubscriptionData GetSubscriptionInfo(string topicName, string subscriptionName)
    {
        var subscriptions = this._subscriptions.GetOrAdd(topicName, _ => new ConcurrentDictionary<string, SubscriptionData>(StringComparer.OrdinalIgnoreCase));
        return subscriptions.GetOrAdd(subscriptionName, _ => new());
    }

    private sealed class SubscriptionData
    {
        private readonly Channel<T> _queue;
        private readonly ConcurrentDictionary<string, T> _inFlightItems;

        private long _lockToken;

        public SubscriptionData()
        {
            this._queue = Channel.CreateUnbounded<T>();
            this._inFlightItems = new ConcurrentDictionary<string, T>();
        }

        public void AddItem(T item) => this._queue.Writer.TryWrite(item); // TryWrite always succeeds with Unbounded channels

        public async ValueTask<(T Item, string LockToken)> GetItemAsync(CancellationToken cancellationToken)
        {
            var item = await this._queue.Reader.ReadAsync(cancellationToken);
            var token = "token-" + Interlocked.Increment(ref this._lockToken).ToString(CultureInfo.InvariantCulture);
            this._inFlightItems.TryAdd(token, item);
            return (item, token);
        }

        public bool RemoveItem(string lockToken)
        {
            return this._inFlightItems.TryRemove(lockToken, out _);
        }

        public bool ReleaseItem(string lockToken)
        {
            if (!this._inFlightItems.TryRemove(lockToken, out var item))
            {
                this.AddItem(item!);
                return true;
            }

            return false;
        }
    }
}
