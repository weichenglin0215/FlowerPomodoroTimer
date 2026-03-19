using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Flower_Pomodoro_Timer
{
    public class WorkSchedule
    {
        public DayOfWeek? Day { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string Task { get; set; } = string.Empty;
        // 其他属性或方法可以根据需要添加
        public List<WorkSchedule> LoadWorkSchedules(string filePath)
        {
            EnsureDefaultFile(filePath);

            var schedules = new List<WorkSchedule>();
            foreach (string rawLine in File.ReadLines(filePath))
            {
                string line = rawLine.Trim();
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
        // CSV格式: Day,Start,End,Task
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
        // 解析Day字段，支持null（每天）和Monday..Sunday
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
        // 如果文件不存在，创建一个默认的工作计划文件
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
