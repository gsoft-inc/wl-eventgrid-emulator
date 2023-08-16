using System.Net;

namespace EventGridEmulator.Tests;

public sealed class YoloHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        return Task.FromResult(response);
    }
}