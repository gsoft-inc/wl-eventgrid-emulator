using EventGridEmulator.Network;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace EventGridEmulator.Tests;

public class SubscriberWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _configureServices;
    private readonly HttpMessageHandler _handler;

    public SubscriberWebApplicationFactory(Action<IServiceCollection> configureServices, HttpMessageHandler handler)
    {
        this._configureServices = configureServices;
        this._handler = handler;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            this._configureServices(services);
            services.AddHttpClient(SubscriberConstants.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => this._handler);
        });
    }
}