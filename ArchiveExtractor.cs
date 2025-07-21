using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using System.IO.Compression;

namespace GModContentWizard
{
    internal class ArchiveExtractor
    {
        /// <summary>
        /// Extracts a .gz, .tar.gz, or .zip archive file to the specified destination path and updates the progress bar.
        /// </summary>
        /// <param name="archivePath">The path to the archive file.</param>
        /// <param name="destinationPath">The path where the contents of the archive will be extracted.</param>
        /// <param name="progressBar">The progress bar to update during extraction.</param>
        /// <exception cref="FileNotFoundException">Thrown when the archive file is not found.</exception>
        /// <exception cref="NotSupportedException">Thrown when the archive format is not supported.</exception>
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, Guna2ProgressBar progressBar)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Archive file not found: {archivePath}");
            }

            Directory.CreateDirectory(destinationPath);

            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                string tarFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archivePath));

                await using (FileStream originalFileStream = new(archivePath, FileMode.Open, FileAccess.Read))
                await using (FileStream decompressedFileStream = new(tarFilePath, FileMode.Create, FileAccess.Write))
                await using (GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress))
                {
                    await decompressionStream.CopyToAsync(decompressedFileStream);
                }

                using (var archive = TarArchive.Open(tarFilePath))
                {
                    var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
                    var totalEntries = entries.Count;
                    var extractedEntries = 0;

                    foreach (var entry in entries)
                    {
                        entry.WriteToDirectory(destinationPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });

                        extractedEntries++;

                        int percentage = (int)((double)extractedEntries / totalEntries * 100);
                        progressBar.Value = percentage;
                    }
                }

                File.Delete(tarFilePath);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(archivePath);
                var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
                var totalEntries = entries.Count;
                var extractedEntries = 0;

                foreach (var entry in entries)
                {
                    entry.WriteToDirectory(destinationPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });

                    extractedEntries++;

                    int percentage = (int)((double)extractedEntries / totalEntries * 100);
                    progressBar.Value = percentage;
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {archivePath}");
            }

            progressBar.Value = 0;
        }
    }
}
