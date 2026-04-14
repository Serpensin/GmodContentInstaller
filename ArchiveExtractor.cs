using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;

namespace GModContentWizard
{
    internal static class ArchiveExtractor
    {
        public static async Task ExtractArchiveAsync(string archivePath, string destinationPath, ProgressBar progressBar)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive file not found: {archivePath}");

            Directory.CreateDirectory(destinationPath);

            if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractTarGzAsync(archivePath, destinationPath, progressBar);
            }
            else if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ExtractZip(archivePath, destinationPath, progressBar);
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {archivePath}");
            }

            progressBar.Value = 0;
        }

        private static async Task ExtractTarGzAsync(string archivePath, string destinationPath, ProgressBar progressBar)
        {
            string tarFilePath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(archivePath));

            await using (FileStream originalFileStream = new(archivePath, FileMode.Open, FileAccess.Read))
            await using (FileStream decompressedFileStream = new(tarFilePath, FileMode.Create, FileAccess.Write))
            await using (GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream);
            }

            ExtractTar(tarFilePath, destinationPath, progressBar);
            File.Delete(tarFilePath);
        }

        private static void ExtractTar(string tarFilePath, string destinationPath, ProgressBar progressBar)
        {
            using var archive = TarArchive.Open(tarFilePath);
            ExtractEntriesWithProgress(archive, destinationPath, progressBar);
        }

        private static void ExtractZip(string zipFilePath, string destinationPath, ProgressBar progressBar)
        {
            using var archive = SharpCompress.Archives.Zip.ZipArchive.Open(zipFilePath);
            ExtractEntriesWithProgress(archive, destinationPath, progressBar);
        }

        private static void ExtractEntriesWithProgress(IArchive archive, string destinationPath, ProgressBar progressBar)
        {
            var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
            int totalEntries = entries.Count;
            int extractedEntries = 0;

            foreach (var entry in entries)
            {
                entry.WriteToDirectory(destinationPath, new SharpCompress.Common.ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });

                extractedEntries++;
                SetProgress(progressBar, extractedEntries, totalEntries);
            }
        }

        private static void SetProgress(ProgressBar progressBar, int extracted, int total)
        {
            int percentage = total > 0 ? (int)((double)extracted / total * 100) : 0;
            progressBar.Value = percentage;
        }
    }
}