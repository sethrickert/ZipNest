using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ZipNest
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                return Run(args ?? new string[0]);
            }
            catch (Exception exception)
            {
                Log.Write("Fatal: " + exception);
                MessageBox.Show(exception.Message, "ZipNest", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            string executable = Application.ExecutablePath;
            if (HasSwitch(args, "--register-defaults"))
            {
                RegistryIntegration.RegisterApplication(executable);
                return 0;
            }
            if (HasSwitch(args, "--install-integration"))
            {
                RegistryIntegration.RegisterApplication(executable);
                RegistryIntegration.InstallExplorerIntegration(executable);
                return 0;
            }
            if (HasSwitch(args, "--remove-integration"))
            {
                RegistryIntegration.RemoveExplorerIntegration();
                return 0;
            }
            if (HasSwitch(args, "--unregister"))
            {
                RegistryIntegration.UnregisterApplication();
                return 0;
            }
            if (HasSwitch(args, "--open-default-apps"))
            {
                RegistryIntegration.OpenDefaultApps();
                return 0;
            }

            var store = new SettingsStore();
            AppSettings settings = store.Load();
            List<string> archives = args.Where(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToList();
            if (archives.Count == 0 || HasSwitch(args, "--settings"))
            {
                Application.Run(new SettingsForm(store, settings));
                return 0;
            }

            ExtractionMode mode = HasSwitch(args, "--organize")
                ? ExtractionMode.Organize
                : HasSwitch(args, "--nest") ? ExtractionMode.Nest : settings.DefaultMode;
            string sevenZip = ResolveSevenZipPath();
            var service = new ExtractionService(new SevenZipRunner(sevenZip), settings, new DialogPasswordProvider());
            var failures = new List<ExtractionResult>();
            foreach (string archive in archives)
            {
                ExtractionResult result = service.Extract(archive, mode);
                if (!result.Succeeded && result.Error != "Password entry was canceled.")
                    failures.Add(result);
            }

            if (failures.Count > 0)
            {
                string message = string.Join(Environment.NewLine + Environment.NewLine,
                    failures.Select(failure => Path.GetFileName(failure.ArchivePath) + Environment.NewLine + failure.Error));
                MessageBox.Show(message, failures.Count == 1 ? "ZipNest could not extract this archive" : "ZipNest could not extract some archives",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            return 0;
        }

        private static string ResolveSevenZipPath()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7zip", "7z.exe");
            if (!File.Exists(path))
                throw new FileNotFoundException("ZipNest's archive engine is missing. Reinstall ZipNest or download the portable package again.", path);
            return path;
        }

        private static bool HasSwitch(IEnumerable<string> args, string value)
        {
            return args.Any(argument => argument.Equals(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
