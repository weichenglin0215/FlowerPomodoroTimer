using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    public partial class formHelp : Form
    {
        public event Action? TestRestImageRequested;
        public event Action? OpenUsageAnalysisRequested;

        public formHelp()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;  //
            //
            buttonOK.DialogResult = DialogResult.OK;
            LoadRestImageSettingsToUi();
        }

        private void buttonOpenStartFolder_Click(object sender, EventArgs e)
        {
            ExplorerOpenStartupFolder();
            CreateAppShortcutToStartupFolder();
        }

        public void ExplorerOpenStartupFolder()
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

        public void CreateAppShortcutToStartupFolder()
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startUpFolderPath, Application.ProductName + ".url");
            string executablePath = Application.ExecutablePath.Replace("\\", "/");

            string content = "[InternetShortcut]" + Environment.NewLine
                + "URL=file:///" + executablePath + Environment.NewLine
                + "IconFile=" + executablePath + Environment.NewLine
                + "IconIndex=0" + Environment.NewLine;

            File.WriteAllText(shortcutPath, content);
        }


        private void LoadRestImageSettingsToUi()
        {
            RestImageReminderSettings.Load();
            textBoxRestImageFolder.Text = RestImageReminderSettings.ImageFolderPath;
            checkBoxEnableRestImage.Checked = RestImageReminderSettings.Enabled;
        }

        private void SaveRestImageSettingsFromUi()
        {
            RestImageReminderSettings.Save(textBoxRestImageFolder.Text, checkBoxEnableRestImage.Checked);
        }

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

        private void buttonTestRestImage_Click(object? sender, EventArgs e)
        {
            SaveRestImageSettingsFromUi();
            TestRestImageRequested?.Invoke();
        }

        private void buttonOpenUsageAnalysis_Click(object? sender, EventArgs e)
        {
            OpenUsageAnalysisRequested?.Invoke();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveRestImageSettingsFromUi();
            base.OnFormClosing(e);
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            
        }
    }
}
