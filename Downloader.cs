using Avalonia.Controls;
using Avalonia.Threading;

namespace GModContentWizard
{
    /// <summary>
    /// Handles downloading files from URLs with progress reporting and cancellation support.
    /// </summary>
    internal class Downloader(ProgressBar progressBar, Button downloadButton, TextBlock statusText)
    {
        private readonly ProgressBar _progressBar = progressBar;
        private readonly Button _downloadButton = downloadButton;
        private readonly TextBlock _statusText = statusText;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _dotCount = 0;
        private System.Timers.Timer? _dotTimer;

        /// <summary>
        /// Downloads a file from the specified URL to the temp path.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="fileName">The name of the file to save as.</param>
        /// <param name="tempPath">The directory to save the file in.</param>
        public async Task DownloadFileAsync(string url, string fileName, string tempPath)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Dispatcher.UIThread.Post(() =>
            {
                _progressBar.IsVisible = true;
                _downloadButton.IsVisible = false;
                _statusText.IsVisible = true;
                _statusText.Text = "Downloading.";
            });

            StartDotAnimation();

            using var client = new HttpClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0L;
            var filePath = Path.Combine(tempPath, fileName);

            using var contentStream = await response.Content.ReadAsStreamAsync(token);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            try
            {
                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    totalRead += bytesRead;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _progressBar.Value = (int)((totalRead * 100) / totalBytes);
                    });
                }
            }
            finally
            {
                StopDotAnimation();
                Dispatcher.UIThread.Post(() =>
                {
                    _progressBar.Value = 0;
                    _progressBar.IsVisible = false;
                    _downloadButton.IsVisible = true;
                    _downloadButton.IsEnabled = true;
                    _statusText.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Cancels the current download operation.
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Starts an animated dot display to indicate ongoing download.
        /// </summary>
        private void StartDotAnimation()
        {
            _dotTimer = new System.Timers.Timer(500);
            _dotTimer.Elapsed += (s, e) =>
            {
                _dotCount = (_dotCount + 1) % 4;
                var dots = new string('.', _dotCount);
                Dispatcher.UIThread.Post(() =>
                {
                    _statusText.Text = $"Downloading{dots}";
                });
            };
            _dotTimer.Start();
        }

        /// <summary>
        /// Stops the dot animation timer.
        /// </summary>
        private void StopDotAnimation()
        {
            _dotTimer?.Stop();
            _dotTimer?.Dispose();
            _dotTimer = null;
        }
    }
}