namespace EventGridEmulator.Network;

/// <summary>
/// Manages a dedicated cancellation token for each tuple of (topic, subscriber).
/// That way, if a subscriber is removed from the configuration at runtime,
/// we can have a handle to its associated cancellation token and cancel any pending HTTP request.
/// </summary>
internal sealed class SubscriberCancellationTokenRegistry : ISubscriberCancellationTokenRegistry, IDisposable
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly Dictionary<string, Lazy<CancellationTokenSource>> _cancellationTokenSources;

    public SubscriberCancellationTokenRegistry(IHostApplicationLifetime applicationLifetime)
    {
        this._applicationLifetime = applicationLifetime;
        this._cancellationTokenSources = new Dictionary<string, Lazy<CancellationTokenSource>>(StringComparer.OrdinalIgnoreCase);
    }

    private static string GetKey(string topic, string subscriber) => $"{topic}/{subscriber}";

    public void Register(string topic, string subscriber)
    {
        this._cancellationTokenSources.TryAdd(GetKey(topic, subscriber), new Lazy<CancellationTokenSource>(this.CancellationTokenSourceFactory));
    }

    private CancellationTokenSource CancellationTokenSourceFactory()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(this._applicationLifetime.ApplicationStopping);
    }

    public CancellationToken Get(string topic, string subscriber)
    {
        return this._cancellationTokenSources.TryGetValue(GetKey(topic, subscriber), out var cts) ? cts.Value.Token : this._applicationLifetime.ApplicationStopping;
    }

    public void Unregister(string topic, string subscriber)
    {
        if (this._cancellationTokenSources.Remove(GetKey(topic, subscriber), out var cts))
        {
            try
            {
                if (cts.IsValueCreated)
                {
                    cts.Value.Cancel();
                    cts.Value.Dispose();
                }
            }
            catch
            {
                // Ignored
            }
        }
    }

    public void Dispose()
    {
        foreach (var cts in this._cancellationTokenSources.Values)
        {
            try
            {
                if (cts.IsValueCreated)
                {
                    cts.Value.Cancel();
                    cts.Value.Dispose();
                }
            }
            catch
            {
                // Ignored
            }
        }

        this._cancellationTokenSources.Clear();
    }
}