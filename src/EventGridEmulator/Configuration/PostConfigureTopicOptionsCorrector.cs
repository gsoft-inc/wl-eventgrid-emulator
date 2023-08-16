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
        if (options.Topics == null)
        {
            options.Topics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        options.Topics = CorrectTopics(options.Topics);
    }

    private static Dictionary<string, string[]> CorrectTopics(Dictionary<string, string[]> topics)
    {
        var correctedTopics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

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
                if (Uri.TryCreate(subscriber, UriKind.Absolute, out var subscriberUri))
                {
                    correctedSubscribers.Add(subscriberUri.ToString());
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

        return correctedTopics;
    }
}