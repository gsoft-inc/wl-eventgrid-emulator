using System.Net;
using System.Security.Authentication;
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
builder.Configuration.AddCommandLine(args);

// Serilog provides a more concise console logging experience with colored tokens
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.WriteTo.Console();
});

builder.Services.AddOptions<TopicOptions>().BindConfiguration(string.Empty);
builder.Services.AddSingleton<SubscriberPolicyHttpMessageHandler>();
builder.Services.AddHttpClient(SubscriberConstants.HttpClientName, SubscriberConstants.ConfigureHttpClient)
    .AddHttpMessageHandler<SubscriberPolicyHttpMessageHandler>()
    .ConfigurePrimaryHttpMessageHandler(handler => new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        CheckCertificateRevocationList = false,
        SslProtocols = SslProtocols.None,
    });
builder.Services.AddSingleton<IPostConfigureOptions<TopicOptions>, PostConfigureTopicOptionsCorrector>();
builder.Services.AddSingleton<IPostConfigureOptions<TopicOptions>, PostConfigureTopicOptionsCancellationManager>();
builder.Services.AddSingleton<ISubscriberCancellationTokenRegistry, SubscriberCancellationTokenRegistry>();
builder.Services.AddSingleton<IEventGridEventHttpContextHandler, EventGridEventHttpContextHandler>();
builder.Services.AddSingleton<ICloudEventHttpContextHandler, CloudEventHttpContextHandler>();
builder.Services.AddSingleton<CompositeEventHttpContextHandler>();
builder.Services.AddHostedService<ApplicationLifetimeLoggingHostedService>();

var app = builder.Build();

app.MapPost(CompositeEventHttpContextHandler.Route, CompositeEventHttpContextHandler.HandleAsync);

app.Run();

// For integration testing purposes only in order to use WebApplicationFactory<TProgram>
public partial class Program
{
}