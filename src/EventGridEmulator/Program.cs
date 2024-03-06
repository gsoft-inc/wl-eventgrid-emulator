using Azure.Messaging;
using EventGridEmulator.Configuration;
using EventGridEmulator.EventHandling;
using EventGridEmulator.Network;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json.Serialization;

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
builder.Services.AddSingleton<CompositeEventHttpContextHandler>();
builder.Services.AddSingleton(typeof(TopicSubscribers<>));
builder.Services.AddHostedService<ApplicationLifetimeLoggingHostedService>();
builder.Services.AddHostedService<PeriodicConfigurationReloadHostedService>();

var app = builder.Build();

app.MapPost(CompositeEventHttpContextHandler.Route, CompositeEventHttpContextHandler.HandleAsync);

// TODO Move somewhere else
app.MapPost("topics/{topic}:publish", async (HttpContext context, [FromRoute] string topic, [FromServices] CompositeEventHttpContextHandler handler) =>
{
    await CompositeEventHttpContextHandler.HandleAsync(context, topic, handler);
    return Results.Ok(new object());
});
// TODO Move somewhere else
app.MapPost("topics/{topic}/eventsubscriptions/{subscription}:receive", async (string topic, string subscription, CancellationToken cancellationToken, [FromServices] TopicSubscribers<CloudEvent> events) =>
{
    var result = await events.GetEventAsync(topic, subscription, cancellationToken);
    return Results.Ok(new { value = new[] { new { brokerProperties = new { deliveryCount = 1, lockToken = result.LockToken }, @event = result.Item } } });
});
// TODO Move somewhere else
app.MapPost("topics/{topic}/eventsubscriptions/{subscription}:acknowledge", (string topic, string subscription, AcknowledgeData data, [FromServices] TopicSubscribers<CloudEvent> events) =>
{
    var succeededLockTokens = new List<string>();
    var failedLockTokens = new List<FailedLockToken>();
    if (data?.LockTokens is not null)
    {
        foreach (var token in data.LockTokens)
        {
            if (token is null)
            {
                continue;
            }

            if (events.TryDeleteEvent(topic, subscription, token))
            {
                succeededLockTokens.Add(token);
            }
            else
            {
                failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
            }
        }
    }

    return Results.Ok(new
    {
        failedLockTokens,
        succeededLockTokens,
    });
});
// TODO Move somewhere else
app.MapPost("topics/{topic}/eventsubscriptions/{subscription}:release", (string topic, string subscription, AcknowledgeData data, [FromServices] TopicSubscribers<CloudEvent> events) =>
{
    var succeededLockTokens = new List<string>();
    var failedLockTokens = new List<FailedLockToken>();
    if (data?.LockTokens is not null)
    {
        foreach (var token in data.LockTokens)
        {
            if (token is null)
            {
                continue;
            }

            if (events.TryReleaseEvent(topic, subscription, token))
            {
                succeededLockTokens.Add(token);
            }
            else
            {
                failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
            }
        }
    }

    return Results.Ok(new
    {
        failedLockTokens,
        succeededLockTokens,
    });
});
// TODO Move somewhere else
// We don't support moving a message to the DLQ, so the logic is similar to acknowledge
app.MapPost("topics/{topic}/eventsubscriptions/{subscription}:reject", (string topic, string subscription, AcknowledgeData data, [FromServices] TopicSubscribers<CloudEvent> events) =>
{
    var succeededLockTokens = new List<string>();
    var failedLockTokens = new List<FailedLockToken>();
    if (data?.LockTokens is not null)
    {
        foreach (var token in data.LockTokens)
        {
            if (token is null)
            {
                continue;
            }

            if (events.TryDeleteEvent(topic, subscription, token))
            {
                succeededLockTokens.Add(token);
            }
            else
            {
                failedLockTokens.Add(new() { LockToken = token, Error = new() { Message = "invalid token" } });
            }
        }
    }

    return Results.Ok(new
    {
        failedLockTokens,
        succeededLockTokens,
    });
});

app.Run();

// TODO Move somewhere else
internal sealed class AcknowledgeData
{
    public string?[]? LockTokens { get; set; }
}

// TODO Move somewhere else
internal sealed class FailedLockToken
{
    public string? LockToken { get; set; }

    public ResponseError? Error { get; set; }
}

// TODO Move somewhere else
internal sealed class ResponseError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// For integration testing purposes only in order to use WebApplicationFactory<TProgram>
public abstract partial class Program
{
}
