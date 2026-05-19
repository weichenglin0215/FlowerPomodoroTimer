using System;
using System.Collections.Generic;
using System.Drawing;
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
        /// 各螢幕上次拖曳後記住的圖片中心位置。
        /// Key：螢幕範圍字串（格式 "X,Y,Width,Height"）。
        /// Value：圖片中心在覆蓋視窗 client 座標中的 Point。
        /// </summary>
        public static Dictionary<string, Point> OverlayPositions { get; private set; } = new Dictionary<string, Point>();

        /// <summary>將螢幕 Bounds 轉換為字典鍵值字串。</summary>
        private static string BoundsKey(Rectangle r) => $"{r.X},{r.Y},{r.Width},{r.Height}";

        /// <summary>
        /// 查詢指定螢幕上次記住的圖片中心位置；若無記錄則回傳 null。
        /// </summary>
        public static Point? GetOverlayPosition(Rectangle screenBounds)
        {
            return OverlayPositions.TryGetValue(BoundsKey(screenBounds), out Point p) ? p : (Point?)null;
        }

        /// <summary>
        /// 更新指定螢幕的圖片中心位置並立即寫入設定檔。
        /// </summary>
        public static void SaveOverlayPosition(Rectangle screenBounds, Point center)
        {
            OverlayPositions[BoundsKey(screenBounds)] = center;
            WriteConfig();
        }

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
            OverlayPositions.Clear();
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
                    else if (line.StartsWith("OverlayPos|", StringComparison.OrdinalIgnoreCase))
                    {
                        // 格式：OverlayPos|X,Y,Width,Height=centerX,centerY
                        int eqIdx = line.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            string key = line.Substring("OverlayPos|".Length, eqIdx - "OverlayPos|".Length);
                            string[] parts = line.Substring(eqIdx + 1).Split(',');
                            if (parts.Length == 2 &&
                                int.TryParse(parts[0].Trim(), out int cx) &&
                                int.TryParse(parts[1].Trim(), out int cy))
                            {
                                OverlayPositions[key] = new Point(cx, cy);
                            }
                        }
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
            WriteConfig();
        }

        /// <summary>
        /// 將所有設定（含各螢幕覆蓋位置）寫入設定檔。
        /// 格式：
        ///   Enabled=true/false
        ///   ImageFolderPath=路徑
        ///   OverlayPos|X,Y,Width,Height=centerX,centerY （每個螢幕一行）
        /// </summary>
        private static void WriteConfig()
        {
            try
            {
                var lines = new List<string>
                {
                    $"Enabled={(Enabled ? "true" : "false")}",
                    $"ImageFolderPath={ImageFolderPath}"
                };
                foreach (KeyValuePair<string, Point> kv in OverlayPositions)
                {
                    lines.Add($"OverlayPos|{kv.Key}={kv.Value.X},{kv.Value.Y}");
                }
                File.WriteAllLines(ConfigPath, lines);
            }
            catch { }
        }
    }
}
