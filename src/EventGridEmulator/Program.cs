using EventGridEmulator.Configuration;
using EventGridEmulator.EventHandling;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// The intended way for consumers is to provide their own appsettings.json through Docker volumes
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.defaults.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Serilog provides a more concise console logging experience with colored tokens
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.WriteTo.Console();
});

builder.Services.AddOptions<TopicOptions>().BindConfiguration(string.Empty);
builder.Services.AddSingleton<SubscriberPolicyHttpMessageHandler>();
builder.Services.AddSingleton<TrustingDevelopmentCertificateHttpClientHandler>();
builder.Services.AddHttpClient(SubscriberConstants.HttpClientName, SubscriberConstants.ConfigureHttpClient)
    .AddHttpMessageHandler<SubscriberPolicyHttpMessageHandler>()
    .ConfigurePrimaryHttpMessageHandler<TrustingDevelopmentCertificateHttpClientHandler>();
builder.Services.AddSingleton<IPostConfigureOptions<TopicOptions>, PostConfigureTopicOptionsCorrector>();
builder.Services.AddSingleton<IPostConfigureOptions<TopicOptions>, PostConfigureTopicOptionsCancellationManager>();
builder.Services.AddSingleton<ISubscriberCancellationTokenRegistry, SubscriberCancellationTokenRegistry>();
builder.Services.AddSingleton<IEventGridEventHttpContextHandler, EventGridEventHttpContextHandler>();
builder.Services.AddSingleton<ICloudEventHttpContextHandler, CloudEventHttpContextHandler>();
builder.Services.AddSingleton<CompositeEventHttpContextHandler>();
builder.Services.AddHostedService<ApplicationLifetimeLoggingHostedService>();
builder.Services.AddHostedService<PeriodicConfigurationReloadHostedService>();

var app = builder.Build();

app.MapPost(CompositeEventHttpContextHandler.Route, CompositeEventHttpContextHandler.HandleAsync);

app.Run();

// For integration testing purposes only in order to use WebApplicationFactory<TProgram>
public abstract partial class Program
{
}