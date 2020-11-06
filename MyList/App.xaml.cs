using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Reflection;

namespace MyList
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int RF_TESTMESSAGE = 0xA123;

        System.Threading.Mutex mutex;
        private void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1)
            {
                if (e.Args[0] == GlobalClass.Flags.AutoTurn)
                    GlobalClass.Flag = GlobalClass.Flags.AutoTurn;
                else
                {
                    SplashScreen splashScreen = new SplashScreen("resources/notepad.jpg");
                    splashScreen.Show(true);
                }
            }
            else
            {
                SplashScreen splashScreen = new SplashScreen("resources/notepad.jpg");
                splashScreen.Show(true);
            }
            bool createdNew;
            string mutName = "MyList";
            mutex = new System.Threading.Mutex(true, mutName, out createdNew);
            if (!createdNew)
            {
                Process[] procList = Process.GetProcesses();

                foreach (var item in procList)
                {
                    if (item.Id != Process.GetCurrentProcess().Id && item.ProcessName == Process.GetCurrentProcess().ProcessName)
                    {
                        if (item.MainWindowHandle == IntPtr.Zero) 
                        {
                            item.Kill();
                        }
                        else
                        {
                            Application.Current.Shutdown();
                            GlobalClass.IsRunning = true;
                        }
                    }
                }
            }
        }
    }
}
