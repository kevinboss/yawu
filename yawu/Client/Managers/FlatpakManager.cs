using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Client.Managers;

public partial class FlatpakManager : IManager
{
    public async Task<bool> IsManagerAvailableAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("which flatpak");
            return !string.IsNullOrWhiteSpace(result);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<Package[]> GetInstalledPackagesAsync()
    {
        var result = await ExecuteCommandAsync("flatpak list --app");
        return ParsePackages(result, false).ToArray();
    }

    public async Task<Package[]> GetPackagesWithAvailableUpdatesAsync()
    {
        var result = await ExecuteFlatpakUpdateAsync("flatpak update --app");
        return ParsePackagesForUpdates(result).ToArray();
    }

    private static async Task<string> ExecuteFlatpakUpdateAsync(string command)
    {
        return await ExecuteCommandAsync(command, true);
    }

    private static async Task<string> ExecuteCommandAsync(string command, bool cancelPrompt = false)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = cancelPrompt,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        // If we need to simulate cancellation, send "n" to cancel the prompt
        if (cancelPrompt)
        {
            await process.StandardInput.WriteLineAsync("n");
            await process.StandardInput.FlushAsync();
        }

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"Command failed: {error}");
        }

        return output;
    }

    private static IEnumerable<Package> ParsePackages(string flatpakListOutput, bool includeAvailableVersion)
    {
        var lines = flatpakListOutput.Split('\n');
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

    private static IEnumerable<Package> ParsePackagesForUpdates(string flatpakUpdateOutput)
    {
        var lines = flatpakUpdateOutput.Split('\n');
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("ID") || line.Contains("Proceed") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts is [_, _, _, "u", _, ..]) // 'u' indicates an update
            {
                var id = parts[1];
                var branch = parts[2];

                yield return new Package
                {
                    Name = id,
                    Version = branch,
                    AvailableVersion = null
                };
            }
        }
    }


    [GeneratedRegex(@"^([^ ]+)\s+([^ ]+)\s+(.+)?$")]
    private static partial Regex PackageInfoRegex();
}