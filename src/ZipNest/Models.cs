using System;
using System.Collections.Generic;

namespace ZipNest
{
    internal enum ExtractionMode
    {
        Nest,
        Organize
    }

    internal sealed class AppSettings
    {
        public ExtractionMode DefaultMode { get; set; } = ExtractionMode.Nest;
        public long MaximumExpandedBytes { get; set; } = 50L * 1024 * 1024 * 1024;
        public int MaximumEntries { get; set; } = 250000;
    }

    internal sealed class ArchiveEntry
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public bool IsLink { get; set; }
    }

    internal sealed class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public string CombinedOutput
        {
            get { return (StandardOutput ?? string.Empty) + Environment.NewLine + (StandardError ?? string.Empty); }
        }
    }

    internal sealed class ExtractionResult
    {
        public string ArchivePath { get; set; }
        public string DestinationPath { get; set; }
        public bool Succeeded { get; set; }
        public string Error { get; set; }
    }

    internal interface IPasswordProvider
    {
        string RequestPassword(string archivePath, int attempt);
    }
}
