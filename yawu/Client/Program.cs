using System.Text.Json;
using Client.Managers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<IManager, AptGetManager>();
        services.AddSingleton<IManager, FlatpakManager>();
        services.AddHttpClient();
        services.AddHostedService<ManagerService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await builder.RunAsync();

internal class ManagerService : BackgroundService
{
    private readonly HubConnection _hubConnection;
    private readonly IEnumerable<IManager> _managers;
    private readonly ILogger<ManagerService> _logger;
    private readonly HttpClient _httpClient;

    public ManagerService(IEnumerable<IManager> managers, ILogger<ManagerService> logger, HttpClient httpClient)
    {
        _managers = managers;
        _logger = logger;
        _httpClient = httpClient;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5275/updatesHub")
            .WithAutomaticReconnect()
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ManagerService is starting.");

        var token = await GetJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to retrieve JWT token.");
            return;
        }

        await _hubConnection.StartAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Checking package managers...");
            await CheckPackageManagersAsync(token, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

        _logger.LogInformation("ManagerService is stopping.");
    }

    private async Task CheckPackageManagersAsync(string token, CancellationToken cancellationToken)
    {
        foreach (var manager in _managers)
        {
            try
            {
                if (await manager.IsManagerAvailableAsync())
                {
                    _logger.LogInformation($"{manager.GetType().Name} is available.");
                    await ProcessManagerAsync(manager, token, cancellationToken);
                }
                else
                {
                    _logger.LogWarning($"{manager.GetType().Name} is not available on this system.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while running {manager.GetType().Name}.");
            }
        }
    }

    private async Task ProcessManagerAsync(IManager manager, string token, CancellationToken cancellationToken)
    {
        var installedPackages = await manager.GetInstalledPackagesAsync();
        _logger.LogInformation($"{manager.GetType().Name} found {installedPackages.Length} installed packages.");
        await _hubConnection.InvokeAsync("Packages", installedPackages, token, cancellationToken);

        var packagesWithUpdates = await manager.GetPackagesWithAvailableUpdatesAsync();
        _logger.LogInformation($"{manager.GetType().Name} found {packagesWithUpdates.Length} packages with updates.");

        if (packagesWithUpdates.Length > 0)
        {
            _logger.LogInformation($"{manager.GetType().Name} packages with updates:");
            foreach (var pkg in packagesWithUpdates)
            {
                _logger.LogInformation(
                    $"- {pkg.Name}: Current Version: {pkg.Version ?? "unknown"}, Available Version: {pkg.AvailableVersion ?? "unknown"}");
            }

            await _hubConnection.InvokeAsync("PackageUpdates", packagesWithUpdates, token, cancellationToken);
        }
        else
        {
            _logger.LogInformation($"{manager.GetType().Name} has no packages with available updates.");
        }
    }

    private async Task<string> GetJwtTokenAsync()
    {
        var response = await _httpClient.GetAsync("http://localhost:5275/token");
        response.EnsureSuccessStatusCode();
        var tokenResponse =
            await JsonSerializer.DeserializeAsync<TokenResult>(await response.Content.ReadAsStreamAsync());
        return tokenResponse?.Token ?? string.Empty;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await _hubConnection.StopAsync(stoppingToken);
        await _hubConnection.DisposeAsync();
        await base.StopAsync(stoppingToken);
    }
}