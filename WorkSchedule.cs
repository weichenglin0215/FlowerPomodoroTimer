using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Flower_Pomodoro_Timer
{
    public class WorkSchedule
    {
        public DayOfWeek? Day { get; set; } // null 代表 everyday
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Task { get; set; }

        public List<WorkSchedule> LoadWorkSchedules(string filePath)
        {
            var schedules = new List<WorkSchedule>();
            if (!File.Exists(filePath))
            {
                // 自動建立 WorkList.txt 並寫入預設內容
                File.WriteAllText(filePath,
                @"# 範例格式：星期,開始時間,結束時間,工作項目
                # 例如：Monday,08:00,12:00,早上工作
                # null 代表每天
                null,09:00,12:00,上午工作
                null,13:00,18:00,下午工作
                ");
            }
            var lines = System.IO.File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(' ');
                if (parts.Length < 3) continue;

                // 處理星期
                DayOfWeek? day = null;
                if (parts[0].ToLower() != "everyday")
                {
                    day = parts[0].ToLower() switch
                    {
                        "monday" => DayOfWeek.Monday,
                        "tuesday" => DayOfWeek.Tuesday,
                        "wednesday" => DayOfWeek.Wednesday,
                        "thursday" => DayOfWeek.Thursday,
                        "friday" => DayOfWeek.Friday,
                        "saturday" => DayOfWeek.Saturday,
                        "sunday" => DayOfWeek.Sunday,
                        _ => null
                    };
                }

                // 處理時間
                var timeRange = parts[1].Split('~');
                if (timeRange.Length != 2) continue;
                if (!TimeSpan.TryParseExact(timeRange[0], "hh\\:mm", CultureInfo.InvariantCulture, out var start)) continue;
                if (!TimeSpan.TryParseExact(timeRange[1], "hh\\:mm", CultureInfo.InvariantCulture, out var end)) continue;

                // 處理工作項目
                var task = string.Join(" ", parts.Skip(2));

                schedules.Add(new WorkSchedule
                {
                    Day = day,
                    Start = start,
                    End = end,
                    Task = task
                });
            }
            return schedules;
        }
    }
}
