using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ZipNest;

namespace ZipNest.SmokeTests
{
    internal static class Program
    {
        private static int _passed;
        private static string _sevenZip;

        private static int Main(string[] args)
        {
            try
            {
                _sevenZip = args.Length > 0 ? Path.GetFullPath(args[0]) : null;
                Run("rejects traversal paths", RejectsTraversalPaths);
                Run("parses 7-Zip technical listings", ParsesTechnicalListing);
                Run("chooses duplicate-safe names", ChoosesDuplicateSafeNames);
                if (_sevenZip != null && File.Exists(_sevenZip))
                {
                    Run("extracts and nests the original archive", ExtractsAndNests);
                    Run("organize removes a redundant wrapper", OrganizeFlattensWrapper);
                    Run("prompts for encrypted archives", ExtractsEncryptedArchive);
                    Run("keeps a bad original and rolls back", RollsBackBadArchive);
                }
                else
                {
                    Console.WriteLine("SKIP end-to-end archive tests (pass path to 7z.exe).");
                }

                Console.WriteLine("PASS: " + _passed + " smoke tests");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception);
                return 1;
            }
        }

        private static void RejectsTraversalPaths()
        {
            AssertThrows<InvalidDataException>(() => PathSafety.ValidateRelativeEntryPath("..\\escape.txt"));
            AssertThrows<InvalidDataException>(() => PathSafety.ValidateRelativeEntryPath("C:\\escape.txt"));
            AssertThrows<InvalidDataException>(() => PathSafety.ValidateRelativeEntryPath("safe.txt:stream"));
            PathSafety.ValidateRelativeEntryPath("folder\\safe.txt");
        }

        private static void ParsesTechnicalListing()
        {
            string listing = "Path = folder/file.txt\r\nSize = 42\r\nPacked Size = 12\r\n\r\nPath = link\r\nSize = 0\r\nSymbolic Link = target\r\n";
            IList<ArchiveEntry> entries = SevenZipRunner.ParseTechnicalList(listing);
            Assert(entries.Count == 2, "Expected two parsed entries.");
            Assert(entries[0].Size == 42, "Expected parsed size.");
            Assert(entries[1].IsLink, "Expected link marker.");
        }

        private static void ChoosesDuplicateSafeNames()
        {
            WithTemporaryDirectory(root =>
            {
                Directory.CreateDirectory(Path.Combine(root, "Demo"));
                string path = PathSafety.GetUniqueDirectory(root, "Demo");
                Assert(path.EndsWith("Demo (2)", StringComparison.Ordinal), "Expected numbered destination.");
            });
        }

        private static void ExtractsAndNests()
        {
            WithTemporaryDirectory(root =>
            {
                string archive = Path.Combine(root, "Sample.zip");
                CreateZip(archive, "hello.txt", "hello");
                ExtractionResult result = CreateService(null).Extract(archive, ExtractionMode.Nest);
                Assert(result.Succeeded, result.Error);
                Assert(File.Exists(Path.Combine(root, "Sample", "hello.txt")), "Extracted file missing.");
                Assert(File.Exists(Path.Combine(root, "Sample", "Sample.zip")), "Original archive was not nested.");
                Assert(!File.Exists(archive), "Original archive remained outside.");

                CreateZip(archive, "second.txt", "second");
                ExtractionResult duplicate = CreateService(null).Extract(archive, ExtractionMode.Nest);
                Assert(duplicate.Succeeded, duplicate.Error);
                Assert(Directory.Exists(Path.Combine(root, "Sample (2)")), "Duplicate destination was not numbered.");
            });
        }

        private static void OrganizeFlattensWrapper()
        {
            WithTemporaryDirectory(root =>
            {
                string source = Path.Combine(root, "source");
                Directory.CreateDirectory(Path.Combine(source, "Widget"));
                File.WriteAllText(Path.Combine(source, "Widget", "readme.txt"), "content");
                string archive = Path.Combine(root, "Widget.zip");
                ZipFile.CreateFromDirectory(source, archive, CompressionLevel.Optimal, false);
                ExtractionResult result = CreateService(null).Extract(archive, ExtractionMode.Organize);
                Assert(result.Succeeded, result.Error);
                Assert(File.Exists(Path.Combine(root, "Widget", "readme.txt")), "Wrapper was not flattened.");
                Assert(!Directory.Exists(Path.Combine(root, "Widget", "Widget")), "Redundant wrapper remains.");
            });
        }

        private static void ExtractsEncryptedArchive()
        {
            WithTemporaryDirectory(root =>
            {
                string input = Path.Combine(root, "secret.txt");
                string archive = Path.Combine(root, "Secret.7z");
                File.WriteAllText(input, "classified");
                RunSevenZip("a -t7z -mhe=on -psecret \"" + archive + "\" \"" + input + "\"");
                File.Delete(input);
                ExtractionResult result = CreateService("secret").Extract(archive, ExtractionMode.Nest);
                Assert(result.Succeeded, result.Error);
                Assert(File.Exists(Path.Combine(root, "Secret", "secret.txt")), "Encrypted file missing.");
            });
        }

        private static void RollsBackBadArchive()
        {
            WithTemporaryDirectory(root =>
            {
                string archive = Path.Combine(root, "Broken.zip");
                File.WriteAllText(archive, "not an archive");
                ExtractionResult result = CreateService(null).Extract(archive, ExtractionMode.Nest);
                Assert(!result.Succeeded, "Bad archive unexpectedly succeeded.");
                Assert(File.Exists(archive), "Bad original was changed.");
                Assert(!Directory.Exists(Path.Combine(root, "Broken")), "Failed extraction left a destination.");
            });
        }

        private static ExtractionService CreateService(string password)
        {
            return new ExtractionService(new SevenZipRunner(_sevenZip), new AppSettings(), new FixedPasswordProvider(password));
        }

        private static void CreateZip(string path, string entryName, string content)
        {
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            using (StreamWriter writer = new StreamWriter(archive.CreateEntry(entryName).Open()))
                writer.Write(content);
        }

        private static void RunSevenZip(string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo
            {
                FileName = _sevenZip,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                process.WaitForExit();
                Assert(process.ExitCode == 0, "7-Zip test setup failed.");
            }
        }

        private static void WithTemporaryDirectory(Action<string> action)
        {
            string path = Path.Combine(Path.GetTempPath(), "ZipNest-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try { action(path); }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }

        private static void Run(string name, Action test)
        {
            test();
            _passed++;
            Console.WriteLine("PASS " + name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private sealed class FixedPasswordProvider : IPasswordProvider
        {
            private readonly string _password;
            public FixedPasswordProvider(string password) { _password = password; }
            public string RequestPassword(string archivePath, int attempt) { return _password; }
        }
    }
}
