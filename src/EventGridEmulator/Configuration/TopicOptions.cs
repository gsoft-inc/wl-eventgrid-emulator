namespace EventGridEmulator.Configuration;

internal sealed class TopicOptions
{
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

    public Dictionary<string, string[]> Topics { get; set; } = new Dictionary<string, string[]>();
}