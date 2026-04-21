using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace GModContentWizard
{
    /// <summary>
    /// Handles checking for application updates by querying the GitHub releases API.
    /// </summary>
    public class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Serpensin/GmodContentInstaller/releases/latest";
        private const string GitHubReleasesUrl = "https://github.com/Serpensin/GmodContentInstaller/releases";

        /// <summary>
        /// Gets the URL to the latest release page on GitHub.
        /// </summary>
        public static string LatestReleaseUrl { get; private set; } = "";

        /// <summary>
        /// Checks for available updates by comparing the current version with the latest GitHub release.
        /// </summary>
        /// <returns>A tuple containing whether an update is available and the latest version string.</returns>
        public static async Task<(bool HasUpdate, string? LatestVersion)> CheckForUpdateAsync()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var currentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "GModContentInstaller");

                var response = await client.GetStringAsync(GitHubApiUrl);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var releaseName = root.GetProperty("name").GetString() ?? "";
                var latestVersion = releaseName.TrimStart('v');

                var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
                LatestReleaseUrl = htmlUrl;

                var hasUpdate = CompareVersions(latestVersion, currentVersion) > 0;

                Log.Information("Update check: current={Current}, latest={Latest}, hasUpdate={HasUpdate}",
                    currentVersion, latestVersion, hasUpdate);

                return (hasUpdate, latestVersion);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to check for updates");
                return (false, null);
            }
        }

        /// <summary>
        /// Compares two version strings.
        /// </summary>
        /// <param name="v1">First version string.</param>
        /// <param name="v2">Second version string.</param>
        /// <returns>1 if v1 > v2, -1 if v1 < v2, 0 if equal.</returns>
        private static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
            {
                int p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                int p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

                if (p1 > p2) return 1;
                if (p1 < p2) return -1;
            }
            return 0;
        }

        /// <summary>
        /// Gets the URL to the GitHub releases page.
        /// </summary>
        /// <returns>The releases URL.</returns>
        public static string GetReleasesUrl() => GitHubReleasesUrl;
    }
}
