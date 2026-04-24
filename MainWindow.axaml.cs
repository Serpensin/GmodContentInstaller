#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace GModContentWizard
{
    /// <summary>
    /// Main window of the GMod Content Wizard application.
    /// Handles the UI for content selection, download, and installation.
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? addonsPath;
        private Dictionary<string, ContentInfo> contentInfoDictionary = new();
        private ServerPreference preferredServer = ServerPreference.Primary;
        private Downloader? downloader;
        private DriveUsageUpdater? driveUsageUpdater;
        private bool isOperationRunning = false;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
            VersionText.Text = $"Version {version}";
            
            var titleAttr = (System.Reflection.AssemblyProductAttribute?)Attribute.GetCustomAttribute(
                assembly, typeof(System.Reflection.AssemblyProductAttribute));
            if (titleAttr != null && !string.IsNullOrEmpty(titleAttr.Product))
                TitleText.Text = titleAttr.Product;

            var descAttr = (System.Reflection.AssemblyDescriptionAttribute?)Attribute.GetCustomAttribute(
                assembly, typeof(System.Reflection.AssemblyDescriptionAttribute));
            if (descAttr != null && !string.IsNullOrEmpty(descAttr.Description))
                DescriptionText.Text = descAttr.Description;
            
            // Versuche zuerst eingebettete Resource zu laden, dann Dateisystem
            var resourceName = "urls.json";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "urls.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                LoadContentFromJson(json);
                Log.Information("Loaded content from file: {Path}", filePath);
            }
            else
            {
                using var stream = assembly.GetManifestResourceStream(resourceName) 
                    ?? throw new FileNotFoundException("Embedded resource urls.json not found");
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                LoadContentFromJson(json);
                Log.Information("Loaded content from embedded resource");
            }
            
            downloader = new Downloader(ProgressBar, DownloadButton, StatusText);
            driveUsageUpdater = new DriveUsageUpdater(DriveSpaceUsageBar);

            _ = CheckForUpdateAsync();
        }

        /// <summary>
        /// Checks for application updates and displays a notification if a newer version is available.
        /// </summary>
        private async Task CheckForUpdateAsync()
        {
            try
            {
                var (hasUpdate, latestVersion) = await UpdateChecker.CheckForUpdateAsync();
                if (hasUpdate && !string.IsNullOrEmpty(latestVersion))
                {
                    var linkText = $"Version {latestVersion} verfügbar";
                    UpdateText.Text = linkText;
                    UpdateText.IsVisible = true;
                    UpdateText.Tag = UpdateChecker.LatestReleaseUrl;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Update check failed");
            }
        }

        /// <summary>
        /// Loads content information asynchronously for all game sections.
        /// </summary>
        public async Task LoadInfoAsync()
        {
            if (string.IsNullOrEmpty(addonsPath)) return;

            Log.Information("Loading info for addonsPath: {Path}", addonsPath);

            var tasks = new List<Task>
            {
                UpdateContentAndMapsAsync(GetContentInfo("CSS Content"), CSSLabelContent, CSSButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("CSS Maps"), null, null, CSSLabelMaps, CSSButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("DOD Content"), DODLabelContent, DODButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("DOD Maps"), null, null, DODLabelMaps, DODButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("HL1 Content"), HL1LabelContent, HL1ButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("HL1 Maps"), null, null, HL1LabelMaps, HL1ButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Ep1 Content"), HL2Ep1LabelContent, HL2Ep1ButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Ep1 Maps"), null, null, HL2Ep1LabelMaps, HL2Ep1ButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Ep2 Content"), HL2Ep2LabelContent, HL2Ep2ButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Ep2 Maps"), null, null, HL2Ep2LabelMaps, HL2Ep2ButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Extras Content"), HL2ExtrasLabelContent, HL2ExtrasButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("HL2 Extras Maps"), null, null, HL2ExtrasLabelMaps, HL2ExtrasButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("Portal Content"), PortalLabelContent, PortalButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("Portal Maps"), null, null, PortalLabelMaps, PortalButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("Portal 2 Content"), Portal2LabelContent, Portal2ButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("Portal 2 Maps"), null, null, Portal2LabelMaps, Portal2ButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("TF2 Content"), TF2LabelContent, TF2ButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("TF2 Maps"), null, null, TF2LabelMaps, TF2ButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("L4D Content"), L4DLabelContent, L4DButtonContent, null, null),
                UpdateContentAndMapsAsync(GetContentInfo("L4D Maps"), null, null, L4DLabelMaps, L4DButtonMaps),
                UpdateContentAndMapsAsync(GetContentInfo("L4D2 Content"), L4D2LabelContent, L4D2ButtonContent, null, null),
            };

            await Task.WhenAll(tasks);
            DownloadButton.IsEnabled = true;
            Log.Information("All content info loaded");
        }

        /// <summary>
        /// Launches Garry's Mod via Steam protocol.
        /// </summary>
        private void LaunchGMod_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://run/4000",
                    UseShellExecute = true
                });
                Log.Information("Launched GMod via Steam");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to launch GMod");
            }
        }

        /// <summary>
        /// Resets the window size to default dimensions (1280x800).
        /// </summary>
        private void ResetSize_Click(object? sender, RoutedEventArgs e)
        {
            Width = 1280;
            Height = 800;
            WindowState = WindowState.Normal;
            Log.Information("Window size reset to 1280x800");
        }

        /// <summary>
        /// Opens the Discord link in the default browser.
        /// </summary>
        private async void Logo_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://url.serpensin.com/discord",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open Discord link");
            }
        }

        /// <summary>
        /// Opens the addons folder in the file explorer.
        /// </summary>
        private void PathShowLabel_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(addonsPath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = addonsPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open addons folder");
            }
        }

        /// <summary>
        /// Minimizes the window to the taskbar.
        /// </summary>
        private void Minimize_Click(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Closes the application window.
        /// </summary>
        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Opens the update release page in the browser when the update text is clicked.
        /// </summary>
        private void Update_Click(object? sender, RoutedEventArgs e)
        {
            if (UpdateText.Tag is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to open update link");
                }
            }
        }

        /// <summary>
        /// Handles the path detection button click, detecting or prompting for the GMod addons path.
        /// </summary>
        private async void PathDetectButton_Click(object? sender, RoutedEventArgs e)
        {
            PathDetectButton.IsEnabled = false;
            
            var path = PathDetector.Select();
            if (string.IsNullOrEmpty(path))
            {
                path = await PathDetector.SelectWithDialog(this);
            }
            
            if (string.IsNullOrEmpty(path))
            {
                PathDetectButton.IsEnabled = true;
                return;
            }

            addonsPath = path;
            PathShowLabel.Text = path;

            // Initialize DriveUsageUpdater
            string? driveLetter = null;
            if (OperatingSystem.IsWindows() && path.Length >= 1)
            {
                driveLetter = path.Substring(0, 1);
            }
            else if (!OperatingSystem.IsWindows())
            {
                driveLetter = path;
            }
            driveUsageUpdater = new DriveUsageUpdater(driveLetter, DriveSpaceUsageBar);
            driveUsageUpdater.UpdateDriveSizeBar();

            var firstContent = contentInfoDictionary.Values.FirstOrDefault();
            if (firstContent != null)
            {
                string? primaryHost = null, secondaryHost = null;
                if (!string.IsNullOrWhiteSpace(firstContent.PrimaryUrl))
                    primaryHost = new Uri(firstContent.PrimaryUrl).Host;
                if (!string.IsNullOrEmpty(firstContent.SecondaryUrl))
                    secondaryHost = new Uri(firstContent.SecondaryUrl).Host;

                var (primaryPing, secondaryPing) = await GetPingTimesAsync(primaryHost, secondaryHost);
                preferredServer = primaryPing <= secondaryPing ? ServerPreference.Primary : ServerPreference.Secondary;
            }

            await LoadInfoAsync();
            PathDetectButton.IsEnabled = true;
        }

        /// <summary>
        /// Handles the download/apply button click, processing all toggles.
        /// </summary>
        private async void DownloadButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(addonsPath))
            {
                Log.Warning("No addons path selected");
                return;
            }

            if (isOperationRunning)
            {
                Log.Warning("Operation already running");
                return;
            }

            isOperationRunning = true;
            DownloadButton.IsEnabled = false;
            PathDetectButton.IsEnabled = false;
            LaunchGModButton.IsEnabled = false;
            SetAllTogglesEnabled(false);

            try
            {
                await ProcessTogglesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Download/Apply operation failed");
            }
            finally
            {
                isOperationRunning = false;
                DownloadButton.IsEnabled = true;
                PathDetectButton.IsEnabled = true;
                LaunchGModButton.IsEnabled = true;
                SetAllTogglesEnabled(true);
                driveUsageUpdater?.SetDriveLetter(addonsPath);
                driveUsageUpdater?.UpdateDriveSizeBar();
            }
        }

        /// <summary>
        /// Enables or disables all content toggle switches.
        /// </summary>
        private void SetAllTogglesEnabled(bool enabled)
        {
            foreach (var gameKey in GetGameKeys())
            {
                var (contentSwitch, mapsSwitch) = GetToggles(gameKey);
                contentSwitch.IsEnabled = enabled;
                mapsSwitch.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Handles toggle switch changes, updating the drive usage display.
        /// </summary>
        private void ContentToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (isOperationRunning || addonsPath == null)
            {
                if (sender is ToggleSwitch toggle && !isOperationRunning)
                {
                    toggle.IsChecked = !toggle.IsChecked;
                }
                return;
            }

            try
            {
                long totalChange = 0;

                foreach (var gameKey in GetGameKeys())
                {
                    var (contentSwitch, mapsSwitch) = GetToggles(gameKey);
                    var contentKey = $"{gameKey} Content";
                    var mapsKey = $"{gameKey} Maps";

                    if (contentInfoDictionary.TryGetValue(contentKey, out var content) && contentSwitch.IsChecked == true)
                    {
                        bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
                        if (!isInstalled) totalChange += content.InstallSize;
                    }

                    if (contentInfoDictionary.TryGetValue(mapsKey, out var maps) && mapsSwitch.IsChecked == true)
                    {
                        bool isInstalled = Directory.Exists(Path.Combine(addonsPath, maps.InternalName));
                        if (!isInstalled) totalChange += maps.InstallSize;
                    }
                }

                Log.Debug("Total cumulative change from all switches: {Change}", totalChange);
                driveUsageUpdater?.SetDriveLetter(addonsPath);
                driveUsageUpdater?.UpdateDriveSizeBar(totalChange);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Drive update failed");
            }
        }

        /// <summary>
        /// Array of game section keys used for iteration.
        /// </summary>
        private static readonly string[] GameKeys = { "CSS", "DOD", "HL1", "HL2 Ep1", "HL2 Ep2", "HL2 Extras", "Portal", "Portal 2", "TF2", "L4D", "L4D2" };

        /// <summary>
        /// Gets the list of game keys.
        /// </summary>
        private static string[] GetGameKeys() => GameKeys;

        /// <summary>
        /// Gets the toggle switches for a given game key.
        /// </summary>
        private (ToggleSwitch content, ToggleSwitch maps) GetToggles(string gameKey) => gameKey switch
        {
            "CSS" => (CSSButtonContent, CSSButtonMaps),
            "DOD" => (DODButtonContent, DODButtonMaps),
            "HL1" => (HL1ButtonContent, HL1ButtonMaps),
            "HL2 Ep1" => (HL2Ep1ButtonContent, HL2Ep1ButtonMaps),
            "HL2 Ep2" => (HL2Ep2ButtonContent, HL2Ep2ButtonMaps),
            "HL2 Extras" => (HL2ExtrasButtonContent, HL2ExtrasButtonMaps),
            "Portal" => (PortalButtonContent, PortalButtonMaps),
            "Portal 2" => (Portal2ButtonContent, Portal2ButtonMaps),
            "TF2" => (TF2ButtonContent, TF2ButtonMaps),
            "L4D" => (L4DButtonContent, L4DButtonMaps),
            "L4D2" => (L4D2ButtonContent, L4D2ButtonMaps),
            _ => throw new ArgumentException($"Unknown game key: {gameKey}")
        };

        /// <summary>
        /// Gets the text labels for a given game key.
        /// </summary>
        private (TextBlock content, TextBlock maps) GetLabels(string gameKey) => gameKey switch
        {
            "CSS" => (CSSLabelContent, CSSLabelMaps),
            "DOD" => (DODLabelContent, DODLabelMaps),
            "HL1" => (HL1LabelContent, HL1LabelMaps),
            "HL2 Ep1" => (HL2Ep1LabelContent, HL2Ep1LabelMaps),
            "HL2 Ep2" => (HL2Ep2LabelContent, HL2Ep2LabelMaps),
            "HL2 Extras" => (HL2ExtrasLabelContent, HL2ExtrasLabelMaps),
            "Portal" => (PortalLabelContent, PortalLabelMaps),
            "Portal 2" => (Portal2LabelContent, Portal2LabelMaps),
            "TF2" => (TF2LabelContent, TF2LabelMaps),
            "L4D" => (L4DLabelContent, L4DLabelMaps),
            "L4D2" => (L4D2LabelContent, L4D2LabelMaps),
            _ => throw new ArgumentException($"Unknown game key: {gameKey}")
        };

        /// <summary>
        /// Formats a byte count into a human-readable string.
        /// </summary>
        /// <param name="bytes">The size in bytes.</param>
        /// <returns>A formatted string representation.</returns>
        public static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Loads content information from JSON data.
        /// </summary>
        /// <param name="json">The JSON string containing content information.</param>
        private void LoadContentFromJson(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ContentInfo>>(json);
                if (data != null)
                {
                    contentInfoDictionary = data;
                    Log.Information("Loaded {Count} content infos from embedded", contentInfoDictionary.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load content from embedded JSON");
            }
        }

        /// <summary>
        /// Gets ping times for primary and secondary servers.
        /// </summary>
        /// <param name="primaryHost">The primary server hostname.</param>
        /// <param name="secondaryHost">The secondary server hostname.</param>
        /// <returns>A tuple of ping times in milliseconds.</returns>
        private async Task<(long primary, long secondary)> GetPingTimesAsync(string? primaryHost, string? secondaryHost)
        {
            long primaryPing = long.MaxValue, secondaryPing = long.MaxValue;

            using var ping = new Ping();
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
            return (primaryPing, secondaryPing);
        }

        /// <summary>
        /// Gets content info from the dictionary by key.
        /// </summary>
        /// <param name="key">The content key.</param>
        /// <returns>The ContentInfo object or null if not found.</returns>
        private ContentInfo? GetContentInfo(string key)
        {
            return contentInfoDictionary.TryGetValue(key, out var info) ? info : null;
        }

        /// <summary>
        /// Updates the UI for content and maps sections based on installation status and availability.
        /// </summary>
        private async Task UpdateContentAndMapsAsync(ContentInfo? content, TextBlock? contentLabel, ToggleSwitch? contentButton, TextBlock? mapLabel, ToggleSwitch? mapButton)
        {
            if (content == null || string.IsNullOrEmpty(addonsPath)) return;

            bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
            var pref = preferredServer;
            bool canEnable = await CanBeEnabledAsync(content, pref);
            var (url, downloadSize, format) = GetServerInfo(content, pref);

            if (contentLabel != null && contentButton != null)
            {
                UpdateContentLabelAndButton(contentLabel, contentButton, canEnable, isInstalled, downloadSize, content.InstallSize);
            }

            if (mapLabel != null && mapButton != null)
            {
                UpdateMapLabelAndButton(mapLabel, mapButton, canEnable, isInstalled, downloadSize, content.InstallSize);
            }
        }

        /// <summary>
        /// Updates the content label and toggle button based on availability and installation status.
        /// </summary>
        private void UpdateContentLabelAndButton(TextBlock label, ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            button.IsEnabled = canEnable || isInstalled;
            var defaultColor = Application.Current?.ActualThemeVariant == ThemeVariant.Light
                ? Avalonia.Media.Brushes.Black
                : Avalonia.Media.Brushes.White;
            
            if (isInstalled)
            {
                label.Foreground = Avalonia.Media.Brushes.LimeGreen;
                label.Text = $"Content ({FormatSize(installSize)})";
                button.IsChecked = true;
                button.IsEnabled = true;
            }
            else if (canEnable)
            {
                label.Foreground = defaultColor;
                label.Text = $"Content ({FormatSize(downloadSize)} / {FormatSize(installSize)})";
                button.IsChecked = false;
            }
            else
            {
                label.Foreground = Avalonia.Media.Brushes.Red;
                label.Text = $"Content (Nicht verfügbar)";
                button.IsChecked = false;
                button.IsEnabled = false;
            }
        }

        /// <summary>
        /// Updates the maps label and toggle button based on availability and installation status.
        /// </summary>
        private void UpdateMapLabelAndButton(TextBlock label, ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            button.IsEnabled = canEnable || isInstalled;
            var defaultColor = Application.Current?.ActualThemeVariant == ThemeVariant.Light
                ? Avalonia.Media.Brushes.Black
                : Avalonia.Media.Brushes.White;
            
            if (isInstalled)
            {
                label.Foreground = Avalonia.Media.Brushes.LimeGreen;
                label.Text = $"Maps ({FormatSize(installSize)})";
                button.IsChecked = true;
                button.IsEnabled = true;
            }
            else if (canEnable)
            {
                label.Foreground = defaultColor;
                label.Text = $"Maps ({FormatSize(downloadSize)} / {FormatSize(installSize)})";
                button.IsChecked = false;
            }
            else
            {
                label.Foreground = Avalonia.Media.Brushes.Red;
                label.Text = $"Maps (Nicht verfügbar)";
                button.IsChecked = false;
                button.IsEnabled = false;
            }
        }

        /// <summary>
        /// Determines if content can be enabled based on server availability.
        /// </summary>
        /// <param name="contentInfo">The content information.</param>
        /// <param name="serverPref">The server preference.</param>
        /// <returns>True if the content can be enabled.</returns>
        private async Task<bool> CanBeEnabledAsync(ContentInfo contentInfo, ServerPreference serverPref)
        {
            string url = serverPref == ServerPreference.Primary ? contentInfo.PrimaryUrl : contentInfo.SecondaryUrl;
            bool reachable = await UrlChecker.IsUrlReachableAsync(url);
            return reachable;
        }

        /// <summary>
        /// Gets server information based on the server preference.
        /// </summary>
        private static (string url, long downloadSize, string format) GetServerInfo(ContentInfo content, ServerPreference pref)
        {
            return pref == ServerPreference.Primary
                ? (content.PrimaryUrl, content.PrimaryDownloadSize, content.PrimaryFormat)
                : (content.SecondaryUrl, content.SecondaryDownloadSize, content.SecondaryFormat);
        }

        /// <summary>
        /// Processes all toggle switches, executing download/delete actions based on their state.
        /// </summary>
        private async Task ProcessTogglesAsync()
        {
            var installActions = new List<(ContentInfo Info, TextBlock Label, bool IsContent)>();
            var deleteActions = new List<(ContentInfo Info, TextBlock Label, bool IsContent)>();

            foreach (var gameKey in GetGameKeys())
            {
                var (contentSwitch, mapsSwitch) = GetToggles(gameKey);
                var (contentLabel, mapsLabel) = GetLabels(gameKey);
                var contentKey = $"{gameKey} Content";
                var mapsKey = $"{gameKey} Maps";

                if (contentInfoDictionary.TryGetValue(contentKey, out var content))
                {
                    bool isInstalled = Directory.Exists(Path.Combine(addonsPath!, content.InternalName));
                    if (contentSwitch.IsChecked == true && !isInstalled)
                    {
                        installActions.Add((content, contentLabel, true));
                    }
                    else if (contentSwitch.IsChecked == false && isInstalled)
                    {
                        deleteActions.Add((content, contentLabel, true));
                    }
                }

                if (contentInfoDictionary.TryGetValue(mapsKey, out var maps))
                {
                    bool isInstalled = Directory.Exists(Path.Combine(addonsPath!, maps.InternalName));
                    if (mapsSwitch.IsChecked == true && !isInstalled)
                    {
                        installActions.Add((maps, mapsLabel, false));
                    }
                    else if (mapsSwitch.IsChecked == false && isInstalled)
                    {
                        deleteActions.Add((maps, mapsLabel, false));
                    }
                }
            }

            foreach (var (info, label, isContent) in deleteActions)
            {
                if (!isOperationRunning) break;
                await HandleDeleteAsync(info, label, preferredServer, isContent);
                driveUsageUpdater?.UpdateDriveSizeBar(-info.InstallSize);
            }

            foreach (var (info, label, isContent) in installActions)
            {
                if (!isOperationRunning) break;
                await HandleDownloadAndExtractAsync(info, label, preferredServer, isContent);
                driveUsageUpdater?.UpdateDriveSizeBar(info.InstallSize);
            }

            if (isOperationRunning)
            {
                await RefreshToggleStatesAsync();
            }
        }

        /// <summary>
        /// Refreshes all toggle states based on actual installation status.
        /// </summary>
        private async Task RefreshToggleStatesAsync()
        {
            foreach (var gameKey in GetGameKeys())
            {
                var (contentSwitch, mapsSwitch) = GetToggles(gameKey);
                var (contentLabel, mapsLabel) = GetLabels(gameKey);
                var contentKey = $"{gameKey} Content";
                var mapsKey = $"{gameKey} Maps";

                if (contentInfoDictionary.TryGetValue(contentKey, out var content))
                {
                    bool isInstalled = Directory.Exists(Path.Combine(addonsPath!, content.InternalName));
                    contentSwitch.IsChecked = isInstalled;
                    bool canEnable = await CanBeEnabledAsync(content, preferredServer);
                    contentSwitch.IsEnabled = isInstalled || canEnable;
                    UpdateContentLabelAndButton(contentLabel, contentSwitch, canEnable, isInstalled, content.PrimaryDownloadSize, content.InstallSize);
                }

                if (contentInfoDictionary.TryGetValue(mapsKey, out var maps))
                {
                    bool isInstalled = Directory.Exists(Path.Combine(addonsPath!, maps.InternalName));
                    mapsSwitch.IsChecked = isInstalled;
                    bool canEnable = await CanBeEnabledAsync(maps, preferredServer);
                    mapsSwitch.IsEnabled = isInstalled || canEnable;
                    UpdateMapLabelAndButton(mapsLabel, mapsSwitch, canEnable, isInstalled, maps.PrimaryDownloadSize, maps.InstallSize);
                }
            }
        }

        /// <summary>
        /// Handles downloading and extracting content from the server.
        /// </summary>
        private async Task HandleDownloadAndExtractAsync(ContentInfo content, TextBlock label, ServerPreference pref, bool isContent)
        {
            if (addonsPath == null) return;

            Log.Information("Downloading {Name} from {Url}", content.InternalName, pref);
            var (url, downloadSize, format) = GetServerInfo(content, pref);
            string fileExt = format == "zip" ? ".zip" : ".tar.gz";
            string file = Path.Combine(addonsPath, content.InternalName + fileExt);

            try
            {
                await downloader!.DownloadFileAsync(url, content.InternalName + fileExt, addonsPath);
                Log.Information("Download successful for {Name}", content.InternalName);

                await ArchiveExtractor.ExtractArchiveAsync(file, addonsPath, ProgressBar, DownloadButton, StatusText);
                await Task.Run(() => File.Delete(file));
                Log.Information("Extraction done for {Name}", content.InternalName);

                label.Foreground = Avalonia.Media.Brushes.LimeGreen;
                label.Text = isContent ? $"Content ({FormatSize(downloadSize)})" : $"Maps ({FormatSize(content.InstallSize)})";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Download/Extract failed for {Name}", content.InternalName);
            }
        }

        /// <summary>
        /// Handles deleting content from the addons directory.
        /// </summary>
        private async Task HandleDeleteAsync(ContentInfo content, TextBlock label, ServerPreference pref, bool isContent)
        {
            if (addonsPath == null || !isOperationRunning) return;

            Log.Information("Deleting {Name}", content.InternalName);
            string contentPath = Path.Combine(addonsPath, content.InternalName);

            if (Directory.Exists(contentPath))
            {
                try
                {
                    await Task.Run(() => Directory.Delete(contentPath, recursive: true));
                    Log.Information("Delete successful for {Name}", content.InternalName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Delete failed for {Name}", content.InternalName);
                }
            }
        }

        /// <summary>
        /// Specifies the preferred download server.
        /// </summary>
        private enum ServerPreference { Primary, Secondary }
    }
}