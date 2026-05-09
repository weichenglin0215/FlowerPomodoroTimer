using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    public class FormUsageAnalysis : Form
    {
        private readonly DateTimePicker m_StartDatePicker = new DateTimePicker();
        private readonly DateTimePicker m_EndDatePicker = new DateTimePicker();
        private readonly Panel m_DailyPanel = new Panel();
        private readonly Panel m_Top20Panel = new Panel();
        private readonly SplitContainer m_Split = new SplitContainer();
        private readonly List<UsageLogEntry> m_Entries = new List<UsageLogEntry>();
        private readonly Color[] m_TableauColors =
        {
            Color.FromArgb(78,121,167), Color.FromArgb(242,142,43), Color.FromArgb(225,87,89),
            Color.FromArgb(118,183,178), Color.FromArgb(89,161,79), Color.FromArgb(237,201,72),
            Color.FromArgb(176,122,161), Color.FromArgb(255,157,167), Color.FromArgb(156,117,95),
            Color.FromArgb(186,176,172)
        };

        private List<(string Date, Dictionary<string, int> ProcessSeconds)> m_DailyData = new();
        private List<(string Process, int Seconds)> m_Top20Data = new();
        private Dictionary<string, Color> m_ColorMap = new(StringComparer.OrdinalIgnoreCase);
        private bool m_SplitHover;
        private readonly Font m_TextFont = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular);

        public FormUsageAnalysis()
        {
            Text = "番茄花鐘-統計分析";
            WindowState = FormWindowState.Maximized;
            BackColor = Color.White;
            InitializeLayout();
            LoadLogs();
            InitializeDateRange();
            RefreshDataAndRedraw();
            this.AutoScaleMode = AutoScaleMode.Dpi;  // 改這行
            // 預設是 AutoScaleMode.Font，有時會造成元件重疊
        }

        private void InitializeLayout()
        {
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 56 };
            topPanel.Controls.Add(new Label { Text = "起始日期", AutoSize = true, Location = new Point(20, 18), Font = m_TextFont });
            topPanel.Controls.Add(new Label { Text = "結束日期", AutoSize = true, Location = new Point(360, 18), Font = m_TextFont });

            m_StartDatePicker.Format = DateTimePickerFormat.Short;
            m_EndDatePicker.Format = DateTimePickerFormat.Short;
            m_StartDatePicker.Font = m_TextFont;
            m_EndDatePicker.Font = m_TextFont;
            m_StartDatePicker.Location = new Point(100, 14);
            m_EndDatePicker.Location = new Point(440, 14);
            m_StartDatePicker.ValueChanged += (_, _) => RefreshDataAndRedraw();
            m_EndDatePicker.ValueChanged += (_, _) => RefreshDataAndRedraw();
            topPanel.Controls.Add(m_StartDatePicker);
            topPanel.Controls.Add(m_EndDatePicker);
            Controls.Add(topPanel);

            m_Split.Dock = DockStyle.Fill;
            m_Split.Orientation = Orientation.Horizontal;
            m_Split.BackColor = Color.LightGray;
            m_Split.SplitterWidth = 8;
            m_Split.Panel1MinSize = 120;
            m_Split.Panel2MinSize = 120;
            m_Split.MouseMove += Split_MouseMove;
            m_Split.MouseLeave += (_, _) =>
            {
                m_SplitHover = false;
                m_Split.BackColor = Color.Gray;
                m_Split.Cursor = Cursors.Default;
            };

            m_DailyPanel.Dock = DockStyle.Fill;
            m_DailyPanel.Paint += DailyPanel_Paint;
            m_Top20Panel.Dock = DockStyle.Fill;
            m_Top20Panel.Paint += Top20Panel_Paint;

            m_Split.Panel1.Controls.Add(m_DailyPanel);
            m_Split.Panel2.Controls.Add(m_Top20Panel);
            Controls.Add(m_Split);

            Shown += (_, _) =>
            {
                m_Split.SplitterDistance = Math.Max(m_Split.Panel1MinSize, m_Split.Height / 3);
            };
        }

        private void Split_MouseMove(object? sender, MouseEventArgs e)
        {
            Rectangle splitterRect = new Rectangle(0, m_Split.SplitterDistance, m_Split.Width, m_Split.SplitterWidth);
            bool hover = splitterRect.Contains(e.Location);
            if (hover != m_SplitHover)
            {
                m_SplitHover = hover;
                m_Split.BackColor = hover ? Color.DimGray : Color.Gray;
                m_Split.Cursor = hover ? Cursors.HSplit : Cursors.Default;
            }
        }

        private void LoadLogs()
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "FlowerPomodoroTimer_Usage.log");
            if (!File.Exists(logPath))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(logPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                try
                {
                    UsageLogEntry? entry = JsonSerializer.Deserialize<UsageLogEntry>(line);
                    if (entry != null)
                    {
                        m_Entries.Add(entry);
                    }
                }
                catch { }
            }
        }

        private void InitializeDateRange()
        {
            DateTime today = DateTime.Today;
            DateTime minDay = m_Entries.Count > 0 ? m_Entries.Min(e => ParseDate(e.Date)) : today;
            m_StartDatePicker.Value = minDay;
            m_EndDatePicker.Value = today;
        }

        private void RefreshDataAndRedraw()
        {
            DateTime start = m_StartDatePicker.Value.Date;
            DateTime end = m_EndDatePicker.Value.Date;
            if (start > end)
            {
                return;
            }

            var filtered = m_Entries.Where(e =>
            {
                DateTime d = ParseDate(e.Date);
                return d >= start && d <= end;
            }).ToList();

            m_DailyData = filtered
                .GroupBy(e => ParseDate(e.Date).ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.SelectMany(x => x.Processes)
                    .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Sum(s => s.Seconds), StringComparer.OrdinalIgnoreCase)))
                .ToList();

            m_Top20Data = filtered
                .SelectMany(e => e.Processes)
                .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.Key, g.Sum(x => x.Seconds)))
                .OrderByDescending(x => x.Item2)
                .Take(20)
                .ToList();

            var processNames = filtered
                .SelectMany(e => e.Processes)
                .Select(p => p.ProcessName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            m_ColorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < processNames.Count; i++)
            {
                m_ColorMap[processNames[i]] = m_TableauColors[i % m_TableauColors.Length];
            }

            m_DailyPanel.Invalidate();
            m_Top20Panel.Invalidate();
        }

        private void DailyPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            g.DrawString("依日期分析", m_TextFont, Brushes.Black, 10, 8);

            if (m_DailyData.Count == 0)
            {
                g.DrawString("無資料", m_TextFont, Brushes.Gray, 20, 40);
                return;
            }

            Rectangle plot = new Rectangle(70, 40, Math.Max(220, m_DailyPanel.Width - 90), Math.Max(140, m_DailyPanel.Height - 70));
            int axisTop = plot.Top + 30;
            int axisBottom = plot.Bottom - 20;

            int maxDaySec = Math.Max(1, m_DailyData.Max(d => d.ProcessSeconds.Values.Sum()));
            // Dynamic Y-axis: at least 1 hour, split into 5 equal steps, minute granularity.
            int stepMinutes = Math.Max(1, (int)Math.Ceiling(Math.Max(3600, maxDaySec) / 60.0 / 6.0));
            int axisMaxSec = stepMinutes * 6 * 60;
            const int tickCount = 7; // 0..5
            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                for (int i = 0; i < (tickCount + 1) ; i++)
                {
                    double ratio = i / (double)(tickCount - 1);
                    int yTick = axisBottom - (int)Math.Round((axisBottom - axisTop) * ratio);
                    g.DrawLine(gridPen, plot.Left, yTick, plot.Right, yTick);
                    int labelSeconds = (int)Math.Round(axisMaxSec * ratio);
                    g.DrawString(FormatDurationLabel(labelSeconds), m_TextFont, Brushes.DimGray, 8, yTick -12);
                }
            }

            int availableWidth = plot.Width - 20;
            int barWidth = Math.Max(12, availableWidth / Math.Max(1, m_DailyData.Count * 2));
            int gap = Math.Max(6, barWidth / 2);
            int totalBarsWidth = m_DailyData.Count * barWidth + Math.Max(0, m_DailyData.Count - 1) * gap;
            int x = plot.Left + Math.Max(0, (availableWidth - totalBarsWidth) / 2) + 10;

            foreach (var day in m_DailyData)
            {
                int cumulativeSec = 0;
                int columnTop = axisBottom;
                foreach (var kv in day.ProcessSeconds.OrderBy(k => k.Key))
                {
                    int prevY = axisBottom - (int)Math.Round((cumulativeSec / (double)axisMaxSec) * (axisBottom - axisTop));
                    cumulativeSec += kv.Value;
                    int newY = axisBottom - (int)Math.Round((cumulativeSec / (double)axisMaxSec) * (axisBottom - axisTop));
                    int h = Math.Max(1, prevY - newY);
                    Color c = m_ColorMap.TryGetValue(kv.Key, out Color cc) ? cc : Color.Gray;
                    using Brush b = new SolidBrush(c);
                    g.FillRectangle(b, x, newY, barWidth, h);
                    columnTop = Math.Min(columnTop, newY);
                }
                g.DrawRectangle(Pens.Black, x, columnTop, barWidth, axisBottom - columnTop);
                g.DrawString(day.Date.Substring(5), m_TextFont, Brushes.Black, x - 16, plot.Bottom - 18);
                x += barWidth + gap;
            }
        }

        private void Top20Panel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            g.DrawString("前20名軟體使用量", m_TextFont, Brushes.Black, 10, 8);

            if (m_Top20Data.Count == 0)
            {
                g.DrawString("無資料", m_TextFont, Brushes.Gray, 20, 40);
                return;
            }

            Rectangle plot = new Rectangle(220, 36, Math.Max(220, m_Top20Panel.Width - 240), Math.Max(120, m_Top20Panel.Height - 50));
            int maxSec = Math.Max(1, m_Top20Data.Max(x => x.Seconds));
            int rowH = Math.Max(18, (plot.Height - 10) / m_Top20Data.Count);
            int y = plot.Top + 2;

            foreach (var item in m_Top20Data)
            {
                int barW = (int)Math.Round((item.Seconds / (double)maxSec) * (plot.Width - 20));
                Color c = m_ColorMap.TryGetValue(item.Process, out Color cc) ? cc : Color.Gray;
                using Brush b = new SolidBrush(c);
                g.FillRectangle(b, plot.Left, y, barW, rowH - 3);
                g.DrawString(item.Process, m_TextFont, Brushes.Black, 10, y);

                string timeText = TimeSpan.FromSeconds(item.Seconds).ToString(@"hh\:mm\:ss");
                SizeF textSize = g.MeasureString(timeText, m_TextFont);
                float textY = y + ((rowH - 3) - textSize.Height) / 2f;
                Brush textBrush = barW > textSize.Width + 10 ? Brushes.White : Brushes.Black;
                float textX = barW > textSize.Width + 10 ? plot.Left + 6 : plot.Left + barW + 4;
                g.DrawString(timeText, m_TextFont, textBrush, textX, textY);

                y += rowH;
            }
        }

        private static DateTime ParseDate(string date)
        {
            return DateTime.TryParse(date, out DateTime dt) ? dt.Date : DateTime.Today;
        }

        private static string FormatDurationLabel(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            int totalMinutes = (int)Math.Round(totalSeconds / 60.0);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}:{minutes:00}";
        }
    }

    public class UsageLogEntry
    {
        public string Date { get; set; } = "";
        public string GeneratedAt { get; set; } = "";
        public int TotalAppSeconds { get; set; }
        public int TotalActiveWindowSeconds { get; set; }
        public List<UsageProcessEntry> Processes { get; set; } = new List<UsageProcessEntry>();
    }

    public class UsageProcessEntry
    {
        public string ProcessName { get; set; } = "";
        public int Seconds { get; set; }
    }
}
