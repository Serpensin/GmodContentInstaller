using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;

namespace GModContentWizard
{
    internal class ArchiveExtractor
    {
        /// <summary>
        /// Extracts a .gz archive file to the specified destination path and updates the progress bar.
        /// </summary>
        /// <param name="archivePath">The path to the .gz archive file.</param>
        /// <param name="destinationPath">The path where the contents of the archive will be extracted.</param>
        /// <param name="progressBar">The progress bar to update during extraction.</param>
        /// <exception cref="FileNotFoundException">Thrown when the archive file is not found.</exception>
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, Guna2ProgressBar progressBar)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Archive file not found: {archivePath}");
            }

            Directory.CreateDirectory(destinationPath);

            string tarFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archivePath));

            // Step 1: Decompress the .gz file to a .tar file
            await using (FileStream originalFileStream = new(archivePath, FileMode.Open, FileAccess.Read))
            await using (FileStream decompressedFileStream = new(tarFilePath, FileMode.Create, FileAccess.Write))
            await using (GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream);
            }

            // Step 2: Open the .tar file and count the entries
            using (var archive = TarArchive.Open(tarFilePath))
            {
                var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
                var totalEntries = entries.Count;
                var extractedEntries = 0;

                foreach (var entry in entries)
                {
                    // Extract the entry
                    entry.WriteToDirectory(destinationPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });

                    // Increment the count of extracted entries
                    extractedEntries++;

                    // Calculate and update progress bar directly
                    int percentage = (int)((double)extractedEntries / totalEntries * 100);
                    progressBar.Value = percentage;
                }
            }

            // Cleanup: Delete the .tar file
            File.Delete(tarFilePath);
            progressBar.Value = 0;
        }
    }
}
