using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace ZipNest
{
    internal sealed class SevenZipRunner
    {
        private readonly string _executablePath;

        public SevenZipRunner(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
                throw new ArgumentNullException(nameof(executablePath));
            _executablePath = executablePath;
        }

        public IList<ArchiveEntry> List(string archivePath, string password, out ProcessResult result)
        {
            result = Run(BuildArguments("l", archivePath, null, password, "-slt", "-ba", "-sccUTF-8"), password);
            if (result.ExitCode != 0)
                return new List<ArchiveEntry>();

            return ParseTechnicalList(result.StandardOutput);
        }

        public ProcessResult Extract(string archivePath, string destinationPath, string password)
        {
            return Run(BuildArguments("x", archivePath, destinationPath, password,
                "-y", "-aoa", "-bb0", "-bd", "-sccUTF-8"), password);
        }

        private static string BuildArguments(string command, string archivePath, string destinationPath, string password, params string[] switches)
        {
            var arguments = new StringBuilder();
            arguments.Append(command);
            foreach (string value in switches)
                arguments.Append(' ').Append(value);
            if (password != null)
                arguments.Append(" -p").Append(Quote(password));
            if (destinationPath != null)
                arguments.Append(" -o").Append(Quote(destinationPath));
            arguments.Append(" -- ").Append(Quote(archivePath));
            return arguments.ToString();
        }

        private ProcessResult Run(string arguments, string password)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(_executablePath)
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                var standardOutput = new StringBuilder();
                var standardError = new StringBuilder();
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        standardOutput.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                        standardError.AppendLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.StandardInput.Close();
                process.WaitForExit();
                process.WaitForExit(); // Flush asynchronous output handlers on .NET Framework.

                return new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = standardOutput.ToString(),
                    StandardError = standardError.ToString()
                };
            }
        }

        internal static IList<ArchiveEntry> ParseTechnicalList(string output)
        {
            var entries = new List<ArchiveEntry>();
            ArchiveEntry current = null;
            using (var reader = new StringReader(output ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        if (current != null && !string.IsNullOrEmpty(current.Path))
                            entries.Add(current);
                        current = null;
                        continue;
                    }

                    int separator = line.IndexOf(" = ", StringComparison.Ordinal);
                    if (separator < 0)
                        continue;
                    string key = line.Substring(0, separator);
                    string value = line.Substring(separator + 3);
                    if (key == "Path")
                    {
                        if (current != null && !string.IsNullOrEmpty(current.Path))
                            entries.Add(current);
                        current = new ArchiveEntry { Path = value };
                    }
                    else if (current != null && key == "Size")
                    {
                        long size;
                        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out size))
                            current.Size = size;
                    }
                    else if (current != null && (key == "Symbolic Link" || key == "Hard Link"))
                    {
                        current.IsLink = true;
                    }
                }
            }
            if (current != null && !string.IsNullOrEmpty(current.Path))
                entries.Add(current);
            return entries;
        }

        internal static bool IndicatesPasswordRequired(ProcessResult result)
        {
            string text = result == null ? string.Empty : result.CombinedOutput;
            return text.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("encrypted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("Wrong password", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
