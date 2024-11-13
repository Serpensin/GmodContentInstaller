namespace GModContentWizard
{
    /// <summary>
    /// Updates the drive usage progress bar and tooltip based on the drive's current usage.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DriveUsageUpdater"/> class.
    /// </remarks>
    /// <param name="driveLetter">The letter of the drive to monitor.</param>
    /// <param name="progressBar">The progress bar to update with drive usage.</param>
    internal class DriveUsageUpdater(string driveLetter, Guna2ProgressBar progressBar)
    {
        private readonly string driveLetter = driveLetter;
        private long cumulativeChange = 0;
        private readonly Guna2ProgressBar drivespaceProgressBar = progressBar;
        private readonly Guna2HtmlToolTip drivespaceProgressBarToolTip = new();

        /// <summary>
        /// Updates the drive size bar and tooltip with the current drive usage.
        /// </summary>
        /// <param name="inputChange">The change in used space to account for.</param>
        /// <exception cref="InvalidOperationException">Thrown when the drive is not ready.</exception>
        public void UpdateDriveSizeBar(long inputChange = 0)
        {
            cumulativeChange += inputChange;

            var driveInfo = new DriveInfo(driveLetter);

            if (!driveInfo.IsReady)
            {
                throw new InvalidOperationException("Drive not ready.");
            }

            double totalSpace = driveInfo.TotalSize;
            double usedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;

            double adjustedUsedSpace = usedSpace + cumulativeChange;

            adjustedUsedSpace = Math.Max(0, adjustedUsedSpace);

            int usagePercent = (int)((adjustedUsedSpace / totalSpace) * 100);

            usagePercent = Math.Min(usagePercent, 100);

            drivespaceProgressBar.Value = usagePercent;
            drivespaceProgressBar.Text = $"{driveLetter}\\ {usagePercent}%";

            // Change ToolTip from drivespaceProgressBar
            drivespaceProgressBarToolTip.IsBalloon = true;
            drivespaceProgressBarToolTip.SetToolTip(drivespaceProgressBar, $"Total: {FormatSize(totalSpace)}\nUsed: {FormatSize(adjustedUsedSpace)}\nFree: {FormatSize(totalSpace - adjustedUsedSpace)}");
        }

        /// <summary>
        /// Formats the size in bytes to a more readable string with appropriate units.
        /// </summary>
        /// <param name="size">The size in bytes.</param>
        /// <returns>A formatted string representing the size in appropriate units.</returns>
        public static string FormatSize(double size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
