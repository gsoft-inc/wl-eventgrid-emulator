namespace EventGridEmulator.EventHandling;

internal interface ICloudEventHttpContextHandler
{
    public Task HandleAsync(HttpContext context, string topic);
}