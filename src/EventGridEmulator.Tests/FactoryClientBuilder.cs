using EventGridEmulator.Configuration;

namespace EventGridEmulator.Tests;

public class FactoryClientBuilder
{
    private readonly HttpMessageHandler _handler;
    private readonly List<Topic> _topics = [];
    private readonly List<Filter> _filters = [];

    public FactoryClientBuilder(HttpMessageHandler handler)
    {
        this._handler = handler;
    }

    public FactoryClientBuilder WithTopic(string name, string url)
    {
        this._topics.Add(new Topic(name, url));
        return this;
    }

    public FactoryClientBuilder WithFilter(Filter filter)
    {
        this._filters.Add(filter);
        return this;
    }

    public HttpClient Build()
    {
        var factory = new SubscriberWebApplicationFactory(
            services =>
            {
                _ = services.Configure<TopicOptions>(x =>
                {
                    x.Topics = this._topics.ToDictionary(k => k.Name, v => new[] { v.Url });
                    x.Filters = [.. this._filters];
                });
            },
            this._handler);
        return factory.CreateClient();
    }

    private sealed class Topic
    {
        public Topic(string name, string url)
        {
            this.Name = name;
            this.Url = url;
        }

        public string Name { get; }

        public string Url { get; }
    }
}
