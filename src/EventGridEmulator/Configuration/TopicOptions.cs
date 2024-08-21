using System.Diagnostics.CodeAnalysis;

namespace EventGridEmulator.Configuration;

internal sealed class TopicOptions
{
    private const string PullScheme = "pull";

    public TopicOptions()
    {
    }

    /// <summary>
    /// Creates a deep copy of the provided <see cref="TopicOptions"/>.
    /// </summary>
    public TopicOptions(TopicOptions original)
    {
        foreach (var (topic, subscribers) in original.Topics)
        {
            var subscribersCopy = new string[subscribers.Length];
            Array.Copy(subscribers, subscribersCopy, subscribers.Length);
            this.Topics.Add(topic, subscribersCopy);
        }
    }

    public HashSet<string>? InvalidUrls { get; set; }
    public Dictionary<string, string[]> Topics { get; set; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

    internal static bool IsValidScheme(Uri uri)
    {
        return IsHttpOrHttps(uri) || IsPullScheme(uri);
    }

    public IEnumerable<PushSubscriber> GetPushSubscribers(string topic)
    {
        if (!this.Topics.TryGetValue(topic, out var subscribers))
        {
            yield break;
        }

        foreach (var value in subscribers)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var url) && IsHttpOrHttps(url))
            {
                yield return new PushSubscriber(url.OriginalString);
            }
        }
    }

    public IEnumerable<PullSubscriber> GetPullSubscribers(string topic)
    {
        if (!this.Topics.TryGetValue(topic, out var subscribers))
        {
            yield break;
        }

        foreach (var value in subscribers)
        {
            // pull://subscriptionName
            if (Uri.TryCreate(value, UriKind.Absolute, out var url) && IsPullScheme(url))
            {
                yield return new PullSubscriber(url.Host);
            }
        }
    }

    private static bool IsHttpOrHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    private static bool IsPullScheme(Uri uri) => string.Equals(uri.Scheme, PullScheme, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is TopicOptions other && this.Equals(other);
    }

    private bool Equals(TopicOptions other) => (this.Topics, other.Topics) switch
    {
        (null, null) => true,
        (not null, null) => false,
        (null, not null) => false,
        (not null, not null) => this.TopicsEquals(other),
    };

    private bool TopicsEquals(TopicOptions other)
    {
        if (this.Topics.Count != other.Topics.Count)
        {
            return false;
        }

        foreach (var (topicName, subscribers) in this.Topics)
        {
            if (!other.Topics.TryGetValue(topicName, out var otherSubscribers))
            {
                return false;
            }

            if (!subscribers.SequenceEqual(otherSubscribers, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "This is a DTO, also we don't plan on storing this in a hash table.")]
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;

            foreach (var pair in this.Topics)
            {
                hash = hash * 23 + pair.Key.GetHashCode();
                hash = hash * 23 + (pair.Value != null ? pair.Value.GetHashCode() : 0);
            }

            return hash;
        }
    }
}

internal sealed record PushSubscriber(string Uri);

internal sealed record PullSubscriber(string SubscriptionName);