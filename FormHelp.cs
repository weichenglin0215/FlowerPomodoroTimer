using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    /// <summary>
    /// 說明視窗：提供使用說明、開機自動啟動捷徑建立、
    /// 休息提醒圖片設定，以及開啟使用情形分析視窗的入口。
    /// </summary>
    public partial class formHelp : Form
    {
        /// <summary>「測試休息圖片提醒」被觸發時引發的事件，由主視窗（formFlowerPomodoroTimer）訂閱並處理。</summary>
        public event Action? TestRestImageRequested;

        /// <summary>「開啟使用情形分析」被觸發時引發的事件，由主視窗訂閱並開啟 FormUsageAnalysis。</summary>
        public event Action? OpenUsageAnalysisRequested;

        public formHelp()
        {
            InitializeComponent();
            // 使用 DPI 感知縮放，避免高 DPI 環境下元件錯位
            this.AutoScaleMode = AutoScaleMode.Dpi;
            // formHelp 以 Show()（非 Modal）開啟，DialogResult 無法自動關閉視窗，
            // 因此直接綁定 Click 事件呼叫 Close()。
            buttonOK.Click += (s, e) => Close();
            LoadRestImageSettingsToUi();
        }

        /// <summary>
        /// 點擊「在自動執行資料夾中建立捷徑」按鈕：
        /// 以檔案總管開啟 Windows Startup 資料夾，並建立本程式的 .url 捷徑。
        /// </summary>
        private void buttonOpenStartFolder_Click(object sender, EventArgs e)
        {
            ExplorerOpenStartupFolder();
            CreateAppShortcutToStartupFolder();
        }

        /// <summary>
        /// 以 Windows 檔案總管開啟 Startup（開機自動執行）資料夾，
        /// 方便使用者確認捷徑是否已建立。
        /// </summary>
        private void ExplorerOpenStartupFolder()
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = startUpFolderPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        /// <summary>
        /// 在 Windows Startup 資料夾中建立本應用程式的 .url 捷徑，
        /// 使程式在每次開機後自動啟動。
        /// </summary>
        private void CreateAppShortcutToStartupFolder()
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startUpFolderPath, Application.ProductName + ".url");
            // 將路徑中的反斜線替換為正斜線，符合 .url 捷徑的 file:/// URL 格式
            string executablePath = Application.ExecutablePath.Replace("\\", "/");

            string content = "[InternetShortcut]" + Environment.NewLine
                + "URL=file:///" + executablePath + Environment.NewLine
                + "IconFile=" + executablePath + Environment.NewLine
                + "IconIndex=0" + Environment.NewLine;

            File.WriteAllText(shortcutPath, content);
        }

        /// <summary>
        /// 從磁碟讀取休息圖片提醒設定，並將值顯示到 UI 控制項。
        /// 每次開啟說明視窗時呼叫，確保顯示最新的設定值。
        /// </summary>
        private void LoadRestImageSettingsToUi()
        {
            RestImageReminderSettings.Load();
            textBoxRestImageFolder.Text = RestImageReminderSettings.ImageFolderPath;
            checkBoxEnableRestImage.Checked = RestImageReminderSettings.Enabled;
        }

        /// <summary>
        /// 將 UI 控制項上的設定值儲存回磁碟設定檔。
        /// </summary>
        private void SaveRestImageSettingsFromUi()
        {
            RestImageReminderSettings.Save(textBoxRestImageFolder.Text, checkBoxEnableRestImage.Checked);
        }

        /// <summary>
        /// 點擊「選擇圖片資料夾」按鈕：
        /// 開啟資料夾瀏覽對話方塊，選取後立即儲存設定。
        /// </summary>
        private void buttonSelectRestImageFolder_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "選擇休息提醒圖片資料夾";
            dialog.SelectedPath = textBoxRestImageFolder.Text;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                textBoxRestImageFolder.Text = dialog.SelectedPath;
                SaveRestImageSettingsFromUi();
            }
        }

        /// <summary>
        /// 點擊「測試休息圖片提醒」按鈕：
        /// 儲存目前設定後，觸發主視窗立即顯示一次覆蓋圖片。
        /// </summary>
        private void buttonTestRestImage_Click(object? sender, EventArgs e)
        {
            SaveRestImageSettingsFromUi();
            TestRestImageRequested?.Invoke();
        }

        /// <summary>
        /// 點擊「開啟使用情形分析」按鈕：
        /// 觸發主視窗開啟 FormUsageAnalysis 統計分析視窗。
        /// </summary>
        private void buttonOpenUsageAnalysis_Click(object? sender, EventArgs e)
        {
            OpenUsageAnalysisRequested?.Invoke();
        }

        /// <summary>
        /// 視窗關閉時自動儲存 UI 上的休息圖片設定，
        /// 確保即使使用者直接點叉關閉也不會遺失變更。
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveRestImageSettingsFromUi();
            base.OnFormClosing(e);
        }
    }
}
