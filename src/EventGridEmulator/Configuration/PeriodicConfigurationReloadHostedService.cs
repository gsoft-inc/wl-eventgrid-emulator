namespace EventGridEmulator.Configuration;

internal sealed class PeriodicConfigurationReloadHostedService : IHostedService
{
    private readonly IConfigurationRoot _configuration;
    private readonly ILogger<PeriodicConfigurationReloadHostedService> _logger;

    public PeriodicConfigurationReloadHostedService(IConfiguration configuration, ILogger<PeriodicConfigurationReloadHostedService> logger)
    {
        this._configuration = (IConfigurationRoot)configuration;
        this._logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // .NET appsettings.json "hot reload" doesn't work when the JSON file is mounted as a volume in a Docker container.
        // That's why we need to manually trigger the reload periodically, to detect if the user has changed the configuration.
        // https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_running_in_container-and-dotnet_running_in_containers
        var runsInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") is "1" or "true";
        if (runsInContainer)
        {
            _ = this.PeriodicallyReloadConfigurationAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task PeriodicallyReloadConfigurationAsync(CancellationToken cancellationToken)
    {
        var isConfigurationMalformed = false;

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    this._configuration.Reload();

                    isConfigurationMalformed = false;
                }
                catch (InvalidDataException)
                {
                    if (!isConfigurationMalformed)
                    {
                        this._logger.LogWarning("The configuration file is malformed and could not be reloaded.");
                    }

                    isConfigurationMalformed = true;
                }
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