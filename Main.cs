using System.Diagnostics;
using Resources = GModContentWizard.Properties.Resources;

namespace GModContentWizard
{
    public partial class Main : Form
    {
        private DriveUsageUpdater driveUsageUpdater;
        private Dictionary<string, ContentInfo> contentInfoDictionary;
        private readonly UrlDictionary urlDictionary = new(Resources.urls);
        private readonly ProgressBarTextAnimator progressBarTextAnimator;
        private readonly Downloader downloader;
        private readonly ToggleSwitchStateManager toggleSwitchManager = new();


        public Main()
        {
            InitializeComponent();
            InitializeContentInfo();
            progressBarTextAnimator = new(progressBar);
            downloader = new(progressBar);
        }

        private void InitializeContentInfo()
        {
            contentInfoDictionary = new Dictionary<string, ContentInfo>
            {
                { "CSSContent", urlDictionary.GetContentInfoByName("CSS Content") },
                { "CSSMaps", urlDictionary.GetContentInfoByName("CSS Maps") },
                { "DODContent", urlDictionary.GetContentInfoByName("DOD Content") },
                { "DODMaps", urlDictionary.GetContentInfoByName("DOD Maps") },
                { "HL1Content", urlDictionary.GetContentInfoByName("HL1 Content") },
                { "HL1Maps", urlDictionary.GetContentInfoByName("HL1 Maps") },
                { "HL2Ep1Content", urlDictionary.GetContentInfoByName("HL2 Ep1 Content") },
                { "HL2Ep1Maps", urlDictionary.GetContentInfoByName("HL2 Ep1 Maps") },
                { "HL2Ep2Content", urlDictionary.GetContentInfoByName("HL2 Ep2 Content") },
                { "HL2Ep2Maps", urlDictionary.GetContentInfoByName("HL2 Ep2 Maps") },
                { "HL2ExtrasContent", urlDictionary.GetContentInfoByName("HL2 Extras Content") },
                { "HL2ExtrasMaps", urlDictionary.GetContentInfoByName("HL2 Extras Maps") },
                { "L4DContent", urlDictionary.GetContentInfoByName("L4D Content") },
                { "L4DMaps", urlDictionary.GetContentInfoByName("L4D Maps") },
                { "L4D2Content", urlDictionary.GetContentInfoByName("L4D2 Content") },
                { "Portal2Content", urlDictionary.GetContentInfoByName("Portal 2 Content") },
                { "Portal2Maps", urlDictionary.GetContentInfoByName("Portal 2 Maps") },
                { "PortalContent", urlDictionary.GetContentInfoByName("Portal Content") },
                { "PortalMaps", urlDictionary.GetContentInfoByName("Portal Maps") },
                { "TF2Content", urlDictionary.GetContentInfoByName("TF2 Content") },
                { "TF2Maps", urlDictionary.GetContentInfoByName("TF2 Maps") }
            };
        }
        private Guna2HtmlLabel GetAssociatedLabel(Guna2ToggleSwitch toggleSwitch)
        {
            string labelName = toggleSwitch.Name.Replace("Button", "Label");

            var labelControl = this.Controls.Find(labelName, true).FirstOrDefault();

            return labelControl as Guna2HtmlLabel;
        }

        private static async Task<bool> CanBeEnabledAsync(ContentInfo contentInfo)
        {
            bool isReachable = await UrlChecker.IsUrlReachableAsync(contentInfo.Url);
            return isReachable;
        }

        private async Task LoadInfo(string addonsPath)
        {
#nullable enable
            async Task UpdateContentAndMapsAsync(ContentInfo content, Guna2HtmlLabel? contentLabel, Guna2ToggleSwitch? contentButton, Guna2HtmlLabel? mapLabel, Guna2ToggleSwitch? mapButton)
            {
                bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
                bool canEnable = await CanBeEnabledAsync(content);

                if (contentLabel != null && contentButton != null)
                {
                    if (canEnable)
                    {
                        contentLabel.Text = $"Content ({DriveUsageUpdater.FormatSize(content.DownloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        contentButton.Enabled = true;
                    }
                    else
                    {
                        contentLabel.ForeColor = Color.Red;
                        contentButton.Checked = false;
                        contentButton.Enabled = false;
                    }
                    if (isInstalled)
                    {
                        contentLabel.ForeColor = Color.Green;
                        contentLabel.Text = $"Content ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        contentButton.Checked = true;
                        contentButton.Enabled = true;
                    }
                }

                if (mapLabel != null && mapButton != null)
                {
                    if (canEnable)
                    {
                        mapLabel.Text = $"Maps ({DriveUsageUpdater.FormatSize(content.DownloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        mapButton.Enabled = true;
                    }
                    else
                    {
                        mapLabel.ForeColor = Color.Red;
                        mapButton.Checked = false;
                        mapButton.Enabled = false;
                    }
                    if (isInstalled)
                    {
                        mapLabel.ForeColor = Color.Green;
                        mapLabel.Text = $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        mapButton.Enabled = true;
                        mapButton.Checked = true;
                    }
                }
            }
#nullable disable
            var tasks = new List<Task>
            {
                UpdateContentAndMapsAsync(contentInfoDictionary["CSSContent"], CSSLabelContent, CSSButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["CSSMaps"], null, null, CSSLabelMaps, CSSButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["DODContent"], DODLabelContent, DODButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["DODMaps"], null, null, DODLabelMaps, DODButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL1Content"], HL1LabelContent, HL1ButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL1Maps"], null, null, HL1LabelMaps, HL1ButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep1Content"], HL2Ep1LabelContent, HL2Ep1ButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep1Maps"], null, null, HL2Ep1LabelMaps, HL2Ep1ButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep2Content"], HL2Ep2LabelContent, HL2Ep2ButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep2Maps"], null, null, HL2Ep2LabelMaps, HL2Ep2ButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2ExtrasContent"], HL2ExtrasLabelContent, HL2ExtrasButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2ExtrasMaps"], null, null, HL2ExtrasLabelMaps, HL2ExtrasButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["PortalContent"], PortalLabelContent, PortalButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["PortalMaps"], null, null, PortalLabelMaps, PortalButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["Portal2Content"], Portal2LabelContent, Portal2ButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["Portal2Maps"], null, null, Portal2LabelMaps, Portal2ButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["TF2Content"], TF2LabelContent, TF2ButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["TF2Maps"], null, null, TF2LabelMaps, TF2ButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4DContent"], L4DLabelContent, L4DButtonContent, null, null),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4DMaps"], null, null, L4DLabelMaps, L4DButtonMaps),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4D2Content"], L4D2LabelContent, L4D2ButtonContent, null, null),
            };
            progressBarTextAnimator.StartAnimation("Loading");
            await Task.WhenAll(tasks);
            progressBarTextAnimator.StopAnimation();
        }

        private async void PathDetectButton_Click(object sender, EventArgs e)
        {
            pathDetectButton.Enabled = false;
            string addonsPath = PathDetector.Select();
            if (string.IsNullOrEmpty(addonsPath))
            {
                pathDetectButton.Enabled = true;
                return;
            }
            driveUsageUpdater = new(Path.GetPathRoot(addonsPath)?.Substring(0, 2), drivespaceUsageBar);
            driveUsageUpdater.UpdateDriveSizeBar();
            pathShowLabel.Text = addonsPath;
            await LoadInfo(addonsPath);
            downloadButton.Enabled = true;
        }

        private void Logo_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://url.serpensin.com/serpentmodding",
                    UseShellExecute = true
                });
            }
        }

        private void ContentToggleSwitch_Click(object sender, EventArgs e)
        {
            var toggleSwitch = (Guna2ToggleSwitch)sender;
            var associatedLabel = GetAssociatedLabel(toggleSwitch);

            if (associatedLabel == null)
            {
                MessageBox.Show($"Label for {toggleSwitch.Name} not found!");
                SentrySdk.CaptureMessage($"Label for {toggleSwitch.Name} not found!");
                return;
            }

            string switchName = toggleSwitch.Name.Replace("Button", "");
            if (!contentInfoDictionary.TryGetValue(switchName, out ContentInfo content))
            {
                MessageBox.Show($"Content for {switchName} not found in dictionary!");
                SentrySdk.CaptureMessage($"Content for {switchName} not found in dictionary!");
                return;
            }

            long installSize = content.InstallSize;
            driveUsageUpdater.UpdateDriveSizeBar(toggleSwitch.Checked ? installSize : -installSize);
        }

        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            downloadButton.Enabled = false;
            toggleSwitchManager.SaveToggleSwitchStates(this);
            ToggleSwitchStateManager.DisableAllToggleSwitches(this);

            var toggleSwitches = Controls.OfType<Guna2ToggleSwitch>().ToList();
            foreach (var button in toggleSwitches)
            {
                string contentName = button.Name.Replace("Button", "");
                if (!contentInfoDictionary.TryGetValue(contentName, out ContentInfo content))
                {
                    continue;
                }

                var label = GetAssociatedLabel(button);
                if (button.Checked && label != null && label.ForeColor != Color.Green)
                {
                    var file = Path.Combine(pathShowLabel.Text, content.InternalName + ".tar.gz");
                    progressBarTextAnimator.StartAnimation("Downloading");
                    await downloader.DownloadFileAsync(content.Url, content.InternalName + ".tar.gz", pathShowLabel.Text);

                    progressBarTextAnimator.StartAnimation("Extracting");
                    await Task.Run(() => ArchiveExtractor.ExtractArchiveAsync(file, pathShowLabel.Text, progressBar));
                    await Task.Run(() => File.Delete(file));

                    label.ForeColor = Color.Green;
                    label.Text = label.Text.StartsWith("Content") ? $"Content ({DriveUsageUpdater.FormatSize(content.DownloadSize)})" : $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                }
                else if (!button.Checked && label != null && label.ForeColor == Color.Green)
                {
                    progressBarTextAnimator.StartAnimation("Deleting");
                    string contentPath = Path.Combine(pathShowLabel.Text, content.InternalName);
                    if (Directory.Exists(contentPath))
                    {
                        await Task.Run(() => Directory.Delete(contentPath, recursive: true));
                        string contentSizeInfo = $"{DriveUsageUpdater.FormatSize(content.DownloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)}";
                        bool canEnable = await CanBeEnabledAsync(content);
                        if (canEnable)
                        {
                            label.ForeColor = Color.White;
                            label.Text = label.Text.StartsWith("Content") ? $"Content ({contentSizeInfo})" : $"Maps ({contentSizeInfo})";
                        }
                        else
                        {
                            label.Text = label.Text.StartsWith("Content") ? $"Content ({contentSizeInfo})" : $"Maps ({contentSizeInfo})";
                            label.ForeColor = Color.Red;
                            button.Checked = false;
                            button.Enabled = false;
                        }
                    }
                }
            }

            toggleSwitchManager.RestoreToggleSwitchStates();
            progressBarTextAnimator.StopAnimation();
            downloadButton.Enabled = true;
        }
    }
}
