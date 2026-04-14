#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Serilog;

namespace GModContentWizard
{
    internal static class PathDetector
    {
        public static string? Select()
        {
            Log.Information("PathDetector.Select() called");
            var searchReturn = Search();
            if (searchReturn != null)
            {
                Log.Information("Addons path found automatically: {Path}", searchReturn);
                return searchReturn;
            }

            return null;
        }

        public static string? Search()
        {
            Log.Information("Searching for Garry's Mod addons directory");

            if (OperatingSystem.IsWindows())
            {
                return SearchWindows();
            }
            else
            {
                return SearchLinux();
            }
        }

        private static string? SearchWindows()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed && drive.DriveType != DriveType.Removable)
                    continue;

                var paths = new[]
                {
                    @"SteamLibrary\steamapps\common\GarrysMod\garrysmod\addons",
                    @"Program Files (x86)\Steam\steamapps\common\GarrysMod\garrysmod\addons"
                };

                foreach (var path in paths)
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

            Log.Warning("No addons directory found on Windows");
            return null;
        }

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

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
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
                        var gmodRoot = Path.GetDirectoryName(trimmedPath);
                        if (gmodRoot != null)
                        {
                            var addonsPath = Path.Combine(gmodRoot, "garrysmod", "addons");
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

        private static bool IsGarrysModHL2(string hl2Path)
        {
            try
            {
                var gmodRoot = Path.GetDirectoryName(hl2Path);
                if (gmodRoot == null) return false;

                var garrysmodPath = Path.Combine(gmodRoot, "garrysmod");
                Log.Debug("Checking if {Path} exists", garrysmodPath);
                return Directory.Exists(garrysmodPath);
            }
            catch
            {
                return false;
            }
        }

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
                var addonsPath = Path.Combine(path, "garrysmod", "addons");
                if (Directory.Exists(addonsPath))
                    return addonsPath;
                
                if (Directory.Exists(path) && Path.GetFileName(path) == "addons")
                    return path;
            }

            return null;
        }
    }
}