using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;

namespace GModContentWizard
{
    /// <summary>
    /// Handles extraction of zip and tar.gz archives with progress reporting.
    /// </summary>
    internal static class ArchiveExtractor
    {
        private static Button? _downloadButton;
        private static TextBlock? _statusText;
        private static int _dotCount = 0;
        private static System.Timers.Timer? _dotTimer;

        /// <summary>
        /// Extracts an archive (zip or tar.gz) to the destination directory with progress updates.
        /// </summary>
        /// <param name="archivePath">Path to the archive file.</param>
        /// <param name="destinationPath">Directory to extract to.</param>
        /// <param name="progressBar">The progress bar to update.</param>
        /// <param name="downloadButton">Optional button to hide during extraction.</param>
        /// <param name="statusText">Optional text block to show status.</param>
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, ProgressBar progressBar, Button? downloadButton = null, TextBlock? statusText = null)
        {
            _downloadButton = downloadButton;
            _statusText = statusText;
            
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive file not found: {archivePath}");

            Directory.CreateDirectory(destinationPath);

            Dispatcher.UIThread.Post(() =>
            {
                progressBar.IsVisible = true;
                if (_downloadButton != null) _downloadButton.IsVisible = false;
                if (_statusText != null)
                {
                    _statusText.IsVisible = true;
                    _statusText.Text = "Extracting.";
                }
            });

            StartDotAnimation();

            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractTarGzAsync(archivePath, destinationPath, progressBar);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractZipAsync(archivePath, destinationPath, progressBar);
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {archivePath}");
            }

            StopDotAnimation();
            Dispatcher.UIThread.Post(() =>
            {
                progressBar.Value = 0;
                progressBar.IsVisible = false;
                if (_downloadButton != null)
                {
                    _downloadButton.IsVisible = true;
                    _downloadButton.IsEnabled = true;
                }
                if (_statusText != null)
                {
                    _statusText.IsVisible = false;
                }
            });
        }

        /// <summary>
        /// Extracts an archive with default null parameters.
        /// </summary>
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, ProgressBar progressBar)
        {
            await ExtractArchiveAsync(archivePath, destinationPath, progressBar, null, null);
        }

        /// <summary>
        /// Starts the animated dot display during extraction.
        /// </summary>
        private static void StartDotAnimation()
        {
            _dotTimer = new System.Timers.Timer(500);
            _dotTimer.Elapsed += (s, e) =>
            {
                _dotCount = (_dotCount + 1) % 4;
                var dots = new string('.', _dotCount);
                Dispatcher.UIThread.Post(() =>
                {
                    if (_statusText != null)
                        _statusText.Text = $"Extracting{dots}";
                });
            };
            _dotTimer.Start();
        }

        /// <summary>
        /// Stops the extraction dot animation.
        /// </summary>
        private static void StopDotAnimation()
        {
            _dotTimer?.Stop();
            _dotTimer?.Dispose();
            _dotTimer = null;
        }

        /// <summary>
        /// Extracts a .tar.gz archive by decompressing to tar first, then extracting.
        /// </summary>
        private static async Task ExtractTarGzAsync(string archivePath, string destinationPath, ProgressBar progressBar)
        {
            string tarFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archivePath));

            await using (FileStream originalFileStream = new(archivePath, FileMode.Open, FileAccess.Read))
            await using (FileStream decompressedFileStream = new(tarFilePath, FileMode.Create, FileAccess.Write))
            await using (GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream);
            }

            await ExtractTarAsync(tarFilePath, destinationPath, progressBar);
            File.Delete(tarFilePath);
        }

        /// <summary>
        /// Extracts a tar archive using SharpCompress.
        /// </summary>
        private static async Task ExtractTarAsync(string tarFilePath, string destinationPath, ProgressBar progressBar)
        {
            await Task.Run(() =>
            {
                using var archive = TarArchive.Open(tarFilePath);
                ExtractEntriesWithProgress(archive, destinationPath, progressBar);
            });
        }

        /// <summary>
        /// Extracts a zip archive using SharpCompress.
        /// </summary>
        private static async Task ExtractZipAsync(string zipFilePath, string destinationPath, ProgressBar progressBar)
        {
            await Task.Run(() =>
            {
                using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(zipFilePath);
                ExtractEntriesWithProgress(archive, destinationPath, progressBar);
            });
        }

        /// <summary>
        /// Extracts all entries from an archive with progress updates.
        /// </summary>
        private static void ExtractEntriesWithProgress(IArchive archive, string destinationPath, ProgressBar progressBar)
        {
            var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
            int totalEntries = entries.Count;
            int extractedEntries = 0;

            foreach (var entry in entries)
            {
                entry.WriteToDirectory(destinationPath, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });

                extractedEntries++;
                SetProgress(progressBar, extractedEntries, totalEntries);
            }
        }

        /// <summary>
        /// Updates the progress bar with the current extraction percentage.
        /// </summary>
        private static void SetProgress(ProgressBar progressBar, int extracted, int total)
        {
            int percentage = total > 0 ? (int)((double)extracted / total * 100) : 0;
            
            Dispatcher.UIThread.Post(() =>
            {
                progressBar.Value = percentage;
            });
        }
    }
}