using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    public partial class formHelp : Form
    {
        private TextBox textBoxRestImageFolder = null!;
        private Button buttonSelectRestImageFolder = null!;
        private CheckBox checkBoxEnableRestImage = null!;
        private Button buttonTestRestImage = null!;
        private Button buttonOpenUsageAnalysis = null!;
        private Label labelRestImage = null!;

        public event Action? TestRestImageRequested;
        public event Action? OpenUsageAnalysisRequested;

        public formHelp()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;  // 改這行
            // 預設是 AutoScaleMode.Font，有時會造成元件重疊
            buttonOK.DialogResult = DialogResult.OK;
            InitializeRestImageControls();
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

        private void InitializeRestImageControls()
        {
            labelRestImage = new Label
            {
                Text = "休息遮罩圖片設定",
                AutoSize = true,
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke,
                Location = new Point(68, 292)
            };

            textBoxRestImageFolder = new TextBox
            {
                Name = "textBoxRestImageFolder",
                Location = new Point(72, 322),
                Size = new Size(530, 27)
            };

            buttonSelectRestImageFolder = new Button
            {
                Text = "選擇圖片目錄",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                Location = new Point(612, 319)
            };
            buttonSelectRestImageFolder.Click += buttonSelectRestImageFolder_Click;

            checkBoxEnableRestImage = new CheckBox
            {
                Text = "開啟休息時顯示圖片",
                AutoSize = true,
                ForeColor = Color.WhiteSmoke,
                Location = new Point(72, 360)
            };

            buttonTestRestImage = new Button
            {
                Text = "測試：立即顯示圖片",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                Location = new Point(260, 354)
            };
            buttonTestRestImage.Click += buttonTestRestImage_Click;

            buttonOpenUsageAnalysis = new Button
            {
                Text = "開啟 番茄花鐘-統計分析",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft JhengHei UI", 11F, FontStyle.Regular),
                ForeColor = Color.WhiteSmoke,
                Location = new Point(500, 354)
            };
            buttonOpenUsageAnalysis.Click += (_, _) => OpenUsageAnalysisRequested?.Invoke();

            Controls.Add(labelRestImage);
            Controls.Add(textBoxRestImageFolder);
            Controls.Add(buttonSelectRestImageFolder);
            Controls.Add(checkBoxEnableRestImage);
            Controls.Add(buttonTestRestImage);
            Controls.Add(buttonOpenUsageAnalysis);

            ClientSize = new Size(984, 430);
            buttonOK.Location = new Point(447, 388);
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
            dialog.Description = "選擇休息時顯示圖片的資料夾";
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveRestImageSettingsFromUi();
            base.OnFormClosing(e);
        }
    }
}
