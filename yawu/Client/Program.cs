using Client.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<IManager, AptGetManager>();
        services.AddSingleton<IManager, FlatpakManager>();

        services.AddHostedService<ManagerService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await builder.RunAsync();

internal class ManagerService(IEnumerable<IManager> managers, ILogger<ManagerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ManagerService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Checking package managers...");

            foreach (var manager in managers)
            {
                try
                {
                    if (await manager.IsManagerAvailableAsync())
                    {
                        logger.LogInformation($"{manager.GetType().Name} is available.");

                        var installedPackages = await manager.GetInstalledPackagesAsync();
                        logger.LogInformation(
                            $"{manager.GetType().Name} found {installedPackages.Length} installed packages.");

                        var packagesWithUpdates = await manager.GetPackagesWithAvailableUpdatesAsync();
                        logger.LogInformation(
                            $"{manager.GetType().Name} found {packagesWithUpdates.Length} packages with updates.");

                        if (packagesWithUpdates.Length > 0)
                        {
                            logger.LogInformation($"{manager.GetType().Name} packages with updates:");
                            foreach (var pkg in packagesWithUpdates)
                            {
                                logger.LogInformation(
                                    $"- {pkg.Name}: Current Version: {pkg.Version ?? "unknown"}, Available Version: {pkg.AvailableVersion ?? "unknown"}");
                            }
                        }
                        else
                        {
                            logger.LogInformation($"{manager.GetType().Name} has no packages with available updates.");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"{manager.GetType().Name} is not available on this system.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error occurred while running {manager.GetType().Name}.");
                }
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        logger.LogInformation("ManagerService is stopping.");
    }
}
