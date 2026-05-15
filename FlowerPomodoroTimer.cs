using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

/*
 * 番茄花鐘（Flower Pomodoro Timer）主視窗
 * ─────────────────────────────────────────────────────────────────
 * 計時機制：
 *   m_TimerMain        每秒觸發，更新顯示計時、切換 Work/Rest 狀態
 *   m_TimerActiveWindow 每秒觸發，偵測前景視窗並累計各程序使用秒數
 *
 * 視窗使用統計採用兩層階層：
 *   AWParentStatus（上層）：紀錄程序名稱（如 chrome）及對應橫條按鈕
 *   AWStatus（子層）     ：紀錄視窗標題（如 Facebook - Google Chrome）
 *
 * 效能監控（每秒刷新）：
 *   CPU、RAM、Disk（讀寫 MB/s）、GPU（Engine 使用率）、VRAM（專屬/共享）
 */

namespace Flower_Pomodoro_Timer
{
    public partial class formFlowerPomodoroTimer : Form
    {
        #region 欄位宣告

        // ── 計時器 ──────────────────────────────────────────────────
        /// <summary>主計時器：每 1 秒觸發，更新計時顯示並決定 Work/Rest 切換。</summary>
        System.Windows.Forms.Timer m_TimerMain = null!;
        /// <summary>視窗使用統計計時器：每 1 秒偵測前景視窗，僅在計時運行時啟動。</summary>
        System.Windows.Forms.Timer m_TimerActiveWindow = null!;

        // ── 時間追蹤 ─────────────────────────────────────────────────
        /// <summary>第一次按下 Start 的時間點。</summary>
        DateTime m_FirstStartTime;
        /// <summary>暫停後重新按 Start 的時間點，用於計算本次段落時長。</summary>
        DateTime m_NewStartTime;
        /// <summary>累計各次「Start→Pause」的總時長，用於計算應用程式總執行時間。</summary>
        TimeSpan m_TotalAccumulateTime;
        /// <summary>累計有前景視窗活動的總時長（排除鎖定畫面期間）。</summary>
        TimeSpan m_TotalAWAccumulateTime;
        /// <summary>每個 Work/Rest 階段的起始時間。</summary>
        DateTime m_PhaseStartTime;
        /// <summary>暫停後重新開始當前階段的時間點。</summary>
        DateTime m_NewPhaseStartTime;
        /// <summary>累計當前階段暫停前的已用時長。</summary>
        TimeSpan m_PhaseAccumulateTime;

        // ── 視窗狀態 ─────────────────────────────────────────────────
        /// <summary>是否處於右下角縮小模式。</summary>
        bool m_MinimumSizeOr = false;

        // ── Work/Rest 狀態機 ─────────────────────────────────────────
        enum eWorkStates
        {
            WORK,   // 工作中（55 分鐘）
            REST,   // 休息中（5 分鐘）
            PAUSE   // 暫停
        }
        eWorkStates m_WorkStates;
        /// <summary>使用者自訂的最上層顯示設定（按鈕切換值），Rest 結束後恢復此設定。</summary>
        bool m_TopMost;

        // ── 視窗使用統計：父層（程序） ──────────────────────────────────
        /// <summary>
        /// 上層視窗統計資訊。
        /// 使用 class 而非 struct，避免 List 中的 struct 成員值無法直接修改的問題。
        /// </summary>
        public class AWParentStatus
        {
            public int Seconds;                                    // 該程序的累計使用秒數
            public string ProcessName = string.Empty;              // 程序名稱（如 chrome）
            public int ProcessOrder;                               // 依使用時間排序後的順序
            public Button ProcessPlusButton = new Button();        // 展開/收起子列的按鈕
            public bool ProcessPlusOr = true;                      // true = 收起，false = 展開
            public BarChartBox ProcessPBox = new BarChartBox();    // 顯示使用時間的橫條
            public List<AWStatus> MyAWStatus = new List<AWStatus>();
            public Dictionary<string, AWStatus> WindowTitleMap = new Dictionary<string, AWStatus>(StringComparer.Ordinal);
        }
        /// <summary>所有程序的統計清單（順序依排序結果變動）。</summary>
        List<AWParentStatus> m_AWParentStatus = new List<AWParentStatus>();
        /// <summary>程序名稱 → AWParentStatus 的快速查詢字典（不分大小寫）。</summary>
        readonly Dictionary<string, AWParentStatus> m_AWParentByProcess = new Dictionary<string, AWParentStatus>(StringComparer.OrdinalIgnoreCase);

        // ── 視窗使用統計：子層（視窗標題） ──────────────────────────────
        /// <summary>
        /// 子層視窗統計資訊。
        /// </summary>
        public class AWStatus
        {
            public int Seconds;                                         // 該視窗標題的累計使用秒數
            public string ProcessName = string.Empty;                   // 所屬程序名稱
            public string WindowTitleName = string.Empty;               // 視窗標題（如 Facebook...）
            public int WindowTitleOrder;                                // 依使用時間排序後的順序
            public BarChartBox WindowTitlePBox = new BarChartBox();     // 顯示使用時間的子層橫條
        }

        // ── 背景色彩模式 ──────────────────────────────────────────────
        enum eColor
        {
            DefaultTomato,  // 預設番茄紅
            Grass,          // 草地綠
            Sky,            // 天空藍
            Gray,           // 深灰
            MAX
        }
        eColor m_BackColorMode;
        /// <summary>橫條填充顏色（隨佈景主題變化）。</summary>
        Color m_PBarBackColor = Color.Tomato;
        /// <summary>橫條文字顏色（隨佈景主題變化）。</summary>
        Color m_PBarForeColor = Color.Brown;

        // ── 視窗使用統計：第一條橫條（空閒時間）────────────────────────
        /// <summary>顯示未被任何程序追蹤的時間（Idle/Unknown）的橫條，固定為第一列。</summary>
        BarChartBox m_FirstBar = new BarChartBox();

        // ── 效能監控 ──────────────────────────────────────────────────
        #region 效能監控欄位

        BarChartBox m_BarCPU = new BarChartBox();
        BarChartBox m_BarRAM = new BarChartBox();
        BarChartBox m_BarDisk = new BarChartBox();
        BarChartBox m_BarGPU = new BarChartBox();
        BarChartBox m_BarVRAM = new BarChartBox();

        PerformanceCounter? m_CpuCounter;
        PerformanceCounter? m_DiskReadCounter;
        PerformanceCounter? m_DiskWriteCounter;
        List<PerformanceCounter> m_GpuUsageCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_VramUsageCounters = new List<PerformanceCounter>();
        /// <summary>VRAM 上限計數器（目前 Windows GPU 效能分類不提供 Dedicated Limit，保留備用）。</summary>
        List<PerformanceCounter> m_VramLimitCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_SharedVramUsageCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_SharedVramLimitCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_ProcessDedicatedVramUsageCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_ProcessSharedVramUsageCounters = new List<PerformanceCounter>();
        /// <summary>由 Registry 讀取的實體專屬 VRAM 容量（GB），用於效能計數器未提供上限時的備用值。</summary>
        float m_DedicatedVramTotalGb = 0f;

        #endregion

        // ── 其他雜項 ──────────────────────────────────────────────────
        readonly Random m_Random = new Random();
        /// <summary>上次顯示的休息圖片路徑，避免連續兩次顯示同一張圖。</summary>
        string m_LastRestImagePath = string.Empty;
        /// <summary>目前追蹤中的日期；跨越午夜時用於觸發自動寫出並重設計數。</summary>
        DateTime m_CurrentDate = DateTime.Today;
        /// <summary>各螢幕的休息提醒覆蓋視窗（多螢幕時每個螢幕一個）。</summary>
        readonly List<RestImageOverlayForm> m_RestImageOverlays = new List<RestImageOverlayForm>();
        formHelp? m_FormHelp;
        FormUsageAnalysis? m_FormUsageAnalysis;
        bool m_ClosingChildWindows;

        #endregion

        #region Win32 API 宣告

        /// <summary>取得目前前景（使用中）視窗的控制碼。</summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        /// <summary>由視窗控制碼取得擁有該視窗的程序 ID。</summary>
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        /// <summary>取得視窗標題文字。</summary>
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        /// <summary>呼叫 GlobalMemoryStatusEx 用的記憶體資訊結構。</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        IntPtr LastWindow { get; set; }

        #endregion

        #region 建構子與初始化

        public formFlowerPomodoroTimer()
        {
            InitializeComponent();
            // 使用 DPI 感知縮放，避免高 DPI 模式下元件錯位
            this.AutoScaleMode = AutoScaleMode.Dpi;
            InitializeFirstBar();
            InitializeMainTimer();
            InitializeActiveWindowTimer();
            SetFormSizeNormal();
            InitializePerformanceMonitoring();
            ChangeFormColor();
            m_TopMost = TopMost;
        }

        /// <summary>
        /// 將視窗設定為標準大小（1360×400），並置中於主螢幕工作區域。
        /// 同時設定計時標籤、Start 按鈕、效能橫條及視窗使用統計橫條的位置與尺寸。
        /// </summary>
        private void SetFormSizeNormal()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
            System.Drawing.Rectangle workingRectangle = screen.WorkingArea;

            MinimumSize = new Size(1360, 400);
            MaximumSize = new Size(1360, 800);
            Size = new Size(1360, 400);

            // 置中顯示
            Point newPosition = new Point(0, 0);
            newPosition.X = (workingRectangle.Width - this.Width) / 2;
            newPosition.Y = (workingRectangle.Height - this.Height) / 2;
            this.Location = newPosition;

            labelTimer.Size = new Size(310, 70);
            labelTimer.Font = new Font(labelTimer.Font.FontFamily, 40, labelTimer.Font.Style);
            labelTimer.Location = new Point(20, 5);

            buttonStart.Size = new Size(88, 40);
            buttonStart.MaximumSize = new Size(200, 40);
            buttonStart.Font = new Font(buttonStart.Font.FontFamily, 16, buttonStart.Font.Style);
            buttonStart.Location = new Point(125, 80);
            buttonStart_SizeChanged(this, EventArgs.Empty);

            labelTotalTimer.Size = new Size(310, 46);
            labelTotalTimer.Font = new Font(labelTimer.Font.FontFamily, 24, labelTimer.Font.Style);
            labelTotalTimer.Location = new Point(20, 124);

            // 效能監控橫條：從 Y=180 開始，每條高 25px，間距 3px
            int barY = 180;
            int barWidth = 310;
            int barHeight = 25;
            int spacing = 3;
            m_BarCPU.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarRAM.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarDisk.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarGPU.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarVRAM.Bounds = new Rectangle(20, barY, barWidth, barHeight);

            // 視窗使用統計橫條：從 X=360 開始，佔滿剩餘寬度
            m_FirstBar.Location = new Point(360, 0);
            m_FirstBar.Size = new Size(Math.Max(1000, ClientSize.Width - 360), 30);
        }

        /// <summary>
        /// 將視窗縮小至右下角迷你模式（240×動態高），隱藏邊框，
        /// 僅顯示計時器、效能橫條，不顯示視窗使用統計列表。
        /// </summary>
        private void SetFormSizeMini()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
            System.Drawing.Rectangle workingRectangle = screen.WorkingArea;

            MinimumSize = new Size(240, 240);
            MaximumSize = new Size(1360, 720);
            Size = new Size(240, 240);

            labelTimer.Size = new Size(200, 60);
            labelTimer.Font = new Font(labelTimer.Font.FontFamily, 36, labelTimer.Font.Style);
            labelTimer.Location = new Point(20, 25);

            buttonStart.Size = new Size(44, 24);
            buttonStart.Font = new Font(buttonStart.Font.FontFamily, 10, buttonStart.Font.Style);
            buttonStart.Location = new Point(140, 90);
            buttonStart_SizeChanged(this, EventArgs.Empty);

            labelTotalTimer.Size = new Size(200, 40);
            labelTotalTimer.Font = new Font(labelTimer.Font.FontFamily, 20, labelTimer.Font.Style);
            labelTotalTimer.Location = new Point(20, 125);

            int barY = 170;
            int barWidth = 220;
            int barHeight = 25;
            int spacing = 3;
            m_BarCPU.Bounds = new Rectangle(10, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarRAM.Bounds = new Rectangle(10, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarDisk.Bounds = new Rectangle(10, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarGPU.Bounds = new Rectangle(10, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarVRAM.Bounds = new Rectangle(10, barY, barWidth, barHeight);

            // 動態調整高度以容納所有效能橫條
            Height = barY + barHeight + 10;
            MinimumSize = new Size(180, Height);

            // 固定在螢幕右下角
            this.Location = new Point(workingRectangle.Width - this.Width, workingRectangle.Height - this.Height);
        }

        /// <summary>
        /// 初始化第一條橫條（Idle/空閒時間橫條），顯示在所有程序統計橫條之上。
        /// </summary>
        public void InitializeFirstBar()
        {
            Controls.Add(m_FirstBar);
            m_FirstBar.Location = new Point(360, 0);
            m_FirstBar.Size = new Size(1000, 30);
            m_FirstBar.ForeColor = Color.DodgerBlue;
        }

        /// <summary>
        /// 初始化主計時器（每 1 秒觸發）：重設所有時間追蹤變數，預設狀態為 WORK。
        /// </summary>
        private void InitializeMainTimer()
        {
            m_TimerMain = new System.Windows.Forms.Timer();
            m_TimerMain.Interval = 1000;
            m_TimerMain.Tick += new EventHandler(TimerMain_Tick);

            m_FirstStartTime = DateTime.MinValue;   // 等到第一次按下 Start 才設定
            m_TotalAccumulateTime = TimeSpan.Zero;
            m_TotalAWAccumulateTime = TimeSpan.Zero;
            m_PhaseAccumulateTime = TimeSpan.Zero;

            m_WorkStates = eWorkStates.WORK;
        }

        /// <summary>
        /// 初始化視窗使用統計計時器（每 1 秒觸發）：
        /// 偵測目前前景視窗，僅在計時運行時啟動，鎖定畫面時自動停止累計。
        /// </summary>
        private void InitializeActiveWindowTimer()
        {
            m_TimerActiveWindow = new System.Windows.Forms.Timer();
            m_TimerActiveWindow.Interval = 1000;
            m_TimerActiveWindow.Tick += new EventHandler(TimerActiveWindow_Tick);
            LastWindow = IntPtr.Zero;
        }

        /// <summary>
        /// 初始化效能監控：
        /// 建立 CPU、磁碟的 PerformanceCounter，由 Registry 讀取 VRAM 總容量，
        /// 呼叫 RefreshGpuCounters 建立 GPU/VRAM 計數器，並將橫條加入視窗。
        /// VRAM 橫條支援點擊重設計數器（用於 GPU 插拔或讀數異常時）。
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            try
            {
                m_CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                m_DiskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                m_DiskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
                m_DedicatedVramTotalGb = GetDedicatedVramTotalGbFromRegistry();
                RefreshGpuCounters();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("效能監控初始化失敗: " + ex.Message);
            }

            // 效能橫條不顯示邊框，背景色由 ChangeFormColor 控制
            m_BarCPU.BorderStyle = BorderStyle.None;
            m_BarRAM.BorderStyle = BorderStyle.None;
            m_BarDisk.BorderStyle = BorderStyle.None;
            m_BarGPU.BorderStyle = BorderStyle.None;
            m_BarVRAM.BorderStyle = BorderStyle.None;
            Controls.Add(m_BarCPU);
            Controls.Add(m_BarRAM);
            Controls.Add(m_BarDisk);
            Controls.Add(m_BarGPU);
            Controls.Add(m_BarVRAM);

            // 點擊 VRAM 橫條可重新讀取 GPU 計數器，解決初始顯示 0% 的問題
            m_BarVRAM.Cursor = Cursors.Hand;
            m_BarVRAM.Click += (s, e) =>
            {
                RefreshGpuCounters();
                UpdatePerformanceInfo();
            };
        }

        #endregion

        #region 效能監控

        /// <summary>
        /// 釋放並清空指定的 PerformanceCounter 清單，避免資源洩漏。
        /// </summary>
        private void DisposeCounters(List<PerformanceCounter> counters)
        {
            foreach (var counter in counters)
            {
                try { counter.Dispose(); } catch { }
            }
            counters.Clear();
        }

        /// <summary>
        /// 重新建立所有 GPU 相關的 PerformanceCounter：
        /// - GPU Engine：各引擎使用率，加總後得到整體 GPU 使用率
        /// - GPU Adapter Memory：Dedicated（專屬顯存）與 Shared（共享顯存）使用量
        /// - GPU Process Memory：各程序的 Dedicated/Shared 顯存用量（備用讀取來源）
        /// 建立後各讀取一次初始值（第一次 NextValue 通常為 0，需 prime 一遍）。
        /// </summary>
        private void RefreshGpuCounters()
        {
            try
            {
                // GPU Engine 使用率計數器（只保留含 "engtype_" 的實例）
                var category = new PerformanceCounterCategory("GPU Engine");
                var names = category.GetInstanceNames();
                DisposeCounters(m_GpuUsageCounters);
                foreach (var name in names)
                {
                    if (name.Contains("engtype_", StringComparison.OrdinalIgnoreCase))
                    {
                        try { m_GpuUsageCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", name)); } catch { }
                    }
                }

                // GPU Adapter Memory 計數器（Dedicated/Shared 使用量）
                var vramCategory = new PerformanceCounterCategory("GPU Adapter Memory");
                var vramNames = vramCategory.GetInstanceNames();
                DisposeCounters(m_VramUsageCounters);
                DisposeCounters(m_VramLimitCounters);
                DisposeCounters(m_SharedVramUsageCounters);
                DisposeCounters(m_SharedVramLimitCounters);
                foreach (var name in vramNames)
                {
                    try { m_VramUsageCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", name)); } catch { }
                    // 注意：Windows 標準 GPU 效能分類不提供 "Dedicated Limit" 和 "Shared Limit"，
                    // 強制建立會拋出 InvalidOperationException，故不建立上限計數器。
                    try { m_SharedVramUsageCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Shared Usage", name)); } catch { }
                }

                // GPU Process Memory 計數器（各程序的顯存用量，作為 Adapter Memory 讀數為 0 時的備用）
                var processVramCategory = new PerformanceCounterCategory("GPU Process Memory");
                var processVramNames = processVramCategory.GetInstanceNames();
                DisposeCounters(m_ProcessDedicatedVramUsageCounters);
                DisposeCounters(m_ProcessSharedVramUsageCounters);
                foreach (var name in processVramNames)
                {
                    try { m_ProcessDedicatedVramUsageCounters.Add(new PerformanceCounter("GPU Process Memory", "Dedicated Usage", name)); } catch { }
                    try { m_ProcessSharedVramUsageCounters.Add(new PerformanceCounter("GPU Process Memory", "Shared Usage", name)); } catch { }
                }

                // Prime 所有計數器：第一次呼叫 NextValue() 通常回傳 0（rate-style 計數器需兩次取樣）
                foreach (var counter in m_GpuUsageCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_VramUsageCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_VramLimitCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_SharedVramUsageCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_SharedVramLimitCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_ProcessDedicatedVramUsageCounters) { try { counter.NextValue(); } catch { } }
                foreach (var counter in m_ProcessSharedVramUsageCounters) { try { counter.NextValue(); } catch { } }
            }
            catch { }
        }

        /// <summary>
        /// 讀取所有效能計數器並更新五條效能橫條（CPU/RAM/Disk/GPU/VRAM）。
        /// 每秒由 TimerMain_Tick 呼叫一次。
        ///
        /// VRAM 顯示邏輯：
        /// - 優先使用有讀數（&gt;1MB）的來源（Dedicated 或 Shared）
        /// - 若計數器無法提供總量上限，改用 Registry 讀取的實體 VRAM 容量
        /// </summary>
        private void UpdatePerformanceInfo()
        {
            // 1. CPU 使用率
            float cpuVal = 0;
            try { cpuVal = m_CpuCounter?.NextValue() ?? 0; } catch { }
            m_BarCPU.SetBar($"CPU: {cpuVal:F1}%", cpuVal / 100f, Color.LimeGreen, Color.White);

            // 2. RAM 使用量（透過 GlobalMemoryStatusEx 取得精確數值）
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalGB = memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
                double usedGB = (memStatus.ullTotalPhys - memStatus.ullAvailPhys) / 1024.0 / 1024.0 / 1024.0;
                float ramPercent = memStatus.dwMemoryLoad;
                m_BarRAM.SetBar($"RAM: {usedGB:F2}/{totalGB:F1} GB ({ramPercent}%)", ramPercent / 100f, Color.DeepSkyBlue, Color.White);
            }

            // 3. 磁碟 I/O（讀取 + 寫入 MB/s，以 100MB/s 為滿格基準）
            float rVal = 0, wVal = 0;
            try { rVal = m_DiskReadCounter?.NextValue() ?? 0; wVal = m_DiskWriteCounter?.NextValue() ?? 0; } catch { }
            float rMB = rVal / 1024f / 1024f;
            float wMB = wVal / 1024f / 1024f;
            m_BarDisk.SetBar($"Disk: R:{rMB:F1} W:{wMB:F1} MB/s", Math.Clamp((rMB + wMB) / 100f, 0, 1), Color.Orange, Color.White);

            // 4. GPU 使用率（各引擎加總，夾制在 0~100%）
            float gpuVal = 0;
            foreach (var counter in m_GpuUsageCounters)
            {
                try { gpuVal += counter.NextValue(); } catch { }
            }
            gpuVal = Math.Clamp(gpuVal, 0f, 100f);
            m_BarGPU.SetBar($"GPU: {gpuVal:F1}%", gpuVal / 100f, Color.MediumPurple, Color.White);

            // 5. VRAM 使用量
            float vramVal = 0;
            foreach (var counter in m_VramUsageCounters) { try { vramVal += counter.NextValue(); } catch { } }
            float vramLimit = 0;
            foreach (var counter in m_VramLimitCounters) { try { vramLimit += counter.NextValue(); } catch { } }
            float sharedVramVal = 0;
            foreach (var counter in m_SharedVramUsageCounters) { try { sharedVramVal += counter.NextValue(); } catch { } }
            float sharedVramLimit = 0;
            foreach (var counter in m_SharedVramLimitCounters) { try { sharedVramLimit += counter.NextValue(); } catch { } }

            // 若 Adapter Memory 計數器無讀數，改用 Process Memory 加總（備用路徑）
            if (vramVal <= 0)
            {
                foreach (var counter in m_ProcessDedicatedVramUsageCounters) { try { vramVal += counter.NextValue(); } catch { } }
            }
            if (sharedVramVal <= 0)
            {
                foreach (var counter in m_ProcessSharedVramUsageCounters) { try { sharedVramVal += counter.NextValue(); } catch { } }
            }

            // 選擇顯示專屬或共享 VRAM：優先顯示有實際使用量的那個
            const float usageSwitchThresholdBytes = 1f * 1024f * 1024f; // 1 MB
            string vramMode = "專屬GPU";
            bool dedicatedHasUsage = vramVal > usageSwitchThresholdBytes;
            bool sharedHasUsage = sharedVramVal > usageSwitchThresholdBytes;
            bool dedicatedAvailable = vramLimit > 0;
            bool sharedAvailable = sharedVramLimit > 0;

            if (sharedHasUsage && !dedicatedHasUsage)
            {
                // iGPU 機器可能只有共享顯存有數值
                vramVal = sharedVramVal;
                vramLimit = sharedVramLimit;
                vramMode = "共享";
            }
            else if (!dedicatedAvailable && sharedAvailable)
            {
                vramVal = sharedVramVal;
                vramLimit = sharedVramLimit;
                vramMode = "共享";
            }

            float vramUsedGB = vramVal / 1024f / 1024f / 1024f;
            float vramTotalGB = vramLimit / 1024f / 1024f / 1024f;
            if (vramTotalGB <= 0f)
            {
                // 效能計數器無法提供上限時，使用 Registry 讀取的實體 VRAM（預設 8GB）
                vramTotalGB = m_DedicatedVramTotalGb > 0f ? m_DedicatedVramTotalGb : 8f;
            }
            float vramRatio = Math.Clamp(vramUsedGB / vramTotalGB, 0, 1);
            float vramPercent = vramRatio * 100f;
            m_BarVRAM.SetBar(
                $"VRAM:{vramUsedGB:F3}GB/{vramTotalGB:F0}GB ({vramPercent:F3}%) ({vramMode})",
                vramRatio,
                Color.HotPink,
                Color.White);
        }

        /// <summary>
        /// 從 Windows Registry（HKLM\SYSTEM\...\Video）讀取顯示卡的實體專屬 VRAM 容量（GB）。
        /// 支援 QWORD、DWORD、byte[] 等多種 Registry 值類型。
        /// 讀取失敗時回傳 0。
        /// </summary>
        private float GetDedicatedVramTotalGbFromRegistry()
        {
            try
            {
                ulong maxBytes = 0;
                using RegistryKey? videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video");
                if (videoKey == null)
                {
                    return 0f;
                }

                foreach (string adapterGuid in videoKey.GetSubKeyNames())
                {
                    using RegistryKey? adapterKey = videoKey.OpenSubKey(adapterGuid);
                    if (adapterKey == null) continue;

                    foreach (string childName in adapterKey.GetSubKeyNames())
                    {
                        using RegistryKey? childKey = adapterKey.OpenSubKey(childName);
                        if (childKey == null) continue;

                        // 優先讀取 QWORD（64-bit），其次讀 DWORD（32-bit）
                        ulong current = 0;
                        object? qword = childKey.GetValue("HardwareInformation.qwMemorySize");
                        if (!TryReadRegistryMemoryBytes(qword, out current))
                        {
                            object? dword = childKey.GetValue("HardwareInformation.MemorySize");
                            TryReadRegistryMemoryBytes(dword, out current);
                        }

                        if (current > maxBytes)
                        {
                            maxBytes = current;
                        }
                    }
                }

                if (maxBytes > 0)
                {
                    return maxBytes / 1024f / 1024f / 1024f;
                }
            }
            catch { }

            return 0f;
        }

        /// <summary>
        /// 嘗試將 Registry 讀取的物件值（可能為 ulong/long/uint/int/byte[]）轉換為 ulong 位元組數。
        /// </summary>
        private static bool TryReadRegistryMemoryBytes(object? value, out ulong bytes)
        {
            bytes = 0;
            if (value == null) return false;

            switch (value)
            {
                case ulong u when u > 0: bytes = u; return true;
                case long l when l > 0: bytes = (ulong)l; return true;
                case uint ui when ui > 0: bytes = ui; return true;
                case int i when i > 0: bytes = (uint)i; return true;
                case byte[] arr when arr.Length >= 8:
                    bytes = BitConverter.ToUInt64(arr, 0);
                    return bytes > 0;
                default:
                    return ulong.TryParse(value.ToString(), out bytes) && bytes > 0;
            }
        }

        #endregion

        #region 休息提醒圖片

        /// <summary>
        /// 顯示休息提醒覆蓋圖片。
        /// 若 forceShow 為 false，則遵守使用者設定的 Enabled 開關；
        /// forceShow 為 true 時（測試按鈕或手動觸發）無視設定直接顯示。
        /// 每個連接的螢幕各建立一個獨立覆蓋視窗，且盡量使用不同圖片。
        /// </summary>
        /// <param name="forceShow">true = 強制顯示；false = 遵守 Enabled 設定</param>
        private void ShowRestReminderImage(bool forceShow)
        {
            RestImageReminderSettings.Load();
            if (!forceShow && !RestImageReminderSettings.Enabled)
            {
                return;
            }

            string folder = RestImageReminderSettings.ImageFolderPath;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            string[] imageFiles = GetImageFiles(folder);
            if (imageFiles.Length == 0)
            {
                return;
            }

            // 先關閉並釋放現有的所有覆蓋視窗
            CloseAllRestImageOverlays();

            try
            {
                // 將圖片清單洗牌，讓每個螢幕分配到不同圖片；
                // 若圖片數量大於 1，則把上次使用過的圖片移到最後，降低重複機率。
                List<string> shuffled = imageFiles.OrderBy(_ => m_Random.Next()).ToList();
                if (shuffled.Count > 1 && !string.IsNullOrEmpty(m_LastRestImagePath))
                {
                    int lastIdx = shuffled.FindIndex(
                        f => f.Equals(m_LastRestImagePath, StringComparison.OrdinalIgnoreCase));
                    if (lastIdx >= 0)
                    {
                        shuffled.RemoveAt(lastIdx);
                        shuffled.Add(m_LastRestImagePath);   // 移至末尾，最後才被循環使用
                    }
                }

                // 記錄第一張圖片路徑（供下次避重複使用）
                m_LastRestImagePath = shuffled[0];

                Screen[] screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    string imagePath = shuffled[i % shuffled.Count];
                    Rectangle bounds = screens[i].Bounds;

                    RestImageOverlayForm overlay = new RestImageOverlayForm(imagePath, bounds);
                    // 雙擊任一螢幕的覆蓋視窗 → 關閉所有螢幕的覆蓋視窗
                    overlay.CloseAllRequested += CloseAllRestImageOverlays;
                    overlay.FormClosed += (sender, _) =>
                    {
                        if (sender is RestImageOverlayForm closed)
                        {
                            m_RestImageOverlays.Remove(closed);
                            closed.Dispose();
                        }
                    };
                    m_RestImageOverlays.Add(overlay);
                    overlay.Show();
                    overlay.BringToFront();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ShowRestReminderImage failed: " + ex.Message);
            }
        }

        /// <summary>
        /// 關閉並釋放所有休息提醒覆蓋視窗。
        /// </summary>
        private void CloseAllRestImageOverlays()
        {
            foreach (RestImageOverlayForm overlay in m_RestImageOverlays.ToList())
            {
                try
                {
                    overlay.Close();
                    overlay.Dispose();
                }
                catch { }
            }
            m_RestImageOverlays.Clear();
        }

        /// <summary>
        /// 取得指定資料夾內所有支援格式的圖片檔案路徑。
        /// 支援副檔名：jpg、jpeg、png、bmp、gif、webp（不分大小寫）。
        /// </summary>
        private static string[] GetImageFiles(string folderPath)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
            return Directory.GetFiles(folderPath)
                .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        /// <summary>
        /// 從圖片清單中隨機挑選一張，並記錄為 m_LastRestImagePath。
        /// 若有多張圖片，會先排除上一次顯示過的，避免連續重複。
        /// </summary>
        private string PickRandomImagePath(string[] imageFiles)
        {
            if (imageFiles.Length == 1)
            {
                m_LastRestImagePath = imageFiles[0];
                return imageFiles[0];
            }

            // 排除上次選取的圖片後再隨機挑選
            string[] candidates = imageFiles
                .Where(file => !file.Equals(m_LastRestImagePath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            string[] source = candidates.Length > 0 ? candidates : imageFiles;
            string picked = source[m_Random.Next(source.Length)];
            m_LastRestImagePath = picked;
            return picked;
        }

        #endregion

        #region 使用紀錄

        /// <summary>
        /// 將指定日期的視窗使用統計寫入 FlowerPomodoroTimer_Usage.log（每行一筆 JSON）。
        /// 同時將 Idle/Unknown usage（計時器運行中但無前景視窗的時間）寫為一筆程序紀錄。
        /// 寫入失敗時靜默略過。
        /// </summary>
        /// <param name="date">要記錄的日期（通常為昨天或今天）。</param>
        private void WriteUsageLogForDate(DateTime date)
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "FlowerPomodoroTimer_Usage.log");

                // 建立程序清單，降冪排序後加入 Idle 項目
                List<UsageProcessEntry> processes = m_AWParentStatus
                    .OrderByDescending(p => p.Seconds)
                    .Select(p => new UsageProcessEntry
                    {
                        ProcessName = p.ProcessName,
                        Seconds = p.Seconds
                    })
                    .ToList();

                int idleSeconds = (int)m_TotalAccumulateTime.TotalSeconds - (int)m_TotalAWAccumulateTime.TotalSeconds;
                if (idleSeconds > 0)
                {
                    processes.Add(new UsageProcessEntry
                    {
                        ProcessName = "Idle/Unknown usage",
                        Seconds = idleSeconds
                    });
                }

                UsageLogEntry entry = new UsageLogEntry
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalAppSeconds = (int)m_TotalAccumulateTime.TotalSeconds,
                    TotalActiveWindowSeconds = (int)m_TotalAWAccumulateTime.TotalSeconds,
                    Processes = processes
                };

                string json = JsonSerializer.Serialize(entry);
                File.AppendAllText(logPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteUsageLogForDate failed: " + ex.Message);
            }
        }

        /// <summary>
        /// 跨越午夜時呼叫：寫出前一天的使用紀錄後清除所有追蹤資料，
        /// 重新從零開始計算新的一天。若計時器當時仍在運行，
        /// 先將未結算的時段累入總計，再以午夜為起點開始新的一天。
        /// </summary>
        private void RolloverToNewDay()
        {
            // 先將目前正在執行的時段計入（避免午夜前的時間遺失）
            if (m_TimerMain.Enabled)
            {
                DateTime now = DateTime.Now;
                m_TotalAccumulateTime += now.Subtract(m_NewStartTime);
                m_NewStartTime = now;
            }

            // 寫出前一天的統計
            WriteUsageLogForDate(m_CurrentDate);

            // 移除所有動態建立的程序橫條 Controls 並釋放
            foreach (AWParentStatus parent in m_AWParentStatus)
            {
                Controls.Remove(parent.ProcessPlusButton);
                Controls.Remove(parent.ProcessPBox);
                parent.ProcessPlusButton.Dispose();
                parent.ProcessPBox.Dispose();
                foreach (AWStatus child in parent.MyAWStatus)
                {
                    Controls.Remove(child.WindowTitlePBox);
                    child.WindowTitlePBox.Dispose();
                }
            }
            m_AWParentStatus.Clear();
            m_AWParentByProcess.Clear();

            // 重設計時器與統計累計
            m_TotalAccumulateTime = TimeSpan.Zero;
            m_TotalAWAccumulateTime = TimeSpan.Zero;

            // 若計時器仍在跑，以現在時刻為新一天的起點
            if (m_TimerMain.Enabled)
            {
                m_FirstStartTime = DateTime.Now;
            }

            m_CurrentDate = DateTime.Today;
        }

        #endregion

        #region 休息提醒圖片覆蓋視窗

        /// <summary>
        /// 指定螢幕上的全螢幕休息圖片覆蓋視窗（無邊框、最上層）。
        /// - 單擊：縮小圖片至 90%
        /// - 雙擊：關閉此覆蓋視窗
        /// - 圖片等比例縮放，永遠垂直水平置中。
        /// </summary>
        private sealed class RestImageOverlayForm : Form
        {
            private readonly Image m_SourceImage;
            private readonly PictureBox m_PictureBox = new PictureBox();
            private float m_Scale = 1f;

            /// <summary>雙擊任一覆蓋視窗時觸發，由父視窗訂閱後關閉所有螢幕上的覆蓋視窗。</summary>
            public event Action? CloseAllRequested;

            /// <param name="imagePath">要顯示的圖片路徑。</param>
            /// <param name="screenBounds">此覆蓋視窗所覆蓋的螢幕範圍（Screen.Bounds）。</param>
            public RestImageOverlayForm(string imagePath, Rectangle screenBounds)
            {
                m_SourceImage = Image.FromFile(imagePath);
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                ShowInTaskbar = false;
                BackColor = Color.Black;

                Bounds = screenBounds;

                m_PictureBox.Image = m_SourceImage;
                m_PictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                m_PictureBox.BackColor = Color.Black;
                Controls.Add(m_PictureBox);

                m_PictureBox.Click += (_, _) => ShrinkImage();
                m_PictureBox.DoubleClick += (_, _) => CloseAllRequested?.Invoke();
                Click += (_, _) => ShrinkImage();
                DoubleClick += (_, _) => CloseAllRequested?.Invoke();

                ApplyScale();
            }

            /// <summary>每次點擊將圖片縮小 10%，最小縮放至 10%。</summary>
            private void ShrinkImage()
            {
                m_Scale *= 0.9f;
                if (m_Scale < 0.1f)
                {
                    m_Scale = 0.1f;
                }
                ApplyScale();
            }

            /// <summary>依目前 m_Scale 重新計算圖片大小並置中顯示。</summary>
            private void ApplyScale()
            {
                Rectangle screenBounds = Bounds;
                float targetHeight = screenBounds.Height * m_Scale;
                float ratio = (float)m_SourceImage.Width / m_SourceImage.Height;
                int width = Math.Max(1, (int)Math.Round(targetHeight * ratio));
                int height = Math.Max(1, (int)Math.Round(targetHeight));

                m_PictureBox.Size = new Size(width, height);
                m_PictureBox.Location = new Point(
                    (screenBounds.Width - width) / 2,
                    (screenBounds.Height - height) / 2);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    m_PictureBox.Image?.Dispose();
                    m_SourceImage.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        #endregion

        #region 計時器事件

        /// <summary>
        /// 主計時器 Tick（每 1 秒）：
        /// 1. 刷新效能監控橫條
        /// 2. 更新階段計時（mm:ss）與總計時（hh:mm:ss）顯示
        /// 3. 判斷是否到達 Work（55 分鐘）或 Rest（5 分鐘）的切換條件
        /// 4. 更新「PIN/UNPIN」按鈕文字
        /// </summary>
        private void TimerMain_Tick(object? Sender, EventArgs e)
        {
            // 偵測跨越午夜：自動寫出前一天統計並重設當日計數
            if (DateTime.Today != m_CurrentDate)
            {
                RolloverToNewDay();
            }

            UpdatePerformanceInfo();

            // 計算當前階段已用時長
            TimeSpan tmpTime = m_PhaseAccumulateTime + DateTime.Now.Subtract(m_NewPhaseStartTime);
            labelTimer.Text = tmpTime.ToString(@"mm\:ss");

            // 計算應用程式總執行時長（含當前未暫停的段落）
            TimeSpan totalElapsed = m_TotalAccumulateTime;
            if (m_TimerMain.Enabled)
            {
                totalElapsed += DateTime.Now.Subtract(m_NewStartTime);
            }
            labelTotalTimer.Text = totalElapsed.ToString(@"hh\:mm\:ss");

            // 狀態切換：Work 滿 55 分鐘 → Rest；Rest 滿 5 分鐘 → Work
            switch (m_WorkStates)
            {
                case eWorkStates.WORK:
                    if (tmpTime.Minutes >= 55)
                    {
                        ChangeWorkState(eWorkStates.REST);
                        TopMost = true;   // Rest 開始時強制最上層，確保使用者看到休息提醒
                    }
                    break;
                case eWorkStates.REST:
                    if (tmpTime.Minutes >= 5)
                    {
                        ChangeWorkState(eWorkStates.WORK);
                        TopMost = m_TopMost;   // 恢復使用者自訂的最上層設定
                    }
                    break;
                case eWorkStates.PAUSE:
                default:
                    break;
            }

            // 同步更新 PIN/UNPIN 按鈕文字
            buttonAlwaysTop.Text = TopMost ? "PIN" : "UNPIN";
            toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
        }

        /// <summary>
        /// 視窗使用統計計時器 Tick（每 1 秒）：
        /// 偵測目前前景視窗的程序名稱與標題，累計使用秒數後重新排序並更新橫條顯示。
        /// 若前景視窗瞬間消失（如彈窗關閉）則靜默略過本輪統計。
        /// </summary>
        private void TimerActiveWindow_Tick(object? sender, EventArgs e)
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                int pId;
                GetWindowThreadProcessId(hwnd, out pId);
                if (pId == 0) return;

                Process p = Process.GetProcessById(pId);
                const int nChars = 256;
                StringBuilder windowTitle = new StringBuilder(nChars);
                if (GetWindowText(hwnd, windowTitle, nChars) <= 0) return;

                string currentWindowTitle = windowTitle.ToString().Trim();
                if (string.IsNullOrEmpty(currentWindowTitle)) return;

                CalAWParentSec(p.ProcessName, currentWindowTitle);
                SortAWParentStatus();
                SetAWParentStatusBars();
                LastWindow = hwnd;
            }
            catch (Exception)
            {
                // 前景視窗標題可能瞬間消失，略過本輪統計即可
            }
        }

        #endregion

        #region Work/Rest 狀態切換

        /// <summary>
        /// 切換 Work/Rest 狀態：重設階段計時、更新視窗顏色並強制顯示視窗。
        /// 切換至 REST 時顯示休息圖片覆蓋；切換至 WORK 時關閉覆蓋圖片。
        /// </summary>
        private void ChangeWorkState(eWorkStates _workStates)
        {
            switch (_workStates)
            {
                case eWorkStates.WORK:
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.WORK;
                    // 結束 Rest，關閉所有螢幕上的覆蓋圖片
                    CloseAllRestImageOverlays();
                    break;
                case eWorkStates.REST:
                    buttonStart.Enabled = true;
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.REST;
                    // 注意：覆蓋圖片不在此處建立，而是在下方 BringToFront() 之後
                    // 透過 BeginInvoke 延後至下一個訊息循環，確保主視窗已切回正確的
                    // 虛擬桌面，覆蓋視窗才會顯示在同一個桌面上。
                    break;
                case eWorkStates.PAUSE:
                default:
                    break;
            }

            ChangeFormColor();
            Activate();
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();

            // 切換到 REST 時：等主視窗確實切回所在的虛擬桌面後，
            // 才在同一桌面的每個螢幕上建立覆蓋視窗。
            if (_workStates == eWorkStates.REST)
            {
                BeginInvoke(new Action(() => ShowRestReminderImage(false)));
            }
        }

        #endregion

        #region 視窗使用秒數統計

        /// <summary>前 20 名才渲染橫條，超過此數目的程序僅計時不顯示。</summary>
        private const int MaxProcessesToRender = 20;

        /// <summary>
        /// 累計指定程序與視窗標題的使用秒數（每秒呼叫一次）。
        /// 若程序尚未建立父層記錄則自動建立；視窗標題子層同理。
        /// 同時累加 m_TotalAWAccumulateTime（總前景活動時長）。
        /// </summary>
        /// <param name="_ProcessName">程序名稱（如 chrome）</param>
        /// <param name="_WindowTitle">視窗標題文字</param>
        private void CalAWParentSec(string _ProcessName, string _WindowTitle)
        {
            if (string.IsNullOrWhiteSpace(_ProcessName) || string.IsNullOrWhiteSpace(_WindowTitle))
            {
                return;
            }

            // 查找或建立父層（程序）記錄
            if (!m_AWParentByProcess.TryGetValue(_ProcessName, out AWParentStatus? parent))
            {
                parent = CreateParentStatus(_ProcessName);
                m_AWParentByProcess[_ProcessName] = parent;
                m_AWParentStatus.Add(parent);
            }
            parent.Seconds++;

            // 查找或建立子層（視窗標題）記錄
            if (!parent.WindowTitleMap.TryGetValue(_WindowTitle, out AWStatus? child))
            {
                child = CreateChildStatus(_ProcessName, _WindowTitle);
                parent.WindowTitleMap[_WindowTitle] = child;
                parent.MyAWStatus.Add(child);
                Controls.Add(child.WindowTitlePBox);
            }
            child.Seconds++;

            m_TotalAWAccumulateTime = m_TotalAWAccumulateTime.Add(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// 建立新的父層（程序）統計物件，包含展開按鈕與橫條控制項，並加入視窗。
        /// </summary>
        private AWParentStatus CreateParentStatus(string processName)
        {
            AWParentStatus parent = new AWParentStatus
            {
                ProcessName = processName,
                Seconds = 0,
                ProcessOrder = m_AWParentStatus.Count,
                ProcessPlusOr = true,
                ProcessPlusButton = new Button(),
                ProcessPBox = new BarChartBox()
            };

            parent.ProcessPlusButton.FlatStyle = FlatStyle.Flat;
            parent.ProcessPlusButton.FlatAppearance.BorderSize = 0;
            parent.ProcessPlusButton.BackColor = m_PBarBackColor;
            parent.ProcessPlusButton.ForeColor = m_PBarForeColor;
            parent.ProcessPlusButton.Size = new Size(30, 30);
            parent.ProcessPlusButton.Image = imageListPlus.Images[0]; // + 圖示
            parent.ProcessPlusButton.Click += new EventHandler(buttonPlus_Click);
            parent.ProcessPlusButton.Tag = parent;
            toolTipAll.SetToolTip(parent.ProcessPlusButton, "展開 / 收起");
            Controls.Add(parent.ProcessPlusButton);

            parent.ProcessPBox.Location = new Point(360, 0);
            parent.ProcessPBox.Size = new Size(1000, 30);
            parent.ProcessPBox.ForeColor = Color.DodgerBlue;
            Controls.Add(parent.ProcessPBox);

            return parent;
        }

        /// <summary>
        /// 建立新的子層（視窗標題）統計物件與對應橫條（尚未加入視窗，由呼叫端負責）。
        /// </summary>
        private AWStatus CreateChildStatus(string processName, string windowTitle)
        {
            AWStatus status = new AWStatus
            {
                ProcessName = processName,
                WindowTitleName = windowTitle,
                Seconds = 0,
                WindowTitlePBox = new BarChartBox()
            };

            status.WindowTitlePBox.Location = new Point(400, 0);
            status.WindowTitlePBox.Size = new Size(960, 30);
            status.WindowTitlePBox.ForeColor = Color.DodgerBlue;
            return status;
        }

        /// <summary>
        /// 依使用秒數降冪排序所有父層程序，更新 ProcessOrder，
        /// 並同時對每個父層的子層視窗標題進行排序。
        /// </summary>
        private void SortAWParentStatus()
        {
            List<AWParentStatus> orderedParents = m_AWParentStatus
                .OrderByDescending(x => x.Seconds)
                .ThenBy(x => x.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < orderedParents.Count; i++)
            {
                orderedParents[i].ProcessOrder = i;
                SortAWStatus(orderedParents[i]);
            }
        }

        /// <summary>
        /// 對指定父層的子層清單依使用秒數降冪排序，並更新 WindowTitleOrder。
        /// </summary>
        private void SortAWStatus(AWParentStatus _myAWParentStatus)
        {
            List<AWStatus> orderedChildren = _myAWParentStatus.MyAWStatus
                .OrderByDescending(x => x.Seconds)
                .ThenBy(x => x.WindowTitleName, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < orderedChildren.Count; i++)
            {
                orderedChildren[i].WindowTitleOrder = i;
            }
        }

        #endregion

        #region 橫條顯示

        /// <summary>
        /// 更新所有視窗使用統計橫條的顯示：
        /// 先隱藏全部橫條，再依排名順序顯示前 MaxProcessesToRender 名的父層與其展開的子層。
        /// 第一條橫條（m_FirstBar）顯示未被追蹤的時間（Idle/Unknown）。
        /// </summary>
        private void SetAWParentStatusBars()
        {
            // 計算未被前景視窗追蹤的時間差
            int tmpT = (int)DateTime.Now.Subtract(m_FirstStartTime).Subtract(m_TotalAWAccumulateTime).TotalSeconds;
            SetPicValue(m_FirstBar, tmpT <= 0 ? "Idle/Unknown usage" : "Time not tracked by app", tmpT);

            // 先全部隱藏
            foreach (AWParentStatus parent in m_AWParentStatus)
            {
                parent.ProcessPlusButton.Visible = false;
                parent.ProcessPBox.Visible = false;
                foreach (AWStatus child in parent.MyAWStatus)
                {
                    child.WindowTitlePBox.Visible = false;
                }
            }

            // 依排名順序顯示前 MaxProcessesToRender 名
            int tmpCurrentRow = 1;
            foreach (AWParentStatus parent in m_AWParentStatus.OrderBy(x => x.ProcessOrder).Take(MaxProcessesToRender))
            {
                tmpCurrentRow = ShowParentPicBox(parent, tmpCurrentRow);
                tmpCurrentRow = SetAWStatusBars(parent, tmpCurrentRow, parent.ProcessPlusOr);
            }
        }

        /// <summary>
        /// 更新第一條橫條（空閒時間）的顯示內容與填充比例。
        /// </summary>
        public void SetPicValue(BarChartBox picBar, string awName, int value)
        {
            TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, value));
            string text = t.ToString(@"hh\:mm\:ss") + " : " + awName;
            float ratio = CalculateRatio(value);
            picBar.SetBar(text, ratio, m_PBarBackColor, m_PBarForeColor);
        }

        /// <summary>
        /// 在指定列（rowIndex）顯示父層程序的橫條與展開按鈕，並回傳下一列的索引。
        /// </summary>
        public int ShowParentPicBox(AWParentStatus _awParentStatus, int _currentRow)
        {
            TimeSpan t = TimeSpan.FromSeconds(_awParentStatus.Seconds);
            string text = t.ToString(@"hh\:mm\:ss") + " : " + _awParentStatus.ProcessOrder + " : " + _awParentStatus.ProcessName;

            int rowTop = _currentRow * 30;
            _awParentStatus.ProcessPlusButton.Location = new Point(330, rowTop);
            _awParentStatus.ProcessPlusButton.Visible = true;
            _awParentStatus.ProcessPBox.Location = new Point(360, rowTop);
            _awParentStatus.ProcessPBox.Size = new Size(Math.Max(1000, ClientSize.Width - 360), 30);
            _awParentStatus.ProcessPBox.Visible = true;
            _awParentStatus.ProcessPBox.SetBar(text, CalculateRatio(_awParentStatus.Seconds), m_PBarBackColor, m_PBarForeColor);

            return _currentRow + 1;
        }

        /// <summary>
        /// 若父層處於展開狀態（ProcessPlusOr = false），逐一顯示各子層橫條。
        /// </summary>
        private int SetAWStatusBars(AWParentStatus _awParentStatus, int _tmpCurrentRow, bool _processPlusOr)
        {
            if (_processPlusOr)
            {
                // 收起狀態，不顯示子層
                return _tmpCurrentRow;
            }

            foreach (AWStatus item in _awParentStatus.MyAWStatus.OrderBy(x => x.WindowTitleOrder))
            {
                _tmpCurrentRow = ShowPicBox(item, _tmpCurrentRow);
            }

            return _tmpCurrentRow;
        }

        /// <summary>
        /// 在指定列顯示子層（視窗標題）橫條，並回傳下一列的索引。
        /// </summary>
        public int ShowPicBox(AWStatus _awStatus, int _currentRow)
        {
            TimeSpan t = TimeSpan.FromSeconds(_awStatus.Seconds);
            string text = t.ToString(@"hh\:mm\:ss") + " : " + _awStatus.WindowTitleOrder + " : " + _awStatus.WindowTitleName;

            _awStatus.WindowTitlePBox.Location = new Point(430, _currentRow * 30);
            _awStatus.WindowTitlePBox.Size = new Size(Math.Max(930, ClientSize.Width - 430), 30);
            _awStatus.WindowTitlePBox.Visible = true;
            _awStatus.WindowTitlePBox.SetBar(text, CalculateRatio(_awStatus.Seconds), m_PBarBackColor, m_PBarForeColor);

            return _currentRow + 1;
        }

        /// <summary>
        /// 計算某秒數佔總前景活動時長的比例（0~1），用於橫條填充。
        /// 若總時長為 0 則回傳 0，避免除以零。
        /// </summary>
        private float CalculateRatio(int value)
        {
            double totalSeconds = m_TotalAWAccumulateTime.TotalSeconds;
            if (totalSeconds <= 0)
            {
                return 0f;
            }

            return Math.Clamp((float)(Math.Max(0, value) / totalSeconds), 0f, 1f);
        }

        /// <summary>
        /// 依目前 Work/Rest 狀態與佈景色彩模式，更新視窗內所有控制項的背景色與前景色。
        /// 效能橫條的背景色稍深（80%），與一般控制項區別。
        /// 同時更新 m_PBarBackColor / m_PBarForeColor 供後續橫條重繪使用。
        /// </summary>
        void ChangeFormColor()
        {
            Color tmpForeColor = Color.FromArgb(50, 50, 50);
            Color tmpBackColor = Color.FromArgb(200, 200, 200);

            if (m_WorkStates == eWorkStates.WORK)
            {
                switch (m_BackColorMode)
                {
                    case eColor.DefaultTomato:
                        tmpBackColor = Color.Tomato;
                        tmpForeColor = Color.Brown;
                        break;
                    case eColor.Grass:
                        tmpBackColor = Color.YellowGreen;
                        tmpForeColor = Color.DarkGreen;
                        break;
                    case eColor.Sky:
                        tmpBackColor = Color.MediumTurquoise;
                        tmpForeColor = Color.SteelBlue;
                        break;
                    case eColor.Gray:
                        tmpBackColor = Color.DimGray;
                        tmpForeColor = Color.Black;
                        break;
                    default:
                        break;
                }
            }
            else // REST 狀態固定使用天空藍
            {
                tmpBackColor = Color.SkyBlue;
                tmpForeColor = Color.SteelBlue;
            }

            // 遍歷視窗內所有控制項並套用顏色
            foreach (Control tempcon in this.Controls)
            {
                if (tempcon is Label)
                {
                    tempcon.BackColor = tmpBackColor;
                    // 計時標籤使用較亮的暖白色，增加可讀性
                    tempcon.ForeColor = (tempcon.Name == "labelTimer" || tempcon.Name == "labelTotalTimer")
                        ? Color.FromArgb(255, 224, 192)
                        : tmpForeColor;
                }
                else if (tempcon is Button || tempcon is PictureBox)
                {
                    // 效能橫條使用略深的背景色
                    if (tempcon == m_BarCPU || tempcon == m_BarRAM || tempcon == m_BarDisk
                        || tempcon == m_BarGPU || tempcon == m_BarVRAM)
                    {
                        tempcon.BackColor = Color.FromArgb(
                            (int)(tmpBackColor.R * 0.8),
                            (int)(tmpBackColor.G * 0.8),
                            (int)(tmpBackColor.B * 0.8));
                        continue;
                    }
                    tempcon.BackColor = tmpBackColor;
                    tempcon.ForeColor = tmpForeColor;
                }
            }

            this.BackColor = tmpBackColor;

            // 橫條背景色比視窗背景色亮 30，提升視覺層次感
            m_PBarBackColor = Color.FromArgb(
                (int)MathF.Min(255, tmpBackColor.R + 30),
                (int)MathF.Min(255, tmpBackColor.G + 30),
                (int)MathF.Min(255, tmpBackColor.B + 30));
            m_PBarForeColor = tmpForeColor;
        }

        #endregion

        #region 按鈕事件處理

        /// <summary>
        /// Start / Pause / Continue 按鈕點擊：
        /// - 第一次點擊記錄 m_FirstStartTime，啟動兩個計時器
        /// - 再次點擊暫停，累計已用時長到 m_TotalAccumulateTime 與 m_PhaseAccumulateTime
        /// </summary>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (m_FirstStartTime == DateTime.MinValue)
            {
                m_FirstStartTime = DateTime.Now;
                m_PhaseStartTime = DateTime.Now;
            }

            if (buttonStart.Text == "Start" || buttonStart.Text == "Continue")
            {
                buttonStart.Text = "Pause";
                m_NewStartTime = DateTime.Now;
                m_NewPhaseStartTime = DateTime.Now;
                m_TimerMain.Enabled = true;
                m_TimerActiveWindow.Enabled = true;
            }
            else
            {
                buttonStart.Text = "Continue";
                m_TimerMain.Enabled = false;
                m_TimerActiveWindow.Enabled = false;
                // 將本次段落時長累計至總時長與階段時長
                m_TotalAccumulateTime += DateTime.Now.Subtract(m_NewStartTime);
                m_PhaseAccumulateTime += DateTime.Now.Subtract(m_NewPhaseStartTime);
            }
        }

        /// <summary>
        /// Start 按鈕尺寸改變時觸發，確保按鈕在正常或縮小模式下都能水平置中。
        /// </summary>
        private void buttonStart_SizeChanged(object sender, EventArgs e)
        {
            if (!m_MinimumSizeOr)
            {
                buttonStart.Location = new Point(170 - buttonStart.Size.Width / 2, buttonStart.Location.Y);
            }
            else
            {
                buttonStart.Location = new Point(90 - buttonStart.Size.Width / 2, buttonStart.Location.Y);
            }
        }

        /// <summary>
        /// 展開/收起程序子列的「+/-」按鈕點擊：
        /// 切換 ProcessPlusOr 旗標並更換按鈕圖示。
        /// </summary>
        public void buttonPlus_Click(object? sender, EventArgs e)
        {
            if (sender is not Button tmpbutton || tmpbutton.Tag is not AWParentStatus tmpAWP)
            {
                return;
            }

            tmpAWP.ProcessPlusButton.Image = tmpAWP.ProcessPlusOr
                ? imageListPlus.Images[1]   // 切換為「-」（展開）圖示
                : imageListPlus.Images[0];  // 切換為「+」（收起）圖示
            tmpAWP.ProcessPlusOr = !tmpAWP.ProcessPlusOr;
        }

        /// <summary>
        /// 透明度按鈕：每次點擊依序切換 100% → 75% → 50% → 25% → 回到 100%。
        /// </summary>
        private void buttonOpacity_Click(object sender, EventArgs e)
        {
            Opacity -= 0.25;
            switch (Opacity)
            {
                case 1:    buttonOpacity.Text = "100%"; break;
                case 0.75: buttonOpacity.Text = "75%";  break;
                case 0.5:  buttonOpacity.Text = "50%";  break;
                case 0.25: buttonOpacity.Text = "25%";  break;
                default:
                    buttonOpacity.Text = "100%";
                    Opacity = 1;
                    break;
            }
        }

        /// <summary>
        /// PIN/UNPIN 按鈕：切換視窗最上層顯示，並記錄使用者偏好至 m_TopMost。
        /// </summary>
        private void buttonAlwaysTop_Click(object sender, EventArgs e)
        {
            TopMost = !TopMost;
            buttonAlwaysTop.Text = TopMost ? "PIN" : "UNPIN";
            toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
            m_TopMost = TopMost;
        }

        /// <summary>
        /// 背景色彩按鈕：循環切換佈景主題（DefaultTomato → Grass → Sky → Gray → DefaultTomato）。
        /// </summary>
        private void buttonBackColor_Click(object sender, EventArgs e)
        {
            m_BackColorMode++;
            if (m_BackColorMode.ToString() == "MAX")
            {
                m_BackColorMode = eColor.DefaultTomato;
            }
            ChangeFormColor();
        }

        /// <summary>
        /// 說明按鈕：開啟說明視窗（formHelp），若已開啟則將其帶到最前面。
        /// 同時設定說明視窗的事件訂閱（測試圖片、開啟統計分析）。
        /// </summary>
        private void buttonHelp_Click(object sender, EventArgs e)
        {
            if (m_FormHelp != null && !m_FormHelp.IsDisposed)
            {
                m_FormHelp.BringToFront();
                m_FormHelp.Activate();
                return;
            }

            m_FormHelp = new formHelp();
            m_FormHelp.TestRestImageRequested += () => ShowRestReminderImage(true);
            m_FormHelp.OpenUsageAnalysisRequested += () =>
            {
                if (m_FormUsageAnalysis != null && !m_FormUsageAnalysis.IsDisposed)
                {
                    m_FormUsageAnalysis.BringToFront();
                    m_FormUsageAnalysis.Activate();
                    return;
                }
                m_FormUsageAnalysis = new FormUsageAnalysis();
                m_FormUsageAnalysis.FormClosed += (_, _) => m_FormUsageAnalysis = null;
                m_FormUsageAnalysis.Show(this);
            };
            m_FormHelp.FormClosed += (_, _) => m_FormHelp = null;
            m_FormHelp.Show(this);
        }

        /// <summary>
        /// 縮小/還原按鈕：在正常模式與右下角迷你模式之間切換。
        /// </summary>
        private void buttonMinimumSize_Click(object sender, EventArgs e)
        {
            if (m_MinimumSizeOr)
            {
                m_MinimumSizeOr = false;
                buttonMinimumSize.Text = "◢";
                SetFormSizeNormal();
            }
            else
            {
                buttonMinimumSize.Text = "◤";
                m_MinimumSizeOr = true;
                SetFormSizeMini();
            }
        }

        /// <summary>
        /// 離開按鈕：關閉子視窗後，以動畫縮小視窗再結束程式。
        /// </summary>
        private void buttonQuit_Click(object sender, EventArgs e)
        {
            CloseChildWindowsInOrder();
            MinimumSize = new Size(MinimumSize.Width, 40);
            int tmpHeight = this.Size.Height;
            for (int i = 0; i < (tmpHeight - 40) / 2; i++)
            {
                this.Size = new Size(this.Size.Width, tmpHeight - (i * 2));
            }
            Application.Exit();
        }

        /// <summary>
        /// 測試按鈕（開發用）：強制顯示一次休息圖片覆蓋。
        /// </summary>
        private void buttonTest_Click(object sender, EventArgs e)
        {
            ShowRestReminderImage(true);
        }

        #endregion

        #region 子視窗管理與關閉處理

        /// <summary>
        /// 依序關閉並釋放統計分析視窗與說明視窗。
        /// 使用旗標 m_ClosingChildWindows 避免重複執行（如 OnFormClosing 與 buttonQuit 同時觸發）。
        /// </summary>
        private void CloseChildWindowsInOrder()
        {
            if (m_ClosingChildWindows)
            {
                return;
            }
            m_ClosingChildWindows = true;

            var analysis = m_FormUsageAnalysis;
            m_FormUsageAnalysis = null;
            if (analysis != null && !analysis.IsDisposed)
            {
                analysis.Close();
                analysis.Dispose();
            }

            var help = m_FormHelp;
            m_FormHelp = null;
            if (help != null && !help.IsDisposed)
            {
                help.Close();
                help.Dispose();
            }
        }

        /// <summary>
        /// 視窗關閉前：關閉子視窗、寫入今日使用紀錄、停止並釋放計時器與覆蓋圖片。
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseChildWindowsInOrder();
            WriteUsageLogForDate(m_CurrentDate);
            CloseAllRestImageOverlays();
            m_TimerMain?.Stop();
            m_TimerMain?.Dispose();
            m_TimerActiveWindow?.Stop();
            m_TimerActiveWindow?.Dispose();
            base.OnFormClosing(e);
        }

        #endregion
    }
}
