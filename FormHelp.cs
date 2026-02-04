using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
//using System.Runtime.InteropServices;//互動服務
// To resolve the CS0246 error, you need to add a reference to the "Windows Script Host Object Model" COM library in your project.
// Follow these steps in Visual Studio:
// 1. Right-click on your project in the Solution Explorer and select "Add" -> "Reference".
// 2. In the Reference Manager, go to "COM" and search for "Windows Script Host Object Model".
// 3. Select it and click "OK" to add the reference.

using IWshRuntimeLibrary; // This will work after adding the reference as described above.

namespace Flower_Pomodoro_Timer
{
    public partial class formHelp : Form
    {
        public formHelp()
        {
            InitializeComponent();
            buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;//設定button1為OK
            //button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;//設定button為Cancel
        }

        private void buttonOpenStartFolder_Click(object sender, EventArgs e)
        {
            ExplorerOpenStartupFolder();
            CreateAppShortCutToStartupFolder();
        }

        public void ExplorerOpenStartupFolder() //檔案總管開啟自動執行資料夾位置
        {
            //string defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            Console.WriteLine("自動執行資料夾位置：{0}",startUpFolderPath);
            string program = @"C:\Windows\explorer.exe";
            //string argument = @"/select," + someFileName; //開啟檔案總管後可以直接選擇該檔案
            string argument = @startUpFolderPath;
            System.Diagnostics.Process.Start(program, argument);

        }
        public void CreateAppShortCutToStartupFolder() //在自動執行資料夾位置中建立花時鐘捷徑
        {
            string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            //要使用之前請 專案->加入參考...->COM->引用"Windows Script Host Object Model "
            //引用時記得要雙擊"Windows Script Host Object Model "項目或是勾選之後，才能按下確定，不然根本沒有啟用這個參考。
            IWshRuntimeLibrary.WshShell wshShell = new WshShell();
            IWshRuntimeLibrary.IWshShortcut shortcut;
            // Create the shortcut
            shortcut =
              (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(
                startUpFolderPath + "\\" +
                Application.ProductName + ".lnk");

            shortcut.TargetPath = Application.ExecutablePath;
            shortcut.WorkingDirectory = Application.StartupPath;
            shortcut.Description = "Launch My Application";
            // shortcut.IconLocation = Application.StartupPath + @"\App.ico";
            shortcut.Save();
        }
    }
}
