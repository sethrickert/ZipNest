using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZipNest
{
    internal static class PathSafety
    {
        private static readonly char[] InvalidFileNameCharacters = Path.GetInvalidFileNameChars();

        public static void ValidateArchiveEntries(IList<ArchiveEntry> entries, AppSettings settings)
        {
            if (entries.Count > settings.MaximumEntries)
                throw new InvalidDataException("The archive contains too many entries (" + entries.Count + ").");

            long expandedBytes = 0;
            foreach (ArchiveEntry entry in entries)
            {
                ValidateRelativeEntryPath(entry.Path);
                if (entry.IsLink)
                    throw new InvalidDataException("The archive contains a symbolic or hard link, which ZipNest blocks for safety: " + entry.Path);

                try
                {
                    expandedBytes = checked(expandedBytes + Math.Max(0, entry.Size));
                }
                catch (OverflowException)
                {
                    throw new InvalidDataException("The archive reports an invalid expanded size.");
                }
            }

            if (expandedBytes > settings.MaximumExpandedBytes)
                throw new InvalidDataException("The archive would expand beyond ZipNest's " + FormatBytes(settings.MaximumExpandedBytes) + " safety limit.");
        }

        internal static void ValidateRelativeEntryPath(string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath))
                throw new InvalidDataException("The archive contains an empty path.");

            string normalized = entryPath.Replace('/', '\\');
            if (normalized.StartsWith("\\", StringComparison.Ordinal) ||
                Path.IsPathRooted(normalized) ||
                normalized.IndexOf(':') >= 0)
                throw new InvalidDataException("The archive contains an unsafe absolute or alternate-stream path: " + entryPath);

            string[] segments = normalized.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".." || segment == "."))
                throw new InvalidDataException("The archive contains an unsafe traversal path: " + entryPath);
            if (segments.Any(segment => segment.IndexOfAny(InvalidFileNameCharacters) >= 0))
                throw new InvalidDataException("The archive contains a path Windows cannot safely create: " + entryPath);
        }

        public static void VerifyExtractedTree(string root)
        {
            string canonicalRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
            foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
            {
                string canonical = Path.GetFullPath(path);
                if (!canonical.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Extraction attempted to write outside its staging folder.");
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("The extracted archive contains a reparse point, which ZipNest blocks for safety.");
            }
        }

        public static string GetUniqueDirectory(string parent, string baseName)
        {
            string candidate = Path.Combine(parent, baseName);
            if (!Directory.Exists(candidate) && !File.Exists(candidate))
                return candidate;
            for (int index = 2; index < 10000; index++)
            {
                candidate = Path.Combine(parent, baseName + " (" + index + ")");
                if (!Directory.Exists(candidate) && !File.Exists(candidate))
                    return candidate;
            }
            throw new IOException("Could not choose an unused destination folder.");
        }

        public static string GetUniqueFilePath(string directory, string fileName)
        {
            string candidate = Path.Combine(directory, fileName);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;

            string baseName = Path.GetFileNameWithoutExtension(fileName) + " (original)";
            string extension = Path.GetExtension(fileName);
            candidate = Path.Combine(directory, baseName + extension);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
            for (int index = 2; index < 10000; index++)
            {
                candidate = Path.Combine(directory, baseName + " (" + index + ")" + extension);
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }
            throw new IOException("Could not choose an unused name for the original archive.");
        }

        public static string GetArchiveBaseName(string archivePath)
        {
            string fileName = Path.GetFileName(archivePath);
            foreach (string compoundExtension in new[] { ".tar.gz", ".tar.bz2", ".tar.xz" })
            {
                if (fileName.EndsWith(compoundExtension, StringComparison.OrdinalIgnoreCase))
                    return fileName.Substring(0, fileName.Length - compoundExtension.Length);
            }
            return Path.GetFileNameWithoutExtension(fileName);
        }

        private static string EnsureTrailingSeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string FormatBytes(long bytes)
        {
            return (bytes / (1024L * 1024 * 1024)).ToString() + " GB";
        }
    }
}
