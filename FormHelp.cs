using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    public partial class formHelp : Form
    {
        public formHelp()
        {
            InitializeComponent();
            buttonOK.DialogResult = DialogResult.OK;
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
    }
}
