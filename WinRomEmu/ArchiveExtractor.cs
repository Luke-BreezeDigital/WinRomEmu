using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace WinRomEmu
{
    public class ArchiveHandler
    {
        private static readonly HashSet<string> ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "zip", "rar", "7z", "tar", "gz", "gzip", "bz2", "bzip2",
            "lz", "lzma", "xz", "iso", "cab", "arj", "z"
        };

        public class ArchiveResult
        {
            public bool Success { get; set; }
            public string? RomPath { get; set; }
            public bool UseOriginalArchive { get; set; }
        }

        public class ExtractionProgress
        {
            public double Percentage { get; set; }
            public string CurrentFile { get; set; } = string.Empty;
            public long CurrentBytes { get; set; }
            public long TotalBytes { get; set; }
        }

        public static async Task<ArchiveResult> HandleArchive(string archivePath, IEnumerable<string> supportedExtensions)
        {
            var extension = Path.GetExtension(archivePath).TrimStart('.').ToLower();

            // Check if file is an archive
            if (!IsArchive(extension))
            {
                return new ArchiveResult { Success = true, RomPath = archivePath, UseOriginalArchive = true };
            }

            // Check for existing ROM files before showing extraction prompt
            var extractPath = Path.GetDirectoryName(archivePath)!;
            var archiveNameWithoutExt = Path.GetFileNameWithoutExtension(archivePath);

            // Handle .tar.gz type extensions
            if (archiveNameWithoutExt.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
            {
                archiveNameWithoutExt = Path.GetFileNameWithoutExtension(archiveNameWithoutExt);
            }

            // First, check for existing ROM files in the same directory
            var existingRomFiles = FindRomFiles(extractPath, supportedExtensions)
                .Where(file => Path.GetFileNameWithoutExtension(file).Equals(
                    archiveNameWithoutExt,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            // If we found a matching ROM file, use it directly and ensure archive name matches
            if (existingRomFiles.Count == 1)
            {
                await EnsureArchiveNameMatchesRom(archivePath, existingRomFiles.First());
                return new ArchiveResult { Success = true, RomPath = existingRomFiles.First(), UseOriginalArchive = false };
            }

            // If multiple matching ROMs found, show warning and let user handle it manually
            if (existingRomFiles.Count > 1)
            {
                MessageBox.Show(
                    "Multiple ROM files matching this archive name were found. " +
                    "Please navigate to the directory and run the desired ROM file directly.",
                    "Multiple ROM Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return new ArchiveResult { Success = false };
            }

            // No existing ROM found, show extraction prompt
            var result = MessageBox.Show(
                "The selected file has been detected as an archive and the extracted files have not been found. " +
                "Would you like to extract the contents to the same directory?\r\n\r\n" +
                "Some emulator's can handle archives directly; Select 'Yes' to extract, 'No' to attempt to load " +
                "the archive file into the emulator or 'Cancel' to exit.\r\n\r\n" +
                "Please do not modify the filename or path as this process will be automatically skipped in the " +
                "future if the extracted files are detected.",
                "Archive Detected",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    try
                    {
                        var progressWindow = new ExtractionProgressWindow();
                        progressWindow.Show();

                        var progress = new Progress<ExtractionProgress>(p =>
                        {
                            progressWindow.UpdateProgress(p.Percentage, p.CurrentFile);
                        });

                        await ExtractArchiveAsync(archivePath, progress);

                        progressWindow.Close();

                        // Find ROM files in the extracted contents
                        var romFiles = FindRomFiles(extractPath, supportedExtensions)
                            .Where(file => Path.GetFileNameWithoutExtension(file).Equals(
                                archiveNameWithoutExt,
                                StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!romFiles.Any())
                        {
                            MessageBox.Show(
                                "No compatible ROM files were found in the extracted archive. " +
                                "Please check the extracted contents and try to run the ROM file directly.",
                                "No Compatible Files",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return new ArchiveResult { Success = false };
                        }

                        if (romFiles.Count() > 1)
                        {
                            MessageBox.Show(
                                "Multiple compatible ROM files were found in the extracted archive. " +
                                "Please navigate to the extracted directory and run the desired ROM file directly.",
                                "Multiple ROM Files",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return new ArchiveResult { Success = false };
                        }

                        // Single ROM file found - ensure archive name matches
                        var romPath = romFiles.First();
                        await EnsureArchiveNameMatchesRom(archivePath, romPath);
                        return new ArchiveResult { Success = true, RomPath = romPath, UseOriginalArchive = false };
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to extract archive: {ex.Message}\r\n{ex.StackTrace}",
                            "Extraction Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return new ArchiveResult { Success = false };
                    }

                case MessageBoxResult.No:
                    return new ArchiveResult { Success = true, RomPath = archivePath, UseOriginalArchive = true };

                default:
                    return new ArchiveResult { Success = false };
            }
        }

        private static async Task EnsureArchiveNameMatchesRom(string archivePath, string romPath)
        {
            try
            {
                var romNameWithoutExt = Path.GetFileNameWithoutExtension(romPath);
                var archiveExt = Path.GetExtension(archivePath);
                var newArchivePath = Path.Combine(Path.GetDirectoryName(archivePath)!, romNameWithoutExt + archiveExt);

                if (archivePath != newArchivePath)
                {
                    // If a file with the new name already exists, delete it first
                    if (File.Exists(newArchivePath))
                    {
                        File.Delete(newArchivePath);
                    }

                    // Rename the archive to match the ROM name
                    await Task.Run(() => File.Move(archivePath, newArchivePath));
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this is not critical functionality
                Console.WriteLine($"Failed to rename archive: {ex.Message}");
            }
        }

        private static async Task ExtractArchiveAsync(string archivePath, IProgress<ExtractionProgress> progress)
        {
            var extractPath = Path.GetDirectoryName(archivePath);
            var extension = Path.GetExtension(archivePath).TrimStart('.').ToLower();
            var fullName = Path.GetFileName(archivePath).ToLower();

            // Handle special case for .tar.gz, .tar.bz2, etc.
            if (fullName.Contains(".tar."))
            {
                extension = "tar";
            }

            switch (extension)
            {
                case "zip":
                    using (var archive = ZipArchive.Open(archivePath))
                    {
                        await ExtractEntriesAsync(archive, extractPath!, progress);
                    }
                    break;

                case "rar":
                    using (var archive = RarArchive.Open(archivePath))
                    {
                        await ExtractEntriesAsync(archive, extractPath!, progress);
                    }
                    break;

                case "7z":
                    using (var archive = SevenZipArchive.Open(archivePath))
                    {
                        await ExtractEntriesAsync(archive, extractPath!, progress);
                    }
                    break;

                case "tar":
                    using (var archive = TarArchive.Open(archivePath))
                    {
                        await ExtractEntriesAsync(archive, extractPath!, progress);
                    }
                    break;

                case "gz":
                case "gzip":
                    using (var archive = GZipArchive.Open(archivePath))
                    {
                        await ExtractEntriesAsync(archive, extractPath!, progress);
                    }
                    break;

                case "bz2":
                case "bzip2":
                case "lz":
                case "lzma":
                case "xz":
                    await Task.Run(() =>
                    {
                        using (Stream stream = File.OpenRead(archivePath))
                        using (var reader = ReaderFactory.Open(stream))
                        {
                            while (reader.MoveToNextEntry())
                            {
                                if (!reader.Entry.IsDirectory)
                                {
                                    var entryName = reader.Entry.Key;
                                    progress.Report(new ExtractionProgress
                                    {
                                        Percentage = 0,
                                        CurrentFile = entryName,
                                        CurrentBytes = 0,
                                        TotalBytes = 100
                                    });

                                    reader.WriteEntryToDirectory(extractPath!, new ExtractionOptions
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = false,
                                        PreserveFileTime = true,
                                        PreserveAttributes = true
                                    });
                                }
                            }
                        }
                    });
                    break;

                default:
                    try
                    {
                        using (var archive = ArchiveFactory.Open(archivePath))
                        {
                            await ExtractEntriesAsync(archive, extractPath!, progress);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new NotSupportedException($"Archive type '{extension}' is not supported. Error: {ex.Message}");
                    }
                    break;
            }
        }

        private static async Task ExtractEntriesAsync(IArchive archive, string extractPath, IProgress<ExtractionProgress> progress)
        {
            // Calculate total decompressed size
            long totalBytes = archive.Entries.Where(e => !e.IsDirectory).Sum(e => e.Size);

            // Create a cancellation token source for the progress monitoring
            using var cts = new System.Threading.CancellationTokenSource();

            // Start the extraction in a separate task
            var extractionTask = Task.Run(() =>
            {
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    entry.WriteToDirectory(extractPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            });

            // Create a dictionary to track output files and their expected sizes
            var outputFiles = archive.Entries
                .Where(e => !e.IsDirectory)
                .ToDictionary(
                    e => Path.Combine(extractPath, e.Key.Replace('/', Path.DirectorySeparatorChar)),
                    e => e.Size
                );

            // Start progress monitoring task
            var progressTask = Task.Run(async () =>
            {
                while (!extractionTask.IsCompleted)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;

                    // Calculate current total extracted size
                    long currentBytes = 0;
                    foreach (var file in outputFiles)
                    {
                        if (File.Exists(file.Key))
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file.Key);
                                currentBytes += fileInfo.Length;
                            }
                            catch (IOException)
                            {
                                // File might be locked for writing, skip it
                                continue;
                            }
                        }
                    }

                    // Calculate and report progress
                    var percentage = (double)currentBytes / totalBytes * 100;
                    percentage = Math.Min(99.98, percentage); // Cap at 99% until fully complete

                    progress.Report(new ExtractionProgress
                    {
                        Percentage = percentage,
                        CurrentFile = String.Join(',', outputFiles.Select(x => Path.GetFileName(x.Key))),
                        CurrentBytes = currentBytes,
                        TotalBytes = totalBytes
                    });

                    await Task.Delay(300);
                }

                // Report 100% completion
                progress.Report(new ExtractionProgress
                {
                    Percentage = 100,
                    CurrentFile = "Extraction complete",
                    CurrentBytes = totalBytes,
                    TotalBytes = totalBytes
                });
            });

            // Wait for both tasks to complete
            try
            {
                await Task.WhenAll(extractionTask, progressTask);
            }
            finally
            {
                cts.Cancel(); // Ensure the progress monitoring stops
            }
        }

        private static bool IsArchive(string extension)
        {
            if (extension.StartsWith("tar.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return ArchiveExtensions.Contains(extension.TrimStart('.'));
        }

        private static IEnumerable<string> FindRomFiles(string directory, IEnumerable<string> supportedExtensions)
        {
            var cleanExtensions = supportedExtensions
                .Select(ext => ext.Trim().TrimStart('.', '*').ToLower())
                .ToHashSet();

            return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(file => cleanExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLower()));
        }
    }
}