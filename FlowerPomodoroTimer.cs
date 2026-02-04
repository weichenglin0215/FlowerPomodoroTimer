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
        public formHelp m_FormHelp; //說明視窗
                                    // 修正 CS0120: 需要有物件參考，才可使用非靜態欄位、方法或屬性 'WorkSchedule.LoadWorkSchedules(string)'
                                    // 修正方式：先建立 WorkSchedule 實例，再呼叫其方法
        List<WorkSchedule> workSchedules = new WorkSchedule().LoadWorkSchedules("WorkList.txt"); //讀取工作清單，包含星期、開始時間、結束時間、工作項目
        // Update the instantiation of WorkSchedule to use an instance method instead of a static method.  
        // This resolves the CS0120 error by ensuring that the method is called on an object instance.  

        PictureBox m_FirstBar = new PictureBox(); //第一條BAR
        System.Windows.Forms.Timer m_TimerMain; //整體時間使用狀況的計時器(切換Work或Rest狀態)
        System.Windows.Forms.Timer m_TimerActiveWindow; //視窗使用狀況的計時器

        DateTime m_FirstStartTime; //第一次起始時間
        DateTime m_NewStartTime; //按下暫停之後的新一次起始時間
        TimeSpan m_TotalAccumulateTime; //累加每次開始至暫停之間的時間，如果要統計所有非暫停期間的總時間 DateTime.Now.Subtract(m_NewStartTime)+m_TotalAccumulateTime
        TimeSpan m_TotalAWAccumulateTime; //累加有Active Window的時間，排除螢幕保護程式啟動時間
        DateTime m_PhaseStartTime; //階段性起始時間，每25分鐘工作或5分鐘休息都稱為一個階段
        DateTime m_NewPhaseStartTime; //按下暫停之後的新一次起始時間
        TimeSpan m_PhaseAccumulateTime; //每階段的累加秒數，如果要統計所有非暫停期間的總時間 m_NewPhaseStartTime-NOW+m_PhaseAccumulateTime

        bool m_MinimumSizeOr = false; //視窗是否最小化

        enum eWorkStates
        {
            WORK,
            REST,
            PAUSE
        }
        eWorkStates m_WorkStates;
        bool m_TopMost;

        public class AWParentStatus //改用class是因為用struct會被限制無法直接指定List struct內部成員的值，需先生成一個struct P，指定成員數值後，再讓List struct[0]=P
        {
            public int Seconds; //使用秒數
            public string FullName; //程序名稱+視窗分頁名稱：Chrome:Facebook...
            public string ProcessName; //程序名稱：Chrome、IE
            public int ProcessOrder; //依使用秒數來排序，給TreeView父階層用
            public Button ProcessPlusButton = new Button(); //+號按鍵
            public bool ProcessPlusOr = true;
            public PictureBox ProcessPBox = new PictureBox();
            public List<AWStatus> MyAWStatus = new List<AWStatus>();
        }
        List<AWParentStatus> m_AWParentStatus = new List<AWParentStatus>(); //把找到過的動作
        public class AWStatus //改用class是因為用struct會被限制無法直接指定List struct內部成員的值，需先生成一個struct P，指定成員數值後，再讓List struct[0]=P
        {
            public int Seconds; //使用秒數
            public string FullName; //程序名稱+視窗分頁名稱：Chrome:Facebook...
            public string ProcessName; //程序名稱：Chrome、IE
            public string WindowTitleName; //視窗分頁名稱：Facebook...
            public int WindowTitleOrder; //依使用秒數來排序，給TreeView子階層用
            public PictureBox WindowTitlePBox = new PictureBox();
        }
        String m_LastAWParentFullName = "";
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
        #endregion

        #region 取得最上層視窗資訊
        //取得最上層視窗資訊，用來統計使用者執行各種程式的分鐘數。
        //http://codingjames.blogspot.com/2010/09/cforegroundwindow.html
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")] 
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        //https://ithelp.ithome.com.tw/articles/10198779
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);//取視窗title

        IntPtr LastWindow { get; set; }
        #endregion


        // 在 formFlowerPomodoroTimer 建構子中，List<WorkSchedule> workSchedules 初始化前加入防呆檢查
        public formFlowerPomodoroTimer()
        {
            string workListPath = "WorkList.txt";
            if (!File.Exists(workListPath))
            {
                // 自動建立 WorkList.txt 並寫入預設內容
                File.WriteAllText(workListPath,
                @"# 範例格式：星期,開始時間,結束時間,工作項目
                # 例如：Monday,08:00,12:00,早上工作
                # null 代表每天
                null,09:00,12:00,上午工作
                null,13:00,18:00,下午工作
                ");
            }
            workSchedules = new WorkSchedule().LoadWorkSchedules(workListPath); //讀取工作清單
            InitializeComponent();
            InitializeFirstBar();
            InitializeMainTimer();
            InitializeActiveWindowTimer();
            SetFormSizeNormal();
            ChangeFormColor();
            m_TopMost = TopMost;
        }
        private void SetFormSizeNormal()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            // Retrieve the working rectangle from the Screen class
            // using the PrimaryScreen and the WorkingArea properties.
            System.Drawing.Rectangle workingRectangle = Screen.PrimaryScreen.WorkingArea;
            // Set the size of the form slightly less than size of 
            // working rectangle.
            MinimumSize = new Size(1360, 360);
            MaximumSize = new Size(1360, 720);
            this.Size = new System.Drawing.Size(1360, 360);
            //this.Size = new System.Drawing.Size(workingRectangle.Width - 10, workingRectangle.Height - 10);
            // Set the location so the entire form is visible.
            //this.Location = new System.Drawing.Point(5, 5);
            Point newPosition = new Point(0, 0);
            newPosition.X = (workingRectangle.Width - this.Width) / 2;
            newPosition.Y = (workingRectangle.Height - this.Height) / 2;
            this.Location = newPosition;

            labelTimer.Size = new Size(310, 127);
            labelTimer.Font = new Font(labelTimer.Font.FontFamily, 90, labelTimer.Font.Style);
            labelTimer.Location = new Point(20, 25);

            buttonStart.Size = new Size(88, 40);
            buttonStart.Font = new Font(buttonStart.Font.FontFamily, 22, buttonStart.Font.Style);
            buttonStart.Location = new Point(125, 165);
            buttonStart_SizeChanged(null,null);

            labelTotalTimer.Size = new Size(310, 95);
            labelTotalTimer.Font = new Font(labelTimer.Font.FontFamily, 60, labelTimer.Font.Style);
            labelTotalTimer.Location = new Point(20, 220);
        }
        private void SetFormSizeMini()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            // Retrieve the working rectangle from the Screen class
            // using the PrimaryScreen and the WorkingArea properties.
            System.Drawing.Rectangle workingRectangle = Screen.PrimaryScreen.WorkingArea;
            // Set the size of the form slightly less than size of 
            // working rectangle.
            MinimumSize = new Size(180, 160);
            MaximumSize = new Size(1360, 720);
            this.Size = new System.Drawing.Size(180, 160);
            //this.Size = new System.Drawing.Size(workingRectangle.Width - 10, workingRectangle.Height - 10);
            // Set the location so the entire form is visible.
            //this.Location = new System.Drawing.Point(5, 5);
            Point newPosition = new Point(0, 0);
            newPosition.X = workingRectangle.Width - MinimumSize.Width;
            newPosition.Y = workingRectangle.Height - MinimumSize.Height;
            this.Location = newPosition;

            labelTimer.Size = new Size(155, 64);
            labelTimer.Font = new Font(labelTimer.Font.FontFamily, 45, labelTimer.Font.Style);
            labelTimer.Location = new Point(12, 25);

            buttonStart.Size = new Size(44, 24);
            buttonStart.Font = new Font(buttonStart.Font.FontFamily, 12, buttonStart.Font.Style);
            buttonStart.Location = new Point(60, 90);
            buttonStart_SizeChanged(null, null);

            labelTotalTimer.Size = new Size(140, 40);
            labelTotalTimer.Font = new Font(labelTimer.Font.FontFamily, 24, labelTimer.Font.Style);
            labelTotalTimer.Location = new Point(20, 120);
        }
        public void InitializeFirstBar() //初始化第一條橫條，顯示離開時間
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
            m_TimerMain.Interval = 100;
            m_TimerMain.Tick += new EventHandler(TimerMain_Tick);

            // Enable timer.  
            m_FirstStartTime = DateTime.MinValue; //等到第一次按下Start才會設定成目前時間
            m_TotalAccumulateTime = TimeSpan.Zero;
            m_TotalAWAccumulateTime = TimeSpan.Zero;
            m_PhaseAccumulateTime = TimeSpan.Zero;

            m_WorkStates = eWorkStates.WORK;
        }

        private void InitializeActiveWindowTimer() //視窗使用狀況的計時器
        {
            m_TimerActiveWindow = new System.Windows.Forms.Timer();
            m_TimerActiveWindow.Interval = 1000;
            m_TimerActiveWindow.Tick += new EventHandler(TimerActiveWindow_Tick);
            LastWindow = IntPtr.Zero;
        }

        private void TimerMain_Tick(object Sender, EventArgs e) //顯示整體秒數並決定是否切換狀態 Work或Rest
        {
            TimeSpan tmpTime = m_PhaseAccumulateTime + DateTime.Now.Subtract(m_NewPhaseStartTime);
            // Set the caption to the current time.  
            labelTimer.Text = tmpTime.ToString(@"mm\:ss");
            labelTotalTimer.Text = DateTime.Now.Subtract(m_FirstStartTime).ToString(@"hh\:mm\:ss");
            switch (m_WorkStates)
            {
                case eWorkStates.WORK:
                    //if (tmpTime.Seconds >= 10) //測試用超過10秒
                    if (tmpTime.Minutes >= 55) //超過55分鐘
                    {
                        ChangeWorkState(eWorkStates.REST);
                        TopMost = true; //最上層顯示
                    }
                    break;
                case eWorkStates.REST:
                    //if (tmpTime.Seconds >= 10) //測試用超過10秒
                    if (tmpTime.Minutes >= 5) //超過5分鐘
                    {
                        ChangeWorkState(eWorkStates.WORK);
                        TopMost = m_TopMost; //回歸上次調整的最上層顯示設定
                    }
                    break;
                case eWorkStates.PAUSE:
                    break;
                default:
                    break;
            }
            if (TopMost)
            {
                buttonAlwaysTop.Text = "╦";
                toolTipAll.SetToolTip(buttonAlwaysTop, "關閉最上層顯示");
            }
            else
            {
                buttonAlwaysTop.Text = "╩";
                toolTipAll.SetToolTip(buttonAlwaysTop, "開啟最上層顯示");
            }


        }
        private void TimerActiveWindow_Tick(object sender, EventArgs e) //偵測目前是哪一個視窗在工作
        {
            //取得最上層視窗資訊，用來統計使用者執行各種程式的分鐘數。
            //http://codingjames.blogspot.com/2010/09/cforegroundwindow.html
            // get foreground window handle
            IntPtr hwnd = GetForegroundWindow();
            //if (LastWindow != hwnd)
            //{
                /// get foreground process from handle
                int pId;
                GetWindowThreadProcessId(hwnd, out pId);
                Process p = Process.GetProcessById(pId);

            //https://ithelp.ithome.com.tw/articles/10198779
            const int nChars = 256;
                System.Text.StringBuilder WindowTitle = new System.Text.StringBuilder(nChars);
                if (GetWindowText(hwnd, WindowTitle, nChars) > 0)
                {
                    /* //有些視窗Title是簡體，某些字變成?，嘗試轉碼失敗，以後再解決
                    //System.Console.OutputEncoding = System.Text.Encoding.Unicode; //沒用反而是錯誤
                    //Console.WriteLine("測試轉碼之前 ProcessName：{0}+WindowTitle：{1}+AWFullName：{2}", p.ProcessName, WindowTitle.ToString(), p.ProcessName + ":" + WindowTitle.ToString());
                    //string tmpProcessName;
                    ////byte[] buffer = Encoding.GetEncoding("GB2312").GetBytes(p.ProcessName);
                    //byte[] buffer = Encoding.GetEncoding(936).GetBytes(p.ProcessName);
                    //tmpProcessName = Encoding.UTF8.GetString(buffer);
                    //string tmpWindowTitle;
                    //byte[] buffer2 = Encoding.GetEncoding(936).GetBytes(WindowTitle.ToString());
                    ////tmpWindowTitle = Encoding.UTF8.GetString(buffer2);
                    ////Byte[] buffer2 = Encoding.Default.GetBytes(WindowTitle.ToString());
                    //tmpWindowTitle = Encoding.Unicode.GetString(buffer2);
                    //string tmpAWFullName = tmpProcessName + ":" + tmpWindowTitle + "轉碼GB2312";
                    //Console.WriteLine("測試轉碼後 ProcessName：{0}+WindowTitle：{1}+AWFullName：{2}", tmpProcessName, tmpWindowTitle, tmpAWFullName);
                    //CalAWParentSec(tmpAWFullName, m_LastAWParentFullName, tmpProcessName, tmpWindowTitle); //計算使用秒數
                    */
                    string tmpAWFullName = p.ProcessName + ":" + WindowTitle.ToString();
                    CalAWParentSec(tmpAWFullName, m_LastAWParentFullName, p.ProcessName, WindowTitle.ToString()); //計算使用秒數
                    SortAWParentStatus();
                    SetAWParentStatusBars();
                }
                LastWindow = hwnd;
            //}

        }

        private void ChangeWorkState(eWorkStates _workStates) //更改工作狀態，Work或Rest
        {
            switch (_workStates)
            {
                case eWorkStates.WORK:
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.WORK;
                    break;
                case eWorkStates.REST:
                    buttonStart.Enabled = true;
                    m_PhaseStartTime = DateTime.Now;
                    m_NewPhaseStartTime = DateTime.Now;
                    m_PhaseAccumulateTime = TimeSpan.Zero;
                    m_WorkStates = eWorkStates.REST;
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

            return task?.Task ?? "目前無排定工作";
        }

        #region 處理秒數資料
        private void CalAWParentSec(string _AWFullName, string _LastAWFullName, string _ProcessName, string _WindowTitle) //計算每一個視窗的使用時間秒數，並創建新生成的狀態橫條
        {
            int tmpParentCount = 0; 
            int tmpCount = 0;
            bool findOr = false; //有沒有在清單中找到已儲存的ActiveWindowName
            while (m_AWParentStatus.Count > tmpParentCount) //在清單中找已儲存的ActiveWindowName
            {
                if (_ProcessName == m_AWParentStatus[tmpParentCount].ProcessName)
                {
                    bool findWindowTitleNameOr = false;
                    m_AWParentStatus[tmpParentCount].Seconds++;
                    //找到子階層，並添加秒數
                    foreach (var item in m_AWParentStatus[tmpParentCount].MyAWStatus)
                    {
                        if (item.WindowTitleName == _WindowTitle)//找到子階層，並添加秒數
                        {
                            item.Seconds++;
                            findWindowTitleNameOr = true;
                            break;
                        }
                    }
                    if (!findWindowTitleNameOr)//沒找到子階層，添加子階層
                    {
                        //添加子階層資料
                        AWStatus tmpAWStatus = new AWStatus();
                        tmpAWStatus.ProcessName = _ProcessName;
                        tmpAWStatus.WindowTitleName = _WindowTitle;
                        tmpAWStatus.Seconds = 1;
                        tmpAWStatus.WindowTitleOrder = m_AWParentStatus[tmpParentCount].MyAWStatus.Count;
                        tmpAWStatus.WindowTitlePBox = new PictureBox();
                        tmpAWStatus.WindowTitlePBox.Location = new Point(360, 0);
                        tmpAWStatus.WindowTitlePBox.Size = new Size(1000, 30);
                        tmpAWStatus.WindowTitlePBox.ForeColor = Color.DodgerBlue;
                        //tmpAWStatus.WindowTitlePBox.BorderStyle = BorderStyle.FixedSingle;

                        Controls.Add(tmpAWStatus.WindowTitlePBox);
                        m_AWParentStatus[tmpParentCount].MyAWStatus.Add(tmpAWStatus);
                    }

                    m_TotalAWAccumulateTime = m_TotalAWAccumulateTime.Add(TimeSpan.FromSeconds(1)); //累加一秒
                    m_LastAWParentFullName = _ProcessName;
                    m_LastAWParentIndex = tmpParentCount;
                    //SetProcessValue(MyProgressBars[tmpCount], MyAWStatus[tmpCount].Name, MyAWStatus[tmpCount].Seconds);
                    //建立新的應用視窗名稱或僅增加秒數
                    //CalAWSec();
                    findOr = true;
                    break;
                }
                tmpParentCount++;
            }
            if (!findOr) //新的視窗資料
            {
                //添加父階層資料
                AWParentStatus tmpAWParentStatus = new AWParentStatus();
                tmpAWParentStatus.ProcessName = _ProcessName;
                tmpAWParentStatus.Seconds = 1;
                tmpAWParentStatus.ProcessOrder = m_AWParentStatus.Count;
                //添加父階層按鍵
                tmpAWParentStatus.ProcessPlusButton = new Button();
                tmpAWParentStatus.ProcessPlusButton.FlatStyle = FlatStyle.Flat;
                tmpAWParentStatus.ProcessPlusButton.FlatAppearance.BorderSize = 0;
                tmpAWParentStatus.ProcessPlusButton.BackColor = m_PBarBackColor;
                tmpAWParentStatus.ProcessPlusButton.ForeColor = m_PBarForeColor;
                tmpAWParentStatus.ProcessPlusButton.Size = new Size(30, 30);
                tmpAWParentStatus.ProcessPlusButton.Image = imageListPlus.Images[0];
                tmpAWParentStatus.ProcessPlusButton.Click += new System.EventHandler(this.buttonPlus_Click);
                tmpAWParentStatus.ProcessPlusButton.Tag = tmpAWParentStatus;
                toolTipAll.SetToolTip(tmpAWParentStatus.ProcessPlusButton, "展開/收起");
                Controls.Add(tmpAWParentStatus.ProcessPlusButton);
                //添加父階層圖片
                tmpAWParentStatus.ProcessPBox = new PictureBox();
                tmpAWParentStatus.ProcessPBox.Location = new Point(360, 0);
                tmpAWParentStatus.ProcessPBox.Size = new Size(1000, 30);
                tmpAWParentStatus.ProcessPBox.ForeColor = Color.DodgerBlue;
                tmpAWParentStatus.ProcessPBox.BorderStyle = BorderStyle.FixedSingle;
                Controls.Add(tmpAWParentStatus.ProcessPBox);
                m_AWParentStatus.Add(tmpAWParentStatus);

                m_TotalAWAccumulateTime = m_TotalAWAccumulateTime.Add(TimeSpan.FromSeconds(1)); //累加一秒
                m_LastAWParentFullName = _ProcessName;
                m_LastAWParentIndex = m_AWParentStatus.Count - 1;
                //Console.WriteLine(MyAWStatus.Count);
                //CalAWSec();

                //添加子階層資料
                AWStatus tmpAWStatus = new AWStatus();
                tmpAWStatus.ProcessName = _ProcessName;
                tmpAWStatus.WindowTitleName = _WindowTitle;
                tmpAWStatus.Seconds = 1;
                tmpAWStatus.WindowTitleOrder = m_AWParentStatus[m_AWParentStatus.Count - 1].MyAWStatus.Count;
                //添加子階層圖片
                tmpAWStatus.WindowTitlePBox = new PictureBox();
                tmpAWStatus.WindowTitlePBox.Location = new Point(400, 0);
                tmpAWStatus.WindowTitlePBox.Size = new Size(960, 30);
                tmpAWStatus.WindowTitlePBox.ForeColor = Color.DodgerBlue;
                //tmpAWStatus.WindowTitlePBox.BorderStyle = BorderStyle.FixedSingle;

                Controls.Add(tmpAWStatus.WindowTitlePBox);
                m_AWParentStatus[m_AWParentStatus.Count - 1].MyAWStatus.Add(tmpAWStatus);
            }
        }

        private void SortAWParentStatus() //排序AW父階層的使用秒數順序，取出前20個。
        {
            for (int i = 0; i < m_AWParentStatus.Count; i++)
            {
                //排序父階層m_AWParentStatus
                for (int j = i+1; j < m_AWParentStatus.Count; j++) 
                {
                    if (
                        (m_AWParentStatus[i].Seconds <= m_AWParentStatus[j].Seconds 
                        && m_AWParentStatus[i].ProcessOrder <= m_AWParentStatus[j].ProcessOrder) 
                        ||(m_AWParentStatus[i].Seconds > m_AWParentStatus[j].Seconds
                        && m_AWParentStatus[i].ProcessOrder > m_AWParentStatus[j].ProcessOrder)
                        )
                    {
                        int tmpOrder = m_AWParentStatus[i].ProcessOrder;
                        m_AWParentStatus[i].ProcessOrder = m_AWParentStatus[j].ProcessOrder;
                        m_AWParentStatus[j].ProcessOrder = tmpOrder;
                    }
                }
                Console.WriteLine(m_AWParentStatus[i].ProcessName +  " : " + m_AWParentStatus[i].ProcessOrder);
                SortAWStatus(m_AWParentStatus[i]); //呼叫排序子階層
            }
        }

        private void SortAWStatus(AWParentStatus _myAWParentStatus) //排序AW子階層的使用秒數順序。
        {
            for (int i = 0; i < _myAWParentStatus.MyAWStatus.Count; i++)
            {
                //排序
                for (int j = 0; j < _myAWParentStatus.MyAWStatus.Count; j++)
                {
                    if (
                        (_myAWParentStatus.MyAWStatus[i].Seconds < _myAWParentStatus.MyAWStatus[j].Seconds 
                        && _myAWParentStatus.MyAWStatus[i].WindowTitleOrder < _myAWParentStatus.MyAWStatus[j].WindowTitleOrder)
                        //|| (_myAWParentStatus.MyAWStatus[i].Seconds > _myAWParentStatus.MyAWStatus[j].Seconds
                        //&& _myAWParentStatus.MyAWStatus[i].WindowTitleOrder > _myAWParentStatus.MyAWStatus[j].WindowTitleOrder)
                        )
                    {
                        int tmpOrder = _myAWParentStatus.MyAWStatus[i].WindowTitleOrder;
                        _myAWParentStatus.MyAWStatus[i].WindowTitleOrder = _myAWParentStatus.MyAWStatus[j].WindowTitleOrder;
                        _myAWParentStatus.MyAWStatus[j].WindowTitleOrder = tmpOrder;
                    }
                }
            }
        }
        #endregion

        #region 顯示橫條
        private void SetAWParentStatusBars() //設置父階層橫條的數值，
        {
            //Console.WriteLine("\n\n\n");
            //第一行先顯示離開時間長度
            int tmpT = (int)DateTime.Now.Subtract(m_FirstStartTime).Subtract(m_TotalAWAccumulateTime).TotalSeconds;
            if (tmpT <= 0)
            {
                SetPicValue(m_FirstBar, "您紮紮實實地度過每分每秒！", tmpT);
            }
            else
            {
                SetPicValue(m_FirstBar, "您離開了些許時間 ", tmpT);
            }
            //第二行開始顯示視窗使用時間
            int tmpCurrentRow = 1; //目前畫到第幾行
            for (int i = 0; i < m_AWParentStatus.Count; i++)
            {
                foreach (var item in m_AWParentStatus)
                {
                    //Console.WriteLine(i.ToString() + item.Name + item.Order.ToString());
                    if (item.ProcessOrder == i)
                    {
                        Console.WriteLine("tmpCurrentRow : " + tmpCurrentRow);
                        tmpCurrentRow = ShowParentPicBox(item, tmpCurrentRow); //顯示父節點
                        //Console.WriteLine("Drawing " + i.ToString() + item.Name + item.Order.ToString() + "\n");
                        
                        tmpCurrentRow = SetAWStatusBars(item, tmpCurrentRow, item.ProcessPlusOr); //繪製子階層
                    }
                }
            }
        }
        public void SetPicValue(PictureBox picBar, string AWName, int value) //繪製picBar狀態橫條，目前僅用在第一列顯示離開時間BAR
        {
            //https://blog.csdn.net/zhuimengshizhe87/article/details/20640157
            TimeSpan t = TimeSpan.FromSeconds(value);
            string tString = t.ToString(@"hh\:mm\:ss");
            string str = tString + " : " + AWName;
            //Font font = new Font("Times New Roman", (float)11, FontStyle.Regular);
            Font font = new Font("標楷體", (float)11, FontStyle.Regular);
            PointF pt = new PointF(0, picBar.Height / 2 - 10);
            //PointF pt = new PointF(pBar.Width / 2 - 10, pBar.Height / 2 - 10);
            Pen penB = new Pen(Brushes.DodgerBlue);
            picBar.CreateGraphics().Clear(picBar.BackColor);
            //picBar.Value = value;
            //picBar.Refresh();
            int tmpV;
            if (m_TotalAWAccumulateTime.TotalSeconds > 0)
            {
                tmpV = value * picBar.Width / (int)m_TotalAWAccumulateTime.TotalSeconds;
            }
            else
            {
                tmpV = 1;
            }
            picBar.CreateGraphics().FillRectangle(new SolidBrush(m_PBarBackColor), 0, 0, tmpV, 30);
            picBar.CreateGraphics().DrawString(str, font, new SolidBrush(m_PBarForeColor), pt);
        }

        public int ShowParentPicBox(AWParentStatus _AWParentStatus, int _currentRow) //繪製父層picBar狀態橫條，目前畫在第幾行
        {
            //https://blog.csdn.net/zhuimengshizhe87/article/details/20640157
            TimeSpan t = TimeSpan.FromSeconds(_AWParentStatus.Seconds);
            string tString = t.ToString(@"hh\:mm\:ss");
            string str = tString + " : " + _AWParentStatus.ProcessOrder + " : " + _AWParentStatus.ProcessName;
//            Font font = new Font("Times New Roman", (float)11, FontStyle.Regular);
            Font font = new Font("標楷體", (float)11, FontStyle.Regular);
            PointF pt = new PointF(0, _AWParentStatus.ProcessPBox.Height / 2 - 10);
            Pen penB = new Pen(Brushes.DodgerBlue);
            int tmpV;
            if (m_TotalAWAccumulateTime.TotalSeconds > 0)
            {
                tmpV = _AWParentStatus.Seconds
                    * _AWParentStatus.ProcessPBox.Width
                    / (int)m_TotalAWAccumulateTime.TotalSeconds;
            }
            else
            {
                tmpV = 1;
            }
            _AWParentStatus.ProcessPlusButton.Location = new Point(330, _currentRow * 30);
            _AWParentStatus.ProcessPBox.Location = new Point(360, _currentRow * 30);
            Console.WriteLine("tmpV : " + tmpV);
            //_AWParentStatus.ProcessPBox.CreateGraphics().Clear(_AWParentStatus.ProcessPBox.BackColor);
            _AWParentStatus.ProcessPBox.Refresh();
            _AWParentStatus.ProcessPBox.CreateGraphics()
                .FillRectangle(new SolidBrush(m_PBarBackColor), 0, 0, tmpV, 30);
            _AWParentStatus.ProcessPBox.CreateGraphics()
                .DrawString(str, font, new SolidBrush(m_PBarForeColor), pt);
            //_AWParentStatus.ProcessPBox.Refresh();

            _currentRow++;
            return _currentRow;
        }

        private int SetAWStatusBars(AWParentStatus _aWParentStatus, int _tmpCurrentRow, bool _ProcessPlusOr) //設置子階層橫條的數值，
        {
            //第二行開始顯示視窗使用時間
            for (int i = 0; i < _aWParentStatus.MyAWStatus.Count; i++)
            {
                foreach (var item in _aWParentStatus.MyAWStatus)
                {
                    //Console.WriteLine(i.ToString() + item.Name + item.Order.ToString());
                    if (item.WindowTitleOrder == i)
                    {
                        Console.WriteLine("tmpCurrentRow : " + _tmpCurrentRow);
                        if (_ProcessPlusOr)
                        {
                            item.WindowTitlePBox.Visible = false;
                        }
                        else
                        {
                            _tmpCurrentRow = ShowPicBox(item, _tmpCurrentRow);
                            //Console.WriteLine("Drawing " + i.ToString() + item.Name + item.Order.ToString() + "\n");
                        }
                    }
                }
            }
            return _tmpCurrentRow;
        }

        public int ShowPicBox(AWStatus _AWStatus, int _currentRow) //繪製picBar狀態橫條，目前畫在第幾行
        {
            //https://blog.csdn.net/zhuimengshizhe87/article/details/20640157
            TimeSpan t = TimeSpan.FromSeconds(_AWStatus.Seconds);
            string tString = t.ToString(@"hh\:mm\:ss");
            string str = tString + " : " + _AWStatus.WindowTitleOrder + " : " + _AWStatus.WindowTitleName;
            //Font font = new Font("Times New Roman", (float)11, FontStyle.Regular);
            Font font = new Font("標楷體", (float)11, FontStyle.Regular);
            PointF pt = new PointF(0, _AWStatus.WindowTitlePBox.Height / 2 - 10);
            Pen penB = new Pen(Brushes.DodgerBlue);
            int tmpV;
            if (m_TotalAWAccumulateTime.TotalSeconds > 0)
            {
                tmpV = _AWStatus.Seconds
                    * _AWStatus.WindowTitlePBox.Width
                    / (int)m_TotalAWAccumulateTime.TotalSeconds;
            }
            else
            {
                tmpV = 1;
            }
            _AWStatus.WindowTitlePBox.Location = new Point(430, _currentRow * 30);
            Console.WriteLine("tmpV : " + tmpV);
            //_AWParentStatus.ProcessPBox.CreateGraphics().Clear(_AWParentStatus.ProcessPBox.BackColor);
            _AWStatus.WindowTitlePBox.Visible = true;
            _AWStatus.WindowTitlePBox.Refresh();
            _AWStatus.WindowTitlePBox.CreateGraphics()
                .FillRectangle(new SolidBrush(m_PBarBackColor), 0, 0, tmpV, 30);
            _AWStatus.WindowTitlePBox.CreateGraphics()
                .DrawString(str, font, new SolidBrush(m_PBarForeColor), pt);
            //_AWParentStatus.ProcessPBox.Refresh();

            _currentRow++;
            return _currentRow;
        }

        void ChangeFormColor() //改變視窗配色
        {
            Color tmpForeColor = Color.FromArgb(50, 50, 50);
            Color tmpBackColor = Color.FromArgb(200, 200, 200);

            if (m_WorkStates == eWorkStates.WORK)
            {
                switch (m_BackColorMode)
                {
                    case eColor.DefaultTomato:
                        //m_BackColorMode = eColor.Grass;
                        tmpBackColor = Color.Tomato;
                        tmpForeColor = Color.Brown;
                        break;
                    case eColor.Grass:
                        //m_BackColorMode = eColor.Sky;
                        tmpBackColor = Color.YellowGreen;
                        tmpForeColor = Color.DarkGreen;
                        break;
                    case eColor.Sky:
                        //m_BackColorMode = eColor.Gray;
                        tmpBackColor = Color.MediumTurquoise;
                        tmpForeColor = Color.SteelBlue;
                        break;
                    case eColor.Gray:
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

            foreach (Control tempcon in this.Controls)
            {
                switch (tempcon.GetType().ToString())
                {
                    case "System.Windows.Forms.Label":
                        tempcon.BackColor = tmpBackColor;
                        if (tempcon.Name == "labelTimer" || tempcon.Name == "labelTotalTimer")
                        {
                            tempcon.ForeColor = Color.FromArgb(255, 224, 192);
                        }
                        else
                        {
                            tempcon.ForeColor = tmpForeColor;
                        }
                        break;
                    case "System.Windows.Forms.Button":
                        tempcon.BackColor = tmpBackColor;
                        tempcon.ForeColor = tmpForeColor;
                        break;
                    case "System.Windows.Forms.PictureBox":
                        tempcon.BackColor = tmpBackColor;
                        tempcon.ForeColor = tmpForeColor;
                        break;
                    case "System.Windows.Forms.Image":
                        tempcon.BackColor = tmpBackColor;
                        tempcon.ForeColor = tmpForeColor;
                        break;
                    default:
                        break;
                }
            }
            this.BackColor = tmpBackColor;

            Color tmpBC = Color.FromArgb(
                (int)((int)tmpBackColor.R * 1.1 - 40),
                (int)((int)tmpBackColor.G * 1.1 - 40),
                (int)((int)tmpBackColor.B * 1.1 - 40)
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

        #region 按鍵觸發
        private void buttonStart_Click(object sender, EventArgs e) //Start按鍵
        {
            if (m_FirstStartTime == DateTime.MinValue) //第一次按下開始鍵
            {
                m_FirstStartTime = DateTime.Now;
                m_PhaseStartTime = DateTime.Now;
                // Enable timer.  
                //m_TimerMain.Enabled = true;
            }

            if (buttonStart.Text == "Start" || buttonStart.Text == "Continue") //按下開始鍵
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

        private void buttonStart_SizeChanged(object sender, EventArgs e) //當畫面縮在右下角時，也要觸發改變Start按鍵尺寸
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

        public void buttonPlus_Click(object sender, EventArgs e) //每一行工作資料的"+"按鍵
        {
            Button tmpbutton = (Button)sender;
            AWParentStatus tmpAWP = (AWParentStatus)tmpbutton.Tag;
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
                    buttonOpacity.Text = "▊";
                    break;
                case 0.75:
                    buttonOpacity.Text = "▌";
                    break;
                case 0.5:
                    buttonOpacity.Text = "▎";
                    break;
                case 0.25:
                    buttonOpacity.Text = "█";
                    break;
                case 0.0:
                    buttonOpacity.Text = "▊";
                    Opacity = 1;
                    break;
                default:
                    buttonOpacity.Text = "█";
                    Opacity = 1;
                    break;
            }
        }

        private void buttonAlwaysTop_Click(object sender, EventArgs e) //最上層顯示
        {
            TopMost = !TopMost;
            if (TopMost)
            {
                buttonAlwaysTop.Text = "╦";
                toolTipAll.SetToolTip(buttonAlwaysTop, "關閉最上層顯示");
            }
            else
            {
                buttonAlwaysTop.Text = "╩";
                toolTipAll.SetToolTip(buttonAlwaysTop, "開啟最上層顯示");
            }
            m_TopMost = TopMost; //紀錄上次調整的最上層顯示設定
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
            //https://oblivious9.pixnet.net/blog/post/192683977

            //if (m_FormHelp == null) //第一次會判定m_FormHelp == null，可是關閉m_FormHelp之後，卻無法判定為null，導致後續錯誤
            //{
            //    Console.WriteLine("m_FormHelp == null");
            m_FormHelp = new formHelp();
            //}
            //m_FormHelp.Show(); //此方法讓formFlowerPomodoroTimer與m_FormHelp為可同時操作的平等級視窗，這方式不適合當成說明視窗
            m_FormHelp.ShowDialog(this);//設定m_FormHelp為formFlowerPomodoroTimer的上層，並開啟m_FormHelp視窗。由於在formFlowerPomodoroTimer的程式碼內使用this，所以this為formFlowerPomodoroTimer的物件本身
            if (m_FormHelp.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                //若使用者在Form2按下了OK，則進入這個判斷式
                Console.WriteLine("按下了" + m_FormHelp.DialogResult.ToString());
            }
            else if (m_FormHelp.DialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                //若使用者在Form2按下了Cancel或者直接點選X關閉視窗，都會進入這個判斷式
                Console.WriteLine("按下了" + m_FormHelp.DialogResult.ToString());
            }
            else
            {
                Console.WriteLine("按下了" + m_FormHelp.DialogResult.ToString());
            }
        }

        private void buttonMinimumSize_Click(object sender, EventArgs e) //讓視窗縮到右下角
        {
            if (m_MinimumSizeOr)
            {
                m_MinimumSizeOr = false;
                buttonMinimumSize.Text = "◢";
                SetFormSizeNormal();
            }
            else
            {
                /*
                MinimumSize = new Size(360, 360);
                MaximumSize = new Size(1360, 360);
                this.Size = new System.Drawing.Size(360, 360);
                System.Drawing.Rectangle workingRectangle = Screen.PrimaryScreen.WorkingArea;
                Point newPosition = new Point(0, 0);
                newPosition.X = workingRectangle.Width - MinimumSize.Width;
                newPosition.Y = workingRectangle.Height - MinimumSize.Height;
                this.Location = newPosition;
                */
                buttonMinimumSize.Text = "◤";
                m_MinimumSizeOr = true;
                SetFormSizeMini();

            }
        }

        private void buttonQuit_Click(object sender, EventArgs e) //離開
        {
            MinimumSize = new Size(MinimumSize.Width, 40);
            //MaximumSize = new Size(1360, 720);
            int tmpHeight = this.Size.Height;
            for (int i = 0; i < (tmpHeight - 40) / 2; i++)
            {
                this.Size = new System.Drawing.Size(this.Size.Width, tmpHeight - (i * 2));
                //Thread.Sleep(1);
                //this.Refresh();
            }
            Application.Exit();
        }

        private void buttonTest_Click(object sender, EventArgs e) //測試按鍵用
        {
            //ChangeWorkState(eWorkStates.Rest);
            //progressBar1.ForeColor = Color.Red;
            //SetProcessValue(progressBar1, "GGGGG", progressBar1.Value + 1);
        }
        #endregion
    }
}