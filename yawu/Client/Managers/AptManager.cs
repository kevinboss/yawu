using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Client.Managers;

public partial class AptGetManager : IManager
{
    public async Task<bool> IsManagerAvailableAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("which apt-get");

            return !string.IsNullOrWhiteSpace(result);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<Package[]> GetInstalledPackagesAsync()
    {
        var result = await ExecuteAptGetCommand("list --installed");
        return ParsePackages(result, false).ToArray<Package>();
    }

    public async Task<PackageUpdate[]> GetPackagesWithAvailableUpdatesAsync()
    {
        var result = await ExecuteAptGetCommand("list --upgradable");
        return ParsePackages(result, true).ToArray();
    }

    private static async Task<string> ExecuteAptGetCommand(string arguments)
    {
        return await ExecuteCommandAsync($"sudo apt-get {arguments} -qq");
    }

    private static async Task<string> ExecuteCommandAsync(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {error}");
        }

        return output;
    }

    private static IEnumerable<PackageUpdate> ParsePackages(string aptListOutput, bool includeAvailableVersion)
    {
        var lines = aptListOutput.Split('\n').Skip(1);
        var regex = PackageInfoRegex();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = regex.Match(line);
            if (!match.Success) continue;
            var name = match.Groups[1].Value;
            var version = match.Groups[2].Value;
            var availableVersion = includeAvailableVersion ? match.Groups[3].Value : null;

            yield return new PackageUpdate
            {
                Name = name,
                Version = version,
                AvailableVersion = availableVersion
            };
        }
    }

    [GeneratedRegex(@"^([^/]+)/[^ ]+ ([^ ]+) \[.*\] (.+)?$")]
    private static partial Regex PackageInfoRegex();
}