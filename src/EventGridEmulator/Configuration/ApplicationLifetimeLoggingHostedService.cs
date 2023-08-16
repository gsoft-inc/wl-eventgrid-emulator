using System.Diagnostics.CodeAnalysis;
using System.Text;
using EventGridEmulator.EventHandling;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace EventGridEmulator.Configuration;

internal sealed class ApplicationLifetimeLoggingHostedService : IHostedService, IDisposable
{
    private readonly IServer _server;
    private readonly ILogger<ApplicationLifetimeLoggingHostedService> _logger;
    private readonly IOptionsMonitor<TopicOptions> _options;
    private readonly CancellationTokenRegistration _startedHandle;
    private readonly CancellationTokenRegistration _stoppingHandle;
    private readonly CancellationTokenRegistration _stoppedHandle;
    private readonly IDisposable? _optionsHandle;
    private int _optionsChangeCount;

    public ApplicationLifetimeLoggingHostedService(
        IHostApplicationLifetime lifetime,
        IServer server,
        ILogger<ApplicationLifetimeLoggingHostedService> logger,
        IOptionsMonitor<TopicOptions> options)
    {
        this._server = server;
        this._logger = logger;
        this._options = options;

        this._startedHandle = lifetime.ApplicationStarted.Register(OnApplicationStarted, this);
        this._stoppingHandle = lifetime.ApplicationStopping.Register(OnApplicationStopping, this);
        this._stoppedHandle = lifetime.ApplicationStopped.Register(OnApplicationStopped, this);
        this._optionsHandle = options.OnChangeDelayed(this.OnOptionsChanged);
        this._optionsChangeCount = 0;
    }

    private static void OnApplicationStarted(object? state)
    {
        var self = (ApplicationLifetimeLoggingHostedService)state!;

        var addressesFeature = self._server.Features.Get<IServerAddressesFeature>();
        if (addressesFeature != null)
        {
            var addresses = string.Join(", ", addressesFeature.Addresses.Select(x => x + CompositeEventHttpContextHandler.Route));
            self._logger.LogInformation("Now listening for events on: {Addresses}", addresses);
        }

        self._logger.LogInformation("Event Grid emulator started. Press Ctrl+C to shut down.");

        self.OnOptionsChanged(self._options.CurrentValue);
    }

    private static void OnApplicationStopping(object? state)
    {
        var self = (ApplicationLifetimeLoggingHostedService)state!;
        self._logger.LogInformation("Event Grid emulator is shutting down...");
    }

    private static void OnApplicationStopped(object? state)
    {
        var self = (ApplicationLifetimeLoggingHostedService)state!;
        self._logger.LogInformation("Event Grid emulator has stopped.");
    }

    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "We use two well-defined static message templates")]
    private void OnOptionsChanged(TopicOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        var linesAdded = 0;
        foreach (var (topic, subscribers) in options.Topics)
        {
            if (linesAdded > 0)
            {
                sb.AppendLine();
            }

            sb.Append(" - ").Append(topic).Append(": ").AppendJoin(", ", subscribers);
            linesAdded++;
        }

        if (linesAdded == 0)
        {
            sb.Append("Nothing");
        }

        this._optionsChangeCount++;
        var messageTemplate = this._optionsChangeCount == 1
            ? "Loaded topics and subscribers:{Configuration}"
            : "Reloaded topics and subscribers:{Configuration}";

        this._logger.LogInformation(messageTemplate, sb.ToString());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        this._optionsHandle?.Dispose();
        this._startedHandle.Dispose();
        this._stoppingHandle.Dispose();
        this._stoppedHandle.Dispose();
    }
}