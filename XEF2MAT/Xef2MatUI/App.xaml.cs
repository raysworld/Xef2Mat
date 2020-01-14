using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using XEF2MATCore;

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

                    string in_file_path = @"C:\Users\raysw\Downloads\20150622_125509_00\20150622_125509_00.xef";
                    string out_folder_path = @"C:\Users\raysw\Downloads\20150622_125509_00\output";
                    Xef2MatCore core = new Xef2MatCore();
                    core.ProgressUpdated += Core_ProgressUpdated;
                    core.Load(in_file_path);
                    WriteToConsole("File Loaded!");
                    core.ExportAll(out_folder_path);
                    WriteToConsole("File Exported!");
                }                
            }
            else
            {
                // Launch GUI and pass arguments in case you want to use them.
                new MainWindow(e).ShowDialog();
            }
            Shutdown();
        }

        private void Core_ProgressUpdated(double progress)
        {
            WriteToConsole(progress.ToString());
        }

        public void WriteToConsole(string message)
        {
            AttachConsole(-1);
            Console.WriteLine(message);
        }
    }    
}
