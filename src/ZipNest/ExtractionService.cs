using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ZipNest
{
    internal sealed class ExtractionService
    {
        private readonly SevenZipRunner _sevenZip;
        private readonly AppSettings _settings;
        private readonly IPasswordProvider _passwordProvider;

        public ExtractionService(SevenZipRunner sevenZip, AppSettings settings, IPasswordProvider passwordProvider)
        {
            _sevenZip = sevenZip;
            _settings = settings;
            _passwordProvider = passwordProvider;
        }

        public ExtractionResult Extract(string archivePath, ExtractionMode mode)
        {
            var result = new ExtractionResult { ArchivePath = archivePath };
            string stagingPath = null;
            string finalPath = null;
            try
            {
                archivePath = Path.GetFullPath(archivePath);
                if (!File.Exists(archivePath))
                    throw new FileNotFoundException("The archive no longer exists.", archivePath);
                if (!IsSupportedArchive(archivePath))
                    throw new NotSupportedException("ZipNest supports ZIP, 7z, and RAR archives.");

                using (Mutex mutex = AcquireArchiveMutex(archivePath))
                {
                    if (mutex == null)
                        throw new IOException("This archive is already being processed by ZipNest.");

                    string parent = Path.GetDirectoryName(archivePath);
                    string baseName = PathSafety.GetArchiveBaseName(archivePath);
                    finalPath = PathSafety.GetUniqueDirectory(parent, baseName);
                    stagingPath = Path.Combine(parent, "." + baseName + ".zipnest-" + Guid.NewGuid().ToString("N") + ".tmp");
                    Directory.CreateDirectory(stagingPath);
                    File.SetAttributes(stagingPath, File.GetAttributes(stagingPath) | FileAttributes.Hidden);

                    string password = null;
                    IList<ArchiveEntry> entries = ListWithPasswordRetry(archivePath, ref password);
                    PathSafety.ValidateArchiveEntries(entries, _settings);

                    ProcessResult extraction = _sevenZip.Extract(archivePath, stagingPath, password);
                    if (extraction.ExitCode != 0 && SevenZipRunner.IndicatesPasswordRequired(extraction))
                    {
                        password = RequestPassword(archivePath, password == null ? 1 : 2);
                        extraction = _sevenZip.Extract(archivePath, stagingPath, password);
                    }
                    if (extraction.ExitCode != 0)
                        throw new InvalidDataException(SummarizeSevenZipError(extraction));

                    PathSafety.VerifyExtractedTree(stagingPath);
                    if (mode == ExtractionMode.Organize)
                        FlattenSingleRedundantRoot(stagingPath, baseName);

                    File.SetAttributes(stagingPath, File.GetAttributes(stagingPath) & ~FileAttributes.Hidden);
                    Directory.Move(stagingPath, finalPath);
                    stagingPath = null;
                    try
                    {
                        string nestedArchivePath = PathSafety.GetUniqueFilePath(finalPath, Path.GetFileName(archivePath));
                        File.Move(archivePath, nestedArchivePath);
                    }
                    catch
                    {
                        stagingPath = finalPath;
                        throw;
                    }

                    result.Succeeded = true;
                    result.DestinationPath = finalPath;
                    Log.Write("Extracted " + archivePath + " to " + finalPath + " using " + mode + ".");
                }
            }
            catch (OperationCanceledException)
            {
                result.Error = "Password entry was canceled.";
                Log.Write("Canceled: " + archivePath);
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
                Log.Write("Failed " + archivePath + ": " + exception);
            }
            finally
            {
                if (stagingPath != null)
                    TryDeleteDirectory(stagingPath);
            }
            return result;
        }

        private IList<ArchiveEntry> ListWithPasswordRetry(string archivePath, ref string password)
        {
            ProcessResult listResult;
            IList<ArchiveEntry> entries = _sevenZip.List(archivePath, password, out listResult);
            if (listResult.ExitCode == 0)
                return entries;
            if (!SevenZipRunner.IndicatesPasswordRequired(listResult))
                throw new InvalidDataException(SummarizeSevenZipError(listResult));

            password = RequestPassword(archivePath, 1);
            entries = _sevenZip.List(archivePath, password, out listResult);
            if (listResult.ExitCode != 0)
                throw new InvalidDataException(SummarizeSevenZipError(listResult));
            return entries;
        }

        private string RequestPassword(string archivePath, int attempt)
        {
            string password = _passwordProvider.RequestPassword(archivePath, attempt);
            if (password == null)
                throw new OperationCanceledException();
            return password;
        }

        private static void FlattenSingleRedundantRoot(string stagingPath, string archiveBaseName)
        {
            string[] entries = Directory.GetFileSystemEntries(stagingPath);
            if (entries.Length != 1 || !Directory.Exists(entries[0]))
                return;

            string wrapper = entries[0];
            string normalizedArchiveName = NormalizeName(archiveBaseName);
            string normalizedWrapperName = NormalizeName(Path.GetFileName(wrapper));
            if (!normalizedArchiveName.Equals(normalizedWrapperName, StringComparison.OrdinalIgnoreCase))
                return;

            foreach (string child in Directory.GetFileSystemEntries(wrapper))
            {
                string destination = Path.Combine(stagingPath, Path.GetFileName(child));
                if (Directory.Exists(child))
                    Directory.Move(child, destination);
                else
                    File.Move(child, destination);
            }
            Directory.Delete(wrapper);
        }

        private static string NormalizeName(string value)
        {
            return new string(value.Where(character => char.IsLetterOrDigit(character)).ToArray());
        }

        private static Mutex AcquireArchiveMutex(string archivePath)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(archivePath.ToUpperInvariant()));
                string name = "Local\\ZipNest-" + BitConverter.ToString(hash).Replace("-", string.Empty);
                bool created;
                var mutex = new Mutex(true, name, out created);
                if (created)
                    return mutex;
                mutex.Dispose();
                return null;
            }
        }

        private static bool IsSupportedArchive(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".7z", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".rar", StringComparison.OrdinalIgnoreCase);
        }

        private static string SummarizeSevenZipError(ProcessResult result)
        {
            string[] lines = result.CombinedOutput
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith("7-Zip", StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToArray();
            return lines.Length == 0
                ? "7-Zip could not read this archive (exit code " + result.ExitCode + ")."
                : string.Join(Environment.NewLine, lines);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception exception)
            {
                Log.Write("Could not remove staging folder " + path + ": " + exception.Message);
            }
        }
    }
}
