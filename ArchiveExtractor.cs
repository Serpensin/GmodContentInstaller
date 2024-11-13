﻿using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;

namespace GModContentWizard
{
    internal class ArchiveExtractor
    {
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, Guna2ProgressBar progressBar)
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Archive file not found: {archivePath}");
            }

            Directory.CreateDirectory(destinationPath);

            string tarFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archivePath));

            // Step 1: Decompress the .gz file to a .tar file
            using (FileStream originalFileStream = new(archivePath, FileMode.Open, FileAccess.Read))
            using (FileStream decompressedFileStream = new(tarFilePath, FileMode.Create, FileAccess.Write))
            using (GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream);
            }

            // Step 2: Open the .tar file and count the entries
            using (var archive = TarArchive.Open(tarFilePath))
            {
                var totalEntries = archive.Entries.Count(entry => !entry.IsDirectory);
                var extractedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
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
            }

            // Cleanup: Delete the .tar file
            progressBar.Value = 0;
            File.Delete(tarFilePath);
        }
    }
}