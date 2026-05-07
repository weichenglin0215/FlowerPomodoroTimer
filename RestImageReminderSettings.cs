using System;
using System.IO;

namespace Flower_Pomodoro_Timer
{
    public static class RestImageReminderSettings
    {
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "RestImageReminder.config");

        public static string ImageFolderPath { get; private set; } = string.Empty;
        public static bool Enabled { get; private set; } = true;

        static RestImageReminderSettings()
        {
            Load();
        }

        public static void Load()
        {
            ImageFolderPath = string.Empty;
            Enabled = true;
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    if (line.StartsWith("Enabled=", StringComparison.OrdinalIgnoreCase))
                    {
                        Enabled = line.EndsWith("true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (line.StartsWith("ImageFolderPath=", StringComparison.OrdinalIgnoreCase))
                    {
                        ImageFolderPath = line.Substring("ImageFolderPath=".Length).Trim();
                    }
                }
            }
            catch { }
        }

        public static void Save(string imageFolderPath, bool enabled)
        {
            ImageFolderPath = imageFolderPath?.Trim() ?? string.Empty;
            Enabled = enabled;
            string[] lines =
            {
                $"Enabled={(Enabled ? "true" : "false")}",
                $"ImageFolderPath={ImageFolderPath}"
            };
            File.WriteAllLines(ConfigPath, lines);
        }
    }
}
