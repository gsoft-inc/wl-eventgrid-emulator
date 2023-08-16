using EventGridEmulator.Network;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.Configuration;

/// <summary>
/// Creates new cancellation token sources for each subscriber and register them in the <see cref="ISubscriberCancellationTokenRegistry"/>.
/// These cancellation token sources are then used to cancel any pending HTTP requests when a subscriber is removed.
/// </summary>
internal sealed class PostConfigureTopicOptionsCancellationManager : IPostConfigureOptions<TopicOptions>
{
    private readonly ISubscriberCancellationTokenRegistry _cancellationTokenRegistry;
    private readonly object _lockObject = new object();
    private TopicOptions? _previousOptions;

    public PostConfigureTopicOptionsCancellationManager(ISubscriberCancellationTokenRegistry cancellationTokenRegistry)
    {
        this._cancellationTokenRegistry = cancellationTokenRegistry;
        this._previousOptions = null;
    }

    public void PostConfigure(string? name, TopicOptions options)
    {
        lock (this._lockObject)
        {
            var removedSubscribers = new List<(string, string)>();
            var addedSubscribers = new List<(string, string)>();

            if (this._previousOptions == null)
            {
                // All these topics are new, flag all subscribers as added
                foreach (var (topic, currentSubscribers) in options.Topics)
                {
                    addedSubscribers.AddRange(currentSubscribers.Select(x => (topic, x)));
                }
            }
            else
            {
                foreach (var (topic, previousSubscribers) in this._previousOptions.Topics)
                {
                    // This topic still exists, compare subscribers and flag those that were added and those that were removed
                    if (options.Topics.TryGetValue(topic, out var currentSubscribers))
                    {
                        var addedSubscribersSet = new HashSet<string>(currentSubscribers, StringComparer.OrdinalIgnoreCase);
                        addedSubscribersSet.ExceptWith(previousSubscribers);

                        var removedSubscribersSet = new HashSet<string>(previousSubscribers, StringComparer.OrdinalIgnoreCase);
                        removedSubscribersSet.ExceptWith(currentSubscribers);

                        addedSubscribers.AddRange(addedSubscribersSet.Select(x => (topic, x)));
                        removedSubscribers.AddRange(removedSubscribersSet.Select(x => (topic, x)));
                    }

                    // This topic was removed, flag all subscribers as removed as well
                    else
                    {
                        removedSubscribers.AddRange(previousSubscribers.Select(x => (topic, x)));
                    }
                }

                foreach (var (topic, currentSubscribers) in options.Topics)
                {
                    // This topic is new, flag all subscribers as added
                    if (!this._previousOptions.Topics.ContainsKey(topic))
                    {
                        addedSubscribers.AddRange(currentSubscribers.Select(x => (topic, x)));
                    }
                }
            }

            foreach (var (topic, addedSubscriber) in addedSubscribers)
            {
                this._cancellationTokenRegistry.Register(topic, addedSubscriber);
            }

            foreach (var (topic, removedSubscriber) in removedSubscribers)
            {
                this._cancellationTokenRegistry.Unregister(topic, removedSubscriber);
            }

            // Make a backup of the current options
            this._previousOptions = new TopicOptions(options);
        }
    }
}