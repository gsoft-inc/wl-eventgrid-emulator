using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace EventGridEmulator.EventHandling;

internal sealed class PullQueueHttpContextHandler
{
    [StringSyntax("Route")]
    public const string ReceiveRoute = "topics/{topic}/eventsubscriptions/{subscription}:receive";

    [StringSyntax("Route")]
    public const string AcknowledgeRoute = "topics/{topic}/eventsubscriptions/{subscription}:acknowledge";

    [StringSyntax("Route")]
    public const string ReleaseRoute = "topics/{topic}/eventsubscriptions/{subscription}:release";

    [StringSyntax("Route")]
    public const string RejectRoute = "topics/{topic}/eventsubscriptions/{subscription}:reject";

    public static async Task<IResult> HandleReceiveAsync([FromRoute] string topic, [FromRoute] string subscription, [FromServices] TopicSubscribers<CloudEvent> events, CancellationToken cancellationToken)
    {
        var result = await events.GetEventAsync(topic, subscription, cancellationToken);
        return Results.Ok(new { value = new[] { new { brokerProperties = new { deliveryCount = 1, lockToken = result.LockToken }, @event = result.Item } } });
    }

    public static async Task<IResult> HandleAcknowledgeAsync([FromRoute] string topic, [FromRoute] string subscription, LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> events)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
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
    }

    public static async Task<IResult> HandleReleaseAsync([FromRoute] string topic, [FromRoute] string subscription, LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> events)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
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
    }

    public static async Task<IResult> HandleRejectAsync([FromRoute] string topic, [FromRoute] string subscription, LockTokensRequestData requestData, [FromServices] TopicSubscribers<CloudEvent> events)
    {
        var succeededLockTokens = new List<string>();
        var failedLockTokens = new List<FailedLockToken>();
        if (requestData?.LockTokens is not null)
        {
            foreach (var token in requestData.LockTokens)
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
    }
}

// TODO: add more accurate name.
internal sealed class LockTokensRequestData
{
    public string?[]? LockTokens { get; set; }
}

internal sealed class FailedLockToken
{
    public string? LockToken { get; set; }

    public ResponseError? Error { get; set; }
}

internal sealed class ResponseError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}