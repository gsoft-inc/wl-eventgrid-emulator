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

        this.Filters = original.Filters.Select(f => new Filter(f)).ToArray();
    }

    public HashSet<string>? InvalidUrls { get; set; }
    public Dictionary<string, string[]> Topics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Filter[] Filters { get; set; } = [];

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

        if (obj is not TopicOptions other)
        {
            return false;
        }

        return this.Topics.Count == other.Topics.Count
            && this.Topics.All(kv =>
                other.Topics.TryGetValue(kv.Key, out var value) && kv.Value.SequenceEqual(value)
            )
            && this.Filters.Length == other.Filters.Length
            && this.Filters.SequenceEqual(other.Filters);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "This is a DTO, also we don't plan on storing this in a hash table.")]
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kv in this.Topics.OrderBy(k => k.Key))
        {
            hash.Add(kv.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(kv.Value);
        }

        foreach (var filter in this.Filters)
        {
            hash.Add(filter.GetHashCode());
        }

        return hash.ToHashCode();
    }
}

internal sealed record PushSubscriber(string Uri);

internal sealed record PullSubscriber(string SubscriptionName);
