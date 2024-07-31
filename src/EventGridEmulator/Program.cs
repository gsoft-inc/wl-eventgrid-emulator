using EventGridEmulator.Configuration;
using EventGridEmulator.EventHandling;
using EventGridEmulator.Network;
using Microsoft.Extensions.Options;
using Serilog;

// Disable automatic propagation of activity context (telemetry) between the publisher and the subscribers
// This usually happens through HTTP headers when using HttpClient (https://stackoverflow.com/q/72277304/825695)
AppContext.SetSwitch("System.Net.Http.EnableActivityPropagation", false);

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
builder.Services.AddSingleton<EventGridPublishHandler>();
builder.Services.AddSingleton<PullQueueHttpContextHandler>();
builder.Services.AddSingleton(typeof(TopicSubscribers<>));
builder.Services.AddHostedService<ApplicationLifetimeLoggingHostedService>();
builder.Services.AddHostedService<PeriodicConfigurationReloadHostedService>();

var app = builder.Build();

app.MapPost(EventGridPublishHandler.CustomTopicRoute, EventGridPublishHandler.HandleCustomTopicEventAsync);
app.MapPost(EventGridPublishHandler.NamespaceTopicRoute, EventGridPublishHandler.HandleNamespaceTopicEventAsync);
app.MapPost(PullQueueHttpContextHandler.ReceiveRoute, PullQueueHttpContextHandler.HandleReceiveAsync);
app.MapPost(PullQueueHttpContextHandler.AcknowledgeRoute, PullQueueHttpContextHandler.HandleAcknowledgeAsync);
app.MapPost(PullQueueHttpContextHandler.ReleaseRoute, PullQueueHttpContextHandler.HandleReleaseAsync);
app.MapPost(PullQueueHttpContextHandler.RejectRoute, PullQueueHttpContextHandler.HandleRejectAsync);

app.Run();

// For integration testing purposes only in order to use WebApplicationFactory<TProgram>
public abstract partial class Program
{
}
