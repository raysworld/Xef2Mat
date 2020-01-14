using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using XEF2MATCore;
using System.Threading;
using System.IO;

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
                if (e.Args.Length > 0)
                {
                    AttachConsole(-1);
                    ConsoleLoad(e.Args);             
                }                
            }
            else
            {
                new MainWindow(e).ShowDialog();
            }
            Shutdown();
        }

        private void ConsoleLoad(string[] args)
        {
            string in_file_path = "";
            string out_folder_path = "";

            if (args.Length == 1)
            {
                Console.WriteLine("[ERROR] Neither .xef input file nor an output folder is specified");
                Environment.Exit(0);
            }
            else if (args.Length == 2)
            {
                in_file_path = args[1];

                Console.WriteLine("[INFO] An output folder is not specified. Use default value.");
                out_folder_path = "";
            }
            else if (args.Length == 3)
            {
                in_file_path = args[1];
                out_folder_path = args[2];
            }
            else
            {
                Console.WriteLine("[ERROR] Wrong number of parameters.");
                Environment.Exit(0);
            }

            if (in_file_path.ToLower().EndsWith(".xef") && File.Exists(in_file_path))
            {
                out_folder_path = string.IsNullOrWhiteSpace(out_folder_path)
                        ? $"{Environment.CurrentDirectory}/output"
                        : out_folder_path;

                
                Console.WriteLine($"[INFO] Input File: {in_file_path}");
                Console.WriteLine($"[INFO] Output Folder: {out_folder_path}");

                Xef2MatCore core = new Xef2MatCore();
                core.ProgressUpdated += Core_ProgressUpdated;
                core.FileLoaded += Core_FileLoaded;
                core.ExportFinished += Core_ExportFinished;

                core.Load(in_file_path, out_folder_path);
            }
            else
            {
                Console.WriteLine("[ERROR] No .xef input file is specified");
                Environment.Exit(0);
            }
        }

        private void Core_ExportFinished()
        {
            Console.WriteLine("Conversion finished!");
        }

        private void Core_FileLoaded()
        {
            Console.Write("Ready to start the conversion!");            
        }

        private void Core_ProgressUpdated(string name, double prog)
        {
            Console.WriteLine($"[{name}] - [ {prog.ToString("F2")}% ]");
        }
    }    
}
