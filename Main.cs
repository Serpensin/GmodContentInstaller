using SerpentModding;
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
        private const string ContentLabelPrefix = "Content";

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

        // PUBLIC METHODS
        public Main()
        {
            Logger.Instance.Trace("Main constructor called");
            InitializeComponent();
            Logger.Instance.Debug("Component initialized");
            InitializeContentInfo();
            Logger.Instance.Info("Content info initialized");
            progressBarTextAnimator = new(progressBar);
            downloader = new(progressBar);
        }
        public async Task LoadInfo(string addonsPath)
        {
            Logger.Instance.Info($"Loading info for addonsPath: {addonsPath}");
            var tasks = new List<Task>
            {
                UpdateContentAndMapsAsync(contentInfoDictionary["CSSContent"], CSSLabelContent, CSSButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["CSSMaps"], null, null, CSSLabelMaps, CSSButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["DODContent"], DODLabelContent, DODButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["DODMaps"], null, null, DODLabelMaps, DODButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL1Content"], HL1LabelContent, HL1ButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL1Maps"], null, null, HL1LabelMaps, HL1ButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep1Content"], HL2Ep1LabelContent, HL2Ep1ButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep1Maps"], null, null, HL2Ep1LabelMaps, HL2Ep1ButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep2Content"], HL2Ep2LabelContent, HL2Ep2ButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2Ep2Maps"], null, null, HL2Ep2LabelMaps, HL2Ep2ButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2ExtrasContent"], HL2ExtrasLabelContent, HL2ExtrasButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["HL2ExtrasMaps"], null, null, HL2ExtrasLabelMaps, HL2ExtrasButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["PortalContent"], PortalLabelContent, PortalButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["PortalMaps"], null, null, PortalLabelMaps, PortalButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["Portal2Content"], Portal2LabelContent, Portal2ButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["Portal2Maps"], null, null, Portal2LabelMaps, Portal2ButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["TF2Content"], TF2LabelContent, TF2ButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["TF2Maps"], null, null, TF2LabelMaps, TF2ButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4DContent"], L4DLabelContent, L4DButtonContent, null, null, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4DMaps"], null, null, L4DLabelMaps, L4DButtonMaps, addonsPath),
                UpdateContentAndMapsAsync(contentInfoDictionary["L4D2Content"], L4D2LabelContent, L4D2ButtonContent, null, null, addonsPath),
            };
            _ = progressBarTextAnimator.StartAnimation("Loading");
            Logger.Instance.Info("Started loading animation");
            await Task.WhenAll(tasks);
            Logger.Instance.Info("All content info loaded");
            progressBarTextAnimator.StopAnimation();
        }

        // PRIVATE METHODS
        private void InitializeContentInfo()
        {
            Logger.Instance.Trace("InitializeContentInfo() called");
            contentInfoDictionary = ContentKeys
                .Select(pair => (pair.Key, Info: urlDictionary.GetContentInfoByName(pair.DisplayName)))
                .Where(x => x.Info != null)
                .ToDictionary(x => x.Key, x => x.Info!);
            Logger.Instance.Debug($"Loaded {contentInfoDictionary.Count} content infos");
        }

        private Guna2HtmlLabel GetAssociatedLabel(Guna2ToggleSwitch toggleSwitch)
        {
            Logger.Instance.Trace($"GetAssociatedLabel for {toggleSwitch.Name}");
            string labelName = toggleSwitch.Name.Replace("Button", "Label");
            var labelControl = this.Controls.Find(labelName, true).FirstOrDefault();
            if (labelControl == null)
                Logger.Instance.Warn($"Label {labelName} not found for toggleSwitch {toggleSwitch.Name}");
            return labelControl as Guna2HtmlLabel;
        }

        private static async Task<bool> CanBeEnabledAsync(ContentInfo contentInfo, ServerPreference serverPref)
        {
            Logger.Instance.Trace($"CanBeEnabledAsync for {contentInfo.InternalName} with serverPref {serverPref}");
            string url = serverPref == ServerPreference.Primary ? contentInfo.PrimaryUrl : contentInfo.SecondaryUrl;
            bool reachable = await UrlChecker.IsUrlReachableAsync(url);
            Logger.Instance.Debug($"URL {url} reachable: {reachable}");
            return reachable;
        }

        private static (string url, long downloadSize, string format) GetServerInfo(ContentInfo content, ServerPreference pref)
        {
            Logger.Instance.Trace($"GetServerInfo for {content.InternalName} with pref {pref}");
            return pref == ServerPreference.Primary
                ? (content.PrimaryUrl, content.PrimaryDownloadSize, content.PrimaryFormat)
                : (content.SecondaryUrl, content.SecondaryDownloadSize, content.SecondaryFormat);
        }

        private async Task UpdateContentAndMapsAsync(ContentInfo content, Guna2HtmlLabel contentLabel, Guna2ToggleSwitch contentButton, Guna2HtmlLabel mapLabel, Guna2ToggleSwitch mapButton, string addonsPath)
        {
            Logger.Instance.Trace($"UpdateContentAndMapsAsync for {content.InternalName}");
            bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
            var pref = serverPreferencePerSession.TryGetValue(content.InternalName, out var p) ? p : preferredServer;
            bool canEnable = await CanBeEnabledAsync(content, pref);
            var (url, downloadSize, format) = GetServerInfo(content, pref);
            Logger.Instance.Debug($"isInstalled: {isInstalled}, canEnable: {canEnable}, url: {url}");

            if (contentLabel != null && contentButton != null)
            {
                UpdateContentLabelAndButton(contentLabel, contentButton, canEnable, isInstalled, downloadSize, content.InstallSize);
            }

            if (mapLabel != null && mapButton != null)
            {
                UpdateMapLabelAndButton(mapLabel, mapButton, canEnable, isInstalled, downloadSize, content.InstallSize);
            }
        }

        private static void UpdateContentLabelAndButton(Guna2HtmlLabel label, Guna2ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            SetLabelSafe(label, l =>
            {
                if (canEnable)
                {
                    l.Text = $"Content ({DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(installSize)})";
                }
                else
                {
                    l.ForeColor = Color.Red;
                }
                if (isInstalled)
                {
                    l.ForeColor = Color.Green;
                    l.Text = $"Content ({DriveUsageUpdater.FormatSize(installSize)})";
                }
            });
            SetButtonSafe(button, b =>
            {
                b.Enabled = canEnable;
                if (!canEnable) b.Checked = false;
                if (isInstalled) { b.Checked = true; b.Enabled = true; }
            });
        }

        private static void UpdateMapLabelAndButton(Guna2HtmlLabel label, Guna2ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            SetLabelSafe(label, l =>
            {
                if (canEnable)
                {
                    l.Text = $"Maps ({DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(installSize)})";
                }
                else
                {
                    l.ForeColor = Color.Red;
                }
                if (isInstalled)
                {
                    l.ForeColor = Color.Green;
                    l.Text = $"Maps ({DriveUsageUpdater.FormatSize(installSize)})";
                }
            });
            SetButtonSafe(button, b =>
            {
                b.Enabled = canEnable;
                if (!canEnable) b.Checked = false;
                if (isInstalled) { b.Checked = true; b.Enabled = true; }
            });
        }

        private static void SetLabelSafe(Guna2HtmlLabel label, Action<Guna2HtmlLabel> update)
        {
            if (label.InvokeRequired)
                label.Invoke(update, label);
            else
                update(label);
        }

        private static void SetButtonSafe(Guna2ToggleSwitch button, Action<Guna2ToggleSwitch> update)
        {
            if (button.InvokeRequired)
                button.Invoke(update, button);
            else
                update(button);
        }

        private async Task HandleDownloadAndExtractAsync(ContentInfo content, Guna2HtmlLabel label, ServerPreference pref, string url, long downloadSize, string format, string file)
        {
            Logger.Instance.Info($"Downloading {content.InternalName} from {url}");
            _ = progressBarTextAnimator.StartAnimation("Downloading");
            try
            {
                bool success = true;
                try
                {
                    await downloader.DownloadFileAsync(url, content.InternalName + (format == "zip" ? ".zip" : ".tar.gz"), pathShowLabel.Text);
                    Logger.Instance.Info($"Download successful for {content.InternalName}");
                }
                catch
                {
                    Logger.Instance.Warn($"Primary download failed for {content.InternalName}, trying secondary");
                    var altPref = pref == ServerPreference.Primary ? ServerPreference.Secondary : ServerPreference.Primary;
                    var (altUrl, altDownloadSize, altFormat) = GetServerInfo(content, altPref);
                    string altFileExt = altFormat == "zip" ? ".zip" : ".tar.gz";
                    var altFile = Path.Combine(pathShowLabel.Text, content.InternalName + altFileExt);
                    try
                    {
                        await downloader.DownloadFileAsync(altUrl, content.InternalName + altFileExt, pathShowLabel.Text);
                        file = altFile;
                        downloadSize = altDownloadSize;
                        serverPreferencePerSession[content.InternalName] = altPref;
                        Logger.Instance.Info($"Secondary download successful for {content.InternalName}");
                    }
                    catch
                    {
                        Logger.Instance.Error($"Download failed for {content.InternalName} on both servers");
                        success = false;
                    }
                }
                if (!success)
                {
                    MessageBox.Show($"Download failed for \"{content.InternalName}\" on both servers.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _ = progressBarTextAnimator.StartAnimation("Extracting");
                Logger.Instance.Info($"Extracting archive for {content.InternalName}");
                await Task.Run(() => ArchiveExtractor.ExtractArchiveAsync(file, pathShowLabel.Text, progressBar));
                await Task.Run(() => File.Delete(file));
                Logger.Instance.Info($"Extraction and cleanup done for {content.InternalName}");
                await UpdateLabelAfterExtractAsync(label, downloadSize, content);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Download/Extract failed: {ex.Message}");
                MessageBox.Show($"Download/Extract failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task UpdateLabelAfterExtractAsync(Guna2HtmlLabel label, long downloadSize, ContentInfo content)
        {
            if (label.InvokeRequired)
            {
                await label.InvokeAsync(() =>
                {
                    label.ForeColor = Color.Green;
                    label.Text = label.Text.StartsWith(ContentLabelPrefix) ? $"{ContentLabelPrefix} ({DriveUsageUpdater.FormatSize(downloadSize)})" : $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
                });
            }
            else
            {
                label.ForeColor = Color.Green;
                label.Text = label.Text.StartsWith(ContentLabelPrefix) ? $"{ContentLabelPrefix} ({DriveUsageUpdater.FormatSize(downloadSize)})" : $"Maps ({DriveUsageUpdater.FormatSize(content.InstallSize)})";
            }
        }

        private async Task HandleDeleteAsync(ContentInfo content, Guna2HtmlLabel label, Guna2ToggleSwitch button, ServerPreference pref, long downloadSize)
        {
            Logger.Instance.Info($"Deleting {content.InternalName} from {pathShowLabel.Text}");
            _ = progressBarTextAnimator.StartAnimation("Deleting");
            string contentPath = Path.Combine(pathShowLabel.Text, content.InternalName);
            if (Directory.Exists(contentPath))
            {
                try
                {
                    await Task.Run(() => Directory.Delete(contentPath, recursive: true));
                    Logger.Instance.Info($"Delete successful for {content.InternalName}");
                    string contentSizeInfo = $"{DriveUsageUpdater.FormatSize(downloadSize)} / {DriveUsageUpdater.FormatSize(content.InstallSize)}";
                    bool canEnable = await CanBeEnabledAsync(content, pref);
                    await UpdateLabelAfterDeleteAsync(label, button, canEnable, contentSizeInfo);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Delete failed: {ex.Message}");
                    MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task UpdateLabelAfterDeleteAsync(Guna2HtmlLabel label, Guna2ToggleSwitch button, bool canEnable, string contentSizeInfo)
        {
            if (label.InvokeRequired)
            {
                await label.InvokeAsync(() =>
                {
                    if (canEnable)
                    {
                        SetLabelAndButtonEnabled(label, button, contentSizeInfo);
                    }
                    else
                    {
                        SetLabelAndButtonDisabled(label, button, contentSizeInfo);
                    }
                });
            }
            else
            {
                if (canEnable)
                {
                    SetLabelAndButtonEnabled(label, button, contentSizeInfo);
                }
                else
                {
                    SetLabelAndButtonDisabled(label, button, contentSizeInfo);
                }
            }
        }

        private void SetLabelAndButtonEnabled(Guna2HtmlLabel label, Guna2ToggleSwitch button, string contentSizeInfo)
        {
            label.ForeColor = Color.White;
            label.Text = label.Text.StartsWith(ContentLabelPrefix) ? $"{ContentLabelPrefix} ({contentSizeInfo})" : $"Maps ({contentSizeInfo})";
            button.Checked = false;
            toggleSwitchManager.AddOrUpdateToggleSwitchState(button, true);
        }

        private void SetLabelAndButtonDisabled(Guna2HtmlLabel label, Guna2ToggleSwitch button, string contentSizeInfo)
        {
            label.Text = label.Text.StartsWith(ContentLabelPrefix) ? $"{ContentLabelPrefix} ({contentSizeInfo})" : $"Maps ({contentSizeInfo})";
            label.ForeColor = Color.Red;
            button.Checked = false;
            toggleSwitchManager.AddOrUpdateToggleSwitchState(button, false);
        }

        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            Logger.Instance.Trace("DownloadButton_Click called");
            downloadButton.Enabled = false;
            toggleSwitchManager.SaveToggleSwitchStates(this);
            ToggleSwitchStateManager.DisableAllToggleSwitches(this);
            Logger.Instance.Info("All toggle switches disabled and states saved");

            var toggleSwitches = Controls.OfType<Guna2ToggleSwitch>().ToList();
            foreach (var button in toggleSwitches)
            {
                string contentName = button.Name.Replace("Button", "");
                if (!contentInfoDictionary.TryGetValue(contentName, out ContentInfo content))
                {
                    Logger.Instance.Warn($"ContentInfo for {contentName} not found. Skipping.");
                    continue;
                }

                var label = GetAssociatedLabel(button);
                var pref = serverPreferencePerSession.TryGetValue(content.InternalName, out var p) ? p : preferredServer;
                var (url, downloadSize, format) = GetServerInfo(content, pref);
                string fileExt = format == "zip" ? ".zip" : ".tar.gz";
                var file = Path.Combine(pathShowLabel.Text, content.InternalName + fileExt);

                if (button.Checked && label != null && label.ForeColor != Color.Green)
                {
                    await HandleDownloadAndExtractAsync(content, label, pref, url, downloadSize, format, file);
                }
                else if (!button.Checked && label != null && label.ForeColor == Color.Green)
                {
                    await HandleDeleteAsync(content, label, button, pref, downloadSize);
                }
            }

            toggleSwitchManager.RestoreToggleSwitchStates();
            Logger.Instance.Info("Toggle switch states restored");
            progressBarTextAnimator.StopAnimation();
            downloadButton.Enabled = true;
            Logger.Instance.Info("DownloadButton_Click finished");
        }

        private async void PathDetectButton_Click(object sender, EventArgs e)
        {
            Logger.Instance.Trace("PathDetectButton_Click called");
            pathDetectButton.Enabled = false;
            string addonsPath = PathDetector.Select();
            Logger.Instance.Debug($"PathDetector.Select() returned: {addonsPath}");
            if (string.IsNullOrEmpty(addonsPath))
            {
                Logger.Instance.Warn("No addons path selected");
                pathDetectButton.Enabled = true;
                return;
            }

            var (primaryHost, secondaryHost, primaryUrl, secondaryUrl) = GetFirstContentHostsAndUrls();
            var (primaryPing, secondaryPing) = await GetPingTimesAsync(primaryHost, secondaryHost);
            preferredServer = SelectPreferredServer(primaryPing, secondaryPing);
            var (primaryReachable, secondaryReachable) = await CheckServerReachabilityAsync(primaryUrl, secondaryUrl);
            if (!ValidatePreferredServer(ref preferredServer, primaryReachable, secondaryReachable))
            {
                pathDetectButton.Enabled = true;
                return;
            }

            Logger.Instance.Info($"Preferred server set to: {preferredServer}");
            driveUsageUpdater = new(Path.GetPathRoot(addonsPath)?.Substring(0, 2), drivespaceUsageBar);
            driveUsageUpdater.UpdateDriveSizeBar();
            pathShowLabel.Text = addonsPath;
            await LoadInfo(addonsPath);
            downloadButton.Enabled = true;
            Logger.Instance.Info("Path detection and info loading complete");
        }

        private (string primaryHost, string secondaryHost, string primaryUrl, string secondaryUrl) GetFirstContentHostsAndUrls()
        {
            var firstContent = contentInfoDictionary.Values.FirstOrDefault();
            string primaryHost = null;
            string secondaryHost = null;
            string primaryUrl = null;
            string secondaryUrl = null;
            if (firstContent != null)
            {
                if (!string.IsNullOrWhiteSpace(firstContent.PrimaryUrl))
                {
                    primaryHost = new Uri(firstContent.PrimaryUrl).Host;
                    primaryUrl = firstContent.PrimaryUrl;
                }
                if (!string.IsNullOrWhiteSpace(firstContent.SecondaryUrl))
                {
                    secondaryHost = new Uri(firstContent.SecondaryUrl).Host;
                    secondaryUrl = firstContent.SecondaryUrl;
                }
            }
            return (primaryHost, secondaryHost, primaryUrl, secondaryUrl);
        }

        private async Task<(long primaryPing, long secondaryPing)> GetPingTimesAsync(string primaryHost, string secondaryHost)
        {
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
                        Logger.Instance.Info($"Ping to primary host {primaryHost} successful: {primaryPing} ms");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"Ping failed for primary host {primaryHost}: {ex.Message}");
                    }
                    if (!string.IsNullOrEmpty(secondaryHost))
                    {
                        try
                        {
                            var reply = await ping.SendPingAsync(secondaryHost, 1500);
                            if (reply.Status == IPStatus.Success)
                                secondaryPing = reply.RoundtripTime;
                            Logger.Instance.Info($"Ping to secondary host {secondaryHost} successful: {secondaryPing} ms");
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error($"Ping failed for secondary host {secondaryHost}: {ex.Message}");
                        }
                    }
                }
            }
            return (primaryPing, secondaryPing);
        }

        private ServerPreference SelectPreferredServer(long primaryPing, long secondaryPing)
        {
            return primaryPing <= secondaryPing ? ServerPreference.Primary : ServerPreference.Secondary;
        }

        private async Task<(bool primaryReachable, bool secondaryReachable)> CheckServerReachabilityAsync(string primaryUrl, string secondaryUrl)
        {
            bool primaryReachable = false;
            bool secondaryReachable = false;
            if (!string.IsNullOrEmpty(primaryUrl))
                primaryReachable = await UrlChecker.IsUrlReachableAsync(primaryUrl);
            if (!string.IsNullOrEmpty(secondaryUrl))
                secondaryReachable = await UrlChecker.IsUrlReachableAsync(secondaryUrl);
            return (primaryReachable, secondaryReachable);
        }

        private bool ValidatePreferredServer(ref ServerPreference pref, bool primaryReachable, bool secondaryReachable)
        {
            if (pref == ServerPreference.Primary && !primaryReachable && secondaryReachable)
            {
                pref = ServerPreference.Secondary;
                Logger.Instance.Warn($"Primary server not reachable, switching to secondary.");
            }
            else if (pref == ServerPreference.Secondary && !secondaryReachable && primaryReachable)
            {
                pref = ServerPreference.Primary;
                Logger.Instance.Warn($"Secondary server not reachable, switching to primary.");
            }
            else if (!primaryReachable && !secondaryReachable)
            {
                Logger.Instance.Error("Neither primary nor secondary server is reachable for the first content!");
                MessageBox.Show("Neither primary nor secondary server is reachable!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void Logo_Click(object sender, MouseEventArgs e)
        {
            Logger.Instance.Trace("Logo_Click called");
            if (e.Button == MouseButtons.Left)
            {
                Logger.Instance.Info("Discord logo clicked");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://url.serpensin.com/discord",
                    UseShellExecute = true
                });
            }
        }

        private void ContentToggleSwitch_Click(object sender, EventArgs e)
        {
            Logger.Instance.Trace("ContentToggleSwitch_Click called");
            var toggleSwitch = (Guna2ToggleSwitch)sender;
            var associatedLabel = GetAssociatedLabel(toggleSwitch);

            if (associatedLabel == null)
            {
                Logger.Instance.Error($"Label for {toggleSwitch.Name} not found!");
                MessageBox.Show($"Label for {toggleSwitch.Name} not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string switchName = toggleSwitch.Name.Replace("Button", "");
            if (!contentInfoDictionary.TryGetValue(switchName, out ContentInfo content))
            {
                Logger.Instance.Error($"Content for {switchName} not found in dictionary!");
                MessageBox.Show($"Content for {switchName} not found in dictionary!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            long installSize = content.InstallSize;
            try
            {
                Logger.Instance.Debug($"Updating drive size bar for {switchName}, checked: {toggleSwitch.Checked}");
                driveUsageUpdater.UpdateDriveSizeBar(toggleSwitch.Checked ? installSize : -installSize);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Drive update failed: {ex.Message}");
                MessageBox.Show($"Drive update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}