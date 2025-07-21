namespace GModContentWizard
{
    internal class PathDetector
    {
        /// <summary>
        /// Dynamically gets all available drive root paths (e.g. C:\, D:\, ...).
        /// </summary>
        static private IEnumerable<string> GetAvailableDrives()
        {
            return DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable).Select(d => d.Name);
        }

        /// <summary>
        /// List of potential paths where Garry's Mod addons might be located.
        /// </summary>
        static private readonly List<string> Paths =
        [
                "SteamLibrary\\steamapps\\common\\GarrysMod\\garrysmod\\addons",
                "Program Files (x86)\\Steam\\steamapps\\common\\GarrysMod\\garrysmod\\addons"
            ];

        /// <summary>
        /// Searches for the Garry's Mod addons directory in the predefined drives and paths.
        /// </summary>
        /// <returns>The path to the Garry's Mod addons directory if found; otherwise, null.</returns>
        static private string Search()
        {
            foreach (var drive in GetAvailableDrives())
            {
                foreach (var path in Paths)
                {
                    var test = Path.Combine(drive, path);
                    if (Directory.Exists(test))
                    {
                        return test;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Prompts the user to select the hl2.exe file from the Garry's Mod installation if the search fails.
        /// </summary>
        /// <returns>The path to the Garry's Mod addons directory if found or selected; otherwise, null.</returns>
        static public string Select()
        {
            var searchReturn = Search();
            if (searchReturn != null)
            {
                return searchReturn;
            }

            while (true)
            {
                string filePath = string.Empty;

                using (OpenFileDialog openFileDialog = new())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Title = "Please select hl2.exe from GarrysMod";
                    openFileDialog.Filter = "GMod Application|hl2.exe";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        filePath = openFileDialog.FileName;
                    }
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    var dialogResult = MessageBox.Show(
                        "You haven't selected anything.\nDo you want to retry?",
                        "Path Selector",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Asterisk
                    );

                    if (dialogResult == DialogResult.No)
                    {
                        return null;
                    }
                    continue;
                }

                var selectedPath = Path.GetDirectoryName(filePath) ?? "";
                var paksPath = Path.Combine(selectedPath, "garrysmod", "addons");

                if (Directory.Exists(paksPath))
                {
                    return paksPath;
                }
                else
                {
                    MessageBox.Show(
                        "Somehow you managed to select the wrong file.\nNow please select the correct one. XD",
                        "Path Selector",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk
                    );
                }
            }
        }
    }
}
