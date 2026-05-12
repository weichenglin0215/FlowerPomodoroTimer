using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// 代表一筆工作排程，記錄特定星期幾（或每天）的工作時段與任務名稱。
    /// 同時提供從 CSV 文字檔讀取排程清單的功能。
    /// CSV 格式：Day,Start,End,Task（例如 "Monday,09:00,12:00,上午工作"）
    /// </summary>
    public class WorkSchedule
    {
        /// <summary>適用的星期幾；null 表示每天皆適用（CSV 中填 "null" 或 "everyday"）。</summary>
        public DayOfWeek? Day { get; set; }

        /// <summary>工作開始時間（格式 HH:mm）。</summary>
        public TimeSpan Start { get; set; }

        /// <summary>工作結束時間（格式 HH:mm）。</summary>
        public TimeSpan End { get; set; }

        /// <summary>任務名稱，顯示給使用者的提示文字。</summary>
        public string Task { get; set; } = string.Empty;

        /// <summary>
        /// 從指定路徑讀取工作排程 CSV 檔，若檔案不存在則自動建立預設範本。
        /// 忽略空白行及以 # 開頭的註解行。
        /// </summary>
        /// <param name="filePath">CSV 排程檔的完整路徑</param>
        /// <returns>解析成功的 WorkSchedule 清單</returns>
        public List<WorkSchedule> LoadWorkSchedules(string filePath)
        {
            EnsureDefaultFile(filePath);

            var schedules = new List<WorkSchedule>();
            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
                // 跳過空行與 # 開頭的註解行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                if (TryParseCsv(line, out WorkSchedule? schedule) && schedule is not null)
                {
                    schedules.Add(schedule);
                }
            }

            return schedules;
        }

        /// <summary>
        /// 嘗試將一行 CSV 文字解析為 WorkSchedule 物件。
        /// 格式：Day,Start,End,Task（最多分割為 4 欄）
        /// 結束時間必須嚴格晚於開始時間，否則視為無效資料。
        /// </summary>
        /// <param name="line">一行 CSV 文字</param>
        /// <param name="schedule">解析成功時輸出的物件；失敗時為 null</param>
        /// <returns>解析是否成功</returns>
        private static bool TryParseCsv(string line, out WorkSchedule? schedule)
        {
            schedule = null;

            string[] parts = line.Split(',', 4, StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                return false;
            }

            if (!TryParseDay(parts[0], out DayOfWeek? day))
            {
                return false;
            }

            if (!TimeSpan.TryParseExact(parts[1], @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan start))
            {
                return false;
            }

            if (!TimeSpan.TryParseExact(parts[2], @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan end))
            {
                return false;
            }

            // 結束時間不得早於或等於開始時間
            if (end <= start)
            {
                return false;
            }

            schedule = new WorkSchedule
            {
                Day = day,
                Start = start,
                End = end,
                Task = parts[3]
            };

            return true;
        }

        /// <summary>
        /// 解析 Day 欄位。
        /// 支援 "null" 或 "everyday"（代表每天），以及 Monday ~ Sunday 的英文名稱。
        /// </summary>
        /// <param name="input">Day 欄位的原始字串</param>
        /// <param name="day">解析結果；"null/everyday" 時輸出 null，指定星期時輸出對應列舉值</param>
        /// <returns>解析是否成功</returns>
        private static bool TryParseDay(string input, out DayOfWeek? day)
        {
            string token = input.Trim();
            if (token.Equals("null", StringComparison.OrdinalIgnoreCase)
                || token.Equals("everyday", StringComparison.OrdinalIgnoreCase))
            {
                day = null;
                return true;
            }

            if (Enum.TryParse(token, true, out DayOfWeek parsed))
            {
                day = parsed;
                return true;
            }

            day = null;
            return false;
        }

        /// <summary>
        /// 若指定路徑的排程檔不存在，自動建立包含預設上午／下午工作時段的範本 CSV 檔，
        /// 並同時建立所需的目錄結構。
        /// </summary>
        /// <param name="filePath">目標檔案路徑</param>
        private static void EnsureDefaultFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            string[] defaultLines =
            {
                "# 格式: Day,Start,End,Task",
                "# Day 可用 Monday..Sunday 或 null (每天)",
                "null,09:00,12:00,上午工作",
                "null,13:00,18:00,下午工作"
            };

            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory);
            File.WriteAllLines(filePath, defaultLines);
        }
    }
}
