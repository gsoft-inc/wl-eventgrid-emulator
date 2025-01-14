using Microsoft.Extensions.Options;

namespace EventGridEmulator.Configuration;

/// <summary>
/// Makes sure the <see cref="TopicOptions"/> are always valid no matter what user-defined configuration is provided.
/// Removes invalid values, consolidates duplicates, etc. so that the application is more resilient.
/// </summary>
internal sealed class PostConfigureTopicOptionsCorrector : IPostConfigureOptions<TopicOptions>
{
    public void PostConfigure(string? name, TopicOptions options)
    {
        options.Topics ??= new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var (topics, invalidUrls) = CorrectTopics(options.Topics);
        options.Topics = topics;
        options.InvalidUrls = invalidUrls;
    }

    private static (Dictionary<string, string[]> Topics, HashSet<string>? InvalidUrls) CorrectTopics(Dictionary<string, string[]> topics)
    {
        var correctedTopics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        HashSet<string>? errors = null;

        foreach (var (topic, subscribers) in topics)
        {
            var correctedTopicName = topic?.Trim() ?? string.Empty;
            if (correctedTopicName.Length == 0 || subscribers == null)
            {
                continue;
            }

            var correctedSubscribers = new List<string>(subscribers.Length);

            if (correctedTopics.TryGetValue(correctedTopicName, out var existingCorrectedSubscribers))
            {
                correctedSubscribers.AddRange(existingCorrectedSubscribers);
            }

            foreach (var subscriber in subscribers)
            {
                if (Uri.TryCreate(subscriber, UriKind.Absolute, out var subscriberUri) && TopicOptions.IsValidScheme(subscriberUri))
                {
                    correctedSubscribers.Add(subscriberUri.ToString());
                }
                else if (!string.IsNullOrWhiteSpace(subscriber))
                {
                    errors ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    errors.Add(subscriber);
                }
            }

            if (correctedSubscribers.Count > 0)
            {
                correctedTopics[correctedTopicName] = correctedSubscribers.ToArray();
            }
        }

        foreach (var (topic, subscribers) in correctedTopics)
        {
            correctedTopics[topic] = new HashSet<string>(subscribers, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        return (correctedTopics, errors);
    }
}
