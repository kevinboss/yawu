using System.Diagnostics;

namespace Client.Managers
{
    public class FlatpakManager : IManager
    {
        public async Task<bool> IsManagerAvailableAsync()
        {
            try
            {
                var result = await ExecuteCommandAsync("which flatpak");
                return !string.IsNullOrWhiteSpace(result);
            }
            catch
            {
                return false;
            }
        }

        public async Task<Package[]> GetInstalledPackagesAsync()
        {
            var result = await ExecuteCommandAsync("flatpak list --app --columns=application,version");
            return ParseInstalledPackages(result).ToArray();
        }

        public async Task<PackageUpdate[]> GetPackagesWithAvailableUpdatesAsync()
        {
            var updatesResult =
                await ExecuteCommandAsync("flatpak remote-ls --updates --app --columns=application,version");
            var updatesPackages = ParseAvailableUpdates(updatesResult);

            return updatesPackages.Select(update => new PackageUpdate
                { Name = update.Name, Version = update.Version, AvailableVersion = null }).ToArray();
        }

        private static async Task<string> ExecuteCommandAsync(string command)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
            {
                throw new InvalidOperationException($"Command failed: {error}");
            }

            return output;
        }

        private static IEnumerable<Package> ParseInstalledPackages(string flatpakListOutput)
        {
            var lines = flatpakListOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                var name = parts[0].Trim();
                var version = parts[1].Trim();

                yield return new Package
                {
                    Name = name,
                    Version = version
                };
            }
        }

        private static IEnumerable<(string Name, string Version)> ParseAvailableUpdates(
            string flatpakUpdateOutput)
        {
            var lines = flatpakUpdateOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                var name = parts[0].Trim();
                var availableVersion = parts[1].Trim();

                yield return new ValueTuple<string, string>(name, availableVersion);
            }
        }
    }
}