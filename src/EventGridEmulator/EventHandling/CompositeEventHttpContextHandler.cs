using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace EventGridEmulator.EventHandling;

internal sealed class CompositeEventHttpContextHandler
{
    [StringSyntax("Route")]
    public const string ApiRoute = "/{topic}/api/events";
    
    [StringSyntax("Route")]
    public const string PublishRoute = "/topics/{topic}:publish";

    private const string CloudEventContentType = "application/cloudevents-batch+json; charset=utf-8";
    private const string EventGridEventContentType = "application/json";

    private readonly IEventGridEventHttpContextHandler _eventGridEventHttpContextHandler;
    private readonly ICloudEventHttpContextHandler _cloudEventHttpContextHandler;

    public CompositeEventHttpContextHandler(IEventGridEventHttpContextHandler eventGridEventHttpContextHandler, ICloudEventHttpContextHandler cloudEventHttpContextHandler)
    {
        this._eventGridEventHttpContextHandler = eventGridEventHttpContextHandler;
        this._cloudEventHttpContextHandler = cloudEventHttpContextHandler;
    }

    public static async Task HandleAsync(HttpContext context, [FromRoute] string topic, [FromServices] CompositeEventHttpContextHandler handler)
    {
        await handler.HandleAsync(context, topic);
    }
    
    public static async Task<IResult> HandlePublishAsync(HttpContext context, [FromRoute] string topic, [FromServices] CompositeEventHttpContextHandler handler)
    {
        await handler.HandleAsync(context, topic);
        return Results.Ok(new object());
    }
    
    private Task HandleAsync(HttpContext context, string topic) => context.Request.ContentType switch
    {
        EventGridEventContentType => this._eventGridEventHttpContextHandler.HandleAsync(context, topic),
        CloudEventContentType => this._cloudEventHttpContextHandler.HandleAsync(context, topic),
        _ => Results.BadRequest().ExecuteAsync(context),
    };
}