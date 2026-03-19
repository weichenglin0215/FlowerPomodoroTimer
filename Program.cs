using System;
using System.Windows.Forms;

namespace Flower_Pomodoro_Timer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new formFlowerPomodoroTimer());
        }
    }
}
