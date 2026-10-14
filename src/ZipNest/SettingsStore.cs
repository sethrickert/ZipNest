using System;
using System.Globalization;
using System.IO;

namespace ZipNest
{
    internal sealed class SettingsStore
    {
        private readonly string _path;

        public SettingsStore()
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            bool portable = File.Exists(Path.Combine(executableDirectory, "ZipNest.portable"));
            string directory = portable
                ? executableDirectory
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZipNest");
            _path = Path.Combine(directory, "settings.ini");
        }

        public AppSettings Load()
        {
            var settings = new AppSettings();
            if (!File.Exists(_path))
                return settings;

            try
            {
                foreach (string line in File.ReadAllLines(_path))
                {
                    int separator = line.IndexOf('=');
                    if (separator < 1)
                        continue;

                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    if (key.Equals("DefaultMode", StringComparison.OrdinalIgnoreCase))
                    {
                        ExtractionMode mode;
                        if (Enum.TryParse(value, true, out mode))
                            settings.DefaultMode = mode;
                    }
                    else if (key.Equals("MaximumExpandedBytes", StringComparison.OrdinalIgnoreCase))
                    {
                        long bytes;
                        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out bytes) && bytes > 0)
                            settings.MaximumExpandedBytes = bytes;
                    }
                    else if (key.Equals("MaximumEntries", StringComparison.OrdinalIgnoreCase))
                    {
                        int count;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) && count > 0)
                            settings.MaximumEntries = count;
                    }
                }
            }
            catch (IOException)
            {
                // Corrupt or inaccessible settings never prevent extraction; defaults remain active.
            }

            return settings;
        }

        public void Save(AppSettings settings)
        {
            string directory = Path.GetDirectoryName(_path);
            Directory.CreateDirectory(directory);
            string temporary = _path + ".tmp";
            File.WriteAllLines(temporary, new[]
            {
                "DefaultMode=" + settings.DefaultMode,
                "MaximumExpandedBytes=" + settings.MaximumExpandedBytes.ToString(CultureInfo.InvariantCulture),
                "MaximumEntries=" + settings.MaximumEntries.ToString(CultureInfo.InvariantCulture)
            });
            if (File.Exists(_path))
                File.Replace(temporary, _path, null);
            else
                File.Move(temporary, _path);
        }
    }
}
