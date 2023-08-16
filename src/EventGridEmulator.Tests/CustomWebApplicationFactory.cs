using EventGridEmulator.Configuration;
using EventGridEmulator.Network;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace EventGridEmulator.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _configureServices;

    public CustomWebApplicationFactory(Action<IServiceCollection> configureServices)
    {
        this._configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            this._configureServices(services);

            OptionsServiceCollectionExtensions.Configure<TopicOptions>(services, options =>
            {
                options.Topics = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["orders"] = new[]
                    {
                        null!,
                        string.Empty,
                        "invalid_url",
                        "https://localhost/orders-webhook1",
                        "https://localhost/orders-webhook2",
                    },
                    ["customers"] = new[]
                    {
                        "https://localhost/customers-webhook1",
                        "https://localhost/customers-webhook1",
                    },
                };
            });

            var handler = new YoloHandler();
            HttpClientFactoryServiceCollectionExtensions.AddHttpClient(services, SubscriberConstants.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        });
    }
}