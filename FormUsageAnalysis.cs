using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// 使用情形統計分析視窗。
    /// 讀取應用程式每日寫入的 JSON 使用紀錄（FlowerPomodoroTimer_Usage.log），
    /// 以日期範圍篩選後，呈現兩種圖表：
    /// 上方：依日期的堆疊長條圖（各程序佔比）
    /// 下方：前 20 名程序的水平長條圖（總使用時間）
    /// </summary>
    public class FormUsageAnalysis : Form
    {
        // ── UI 控制項 ──────────────────────────────────────────────
        private readonly DateTimePicker m_StartDatePicker = new DateTimePicker();
        private readonly DateTimePicker m_EndDatePicker = new DateTimePicker();
        /// <summary>上方：每日堆疊長條圖的繪圖畫布。</summary>
        private readonly Panel m_DailyPanel = new Panel();
        /// <summary>下方：前 20 名水平長條圖的繪圖畫布。</summary>
        private readonly Panel m_Top20Panel = new Panel();
        /// <summary>可調整比例的上下分割容器。</summary>
        private readonly SplitContainer m_Split = new SplitContainer();

        // ── 資料 ───────────────────────────────────────────────────
        /// <summary>從 log 檔讀取的全部原始記錄。</summary>
        private readonly List<UsageLogEntry> m_Entries = new List<UsageLogEntry>();

        /// <summary>
        /// 固定色盤（仿 Tableau 10），依程序名稱字母順序循環分配顏色，
        /// 確保相同程序在不同日期的長條顏色一致。
        /// </summary>
        private readonly Color[] m_TableauColors =
        {
            Color.FromArgb(78,121,167), Color.FromArgb(242,142,43), Color.FromArgb(225,87,89),
            Color.FromArgb(118,183,178), Color.FromArgb(89,161,79), Color.FromArgb(237,201,72),
            Color.FromArgb(176,122,161), Color.FromArgb(255,157,167), Color.FromArgb(156,117,95),
            Color.FromArgb(186,176,172)
        };

        /// <summary>依日期篩選後的每日資料（日期字串 → 各程序秒數字典）。</summary>
        private List<(string Date, Dictionary<string, int> ProcessSeconds)> m_DailyData = new();
        /// <summary>前 20 名程序的總秒數（降冪排列）。</summary>
        private List<(string Process, int Seconds)> m_Top20Data = new();
        /// <summary>程序名稱 → 顏色的對應表，供兩張圖表共用以保持一致性。</summary>
        private Dictionary<string, Color> m_ColorMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>滑鼠是否懸停在分割線上（用於切換游標樣式）。</summary>
        private bool m_SplitHover;
        /// <summary>共用字型（微軟正黑體 12pt），用於所有文字繪製。</summary>
        private readonly Font m_TextFont = new Font("Microsoft JhengHei UI", 12F, FontStyle.Regular);

        public FormUsageAnalysis()
        {
            Text = "番茄花鐘-統計分析";
            AutoScaleMode = AutoScaleMode.Dpi;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.White;
            InitializeLayout();
            LoadLogs();
            InitializeDateRange();
            RefreshDataAndRedraw();
        }

        /// <summary>
        /// 建立視窗版面配置：
        /// - 頂部工具列：起始/結束日期選擇器
        /// - 中央 SplitContainer：上方每日圖、下方 Top20 圖
        /// </summary>
        private void InitializeLayout()
        {
            // ── 頂部工具列 ─────────────────────────────────────────
            FlowLayoutPanel topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(20, 12, 20, 8),
                WrapContents = false,
                AutoScroll = true
            };
            topPanel.Controls.Add(new Label { Text = "起始日期", AutoSize = true, Margin = new Padding(0, 5, 8, 0), Font = m_TextFont });

            m_StartDatePicker.Format = DateTimePickerFormat.Short;
            m_EndDatePicker.Format = DateTimePickerFormat.Short;
            m_StartDatePicker.Font = m_TextFont;
            m_EndDatePicker.Font = m_TextFont;
            m_StartDatePicker.Margin = new Padding(0, 0, 32, 0);
            m_EndDatePicker.Margin = new Padding(0);
            // 任一日期改變時立即重新計算並刷新圖表
            m_StartDatePicker.ValueChanged += (_, _) => RefreshDataAndRedraw();
            m_EndDatePicker.ValueChanged += (_, _) => RefreshDataAndRedraw();
            topPanel.Controls.Add(m_StartDatePicker);
            topPanel.Controls.Add(new Label { Text = "結束日期", AutoSize = true, Margin = new Padding(0, 5, 8, 0), Font = m_TextFont });
            topPanel.Controls.Add(m_EndDatePicker);
            Controls.Add(topPanel);

            // ── 分割容器 ───────────────────────────────────────────
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

            // ── 繪圖畫布 ───────────────────────────────────────────
            m_DailyPanel.Dock = DockStyle.Fill;
            m_DailyPanel.Paint += DailyPanel_Paint;
            m_Top20Panel.Dock = DockStyle.Fill;
            m_Top20Panel.Paint += Top20Panel_Paint;

            m_Split.Panel1.Controls.Add(m_DailyPanel);
            m_Split.Panel2.Controls.Add(m_Top20Panel);
            Controls.Add(m_Split);

            // 視窗顯示後才能取得真實高度，此時設定分割位置為 1/3
            Shown += (_, _) =>
            {
                m_Split.SplitterDistance = Math.Max(m_Split.Panel1MinSize, m_Split.Height / 3);
            };
        }

        /// <summary>
        /// 處理分割線上的滑鼠移動：懸停時改變背景色與游標，離開時還原。
        /// </summary>
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

        /// <summary>
        /// 從 FlowerPomodoroTimer_Usage.log 逐行讀取 JSON 使用紀錄，
        /// 解析為 UsageLogEntry 並存入 m_Entries。
        /// 解析失敗的行靜默略過，不影響其他紀錄。
        /// </summary>
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

        /// <summary>
        /// 根據 m_Entries 中最早的紀錄日期設定起始日期選擇器，
        /// 結束日期預設為今天，確保圖表初始顯示完整的歷史資料。
        /// </summary>
        private void InitializeDateRange()
        {
            DateTime today = DateTime.Today;
            DateTime minDay = m_Entries.Count > 0 ? m_Entries.Min(e => ParseDate(e.Date)) : today;
            m_StartDatePicker.Value = minDay;
            m_EndDatePicker.Value = today;
        }

        /// <summary>
        /// 依日期選擇器的範圍篩選紀錄，重新計算每日資料與 Top20 資料，
        /// 並更新顏色對應表後觸發兩個畫布重繪。
        /// 若起始日期晚於結束日期則不執行任何動作。
        /// </summary>
        private void RefreshDataAndRedraw()
        {
            DateTime start = m_StartDatePicker.Value.Date;
            DateTime end = m_EndDatePicker.Value.Date;
            if (start > end)
            {
                return;
            }

            // 篩選落在指定日期範圍內的紀錄
            var filtered = m_Entries.Where(e =>
            {
                DateTime d = ParseDate(e.Date);
                return d >= start && d <= end;
            }).ToList();

            // 每日資料：依日期分組，各程序秒數加總（不分大小寫）
            m_DailyData = filtered
                .GroupBy(e => ParseDate(e.Date).ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.SelectMany(x => x.Processes)
                    .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.Sum(s => s.Seconds), StringComparer.OrdinalIgnoreCase)))
                .ToList();

            // Top20 資料：所有程序跨日加總，取前 20 名
            m_Top20Data = filtered
                .SelectMany(e => e.Processes)
                .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.Key, g.Sum(x => x.Seconds)))
                .OrderByDescending(x => x.Item2)
                .Take(20)
                .ToList();

            // 依程序名稱字母順序建立顏色對應表，確保同一程序顏色固定
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

        /// <summary>
        /// 繪製上方每日堆疊長條圖。
        /// Y 軸動態調整：至少 1 小時，以 6 等分刻度向上對齊至分鐘整數。
        /// 每根長條依程序名稱字母順序堆疊各程序的使用時間色塊。
        /// </summary>
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

            // 繪圖區域（左側留空顯示 Y 軸標籤，底部留空顯示 X 軸日期）
            Rectangle plot = new Rectangle(70, 40, Math.Max(220, m_DailyPanel.Width - 90), Math.Max(140, m_DailyPanel.Height - 70));
            int axisTop = plot.Top + 30;
            int axisBottom = plot.Bottom - 20;

            // 動態計算 Y 軸最大值：至少 1 小時，每格步進為分鐘整數，共 6 等分
            int maxDaySec = Math.Max(1, m_DailyData.Max(d => d.ProcessSeconds.Values.Sum()));
            int stepMinutes = Math.Max(1, (int)Math.Ceiling(Math.Max(3600, maxDaySec) / 60.0 / 6.0));
            int axisMaxSec = stepMinutes * 6 * 60;
            const int tickCount = 7; // 含 0 共 7 條水平格線

            // 繪製水平格線與 Y 軸時間標籤
            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                for (int i = 0; i < (tickCount + 1); i++)
                {
                    double ratio = i / (double)(tickCount - 1);
                    int yTick = axisBottom - (int)Math.Round((axisBottom - axisTop) * ratio);
                    g.DrawLine(gridPen, plot.Left, yTick, plot.Right, yTick);
                    int labelSeconds = (int)Math.Round(axisMaxSec * ratio);
                    g.DrawString(FormatDurationLabel(labelSeconds), m_TextFont, Brushes.DimGray, 8, yTick - 12);
                }
            }

            // 計算每根長條與間距的寬度
            int availableWidth = plot.Width - 20;
            int barWidth = Math.Max(12, availableWidth / Math.Max(1, m_DailyData.Count * 2));
            int gap = Math.Max(6, barWidth / 2);
            int totalBarsWidth = m_DailyData.Count * barWidth + Math.Max(0, m_DailyData.Count - 1) * gap;
            int x = plot.Left + Math.Max(0, (availableWidth - totalBarsWidth) / 2) + 10;

            // 逐日繪製堆疊長條
            foreach (var day in m_DailyData)
            {
                int cumulativeSec = 0;
                int columnTop = axisBottom;
                // 依程序名稱字母排序堆疊，確保顏色順序固定
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
                // 外框線與 X 軸日期標籤（顯示 MM-dd）
                g.DrawRectangle(Pens.Black, x, columnTop, barWidth, axisBottom - columnTop);
                g.DrawString(day.Date.Substring(5), m_TextFont, Brushes.Black, x - 16, plot.Bottom - 18);
                x += barWidth + gap;
            }
        }

        /// <summary>
        /// 繪製下方前 20 名程序水平長條圖。
        /// 每列左側顯示程序名稱，右側長條寬度代表相對使用時間，
        /// 條內（或條外）顯示格式化的使用時長。
        /// </summary>
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

            // 繪圖區域（左側 220px 留給程序名稱標籤）
            Rectangle plot = new Rectangle(220, 36, Math.Max(220, m_Top20Panel.Width - 240), Math.Max(120, m_Top20Panel.Height - 50));
            int maxSec = Math.Max(1, m_Top20Data.Max(x => x.Seconds));
            int rowH = Math.Max(18, (plot.Height - 10) / m_Top20Data.Count);
            int y = plot.Top + 2;

            foreach (var item in m_Top20Data)
            {
                // 依該程序佔最大值的比例計算長條寬度
                int barW = (int)Math.Round((item.Seconds / (double)maxSec) * (plot.Width - 20));
                Color c = m_ColorMap.TryGetValue(item.Process, out Color cc) ? cc : Color.Gray;
                using Brush b = new SolidBrush(c);
                g.FillRectangle(b, plot.Left, y, barW, rowH - 3);
                // 程序名稱顯示在長條左側固定區域
                g.DrawString(item.Process, m_TextFont, Brushes.Black, 10, y);

                // 時長文字：若長條夠寬則顯示在條內（白字），否則顯示在條右側（黑字）
                string timeText = TimeSpan.FromSeconds(item.Seconds).ToString(@"hh\:mm\:ss");
                SizeF textSize = g.MeasureString(timeText, m_TextFont);
                float textY = y + ((rowH - 3) - textSize.Height) / 2f;
                Brush textBrush = barW > textSize.Width + 10 ? Brushes.White : Brushes.Black;
                float textX = barW > textSize.Width + 10 ? plot.Left + 6 : plot.Left + barW + 4;
                g.DrawString(timeText, m_TextFont, textBrush, textX, textY);

                y += rowH;
            }
        }

        /// <summary>
        /// 將日期字串解析為 DateTime.Date；解析失敗時回傳今天，避免資料遺失。
        /// </summary>
        private static DateTime ParseDate(string date)
        {
            return DateTime.TryParse(date, out DateTime dt) ? dt.Date : DateTime.Today;
        }

        /// <summary>
        /// 將秒數格式化為 Y 軸刻度標籤（格式：小時:分鐘，例如 "1:30"）。
        /// 負數視為 0 處理。
        /// </summary>
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

    /// <summary>
    /// FlowerPomodoroTimer_Usage.log 中每一行 JSON 紀錄的資料結構。
    /// 每次關閉程式或結束當天使用時寫入一筆。
    /// </summary>
    public class UsageLogEntry
    {
        /// <summary>紀錄所屬日期（格式：yyyy-MM-dd）。</summary>
        public string Date { get; set; } = "";
        /// <summary>寫入時間戳記（格式：yyyy-MM-dd HH:mm:ss）。</summary>
        public string GeneratedAt { get; set; } = "";
        /// <summary>當天應用程式執行的總秒數。</summary>
        public int TotalAppSeconds { get; set; }
        /// <summary>當天有前景視窗活動的總秒數（排除鎖定畫面期間）。</summary>
        public int TotalActiveWindowSeconds { get; set; }
        /// <summary>各程序的使用時間明細清單。</summary>
        public List<UsageProcessEntry> Processes { get; set; } = new List<UsageProcessEntry>();
    }

    /// <summary>
    /// 單一程序的使用時間紀錄。
    /// </summary>
    public class UsageProcessEntry
    {
        /// <summary>程序名稱（例如 "chrome"）。</summary>
        public string ProcessName { get; set; } = "";
        /// <summary>該程序的使用秒數。</summary>
        public int Seconds { get; set; }
    }
}
