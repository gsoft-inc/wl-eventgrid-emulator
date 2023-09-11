namespace EventGridEmulator.Configuration;

internal sealed class PeriodicConfigurationReloadHostedService : IHostedService
{
    private readonly IConfigurationRoot _configuration;

    public PeriodicConfigurationReloadHostedService(IConfiguration configuration)
    {
        this._configuration = (IConfigurationRoot)configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // .NET appsettings.json "hot reload" doesn't work when the JSON file is mounted as a volume in a Docker container.
        // That's why we need to manually trigger the reload periodically, to detect if the user has changed the configuration.
        var runsInContainer = Environment.GetEnvironmentVariable("RUNS_IN_CONTAINER") == "true";
        if (runsInContainer)
        {
            _ = this.PeriodicallyReloadConfigurationAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task PeriodicallyReloadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                this._configuration.Reload();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}