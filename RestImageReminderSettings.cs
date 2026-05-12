using System;
using System.IO;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// 休息提醒圖片功能的設定管理（靜態類別）。
    /// 設定檔儲存在應用程式執行目錄下的 RestImageReminder.config，
    /// 格式為純文字鍵值對（Enabled=true/false、ImageFolderPath=路徑）。
    /// </summary>
    public static class RestImageReminderSettings
    {
        /// <summary>設定檔的完整路徑。</summary>
        private static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "RestImageReminder.config");

        /// <summary>休息時顯示圖片的資料夾路徑；空字串表示尚未設定。</summary>
        public static string ImageFolderPath { get; private set; } = string.Empty;

        /// <summary>是否啟用休息圖片提醒功能；預設為 true。</summary>
        public static bool Enabled { get; private set; } = true;

        /// <summary>
        /// 靜態建構子：程式啟動時自動從磁碟讀取一次設定，
        /// 確保其他程式碼取用屬性前已有正確的初始值。
        /// </summary>
        static RestImageReminderSettings()
        {
            Load();
        }

        /// <summary>
        /// 從設定檔讀取並更新靜態屬性值。
        /// 若設定檔不存在則保留預設值（Enabled=true，ImageFolderPath=""）。
        /// 讀取失敗時靜默略過，避免影響主程式運作。
        /// </summary>
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

        /// <summary>
        /// 將指定的設定值寫入靜態屬性，並同步儲存至設定檔。
        /// </summary>
        /// <param name="imageFolderPath">圖片資料夾路徑（前後空白將自動修剪）</param>
        /// <param name="enabled">是否啟用功能</param>
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
