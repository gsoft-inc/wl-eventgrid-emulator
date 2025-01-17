using System.Net;

namespace EventGridEmulator.Network;

internal static class SubscriberConstants
{
    public const string HttpClientName = "subscribers";

    public const string DefaultTopicValue = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test-rg/providers/Microsoft.EventGrid/topics/";

    // Message Delivery Status
    // https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry?WT.mc_id=DT-MVP-5003978#message-delivery-status
    public static readonly HashSet<HttpStatusCode> NonRetriableStatusCodes = new HashSet<HttpStatusCode>
    {
        // Success Codes
        HttpStatusCode.OK,
        HttpStatusCode.Created,
        HttpStatusCode.Accepted,
        HttpStatusCode.NonAuthoritativeInformation,
        HttpStatusCode.NoContent,

        // Failure Codes
        HttpStatusCode.BadRequest,
        HttpStatusCode.Unauthorized,
        HttpStatusCode.Forbidden,
        HttpStatusCode.RequestEntityTooLarge
    };

    // Event Grid retry schedule
    // https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-schedule
    public static readonly TimeSpan[] EventGridRetrySchedule =
    {
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(3),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(12),
        TimeSpan.FromHours(12),
    };

    public static void ConfigureHttpClient(HttpClient httpClient)
    {
        // Event Grid waits 30 seconds for message delivery but this is configured in our http message handler
        // Setting it here will cause the 30 seconds to span over the entire retry schedule instead of being used for each individual retry
        // https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry#retry-schedule
        httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }
}
