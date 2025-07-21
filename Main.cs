using System.Diagnostics;
using System.Net.NetworkInformation;
using Resources = GModContentWizard.Properties.Resources;

namespace GModContentWizard
{
    public partial class Main : Form
    {
        private DriveUsageUpdater driveUsageUpdater;
        private Dictionary<string, ContentInfo> contentInfoDictionary;
        private readonly ProgressBarTextAnimator progressBarTextAnimator;
        private readonly Downloader downloader;
        private readonly ToggleSwitchStateManager toggleSwitchManager = new();
        private readonly UrlDictionary urlDictionary = new(System.Text.Encoding.UTF8.GetString(Resources.urls));

        private static readonly (string Key, string DisplayName)[] ContentKeys =
        [
            ("CSSContent", "CSS Content"),
            ("CSSMaps", "CSS Maps"),
            ("DODContent", "DOD Content"),
            ("DODMaps", "DOD Maps"),
            ("HL1Content", "HL1 Content"),
            ("HL1Maps", "HL1 Maps"),
            ("HL2Ep1Content", "HL2 Ep1 Content"),
            ("HL2Ep1Maps", "HL2 Ep1 Maps"),
            ("HL2Ep2Content", "HL2 Ep2 Content"),
            ("HL2Ep2Maps", "HL2 Ep2 Maps"),
            ("HL2ExtrasContent", "HL2 Extras Content"),
            ("HL2ExtrasMaps", "HL2 Extras Maps"),
            ("L4DContent", "L4D Content"),
            ("L4DMaps", "L4D Maps"),
            ("L4D2Content", "L4D2 Content"),
            ("Portal2Content", "Portal 2 Content"),
            ("Portal2Maps", "Portal 2 Maps"),
            ("PortalContent", "Portal Content"),
            ("PortalMaps", "Portal Maps"),
            ("TF2Content", "TF2 Content"),
            ("TF2Maps", "TF2 Maps")
        ];

        private enum ServerPreference { Primary, Secondary }
        private ServerPreference preferredServer = ServerPreference.Primary;
        private readonly Dictionary<string, ServerPreference> serverPreferencePerSession = [];

        public Main()
        {
            InitializeComponent();
            InitializeContentInfo();
            progressBarTextAnimator = new(progressBar);
            downloader = new(progressBar);
        }

        private void InitializeContentInfo()
        {
            contentInfoDictionary = ContentKeys
                .Select(pair => (pair.Key, Info: urlDictionary.GetContentInfoByName(pair.DisplayName)))
                .Where(x => x.Info != null)
                .ToDictionary(x => x.Key, x => x.Info!);
        }

        private Guna2HtmlLabel GetAssociatedLabel(Guna2ToggleSwitch toggleSwitch)
        {
            string labelName = toggleSwitch.Name.Replace("Button", "Label");
            var labelControl = this.Controls.Find(labelName, true).FirstOrDefault();
            return labelControl as Guna2HtmlLabel;
        }

        private static async Task<bool> CanBeEnabledAsync(ContentInfo contentInfo, ServerPreference serverPref)
        {
            string url = serverPref == ServerPreference.Primary ? contentInfo.PrimaryUrl : contentInfo.SecondaryUrl;
            return await UrlChecker.IsUrlReachableAsync(url);
        }

        private static (string url, long downloadSize, string format) GetServerInfo(ContentInfo content, ServerPreference pref)
        {
            return pref == ServerPreference.Primary
                ? (content.PrimaryUrl, content.PrimaryDownloadSize, content.PrimaryFormat)
                : (content.SecondaryUrl, content.SecondaryDownloadSize, content.SecondaryFormat);
        }

        private async Task LoadInfo(string addonsPath)
        {
            async Task UpdateContentAndMapsAsync(ContentInfo content, Guna2HtmlLabel contentLabel, Guna2ToggleSwitch contentButton, Guna2HtmlLabel mapLabel, Guna2ToggleSwitch mapButton)
            {
                bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
                var pref = serverPreferencePerSession.TryGetValue(content.InternalName, out var p) ? p : preferredServer;
                bool canEnable = await CanBeEnabledAsync(content, pref);
                var (url, downloadSize, format) = GetServerInfo(content, pref);

                void SetLabelSafe(Guna2HtmlLabel label, Action<Guna2HtmlLabel> update)
                {
                    if (label.InvokeRequired)
                        label.Invoke(update, label);
                    else
                        update(label);
                }
                void SetButtonSafe(Guna2ToggleSwitch button, Action<Guna2ToggleSwitch> update)
                {
                    if (button.InvokeRequired)
                        button.Invoke(update, button);
                    else
                        update(button);
                }

                if (contentLabel != null && contentButton != null)
                {
                    SetLabelSafe(contentLabel, l =>
                    {
                        if (canEnable)
                        {
                            l.Text = $"Content ({DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        }
                        else
                        {
                            l.ForeColor = Color.Red;
                        }
                        if (isInstalled)
                        {
                            l.ForeColor = Color.Green;
                            l.Text = $"Content ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        }
                    });
                    SetButtonSafe(contentButton, b =>
                    {
                        b.Enabled = canEnable;
                        if (!canEnable) b.Checked = false;
                        if (isInstalled) { b.Checked = true; b.Enabled = true; }
                    });
                }

                if (mapLabel != null && mapButton != null)
                {
                    SetLabelSafe(mapLabel, l =>
                    {
                        if (canEnable)
                        {
                            l.Text = $"Maps ({DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        }
                        else
                        {
                            l.ForeColor = Color.Red;
                        }
                        if (isInstalled)
                        {
                            l.ForeColor = Color.Green;
                            l.Text = $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        }
                    });
                    SetButtonSafe(mapButton, b =>
                    {
                        b.Enabled = canEnable;
                        if (!canEnable) b.Checked = false;
                        if (isInstalled) { b.Checked = true; b.Enabled = true; }
                    });
                }
            }

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

            var firstContent = contentInfoDictionary.Values.FirstOrDefault();
            string primaryHost = null;
            string secondaryHost = null;
            if (firstContent != null)
            {
                if (!string.IsNullOrWhiteSpace(firstContent.PrimaryUrl))
                    primaryHost = new Uri(firstContent.PrimaryUrl).Host;
                if (!string.IsNullOrWhiteSpace(firstContent.SecondaryUrl))
                    secondaryHost = new Uri(firstContent.SecondaryUrl).Host;
            }
            long primaryPing = long.MaxValue;
            long secondaryPing = long.MaxValue;
            using (var ping = new Ping())
            {
                if (!string.IsNullOrEmpty(primaryHost))
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(primaryHost, 1500);
                        if (reply.Status == IPStatus.Success)
                            primaryPing = reply.RoundtripTime;
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(secondaryHost))
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(secondaryHost, 1500);
                        if (reply.Status == IPStatus.Success)
                            secondaryPing = reply.RoundtripTime;
                    }
                    catch { }
                }
            }
            if (primaryPing <= secondaryPing)
                preferredServer = ServerPreference.Primary;
            else
                preferredServer = ServerPreference.Secondary;

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
                    FileName = "https://url.serpensin.com/discord",
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
                MessageBox.Show($"Label for {toggleSwitch.Name} not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SentrySdk.CaptureMessage($"Label for {toggleSwitch.Name} not found!");
                return;
            }

            string switchName = toggleSwitch.Name.Replace("Button", "");
            if (!contentInfoDictionary.TryGetValue(switchName, out ContentInfo content))
            {
                MessageBox.Show($"Content for {switchName} not found in dictionary!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SentrySdk.CaptureMessage($"Content for {switchName} not found in dictionary!");
                return;
            }

            long installSize = content.InstallSize;
            try
            {
                driveUsageUpdater.UpdateDriveSizeBar(toggleSwitch.Checked ? installSize : -installSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Drive update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SentrySdk.CaptureException(ex);
            }
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
                var pref = serverPreferencePerSession.TryGetValue(content.InternalName, out var p) ? p : preferredServer;
                var (url, downloadSize, format) = GetServerInfo(content, pref);
                string fileExt = format == "zip" ? ".zip" : ".tar.gz";
                var file = Path.Combine(pathShowLabel.Text, content.InternalName + fileExt);

                if (button.Checked && label != null && label.ForeColor != Color.Green)
                {
                    progressBarTextAnimator.StartAnimation("Downloading");
                    try
                    {
                        bool success = true;
                        try
                        {
                            await downloader.DownloadFileAsync(url, content.InternalName + fileExt, pathShowLabel.Text);
                        }
                        catch
                        {
                            var altPref = pref == ServerPreference.Primary ? ServerPreference.Secondary : ServerPreference.Primary;
                            var (altUrl, altDownloadSize, altFormat) = GetServerInfo(content, altPref);
                            string altFileExt = altFormat == "zip" ? ".zip" : ".tar.gz";
                            var altFile = Path.Combine(pathShowLabel.Text, content.InternalName + altFileExt);
                            try
                            {
                                await downloader.DownloadFileAsync(altUrl, content.InternalName + altFileExt, pathShowLabel.Text);
                                file = altFile;
                                downloadSize = altDownloadSize;
                                format = altFormat;
                                serverPreferencePerSession[content.InternalName] = altPref;
                            }
                            catch
                            {
                                success = false;
                            }
                        }
                        if (!success)
                        {
                            MessageBox.Show($"Download fehlgeschlagen für {content.InternalName} auf beiden Servern.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }
                        progressBarTextAnimator.StartAnimation("Extracting");
                        await Task.Run(() => ArchiveExtractor.ExtractArchiveAsync(file, pathShowLabel.Text, progressBar));
                        await Task.Run(() => File.Delete(file));
                        if (label.InvokeRequired)
                        {
                            label.Invoke(new Action(() =>
                            {
                                label.ForeColor = Color.Green;
                                label.Text = label.Text.StartsWith("Content") ? $"Content ({DriveUsageUpdater.FormatSize(downloadSize)})" : $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                            }));
                        }
                        else
                        {
                            label.ForeColor = Color.Green;
                            label.Text = label.Text.StartsWith("Content") ? $"Content ({DriveUsageUpdater.FormatSize(downloadSize)})" : $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Download/Extract failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SentrySdk.CaptureException(ex);
                    }
                }
                else if (!button.Checked && label != null && label.ForeColor == Color.Green)
                {
                    progressBarTextAnimator.StartAnimation("Deleting");
                    string contentPath = Path.Combine(pathShowLabel.Text, content.InternalName);
                    if (Directory.Exists(contentPath))
                    {
                        try
                        {
                            await Task.Run(() => Directory.Delete(contentPath, recursive: true));
                            string contentSizeInfo = $"{DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)}";
                            bool canEnable = await CanBeEnabledAsync(content, pref);
                            if (label.InvokeRequired)
                            {
                                label.Invoke(new Action(() =>
                                {
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
                                }));
                            }
                            else
                            {
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
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            SentrySdk.CaptureException(ex);
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
