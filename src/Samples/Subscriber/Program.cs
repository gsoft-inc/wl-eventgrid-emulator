using Azure.Messaging.EventGrid;

var app = WebApplication.CreateBuilder(args).Build();

app.MapPost("/webhook-200", (EventGridEvent[] events) => Results.Ok());
app.MapPost("/webhook-404", (EventGridEvent[] events) => Results.NotFound());
app.MapPost("/webhook-400", (EventGridEvent[] events) => Results.BadRequest());
app.MapPost("/webhook-401", (EventGridEvent[] events) => Results.Unauthorized());
app.MapPost("/webhook-slow-200", async (EventGridEvent[] events, CancellationToken cancellationToken) =>
{
    try
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }
    catch (OperationCanceledException)
    {
    }

    return Results.Ok();
});

app.Run();
