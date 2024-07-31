using System.Text.Json;

namespace EventGridEmulator.EventHandling;

internal static class EventsSerializer
{
    public static async Task<T[]?> DeserializeEventsAsync<T>(HttpContext context)
    {
        try
        {
            // CloudEvent and EventGridEvent implement their own JsonConverter, so we don't care about specifying serializer options here
            return await JsonSerializer.DeserializeAsync<T[]>(context.Request.Body, options: null, context.RequestAborted);
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    public static async Task<T?> DeserializeEventAsync<T>(HttpContext context)
    {
        try
        {
            // CloudEvent and EventGridEvent implement their own JsonConverter, so we don't care about specifying serializer options here
            return await JsonSerializer.DeserializeAsync<T>(context.Request.Body, options: null, context.RequestAborted);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}