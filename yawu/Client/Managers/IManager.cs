using Shared;

namespace Client.Managers;

public interface IManager
{
    Task<bool> IsManagerAvailableAsync();
    Task<Package[]> GetInstalledPackagesAsync();
    Task<PackageUpdate[]> GetPackagesWithAvailableUpdatesAsync();
}