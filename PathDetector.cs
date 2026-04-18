#nullable enable
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Serilog;

namespace GModContentWizard
{
    /// <summary>
    /// Detects and locates the Garry's Mod addons directory on Windows and Linux systems.
    /// </summary>
    internal static class PathDetector
    {
        private static readonly string[] SearchPaths =
        [
            @"SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons",
            @"Program Files (x86)\Steam\steamapps\common\GarrysMod\garrysmod\addons"
        ];

        /// <summary>
        /// Attempts to automatically detect the Garry's Mod addons directory.
        /// </summary>
        /// <returns>The path to the addons directory if found, otherwise null.</returns>
        public static string? Select()
        {
            Log.Information("PathDetector.Select() called");

            if (OperatingSystem.IsWindows())
            {
                return SelectWindows();
            }
            else
            {
                return SelectLinux();
            }
        }

        /// <summary>
        /// Searches for Garry's Mod addons directory on Windows systems.
        /// </summary>
        /// <returns>The addons path if found, otherwise null.</returns>
        private static string? SelectWindows()
        {
            Log.Information("Searching for Garry's Mod on Windows");

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable)
                    continue;

                foreach (var path in SearchPaths)
                {
                    var testPath = Path.Combine(drive.Name, path);
                    Log.Debug("Checking: {Path}", testPath);
                    if (Directory.Exists(testPath))
                    {
                        Log.Information("Found addons at: {Path}", testPath);
                        return testPath;
                    }
                }
            }

            Log.Warning("No addons directory found automatically on Windows");
            return null;
        }

        /// <summary>
        /// Searches for Garry's Mod addons directory on Linux systems using find.
        /// </summary>
        /// <returns>The addons path if found, otherwise null.</returns>
        private static string? SelectLinux()
        {
            Log.Information("Searching for Garry's Mod on Linux");

            var searchReturn = SearchLinux();
            if (searchReturn != null)
            {
                Log.Information("Addons path found automatically: {Path}", searchReturn);
                return searchReturn;
            }

            return null;
        }

        /// <summary>
        /// Uses the find command to locate hl2_linux binary and derive the addons path.
        /// </summary>
        /// <returns>The addons path if found, otherwise null.</returns>
        private static string? SearchLinux()
        {
            try
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(home))
                {
                    Log.Warning("HOME environment variable not set");
                    return null;
                }

                Log.Information("Searching in HOME: {Home}", home);

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "find",
                        Arguments = $"{home} /var/run /mnt -type f -name \"hl2_linux\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Log.Debug("find output: {Output}", output);
                if (!string.IsNullOrEmpty(error))
                    Log.Debug("find error: {Error}", error);

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedPath = line.Trim();
                    if (string.IsNullOrEmpty(trimmedPath)) continue;

                    Log.Debug("Found hl2_linux at: {Path}", trimmedPath);

                    if (IsGarrysModHL2(trimmedPath))
                    {
                        var gmodRoot = System.IO.Path.GetDirectoryName(trimmedPath);
                        if (gmodRoot != null)
                        {
                            var addonsPath = System.IO.Path.Combine(gmodRoot, "garrysmod", "addons");
                            Log.Information("Found GMod addons at: {Path}", addonsPath);
                            return addonsPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching for GMod on Linux");
            }

            Log.Warning("No addons directory found on Linux");
            return null;
        }

        /// <summary>
        /// Verifies if the given path contains the Garry's Mod hl2 binary.
        /// </summary>
        /// <param name="hl2Path">Path to the hl2_linux binary.</param>
        /// <returns>True if it's Garry's Mod, otherwise false.</returns>
        private static bool IsGarrysModHL2(string hl2Path)
        {
            try
            {
                var gmodRoot = System.IO.Path.GetDirectoryName(hl2Path);
                if (gmodRoot == null) return false;

                return IsValidGModPath(gmodRoot);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates that a path is a valid Garry's Mod installation by checking for garrysmod folder and steam_appid.txt.
        /// </summary>
        /// <param name="rootPath">The potential GMod root directory.</param>
        /// <returns>True if valid, otherwise false.</returns>
        private static bool IsValidGModPath(string rootPath)
        {
            try
            {
                var garrysmodPath = System.IO.Path.Combine(rootPath, "garrysmod");
                Log.Debug("Checking if {Path} exists", garrysmodPath);
                if (!System.IO.Directory.Exists(garrysmodPath))
                    return false;

                var appIdPath = System.IO.Path.Combine(rootPath, "steam_appid.txt");
                if (System.IO.File.Exists(appIdPath))
                {
                    var appId = System.IO.File.ReadAllText(appIdPath).Trim();
                    Log.Debug("steam_appid.txt contains: {AppId}", appId);
                    if (appId == "4000")
                    {
                        Log.Information("Verified GMod via steam_appid.txt (4000)");
                        return true;
                    }
                }

                return System.IO.Directory.Exists(System.IO.Path.Combine(garrysmodPath, "gamemodes"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Opens a folder picker dialog to manually select the Garry's Mod addons directory.
        /// </summary>
        /// <param name="window">The parent window for the dialog.</param>
        /// <returns>The selected addons path, or null if cancelled/invalid.</returns>
        public static async Task<string?> SelectWithDialog(Window window)
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return null;

            var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Garry's Mod addons folder",
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                var path = files[0].Path.LocalPath;
                var addonsPath = System.IO.Path.Combine(path, "garrysmod", "addons");
                if (System.IO.Directory.Exists(addonsPath))
                    return addonsPath;
                
                if (System.IO.Directory.Exists(path) && System.IO.Path.GetFileName(path) == "addons")
                    return path;
            }

            return null;
        }
    }
}