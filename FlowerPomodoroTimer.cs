using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

/*
 * 2026-03-19 更新說明：
 * 使用 OpenAI Codex優化程式碼結構，增加註解說明，修正一些潛在的問題。
 */

/*
 * 2021-04-06 大改版
 * 右側橫條改成有階層，父階層m_AWParentStatus紀錄視窗的應用程式名稱
 * 內含子階層MyAWStatus，紀錄視窗的分頁名稱
 * 以便於讓使用者知悉哪個應用程式的總使用時間
 * 也讓m_AWParentStatus內含橫條的PictureBox，以便於控制，雖然資源使用較多，(原來做法是僅使用20個PictureBox供前20名輪流使用)
 * 
 * 使用兩個計時器 m_TimerMain 總使用時間，m_TimerActiveWindow 僅用於視窗被使用時，
 * 兩者差異便是Window進入鎖定畫面，也就是玩家不在座位時間，記得設定個人化->鎖定畫面(三分鐘)。
   InitializeMainTimer();
   InitializeActiveWindowTimer();
 * m_TimerMain m_TimerMain_Tick()每1/10秒計算一次，更動顯示秒數。
 * m_TimerActiveWindow timerActiveWindow_Tick()每一秒計算一次，
   呼叫CalAWParentSec()統計新的Active視窗或已有的Active視窗使用秒數。
   呼叫SortAWParentStatus()根據使用秒數來排序，中途呼叫SortAWStatus()排序子階層
   呼叫ShowAWParentStatusBars()顯示BAR條
 */
namespace Flower_Pomodoro_Timer
{
    
    public partial class formFlowerPomodoroTimer : Form
    {
        #region 變數
        readonly WorkSchedule m_ScheduleService = new WorkSchedule();
        List<WorkSchedule> workSchedules = new List<WorkSchedule>();

        BarChartBox m_FirstBar = new BarChartBox(); //第一條BAR
        System.Windows.Forms.Timer m_TimerMain = null!; //主要計時器，使用情況統計，切換Work或Rest狀態
        System.Windows.Forms.Timer m_TimerActiveWindow = null!; //視窗使用情況統計計時器

        DateTime m_FirstStartTime; //第一次起始時間
        DateTime m_NewStartTime; //暫停後之重新開始新一次起始時間
        TimeSpan m_TotalAccumulateTime; //累計每次開始暫停的時間，用於系統統計總時間，最後系統時間為 DateTime.Now.Subtract(m_NewStartTime)+m_TotalAccumulateTime
        TimeSpan m_TotalAWAccumulateTime; //累計 Active Window 使用時間，扣除螢幕保護程式時間
        DateTime m_PhaseStartTime; //階段性起始時間（如55分鐘工作或5分鐘休息）
        DateTime m_NewPhaseStartTime; //暫停後之重新開始新一次起始時間
        TimeSpan m_PhaseAccumulateTime; //每階段累計秒數，用於系統統計總時間，最後系統統計為 m_NewPhaseStartTime-NOW+m_PhaseAccumulateTime

        bool m_MinimumSizeOr = false; //視窗是否最小化

        enum eWorkStates
        {
            WORK,
            REST,
            PAUSE
        }
        eWorkStates m_WorkStates;
        bool m_TopMost;

        public class AWParentStatus //使用 class 而非 struct，因為 List 內的 struct 成員值無法直接修改。如果用 struct P，給定值數值後，須讓List struct[0]=P
        {
            public int Seconds; //使用秒數
            public string ProcessName = string.Empty; //程式名稱：Chrome 或 IE
            public int ProcessOrder; //依使用時間排序，給 TreeView 上層用
            public Button ProcessPlusButton = new Button(); //展開按鈕
            public bool ProcessPlusOr = true;
            public BarChartBox ProcessPBox = new BarChartBox();
            public List<AWStatus> MyAWStatus = new List<AWStatus>();
            public Dictionary<string, AWStatus> WindowTitleMap = new Dictionary<string, AWStatus>(StringComparer.Ordinal);
        }
        List<AWParentStatus> m_AWParentStatus = new List<AWParentStatus>(); //存放所有上層視窗資訊
        readonly Dictionary<string, AWParentStatus> m_AWParentByProcess = new Dictionary<string, AWParentStatus>(StringComparer.OrdinalIgnoreCase);
        public class AWStatus //使用 class 而非 struct，因為 List 內的 struct 成員值無法直接修改。如果用 struct P，給定值數值後，須讓List struct[0]=P
        {
            public int Seconds; //使用秒數
            public string ProcessName = string.Empty; //程式名稱：Chrome 或 IE
            public string WindowTitleName = string.Empty; //視窗名稱：Facebook...
            public int WindowTitleOrder; //依使用時間排序，給 TreeView 子層用
            public BarChartBox WindowTitlePBox = new BarChartBox();
        }
        string m_LastAWParentFullName = "";
        int m_LastAWParentIndex = -1;
 

        enum eColor
        {
            DefaultTomato,
            Grass,
            Sky,
            Gray, 
            MAX

        }
        eColor m_BackColorMode;
        Color m_PBarBackColor = Color.Tomato;
        Color m_PBarForeColor = Color.Brown;

        #region 效能監控變數
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
        List<PerformanceCounter> m_VramLimitCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_SharedVramUsageCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_SharedVramLimitCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_ProcessDedicatedVramUsageCounters = new List<PerformanceCounter>();
        List<PerformanceCounter> m_ProcessSharedVramUsageCounters = new List<PerformanceCounter>();
        float m_DedicatedVramTotalGb = 0f;
        readonly Random m_Random = new Random();
        string m_LastRestImagePath = string.Empty;
        RestImageOverlayForm? m_RestImageOverlay;
        formHelp? m_FormHelp;
        FormUsageAnalysis? m_FormUsageAnalysis;
        bool m_ClosingChildWindows;
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
        #endregion
        #endregion

        #region 取得上層視窗資訊
        // 取得上層視窗資訊，用來統計使用者執行哪種程式的時間
        //http://codingjames.blogspot.com/2010/09/cforegroundwindow.html
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] 
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        //https://ithelp.ithome.com.tw/articles/10198779
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);//取得視窗標題

        IntPtr LastWindow { get; set; }
        #endregion


        // 在 formFlowerPomodoroTimer 建構子中，將工作排程載入檢查
        public formFlowerPomodoroTimer()
        {
            string workListPath = Path.Combine(AppContext.BaseDirectory, "WorkList.txt");
            workSchedules = m_ScheduleService.LoadWorkSchedules(workListPath); //讀取工作排程
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;  // 改這行
            // 預設是 AutoScaleMode.Font，有時會造成元件重疊
            InitializeFirstBar();
            InitializeMainTimer();
            InitializeActiveWindowTimer();
            SetFormSizeNormal();
            InitializePerformanceMonitoring();
            ChangeFormColor();
            m_TopMost = TopMost;
        }
        // 設定視窗尺寸為正常大小
        private void SetFormSizeNormal()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            // Retrieve the working rectangle from the Screen class
            // using the PrimaryScreen and the WorkingArea properties.
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
            System.Drawing.Rectangle workingRectangle = screen.WorkingArea;
            // Set the size of the form slightly less than size of 
            // working rectangle.
            MinimumSize = new Size(1360, 400);
            MaximumSize = new Size(1360, 800);
            Size = new Size(1360, 400);
            //this.Size = new System.Drawing.Size(workingRectangle.Width - 10, workingRectangle.Height - 10);
            // Set the location so the entire form is visible.
            //this.Location = new System.Drawing.Point(5, 5);
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

            int barY = 180;
            int barWidth = 310;
            int barHeight = 25;
            int spacing = 3;

            m_BarCPU.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarRAM.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarDisk.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarGPU.Bounds = new Rectangle(20, barY, barWidth, barHeight); barY += barHeight + spacing;
            m_BarVRAM.Bounds = new Rectangle(20, barY, barWidth, barHeight);

            m_FirstBar.Location = new Point(360, 0);
            m_FirstBar.Size = new Size(Math.Max(1000, ClientSize.Width - 360), 30);
        }
        // 縮小視窗至右下角，僅顯示主要計時器及總計時器
        private void SetFormSizeMini()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            // Retrieve the working rectangle from the Screen class
            // using the PrimaryScreen and the WorkingArea properties.
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
            System.Drawing.Rectangle workingRectangle = screen.WorkingArea;
            // Set the size of the form slightly less than size of 
            // working rectangle.
            MinimumSize = new Size(240, 240);
            MaximumSize = new Size(1360, 720);
            Size = new Size(240, 240);
            //this.Size = new System.Drawing.Size(workingRectangle.Width - 10, workingRectangle.Height - 10);
            // Set the location so the entire form is visible.
            //this.Location = new System.Drawing.Point(5, 5);
            Point newPosition = new Point(0, 0);
            newPosition.X = workingRectangle.Width - MinimumSize.Width;
            newPosition.Y = workingRectangle.Height - MinimumSize.Height;
            this.Location = newPosition;

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

            // Increase window height to accommodate the bars
            Height = barY + barHeight + 10;
            MinimumSize = new Size(180, Height);
            
            // Adjust position to stay at the bottom right
            this.Location = new Point(workingRectangle.Width - this.Width, workingRectangle.Height - this.Height);
        }
        public void InitializeFirstBar() //初始化第一條橫條，顯示空閒時間
        {
            Controls.Add(m_FirstBar);
            m_FirstBar.Location = new Point(360, 0);
            m_FirstBar.Size = new Size(1000, 30);
            m_FirstBar.ForeColor = Color.DodgerBlue;
        }

        private void InitializeMainTimer() //初始化主要計時器
        {
            // Call this procedure when the application starts.  
            // Set to 1 second.  
            m_TimerMain = new System.Windows.Forms.Timer();
            m_TimerMain.Interval = 1000;
            m_TimerMain.Tick += new EventHandler(TimerMain_Tick);

            // Enable timer.  
            m_FirstStartTime = DateTime.MinValue; //等到第一次按下 Start 時再設定目標時間
            m_TotalAccumulateTime = TimeSpan.Zero;
            m_TotalAWAccumulateTime = TimeSpan.Zero;
            m_PhaseAccumulateTime = TimeSpan.Zero;

            m_WorkStates = eWorkStates.WORK;
        }

        private void InitializeActiveWindowTimer() //視窗使用情況統計計時器
        {
            m_TimerActiveWindow = new System.Windows.Forms.Timer();
            m_TimerActiveWindow.Interval = 1000;
            m_TimerActiveWindow.Tick += new EventHandler(TimerActiveWindow_Tick);
            LastWindow = IntPtr.Zero;
        }

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

            m_BarVRAM.Cursor = Cursors.Hand;
            m_BarVRAM.Click += (s, e) => 
            { 
                RefreshGpuCounters(); 
                UpdatePerformanceInfo(); 
            };
        }

        private void DisposeCounters(List<PerformanceCounter> counters)
        {
            foreach (var counter in counters)
            {
                try { counter.Dispose(); } catch { }
            }
            counters.Clear();
        }

        private void RefreshGpuCounters()
        {
            try
            {
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

                var vramCategory = new PerformanceCounterCategory("GPU Adapter Memory");
                var vramNames = vramCategory.GetInstanceNames();
                DisposeCounters(m_VramUsageCounters);
                DisposeCounters(m_VramLimitCounters);
                DisposeCounters(m_SharedVramUsageCounters);
                DisposeCounters(m_SharedVramLimitCounters);
                foreach (var name in vramNames)
                {
                    try { m_VramUsageCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", name)); } catch { }
                    // "Dedicated Limit" and "Shared Limit" do not exist in standard Windows GPU performance counters,
                    // so we do not create them to avoid InvalidOperationExceptions.
                    try { m_SharedVramUsageCounters.Add(new PerformanceCounter("GPU Adapter Memory", "Shared Usage", name)); } catch { }
                }

                var processVramCategory = new PerformanceCounterCategory("GPU Process Memory");
                var processVramNames = processVramCategory.GetInstanceNames();
                DisposeCounters(m_ProcessDedicatedVramUsageCounters);
                DisposeCounters(m_ProcessSharedVramUsageCounters);
                foreach (var name in processVramNames)
                {
                    try { m_ProcessDedicatedVramUsageCounters.Add(new PerformanceCounter("GPU Process Memory", "Dedicated Usage", name)); } catch { }
                    try { m_ProcessSharedVramUsageCounters.Add(new PerformanceCounter("GPU Process Memory", "Shared Usage", name)); } catch { }
                }

                // Prime counters once. First NextValue is often stale/zero for rate-style counters.
                foreach (var counter in m_GpuUsageCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_VramUsageCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_VramLimitCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_SharedVramUsageCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_SharedVramLimitCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_ProcessDedicatedVramUsageCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
                foreach (var counter in m_ProcessSharedVramUsageCounters)
                {
                    try { counter.NextValue(); } catch { }
                }
            }
            catch { }
        }

        private void UpdatePerformanceInfo()
        {
            // 1. CPU
            float cpuVal = 0;
            try { cpuVal = m_CpuCounter?.NextValue() ?? 0; } catch { }
            m_BarCPU.SetBar($"CPU: {cpuVal:F1}%", cpuVal / 100f, Color.LimeGreen, Color.White);

            // 2. RAM
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                double totalGB = memStatus.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
                double usedGB = (memStatus.ullTotalPhys - memStatus.ullAvailPhys) / 1024.0 / 1024.0 / 1024.0;
                float ramPercent = memStatus.dwMemoryLoad;
                m_BarRAM.SetBar($"RAM: {usedGB:F2}/{totalGB:F1} GB ({ramPercent}%)", ramPercent / 100f, Color.DeepSkyBlue, Color.White);
            }

            // 3. Disk
            float rVal = 0, wVal = 0;
            try { rVal = m_DiskReadCounter?.NextValue() ?? 0; wVal = m_DiskWriteCounter?.NextValue() ?? 0; } catch { }
            float rMB = rVal / 1024f / 1024f;
            float wMB = wVal / 1024f / 1024f;
            m_BarDisk.SetBar($"Disk: R:{rMB:F1} W:{wMB:F1} MB/s", Math.Clamp((rMB + wMB) / 100f, 0, 1), Color.Orange, Color.White);

            // 4. GPU
            float gpuVal = 0;
            foreach (var counter in m_GpuUsageCounters)
            {
                try { gpuVal += counter.NextValue(); } catch { }
            }
            gpuVal = Math.Clamp(gpuVal, 0f, 100f);
            m_BarGPU.SetBar($"GPU: {gpuVal:F1}%", gpuVal / 100f, Color.MediumPurple, Color.White);

            // 5. VRAM
            float vramVal = 0;
            foreach (var counter in m_VramUsageCounters)
            {
                try { vramVal += counter.NextValue(); } catch { }
            }
            float vramLimit = 0;
            foreach (var counter in m_VramLimitCounters)
            {
                try { vramLimit += counter.NextValue(); } catch { }
            }
            float sharedVramVal = 0;
            foreach (var counter in m_SharedVramUsageCounters)
            {
                try { sharedVramVal += counter.NextValue(); } catch { }
            }
            float sharedVramLimit = 0;
            foreach (var counter in m_SharedVramLimitCounters)
            {
                try { sharedVramLimit += counter.NextValue(); } catch { }
            }

            if (vramVal <= 0)
            {
                foreach (var counter in m_ProcessDedicatedVramUsageCounters)
                {
                    try { vramVal += counter.NextValue(); } catch { }
                }
            }
            if (sharedVramVal <= 0)
            {
                foreach (var counter in m_ProcessSharedVramUsageCounters)
                {
                    try { sharedVramVal += counter.NextValue(); } catch { }
                }
            }

            bool dedicatedAvailable = vramLimit > 0;
            bool sharedAvailable = sharedVramLimit > 0;
            const float usageSwitchThresholdBytes = 1f * 1024f * 1024f; // 1 MB

            string vramMode = "專屬GPU";
            bool dedicatedHasUsage = vramVal > usageSwitchThresholdBytes;
            bool sharedHasUsage = sharedVramVal > usageSwitchThresholdBytes;

            // Prefer the source that currently has real usage, so iGPU machines won't stay at Dedicated 0.0%.
            if (sharedHasUsage && !dedicatedHasUsage)
            {
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
                // If performance counters do not provide limit on this machine, use dedicated VRAM from WMI.
                vramTotalGB = m_DedicatedVramTotalGb > 0f ? m_DedicatedVramTotalGb : 8f;
            }
            float vramRatio = Math.Clamp(vramUsedGB / vramTotalGB, 0, 1);
            float vramPercent = vramRatio * 100f;
            string totalText = $"{vramTotalGB:F0}GB";
            m_BarVRAM.SetBar(
                $"VRAM:{vramUsedGB:F3}GB/{totalText} ({vramPercent:F3}%) ({vramMode})",
                vramRatio,
                Color.HotPink,
                Color.White);
        }

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
                    if (adapterKey == null)
                    {
                        continue;
                    }

                    foreach (string childName in adapterKey.GetSubKeyNames())
                    {
                        using RegistryKey? childKey = adapterKey.OpenSubKey(childName);
                        if (childKey == null)
                        {
                            continue;
                        }

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

        private static bool TryReadRegistryMemoryBytes(object? value, out ulong bytes)
        {
            bytes = 0;
            if (value == null)
            {
                return false;
            }

            switch (value)
            {
                case ulong u when u > 0:
                    bytes = u;
                    return true;
                case long l when l > 0:
                    bytes = (ulong)l;
                    return true;
                case uint ui when ui > 0:
                    bytes = ui;
                    return true;
                case int i when i > 0:
                    bytes = (uint)i;
                    return true;
                case byte[] arr when arr.Length >= 8:
                    bytes = BitConverter.ToUInt64(arr, 0);
                    return bytes > 0;
                default:
                    return ulong.TryParse(value.ToString(), out bytes) && bytes > 0;
            }
        }

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

            string selectedPath = PickRandomImagePath(imageFiles);
            try
            {
                m_RestImageOverlay?.Close();
                m_RestImageOverlay?.Dispose();
                m_RestImageOverlay = new RestImageOverlayForm(selectedPath);
                m_RestImageOverlay.FormClosed += (_, _) =>
                {
                    m_RestImageOverlay?.Dispose();
                    m_RestImageOverlay = null;
                };
                m_RestImageOverlay.Show();
                m_RestImageOverlay.BringToFront();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ShowRestReminderImage failed: " + ex.Message);
            }
        }

        private static string[] GetImageFiles(string folderPath)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
            return Directory.GetFiles(folderPath)
                .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        private string PickRandomImagePath(string[] imageFiles)
        {
            if (imageFiles.Length == 1)
            {
                m_LastRestImagePath = imageFiles[0];
                return imageFiles[0];
            }

            string[] candidates = imageFiles
                .Where(file => !file.Equals(m_LastRestImagePath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            string[] source = candidates.Length > 0 ? candidates : imageFiles;
            string picked = source[m_Random.Next(source.Length)];
            m_LastRestImagePath = picked;
            return picked;
        }

        private void WriteUsageLogForToday()
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "FlowerPomodoroTimer_Usage.log");
                UsageLogEntry entry = new UsageLogEntry
                {
                    Date = DateTime.Today.ToString("yyyy-MM-dd"),
                    GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TotalAppSeconds = (int)m_TotalAccumulateTime.TotalSeconds,
                    TotalActiveWindowSeconds = (int)m_TotalAWAccumulateTime.TotalSeconds,
                    Processes = m_AWParentStatus
                        .OrderByDescending(p => p.Seconds)
                        .Select(p => new UsageProcessEntry
                        {
                            ProcessName = p.ProcessName,
                            Seconds = p.Seconds
                        })
                        .ToList()
                };

                string json = JsonSerializer.Serialize(entry);
                File.AppendAllText(logPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteUsageLogForToday failed: " + ex.Message);
            }
        }

        private sealed class RestImageOverlayForm : Form
        {
            private readonly Image m_SourceImage;
            private readonly PictureBox m_PictureBox = new PictureBox();
            private float m_Scale = 1f;

            public RestImageOverlayForm(string imagePath)
            {
                m_SourceImage = Image.FromFile(imagePath);
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                ShowInTaskbar = false;
                BackColor = Color.Black;

                Rectangle screenBounds = Screen.PrimaryScreen?.Bounds ?? Screen.AllScreens.First().Bounds;
                Bounds = screenBounds;

                m_PictureBox.Image = m_SourceImage;
                m_PictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                m_PictureBox.BackColor = Color.Black;
                Controls.Add(m_PictureBox);

                m_PictureBox.Click += (_, _) => ShrinkImage();
                m_PictureBox.DoubleClick += (_, _) => Close();
                Click += (_, _) => ShrinkImage();
                DoubleClick += (_, _) => Close();

                ApplyScale();
            }

            private void ShrinkImage()
            {
                m_Scale *= 0.9f;
                if (m_Scale < 0.1f)
                {
                    m_Scale = 0.1f;
                }
                ApplyScale();
            }

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
        private void TimerMain_Tick(object? Sender, EventArgs e) //顯示計時秒數並決定是否切換工作或休息狀態 (Work 或 Rest)
        {
            UpdatePerformanceInfo();
            TimeSpan tmpTime = m_PhaseAccumulateTime + DateTime.Now.Subtract(m_NewPhaseStartTime);
            labelTimer.Text = tmpTime.ToString(@"mm\:ss");

            TimeSpan totalElapsed = m_TotalAccumulateTime;
            if (m_TimerMain.Enabled)
            {
                totalElapsed += DateTime.Now.Subtract(m_NewStartTime);
            }
            labelTotalTimer.Text = totalElapsed.ToString(@"hh\:mm\:ss");

            switch (m_WorkStates)
            {
                case eWorkStates.WORK:
                    if (tmpTime.Minutes >= 55)
                    {
                        ChangeWorkState(eWorkStates.REST);
                        TopMost = true;
                    }
                    break;
                case eWorkStates.REST:
                    if (tmpTime.Minutes >= 5)
                    {
                        ChangeWorkState(eWorkStates.WORK);
                        TopMost = m_TopMost;
                    }
                    break;
                case eWorkStates.PAUSE:
                default:
                    break;
            }

            if (TopMost)
            {
                buttonAlwaysTop.Text = "PIN";
                toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
            }
            else
            {
                buttonAlwaysTop.Text = "UNPIN";
                toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
            }
        }

        private void TimerActiveWindow_Tick(object? sender, EventArgs e) //偵測目前哪一個視窗在工作
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                int pId;
                GetWindowThreadProcessId(hwnd, out pId);
                if (pId == 0)
                {
                    return;
                }

                Process p = Process.GetProcessById(pId);
                const int nChars = 256;
                StringBuilder windowTitle = new StringBuilder(nChars);
                if (GetWindowText(hwnd, windowTitle, nChars) <= 0)
                {
                    return;
                }

                string currentWindowTitle = windowTitle.ToString().Trim();
                if (string.IsNullOrEmpty(currentWindowTitle))
                {
                    return;
                }

                string tmpAWFullName = p.ProcessName + ":" + currentWindowTitle;
                CalAWParentSec(tmpAWFullName, m_LastAWParentFullName, p.ProcessName, currentWindowTitle);
                SortAWParentStatus();
                SetAWParentStatusBars();
                LastWindow = hwnd;
            }
            catch (Exception)
            {
                // 前景視窗標題可能瞬時已消失，略過本輪統計即可
            }
        }

        private void ChangeWorkState(eWorkStates _workStates) //更改工作狀態（Work 或 Rest）
        {
            switch (_workStates)
            {
                case eWorkStates.WORK:
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.WORK;
                    if (m_RestImageOverlay != null)
                    {
                        m_RestImageOverlay.Close();
                        m_RestImageOverlay = null;
                    }
                    break;
                case eWorkStates.REST:
                    buttonStart.Enabled = true;
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.REST;
                    ShowRestReminderImage(false);
                    break;
                case eWorkStates.PAUSE:
                    break;
                default:
                    break;
            }
            ChangeFormColor();
            Activate();
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        public string GetCurrentTask(List<WorkSchedule> schedules)
        {
            var now = DateTime.Now;
            var today = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            var task = schedules.FirstOrDefault(s =>
                (s.Day == null || s.Day == today) &&
                currentTime >= s.Start && currentTime < s.End);

            return task?.Task ?? "(No Task)";
        }

                #region 使用秒數資訊
        private const int MaxProcessesToRender = 20;

        private void CalAWParentSec(string _AWFullName, string _LastAWFullName, string _ProcessName, string _WindowTitle) //計算每個視窗的使用秒數，並建立對應的橫條
        {
            if (string.IsNullOrWhiteSpace(_ProcessName) || string.IsNullOrWhiteSpace(_WindowTitle))
            {
                return;
            }

            if (!m_AWParentByProcess.TryGetValue(_ProcessName, out AWParentStatus? parent))
            {
                parent = CreateParentStatus(_ProcessName);
                m_AWParentByProcess[_ProcessName] = parent;
                m_AWParentStatus.Add(parent);
            }

            parent.Seconds++;

            if (!parent.WindowTitleMap.TryGetValue(_WindowTitle, out AWStatus? child))
            {
                child = CreateChildStatus(_ProcessName, _WindowTitle);
                parent.WindowTitleMap[_WindowTitle] = child;
                parent.MyAWStatus.Add(child);
                Controls.Add(child.WindowTitlePBox);
            }

            child.Seconds++;
            m_TotalAWAccumulateTime = m_TotalAWAccumulateTime.Add(TimeSpan.FromSeconds(1));
            m_LastAWParentFullName = _ProcessName;
            m_LastAWParentIndex = parent.ProcessOrder;
        }

        private AWParentStatus CreateParentStatus(string processName)//建立父層狀態，包含應用程式名稱及對應的橫條
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
            parent.ProcessPlusButton.Image = imageListPlus.Images[0];
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

        private void SortAWParentStatus() //對 AW 父層依使用秒數排序，取出前 20 名
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

        private void SortAWStatus(AWParentStatus _myAWParentStatus) //對 AW 子層依使用秒數排序
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

        #region 顯示橫條
        private void SetAWParentStatusBars() //設置父層橫條的數值
        {
            int tmpT = (int)DateTime.Now.Subtract(m_FirstStartTime).Subtract(m_TotalAWAccumulateTime).TotalSeconds;
            if (tmpT <= 0)
            {
                SetPicValue(m_FirstBar, "Idle/Unknown usage", tmpT);
            }
            else
            {
                SetPicValue(m_FirstBar, "Time not tracked by app", tmpT);
            }

            foreach (AWParentStatus parent in m_AWParentStatus)
            {
                parent.ProcessPlusButton.Visible = false;
                parent.ProcessPBox.Visible = false;
                foreach (AWStatus child in parent.MyAWStatus)
                {
                    child.WindowTitlePBox.Visible = false;
                }
            }

            int tmpCurrentRow = 1;
            foreach (AWParentStatus parent in m_AWParentStatus.OrderBy(x => x.ProcessOrder).Take(MaxProcessesToRender))
            {
                tmpCurrentRow = ShowParentPicBox(parent, tmpCurrentRow);
                tmpCurrentRow = SetAWStatusBars(parent, tmpCurrentRow, parent.ProcessPlusOr);
            }
        }

        public void SetPicValue(BarChartBox picBar, string awName, int value) //繪製 picBar 橫條，用於第一條顯示閒置時間
        {
            TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, value));
            string tString = t.ToString(@"hh\:mm\:ss");
            string text = tString + " : " + awName;
            float ratio = CalculateRatio(value);
            picBar.SetBar(text, ratio, m_PBarBackColor, m_PBarForeColor);
        }

        public int ShowParentPicBox(AWParentStatus _awParentStatus, int _currentRow) //繪製父層 picBar 橫條在第幾列
        {
            TimeSpan t = TimeSpan.FromSeconds(_awParentStatus.Seconds);
            string tString = t.ToString(@"hh\:mm\:ss");
            string text = tString + " : " + _awParentStatus.ProcessOrder + " : " + _awParentStatus.ProcessName;

            int rowTop = _currentRow * 30;
            _awParentStatus.ProcessPlusButton.Location = new Point(330, rowTop);
            _awParentStatus.ProcessPlusButton.Visible = true;
            _awParentStatus.ProcessPBox.Location = new Point(360, rowTop);
            _awParentStatus.ProcessPBox.Size = new Size(Math.Max(1000, ClientSize.Width - 360), 30);
            _awParentStatus.ProcessPBox.Visible = true;
            _awParentStatus.ProcessPBox.SetBar(text, CalculateRatio(_awParentStatus.Seconds), m_PBarBackColor, m_PBarForeColor);

            _currentRow++;
            return _currentRow;
        }

        private int SetAWStatusBars(AWParentStatus _awParentStatus, int _tmpCurrentRow, bool _processPlusOr) //設置子層橫條
        {
            if (_processPlusOr)
            {
                return _tmpCurrentRow;
            }

            foreach (AWStatus item in _awParentStatus.MyAWStatus.OrderBy(x => x.WindowTitleOrder))
            {
                _tmpCurrentRow = ShowPicBox(item, _tmpCurrentRow);
            }

            return _tmpCurrentRow;
        }

        public int ShowPicBox(AWStatus _awStatus, int _currentRow) //繪製子層 picBar 橫條在第幾列
        {
            TimeSpan t = TimeSpan.FromSeconds(_awStatus.Seconds);
            string tString = t.ToString(@"hh\:mm\:ss");
            string text = tString + " : " + _awStatus.WindowTitleOrder + " : " + _awStatus.WindowTitleName;

            _awStatus.WindowTitlePBox.Location = new Point(430, _currentRow * 30);
            _awStatus.WindowTitlePBox.Size = new Size(Math.Max(930, ClientSize.Width - 430), 30);
            _awStatus.WindowTitlePBox.Visible = true;
            _awStatus.WindowTitlePBox.SetBar(text, CalculateRatio(_awStatus.Seconds), m_PBarBackColor, m_PBarForeColor);

            _currentRow++;
            return _currentRow;
        }

        private float CalculateRatio(int value)
        {
            double totalSeconds = m_TotalAWAccumulateTime.TotalSeconds;
            if (totalSeconds <= 0)
            {
                return 0f;
            }

            float ratio = (float)(Math.Max(0, value) / totalSeconds);
            return Math.Clamp(ratio, 0f, 1f);
        }
        void ChangeFormColor() //更改視窗顏色
        {
            Color tmpForeColor = Color.FromArgb(50, 50, 50);
            Color tmpBackColor = Color.FromArgb(200, 200, 200);

            if (m_WorkStates == eWorkStates.WORK)
            {
                switch (m_BackColorMode)
                {
                    case eColor.DefaultTomato://預設番茄紅
                        //m_BackColorMode = eColor.Grass;
                        tmpBackColor = Color.Tomato;
                        tmpForeColor = Color.Brown;
                        break;
                    case eColor.Grass://草地綠
                        //m_BackColorMode = eColor.Sky;
                        tmpBackColor = Color.YellowGreen;
                        tmpForeColor = Color.DarkGreen;
                        break;
                    case eColor.Sky://天空藍
                        //m_BackColorMode = eColor.Gray;
                        tmpBackColor = Color.MediumTurquoise;
                        tmpForeColor = Color.SteelBlue;
                        break;
                    case eColor.Gray://灰色
                        //m_BackColorMode = eColor.DefaultTomato;
                        tmpBackColor = Color.DimGray;
                        tmpForeColor = Color.Black;
                        break;
                    default:
                        break;
                }
            }
            else //eWorkStates.Rest
            {
                tmpBackColor = Color.SkyBlue;
                tmpForeColor = Color.SteelBlue;
            }

            foreach (Control tempcon in this.Controls)//遍歷視窗內的所有控制項
            {
                if (tempcon is Label)
                {
                    tempcon.BackColor = tmpBackColor;
                    if (tempcon.Name == "labelTimer" || tempcon.Name == "labelTotalTimer")
                    {
                        tempcon.ForeColor = Color.FromArgb(255, 224, 192);
                    }
                    else
                    {
                        tempcon.ForeColor = tmpForeColor;
                    }
                }
                else if (tempcon is Button || tempcon is PictureBox)//處理按鈕及橫條
                {
                    if (tempcon == m_BarCPU || tempcon == m_BarRAM || tempcon == m_BarDisk || tempcon == m_BarGPU || tempcon == m_BarVRAM)
                    {
                        tempcon.BackColor = Color.FromArgb((int)(tmpBackColor.R * 0.8), (int)(tmpBackColor.G * 0.8), (int)(tmpBackColor.B * 0.8));
                        continue;
                    }
                    tempcon.BackColor = tmpBackColor;
                    tempcon.ForeColor = tmpForeColor;
                }
            }
            this.BackColor = tmpBackColor;
            
            Color tmpBC = Color.FromArgb(
                (int)MathF.Min(255,((int)tmpBackColor.R + 30))  ,
                (int)MathF.Min(255,((int)tmpBackColor.G + 30)),
                (int)MathF.Min(255, ((int)tmpBackColor.B + 30))
                );
            m_PBarBackColor = tmpBC;
            Color tmpFC = tmpForeColor;
            //Color tmpFC = Color.FromArgb(
            //    (int)((int)tmpForeColor.R * 0.3),
            //    (int)((int)tmpForeColor.G * 0.3),
            //    (int)((int)tmpForeColor.B * 0.3)
            //    );
            m_PBarForeColor = tmpFC;
        }
        #endregion

        #region ? 鍵觸發
        private void buttonStart_Click(object sender, EventArgs e) //Start 按鈕
        {
            if (m_FirstStartTime == DateTime.MinValue) //第一次按下開始鍵
            {
                m_FirstStartTime = DateTime.Now;
                m_PhaseStartTime = DateTime.Now;
                // Enable timer.  
                //m_TimerMain.Enabled = true;
            }

            if (buttonStart.Text == "Start" || buttonStart.Text == "Continue") //開始或繼續
            {
                buttonStart.Text = "Pause";
                m_NewStartTime = DateTime.Now;
                m_NewPhaseStartTime = DateTime.Now;
                //m_TotalAccumulateTime = m_NewStartTime.Subtract(DateTime.Now);
                m_TimerMain.Enabled = true;
                m_TimerActiveWindow.Enabled = true;

            }
            else
            {
                buttonStart.Text = "Continue";
                m_TimerMain.Enabled = false;
                m_TimerActiveWindow.Enabled = false;

                m_TotalAccumulateTime += DateTime.Now.Subtract(m_NewStartTime);
                m_PhaseAccumulateTime += DateTime.Now.Subtract(m_NewPhaseStartTime);
            }
        }

        private void buttonStart_SizeChanged(object sender, EventArgs e) //當畫面縮小至右下角時，也觸發調整 Start 按鈕尺寸
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

                public void buttonPlus_Click(object? sender, EventArgs e) //每行工作排程的 "+" 按鈕
        {
            if (sender is not Button tmpbutton || tmpbutton.Tag is not AWParentStatus tmpAWP)
            {
                return;
            }

            if (tmpAWP.ProcessPlusOr)
            {
                tmpAWP.ProcessPlusButton.Image = imageListPlus.Images[1];
            }
            else
            {
                tmpAWP.ProcessPlusButton.Image = imageListPlus.Images[0];
            }
            tmpAWP.ProcessPlusOr = !tmpAWP.ProcessPlusOr;
        }

        private void buttonOpacity_Click(object sender, EventArgs e) //設定透明度
        {
            Opacity -= 0.25;
            switch (Opacity)
            {
                case 1:
                    buttonOpacity.Text = "100%";
                    break;
                case 0.75:
                    buttonOpacity.Text = "75%";
                    break;
                case 0.5:
                    buttonOpacity.Text = "50%";
                    break;
                case 0.25:
                    buttonOpacity.Text = "25%";
                    break;
                case 0.0:
                    buttonOpacity.Text = "100%";
                    Opacity = 1;
                    break;
                default:
                    buttonOpacity.Text = "100%";
                    Opacity = 1;
                    break;
            }
        }

        private void buttonAlwaysTop_Click(object sender, EventArgs e) //最上層顯示
        {
            TopMost = !TopMost;
            if (TopMost)
            {
                buttonAlwaysTop.Text = "PIN";
                toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
            }
            else
            {
                buttonAlwaysTop.Text = "UNPIN";
                toolTipAll.SetToolTip(buttonAlwaysTop, "切換最上層顯示");
            }
            m_TopMost = TopMost; //紀錄本次調整的最上層顯示設定
        }

        private void buttonBackColor_Click(object sender, EventArgs e) //切換背景色彩
        {
            m_BackColorMode++;
            if (m_BackColorMode.ToString() == "MAX")
            {
                m_BackColorMode = eColor.DefaultTomato;
            }
            ChangeFormColor();
        }

        private void buttonHelp_Click(object sender, EventArgs e) //開啟說明視窗
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

        private void buttonMinimumSize_Click(object sender, EventArgs e) //讓視窗縮小至右下角
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

        private void buttonQuit_Click(object sender, EventArgs e) //離開
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

        private void buttonTest_Click(object sender, EventArgs e) //測試按鈕
        {
            ShowRestReminderImage(true);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseChildWindowsInOrder();

            WriteUsageLogForToday();
            m_RestImageOverlay?.Close();
            m_RestImageOverlay?.Dispose();
            m_RestImageOverlay = null;
            m_TimerMain?.Stop();
            m_TimerMain?.Dispose();
            m_TimerActiveWindow?.Stop();
            m_TimerActiveWindow?.Dispose();
            base.OnFormClosing(e);
        }
        #endregion
    }
}

