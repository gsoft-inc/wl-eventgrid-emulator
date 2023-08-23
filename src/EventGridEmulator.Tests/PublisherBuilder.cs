using Azure;
using Azure.Core.Pipeline;
using Azure.Messaging.EventGrid;

namespace EventGridEmulator.Tests;

public class PublisherBuilder
{
    private readonly HttpClient _subscriber;
    private Uri? _uri;

    public PublisherBuilder(HttpClient subscriber)
    {
        this._subscriber = subscriber;
    }

    public PublisherBuilder WithEndpoint(Uri endpointUri)
    {
        this._uri = endpointUri;
        return this;
    }

    public EventGridPublisherClient Build()
    {
        if (this._subscriber is null)
        {
            throw new InvalidOperationException("Missing subscriber");
        }

        if (this._uri is null)
        {
            throw new InvalidOperationException("Missing endpointUri");
        }

        return new EventGridPublisherClient(this._uri, new AzureKeyCredential("noop"), new EventGridPublisherClientOptions
        {
            Transport = new HttpClientTransport(this._subscriber),
        });
    }
}