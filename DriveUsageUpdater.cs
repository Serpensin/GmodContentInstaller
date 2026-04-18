#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using Avalonia.Controls;
using Serilog;

namespace GModContentWizard
{
    /// <summary>
    /// Handles updating the drive usage progress bar and calculating available disk space.
    /// Supports both Windows (DriveInfo) and Linux (df command) systems.
    /// </summary>
    internal class DriveUsageUpdater
    {
        private string? driveLetter;
        private long cumulativeChange = 0;
        private readonly ProgressBar progressBar;

        public DriveUsageUpdater(ProgressBar progressBar)
        {
            this.progressBar = progressBar;
        }

        public DriveUsageUpdater(string? driveLetter, ProgressBar progressBar)
        {
            this.driveLetter = driveLetter;
            this.progressBar = progressBar;
        }

        /// <summary>
        /// Sets the drive letter/path for which to calculate usage.
        /// </summary>
        /// <param name="letter">The drive letter (Windows) or path (Linux).</param>
        public void SetDriveLetter(string? letter)
        {
            cumulativeChange = 0;
            driveLetter = letter;
            Log.Information("DriveLetter set to: {Drive}", driveLetter);
        }

        /// <summary>
        /// Updates the drive usage progress bar with cumulative change applied.
        /// </summary>
        /// <param name="inputChange">The cumulative size change from all toggles.</param>
        public void UpdateDriveSizeBar(long inputChange = 0)
        {
            cumulativeChange = inputChange;

            if (string.IsNullOrEmpty(driveLetter))
            {
                progressBar.Value = 0;
                return;
            }

            try
            {
                double totalSpace;
                double availableSpace;

                if (OperatingSystem.IsWindows())
                {
                    var driveInfo = new DriveInfo(driveLetter);
                    if (!driveInfo.IsReady)
                    {
                        Log.Error("Drive {Drive} not ready", driveLetter);
                        progressBar.Value = 0;
                        return;
                    }
                    totalSpace = driveInfo.TotalSize;
                    availableSpace = driveInfo.AvailableFreeSpace;
                }
                else
                {
                    var mountPoint = GetLinuxMountPoint(driveLetter);
                    var dfResult = GetLinuxDiskUsage(mountPoint);
                    if (dfResult == null)
                    {
                        Log.Error("Failed to get disk usage for {Drive}", driveLetter);
                        progressBar.Value = 0;
                        return;
                    }
                    totalSpace = dfResult.Value.total;
                    availableSpace = dfResult.Value.available;
                }

                double usedSpace = totalSpace - availableSpace;
                double adjustedUsedSpace = Math.Max(0, usedSpace + cumulativeChange);

                int usagePercent = (int)((adjustedUsedSpace / totalSpace) * 100);
                usagePercent = Math.Min(Math.Max(0, usagePercent), 100);

                progressBar.Value = usagePercent;

                var tooltipText = $"Total: {FormatSize(totalSpace)}\nUsed: {FormatSize(adjustedUsedSpace)}\nFree: {FormatSize(availableSpace)}";
                ToolTip.SetTip(progressBar, tooltipText);

                Log.Information("Drive {Drive}: {Percent}% used, {Free} free", 
                    driveLetter, usagePercent, FormatSize(availableSpace));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating drive size bar");
                progressBar.Value = 0;
            }
        }

        /// <summary>
        /// Formats a byte count into a human-readable string.
        /// </summary>
        /// <param name="size">The size in bytes.</param>
        /// <returns>A formatted string representation.</returns>
        public static string FormatSize(double size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Gets the mount point for a given path.
        /// </summary>
        /// <param name="path">The path to find the mount point for.</param>
        /// <returns>The mount point path.</returns>
        private string GetLinuxMountPoint(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";

            if (Directory.Exists(path))
                return path;

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && parent != path)
                return GetLinuxMountPoint(parent);

            return "/";
        }

        /// <summary>
        /// Gets the disk usage for a Linux mount point using the df command.
        /// </summary>
        /// <param name="mountPoint">The mount point to query.</param>
        /// <returns>A tuple of (total, available) bytes, or null if parsing fails.</returns>
        private (double total, double available)? GetLinuxDiskUsage(string mountPoint)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "df",
                        Arguments = $"-B1 \"{mountPoint}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                    return null;

                var data = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (data.Length >= 4)
                {
                    if (double.TryParse(data[1], out double total) &&
                        double.TryParse(data[3], out double available))
                    {
                        return (total, available);
                    }
                }

                Log.Debug("df output parsing failed: {Output}", output);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running df command");
                return null;
            }
        }
    }
}