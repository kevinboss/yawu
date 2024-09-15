namespace Client.Managers;

public interface IManager
{
    Task<bool> IsManagerAvailableAsync();
    Task<Package[]> GetInstalledPackagesAsync();
    Task<PackageUpdate[]> GetPackagesWithAvailableUpdatesAsync();
}

public class Package
{
    public required string Name { get; init; }
    public required string Version { get; init; }
}

public class PackageUpdate : Package
{
    public required string? AvailableVersion { get; init; }
}