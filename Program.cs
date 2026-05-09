using System;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();
            Application.Run(new formFlowerPomodoroTimer());
        }
    }
}
