using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Client.Managers;

public partial class AptGetManager : IManager
{
    public Package[] GetInstalledPackages()
    {
        var result = ExecuteAptGetCommand("list --installed").Result;
        return ParsePackages(result, false).ToArray();
    }

    public Package[] GetPackagesWithAvailableUpdates()
    {
        var result = ExecuteAptGetCommand("list --upgradable").Result;
        return ParsePackages(result, true).ToArray();
    }

    private static async Task<string> ExecuteAptGetCommand(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"apt-get {arguments} -qq", // Using -qq for quiet output
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
            throw new InvalidOperationException($"Apt-get command failed: {error}");
        }

        return output;
    }

    private static IEnumerable<Package> ParsePackages(string aptListOutput, bool includeAvailableVersion)
    {
        var lines = aptListOutput.Split('\n').Skip(1); // Skip the first line which is a header
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

            yield return new Package
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