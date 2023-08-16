namespace EventGridEmulator.EventHandling;

internal interface IEventGridEventHttpContextHandler
{
    public Task HandleAsync(HttpContext context, string topic);
}