namespace GModContentWizard
{
    internal class Downloader(Guna2ProgressBar progressBar)
    {
        private readonly Guna2ProgressBar _progressBar = progressBar;

        /// <summary>
        /// Downloads a file from the specified URL to the specified temporary path.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="tempPath">The temporary path where the file will be saved.</param>
        /// <returns>A task that represents the asynchronous download operation.</returns>
        public async Task DownloadFileAsync(string url, string fileName, string tempPath)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0L;
            var filePath = Path.Combine(tempPath, fileName);

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                    _progressBar.Value = (int)((totalRead * 100) / totalBytes);
                }
            }

            _progressBar.Value = 0;
        }
    }
}
