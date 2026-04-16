#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

namespace GModContentWizard
{
    public partial class MainWindow : Window
    {
        private string? addonsPath;
        private Dictionary<string, ContentInfo> contentInfoDictionary = new();
        private Dictionary<string, ServerPreference> serverPreferencePerSession = new();
        private ServerPreference preferredServer = ServerPreference.Primary;
        private Downloader? downloader;
        private DriveUsageUpdater? driveUsageUpdater;
        private readonly string urlJsonPath = string.Empty;
        private bool isOperationRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
            VersionText.Text = $"Version {version}";
            
            var titleAttr = (System.Reflection.AssemblyTitleAttribute?)Attribute.GetCustomAttribute(
                assembly, typeof(System.Reflection.AssemblyTitleAttribute));
            if (titleAttr != null && !string.IsNullOrEmpty(titleAttr.Title))
                TitleText.Text = titleAttr.Title;

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
        }

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

        private void ResetSize_Click(object? sender, RoutedEventArgs e)
        {
            Width = 1280;
            Height = 800;
            WindowState = WindowState.Normal;
            Log.Information("Window size reset to 1280x800");
        }

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

        private void InitializeContentInfo()
        {
            try
            {
                if (File.Exists(urlJsonPath))
                {
                    var json = File.ReadAllText(urlJsonPath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, ContentInfo>>(json);
                    if (data != null)
                    {
                        contentInfoDictionary = data;
                        Log.Information("Loaded {Count} content infos", contentInfoDictionary.Count);
                    }
                }
                else
                {
                    Log.Warning("URLs file not found at {Path}", urlJsonPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load content info");
            }
        }

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

        private void Minimize_Click(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

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

        private ContentInfo? GetContentInfo(string key)
        {
            return contentInfoDictionary.TryGetValue(key, out var info) ? info : null;
        }

        private async Task UpdateContentAndMapsAsync(ContentInfo? content, TextBlock? contentLabel, ToggleSwitch? contentButton, TextBlock? mapLabel, ToggleSwitch? mapButton)
        {
            if (content == null || string.IsNullOrEmpty(addonsPath)) return;

            bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
            var pref = serverPreferencePerSession.TryGetValue(content.InternalName, out var p) ? p : preferredServer;
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

        private void UpdateContentLabelAndButton(TextBlock label, ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            if (canEnable)
            {
                label.Text = $"Content ({FormatSize(downloadSize)} / {FormatSize(installSize)})";
            }
            else
            {
                label.Foreground = Avalonia.Media.Brushes.Red;
            }
            if (isInstalled)
            {
                label.Foreground = Avalonia.Media.Brushes.LimeGreen;
                label.Text = $"Content ({FormatSize(installSize)})";
            }
            button.IsEnabled = canEnable;
            if (!canEnable) button.IsChecked = false;
            if (isInstalled) { button.IsChecked = true; button.IsEnabled = true; }
        }

        private void UpdateMapLabelAndButton(TextBlock label, ToggleSwitch button, bool canEnable, bool isInstalled, long downloadSize, long installSize)
        {
            if (canEnable)
            {
                label.Text = $"Maps ({FormatSize(downloadSize)} / {FormatSize(installSize)})";
            }
            else
            {
                label.Foreground = Avalonia.Media.Brushes.Red;
            }
            if (isInstalled)
            {
                label.Foreground = Avalonia.Media.Brushes.LimeGreen;
                label.Text = $"Maps ({FormatSize(installSize)})";
            }
            button.IsEnabled = canEnable;
            if (!canEnable) button.IsChecked = false;
            if (isInstalled) { button.IsChecked = true; button.IsEnabled = true; }
        }

        private async Task<bool> CanBeEnabledAsync(ContentInfo contentInfo, ServerPreference serverPref)
        {
            string url = serverPref == ServerPreference.Primary ? contentInfo.PrimaryUrl : contentInfo.SecondaryUrl;
            bool reachable = await UrlChecker.IsUrlReachableAsync(url);
            return reachable;
        }

        private static (string url, long downloadSize, string format) GetServerInfo(ContentInfo content, ServerPreference pref)
        {
            return pref == ServerPreference.Primary
                ? (content.PrimaryUrl, content.PrimaryDownloadSize, content.PrimaryFormat)
                : (content.SecondaryUrl, content.SecondaryDownloadSize, content.SecondaryFormat);
        }

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

            try
            {
                await ProcessTogglesAsync();
            }
            finally
            {
                isOperationRunning = false;
                DownloadButton.IsEnabled = true;
                PathDetectButton.IsEnabled = true;
                driveUsageUpdater?.SetDriveLetter(addonsPath);
                driveUsageUpdater?.UpdateDriveSizeBar();
            }
        }

        private async Task ProcessTogglesAsync()
        {
            var toggleHandlers = new (ToggleSwitch toggle, ContentInfo? content, TextBlock? label, bool isContent)[]
            {
                (CSSButtonContent, GetContentInfo("CSS Content"), CSSLabelContent, true),
                (CSSButtonMaps, GetContentInfo("CSS Maps"), CSSLabelMaps, false),
                (DODButtonContent, GetContentInfo("DOD Content"), DODLabelContent, true),
                (DODButtonMaps, GetContentInfo("DOD Maps"), DODLabelMaps, false),
                (HL1ButtonContent, GetContentInfo("HL1 Content"), HL1LabelContent, true),
                (HL1ButtonMaps, GetContentInfo("HL1 Maps"), HL1LabelMaps, false),
                (HL2Ep1ButtonContent, GetContentInfo("HL2 Ep1 Content"), HL2Ep1LabelContent, true),
                (HL2Ep1ButtonMaps, GetContentInfo("HL2 Ep1 Maps"), HL2Ep1LabelMaps, false),
                (HL2Ep2ButtonContent, GetContentInfo("HL2 Ep2 Content"), HL2Ep2LabelContent, true),
                (HL2Ep2ButtonMaps, GetContentInfo("HL2 Ep2 Maps"), HL2Ep2LabelMaps, false),
                (HL2ExtrasButtonContent, GetContentInfo("HL2 Extras Content"), HL2ExtrasLabelContent, true),
                (HL2ExtrasButtonMaps, GetContentInfo("HL2 Extras Maps"), HL2ExtrasLabelMaps, false),
                (PortalButtonContent, GetContentInfo("Portal Content"), PortalLabelContent, true),
                (PortalButtonMaps, GetContentInfo("Portal Maps"), PortalLabelMaps, false),
                (Portal2ButtonContent, GetContentInfo("Portal 2 Content"), Portal2LabelContent, true),
                (Portal2ButtonMaps, GetContentInfo("Portal 2 Maps"), Portal2LabelMaps, false),
                (TF2ButtonContent, GetContentInfo("TF2 Content"), TF2LabelContent, true),
                (TF2ButtonMaps, GetContentInfo("TF2 Maps"), TF2LabelMaps, false),
                (L4DButtonContent, GetContentInfo("L4D Content"), L4DLabelContent, true),
                (L4DButtonMaps, GetContentInfo("L4D Maps"), L4DLabelMaps, false),
                (L4D2ButtonContent, GetContentInfo("L4D2 Content"), L4D2LabelContent, true),
            };

            foreach (var (toggle, content, label, isContent) in toggleHandlers)
            {
                if (content == null || toggle.IsChecked == null || label == null) continue;

                bool shouldBeEnabled = toggle.IsChecked == true;
                bool isInstalled = Directory.Exists(Path.Combine(addonsPath!, content.InternalName));

                if (shouldBeEnabled && !isInstalled)
                {
                    await HandleDownloadAndExtractAsync(content, label, preferredServer, isContent);
                    driveUsageUpdater?.UpdateDriveSizeBar(content.InstallSize);
                }
                else if (!shouldBeEnabled && isInstalled)
                {
                    await HandleDeleteAsync(content, label, toggle, preferredServer, isContent);
                    driveUsageUpdater?.UpdateDriveSizeBar(-content.InstallSize);
                }
            }

            await LoadInfoAsync();
        }

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

        private async Task HandleDeleteAsync(ContentInfo content, TextBlock label, ToggleSwitch button, ServerPreference pref, bool isContent)
        {
            if (addonsPath == null) return;

            Log.Information("Deleting {Name}", content.InternalName);
            string contentPath = Path.Combine(addonsPath, content.InternalName);

            if (Directory.Exists(contentPath))
            {
                try
                {
                    await Task.Run(() => Directory.Delete(contentPath, recursive: true));
                    Log.Information("Delete successful for {Name}", content.InternalName);

                    bool canEnable = await CanBeEnabledAsync(content, pref);
                    button.IsChecked = false;
                    button.IsEnabled = canEnable;
                    label.Foreground = Avalonia.Media.Brushes.White;
                    label.Text = isContent ? $"Content ({FormatSize(content.PrimaryDownloadSize)} / {FormatSize(content.InstallSize)})" : $"Maps ({FormatSize(content.PrimaryDownloadSize)} / {FormatSize(content.InstallSize)})";
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Delete failed for {Name}", content.InternalName);
                }
            }
        }

        private enum ServerPreference { Primary, Secondary }

        private void ContentToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (addonsPath == null)
            {
                Log.Warning("No addons path set");
                return;
            }

            try
            {
                long totalChange = 0;

                // Alle Switches durchgehen und kumulative Änderung berechnen
                var allSwitches = new (ToggleSwitch toggle, string key)[]
                {
                    (CSSButtonContent, "CSS Content"),
                    (CSSButtonMaps, "CSS Maps"),
                    (DODButtonContent, "DOD Content"),
                    (DODButtonMaps, "DOD Maps"),
                    (HL1ButtonContent, "HL1 Content"),
                    (HL1ButtonMaps, "HL1 Maps"),
                    (HL2Ep1ButtonContent, "HL2 Ep1 Content"),
                    (HL2Ep1ButtonMaps, "HL2 Ep1 Maps"),
                    (HL2Ep2ButtonContent, "HL2 Ep2 Content"),
                    (HL2Ep2ButtonMaps, "HL2 Ep2 Maps"),
                    (HL2ExtrasButtonContent, "HL2 Extras Content"),
                    (HL2ExtrasButtonMaps, "HL2 Extras Maps"),
                    (PortalButtonContent, "Portal Content"),
                    (PortalButtonMaps, "Portal Maps"),
                    (Portal2ButtonContent, "Portal 2 Content"),
                    (Portal2ButtonMaps, "Portal 2 Maps"),
                    (TF2ButtonContent, "TF2 Content"),
                    (TF2ButtonMaps, "TF2 Maps"),
                    (L4DButtonContent, "L4D Content"),
                    (L4DButtonMaps, "L4D Maps"),
                    (L4D2ButtonContent, "L4D2 Content"),
                };

                foreach (var (toggle, key) in allSwitches)
                {
                    if (!contentInfoDictionary.TryGetValue(key, out ContentInfo? content))
                        continue;

                    bool isInstalled = Directory.Exists(Path.Combine(addonsPath, content.InternalName));
                    bool isChecked = toggle.IsChecked == true;

                    if (isChecked && !isInstalled)
                    {
                        totalChange += content.InstallSize;
                    }
                    else if (!isChecked && isInstalled)
                    {
                        totalChange -= content.InstallSize;
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
    }
}