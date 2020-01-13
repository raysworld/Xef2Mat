using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Xef2MatUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processID);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("--no-gui"))
            {
                //Do command line stuff
                if (e.Args.Length > 0)
                {
                    string parameter = e.Args[0].ToString();
                    WriteToConsole(parameter);
                }                
            }
            else
            {
                // Launch GUI and pass arguments in case you want to use them.
                new MainWindow(e).ShowDialog();
            }
            Shutdown();
        }

        public void WriteToConsole(string message)
        {
            AttachConsole(-1);
            Console.WriteLine(message);
        }
    }    
}
