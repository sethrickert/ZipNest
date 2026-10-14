using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZipNest
{
    internal static class RegistryIntegration
    {
        private const string ApplicationName = "ZipNest";
        private const string ClassesRoot = @"Software\Classes";
        private const string CapabilitiesPath = @"Software\ZipNest\Capabilities";
        private static readonly string[] Extensions = { ".zip", ".7z", ".rar" };

        public static bool IsExplorerIntegrationInstalled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ClassesRoot + @"\SystemFileAssociations\.zip\shell\ZipNest.Nest"))
                return key != null;
        }

        public static void RegisterApplication(string executablePath)
        {
            string quotedExecutable = Quote(executablePath);
            using (RegistryKey applications = Registry.CurrentUser.CreateSubKey(ClassesRoot + @"\Applications\ZipNest.exe"))
            {
                applications.SetValue("FriendlyAppName", ApplicationName);
                using (RegistryKey command = applications.CreateSubKey(@"shell\open\command"))
                    command.SetValue(null, quotedExecutable + " \"%1\"");
                using (RegistryKey supported = applications.CreateSubKey("SupportedTypes"))
                {
                    foreach (string extension in Extensions)
                        supported.SetValue(extension, string.Empty);
                }
            }

            RegisterProgId("ZipNest.zip", "ZIP archive", quotedExecutable, @"%SystemRoot%\system32\zipfldr.dll");
            RegisterProgId("ZipNest.7z", "7z archive", quotedExecutable, null);
            RegisterProgId("ZipNest.rar", "RAR archive", quotedExecutable, null);

            using (RegistryKey capabilities = Registry.CurrentUser.CreateSubKey(CapabilitiesPath))
            {
                capabilities.SetValue("ApplicationName", ApplicationName);
                capabilities.SetValue("ApplicationDescription", "Quietly extracts archives into tidy folders and keeps the original archive inside.");
                using (RegistryKey associations = capabilities.CreateSubKey("FileAssociations"))
                {
                    associations.SetValue(".zip", "ZipNest.zip");
                    associations.SetValue(".7z", "ZipNest.7z");
                    associations.SetValue(".rar", "ZipNest.rar");
                }
            }
            using (RegistryKey registeredApplications = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
                registeredApplications.SetValue(ApplicationName, CapabilitiesPath);

            foreach (string extension in Extensions)
            {
                using (RegistryKey openWith = Registry.CurrentUser.CreateSubKey(ClassesRoot + @"\" + extension + @"\OpenWithProgids"))
                    openWith.SetValue("ZipNest" + extension, new byte[0], RegistryValueKind.None);
            }
            RefreshExplorerAssociations();
        }

        public static void InstallExplorerIntegration(string executablePath)
        {
            foreach (string extension in Extensions)
            {
                string shellPath = ClassesRoot + @"\SystemFileAssociations\" + extension + @"\shell";
                CreateVerb(shellPath, "ZipNest.Nest", "Extract & Nest", executablePath, "--nest");
                CreateVerb(shellPath, "ZipNest.Organize", "Extract & Organize", executablePath, "--organize");
            }
            RefreshExplorerAssociations();
        }

        public static void RemoveExplorerIntegration()
        {
            foreach (string extension in Extensions)
            {
                string shellPath = ClassesRoot + @"\SystemFileAssociations\" + extension + @"\shell";
                Registry.CurrentUser.DeleteSubKeyTree(shellPath + @"\ZipNest.Nest", false);
                Registry.CurrentUser.DeleteSubKeyTree(shellPath + @"\ZipNest.Organize", false);
            }
            RefreshExplorerAssociations();
        }

        public static void UnregisterApplication()
        {
            RemoveExplorerIntegration();
            Registry.CurrentUser.DeleteSubKeyTree(ClassesRoot + @"\Applications\ZipNest.exe", false);
            Registry.CurrentUser.DeleteSubKeyTree(ClassesRoot + @"\ZipNest.zip", false);
            Registry.CurrentUser.DeleteSubKeyTree(ClassesRoot + @"\ZipNest.7z", false);
            Registry.CurrentUser.DeleteSubKeyTree(ClassesRoot + @"\ZipNest.rar", false);
            Registry.CurrentUser.DeleteSubKeyTree(CapabilitiesPath, false);
            using (RegistryKey registeredApplications = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true))
            {
                if (registeredApplications != null)
                    registeredApplications.DeleteValue(ApplicationName, false);
            }
            foreach (string extension in Extensions)
            {
                using (RegistryKey openWith = Registry.CurrentUser.OpenSubKey(ClassesRoot + @"\" + extension + @"\OpenWithProgids", true))
                {
                    if (openWith != null)
                        openWith.DeleteValue("ZipNest" + extension, false);
                }
            }
            RefreshExplorerAssociations();
        }

        public static void OpenDefaultApps()
        {
            string uri = "ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(ApplicationName);
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Win32Exception)
            {
                Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
            }
        }

        private static void RegisterProgId(string progId, string description, string quotedExecutable, string icon)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ClassesRoot + @"\" + progId))
            {
                key.SetValue(null, description);
                key.SetValue("FriendlyTypeName", description);
                if (icon != null)
                {
                    using (RegistryKey iconKey = key.CreateSubKey("DefaultIcon"))
                        iconKey.SetValue(null, icon, RegistryValueKind.ExpandString);
                }
                using (RegistryKey command = key.CreateSubKey(@"shell\open\command"))
                    command.SetValue(null, quotedExecutable + " \"%1\"");
            }
        }

        private static void CreateVerb(string shellPath, string keyName, string label, string executablePath, string argument)
        {
            using (RegistryKey verb = Registry.CurrentUser.CreateSubKey(shellPath + @"\" + keyName))
            {
                verb.SetValue(null, label);
                verb.SetValue("Icon", Quote(executablePath) + ",0");
                verb.SetValue("MultiSelectModel", "Player");
                using (RegistryKey command = verb.CreateSubKey("command"))
                    command.SetValue(null, Quote(executablePath) + " " + argument + " %*");
            }
        }

        private static string Quote(string path)
        {
            return "\"" + path.Replace("\"", "\\\"") + "\"";
        }

        private static void RefreshExplorerAssociations()
        {
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
