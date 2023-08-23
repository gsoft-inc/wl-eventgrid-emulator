using System.Net;

namespace EventGridEmulator.Tests;

public sealed class TestHandler : HttpMessageHandler
{
    private readonly Action<string> _requestAction;

    public TestHandler(Action<string> requestAction)
    {
        this._requestAction = requestAction;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        this._requestAction(await request.Content!.ReadAsStringAsync(cancellationToken));
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        return await Task.FromResult(response);
    }
}