using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace EventGridEmulator.EventHandling;

internal sealed class EventGridPublishHandler
{
    [StringSyntax("Route")]
    public const string CustomTopicRoute = "/{topic}/api/events";
    
    [StringSyntax("Route")]
    public const string NamespaceTopicRoute = "/topics/{topic}:publish";

    private const string CloudEventContentType = "application/cloudevents+json; charset=utf-8";
    private const string CloudEventBatchContentType = "application/cloudevents-batch+json; charset=utf-8";
    private const string EventGridEventContentType = "application/json";

    private readonly IEventGridEventHttpContextHandler _eventGridEventHttpContextHandler;
    private readonly ICloudEventHttpContextHandler _cloudEventHttpContextHandler;

    public EventGridPublishHandler(IEventGridEventHttpContextHandler eventGridEventHttpContextHandler, ICloudEventHttpContextHandler cloudEventHttpContextHandler)
    {
        this._eventGridEventHttpContextHandler = eventGridEventHttpContextHandler;
        this._cloudEventHttpContextHandler = cloudEventHttpContextHandler;
    }

    public static async Task HandleCustomTopicEventAsync(HttpContext context, [FromRoute] string topic, [FromServices] EventGridPublishHandler handler)
    {
        await handler.HandleAsync(context, topic);
    }
    
    public static async Task<IResult> HandleNamespaceTopicEventAsync(HttpContext context, [FromRoute] string topic, [FromServices] EventGridPublishHandler handler)
    {
        await handler.HandleAsync(context, topic);
        return Results.Ok(new object());
    }
    
    private Task HandleAsync(HttpContext context, string topic) => context.Request.ContentType switch
    {
        EventGridEventContentType => this._eventGridEventHttpContextHandler.HandleAsync(context, topic),
        CloudEventContentType => this._cloudEventHttpContextHandler.HandleAsync(context, topic, batch: false),
        CloudEventBatchContentType => this._cloudEventHttpContextHandler.HandleAsync(context, topic, batch: true),
        _ => Results.BadRequest().ExecuteAsync(context),
    };
}