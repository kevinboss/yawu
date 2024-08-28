namespace Client.Managers;

public interface IManager
{
    Package[] GetInstalledPackages();
    Package[] GetPackagesWithAvailableUpdates();
}

public class Package
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string? AvailableVersion { get; init; }
}