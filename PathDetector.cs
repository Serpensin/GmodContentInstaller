using SerpentModding;

namespace GModContentWizard
{
    internal static class PathDetector
    {
        private const string InitialDirectory = "c:\\";
        private const string DialogTitle = "Please select hl2.exe from GarrysMod";
        private const string DialogFilter = "GMod Application|hl2.exe";
        private const string RetryMessage = "You haven't selected anything.\nDo you want to retry?";
        private const string RetryTitle = "Path Selector";
        private const string WrongFileMessage = "Somehow you managed to select the wrong file.\nNow please select the correct one. XD";
        private const string AddonsSubPath = "garrysmod";
        private const string AddonsFolder = "addons";
        private static readonly List<string> Paths =
        [
            "SteamLibrary\\steamapps\\common\\GarrysMod\\garrysmod\\addons",
            "Program Files (x86)\\Steam\\steamapps\\common\\GarrysMod\\garrysmod\\addons"
        ];

        /// <summary>
        /// Prompts the user to select the hl2.exe file from the Garry's Mod installation if the search fails.
        /// </summary>
        /// <returns>The path to the Garry's Mod addons directory if found or selected; otherwise, null.</returns>
        public static string Select()
        {
            Logger.Instance.Trace("Select() called");
            var searchReturn = Search();
            if (searchReturn != null)
            {
                Logger.Instance.Info($"Addons path found automatically: {searchReturn}");
                return searchReturn;
            }

            while (true)
            {
                string filePath = ShowFileDialog();
                if (string.IsNullOrEmpty(filePath))
                {
                    Logger.Instance.Warn("No file selected by user");
                    if (!AskUserToRetry())
                    {
                        Logger.Instance.Info("User chose not to retry selecting path");
                        return null;
                    }
                    continue;
                }

                if (IsValidAddonsPath(filePath, out var addonsPath))
                {
                    Logger.Instance.Info($"User selected valid Garry's Mod addons directory: {addonsPath}");
                    return addonsPath;
                }
                else
                {
                    Logger.Instance.Error($"User selected wrong file: {filePath}");
                    ShowWrongFileMessage();
                }
            }
        }

        /// <summary>
        /// Dynamically gets all available drive root paths (e.g. C:\, D:\, ...).
        /// </summary>
        private static IEnumerable<string> GetAvailableDrives()
        {
            Logger.Instance.Trace("GetAvailableDrives() called");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable)
                {
                    Logger.Instance.Debug($"Available drive: {drive.Name}");
                    yield return drive.Name;
                }
            }
        }

        /// <summary>
        /// Searches for the Garry's Mod addons directory in the predefined drives and paths.
        /// </summary>
        /// <returns>The path to the Garry's Mod addons directory if found; otherwise, null.</returns>
        private static string Search()
        {
            Logger.Instance.Trace("Search() called");
            foreach (var drive in GetAvailableDrives())
            {
                foreach (var path in Paths)
                {
                    var test = Path.Combine(drive, path);
                    Logger.Instance.Debug($"Checking path: {test}");
                    if (Directory.Exists(test))
                    {
                        Logger.Instance.Info($"Found Garry's Mod addons directory: {test}");
                        return test;
                    }
                }
            }
            Logger.Instance.Warn("No Garry's Mod addons directory found in predefined paths");
            return null;
        }

        /// <summary>
        /// Shows the OpenFileDialog for selecting hl2.exe.
        /// </summary>
        /// <returns>The selected file path or empty string if cancelled.</returns>
        private static string ShowFileDialog()
        {
            Logger.Instance.Debug("Prompting user to select hl2.exe");
            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.InitialDirectory = InitialDirectory;
                openFileDialog.Title = DialogTitle;
                openFileDialog.Filter = DialogFilter;
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Logger.Instance.Info($"User selected file: {openFileDialog.FileName}");
                    return openFileDialog.FileName;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Validates if the selected file path leads to a valid Garry's Mod addons directory.
        /// </summary>
        /// <param name="filePath">The selected file path.</param>
        /// <param name="addonsPath">The resulting addons path if valid.</param>
        /// <returns>True if valid, otherwise false.</returns>
        private static bool IsValidAddonsPath(string filePath, out string addonsPath)
        {
            var selectedPath = Path.GetDirectoryName(filePath) ?? string.Empty;
            addonsPath = Path.Combine(selectedPath, AddonsSubPath, AddonsFolder);
            Logger.Instance.Debug($"Checking user selected path: {addonsPath}");
            return Directory.Exists(addonsPath);
        }

        /// <summary>
        /// Shows a retry MessageBox and returns true if the user wants to retry.
        /// </summary>
        private static bool AskUserToRetry()
        {
            var dialogResult = MessageBox.Show(
                RetryMessage,
                RetryTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Asterisk
            );
            return dialogResult == DialogResult.Yes;
        }

        /// <summary>
        /// Shows a MessageBox for a wrong file selection.
        /// </summary>
        private static void ShowWrongFileMessage()
        {
            MessageBox.Show(
                WrongFileMessage,
                RetryTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Asterisk
            );
        }
    }
}
