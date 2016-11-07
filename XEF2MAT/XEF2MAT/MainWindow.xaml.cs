using KinectMLConnect;
using Microsoft.Kinect.Tools;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

/***********************************************************************************************
 * Author:  Ray Litchi
 * Contact: raysworld@outlook.com
 * Date:    2016-11-07
 * Instructions:
 *  This app is used for converting Kinect Studio data file (.xef) to Matlab data file (.mat).
 *  The project is based on the following references:
 *      - Microsoft.Kinect (You may find it by installing Microsoft Kinect SDK 2.0)
 *      - Microsoft.Kinect.Tools (You may also find it by installing Microsoft Kinect SDK 2.0)
 *      - MATWriter (Written by SergentMT, which you may find here: http://www.codeproject.com/Tips/819613/Kinect-Version-Depth-Frame-to-mat-File-Exporter) 
 * Notes:
 *  It seems that this app only works on x64 platforms
 * How-to-use:
 *  Simply run xef2mat.exe, select the .xef file by clicking on the 'select' button. The output files
 *  will be at the same folder of the app.
************************************************************************************************/

namespace XEF2MAT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {        
        private const int WIDTH = 512;          // Width of the depth image
        private const int HEIGHT = 424;         // Height of the depth image

        private string fileName = null;         // File name of the imported .xef file

        private int frameCount = 0;             // The number of frames
        private ushort[] timing = null;         // Storage the time stamp of the frames
        private ushort[] outputData = null;     // Storage the Depth and IR data of the frames
        
        private string state = null;            // Current process state

        private BackgroundWorker b;             // Handle the export process in background

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            b = new BackgroundWorker();
            b.WorkerReportsProgress = true;

            b.DoWork += DirtyWork;
            b.ProgressChanged += B_ProgressChanged;
            b.RunWorkerCompleted += B_RunWorkerCompleted;
        }

        private void B_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage;

            progressBar.Value = progress;
            label3.Content = $"{state} - [{progress}%]";
        }

        private void B_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            label3.Content = "Completed!";

            button.IsEnabled = true;
            button.Content = "Select";
        }

        public static void Copy(IntPtr source, ushort[] destination, int startIndex, int length)
        {
            unsafe
            {
                var sourcePtr = (ushort*)source;
                for (int i = startIndex; i < startIndex + length; ++i)
                {
                    destination[i] = *sourcePtr++;
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var folder_path = Environment.CurrentDirectory + "/Xef2Mat_Output";
            if (!Directory.Exists(folder_path))
            {
                Directory.CreateDirectory(folder_path);
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog.Filter = "Kinect Studio Data File|*.xef";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                fileName = openFileDialog.FileName;
                
                button.IsEnabled = false;
                button.Content = "Working...";
                b.RunWorkerAsync();
            }
            else
            {
                return;
            }
        }

        private void DirtyWork(object sender, DoWorkEventArgs e)
        {
            outputData = new ushort[WIDTH * HEIGHT];

            var client = KStudio.CreateClient();

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            var file = client.OpenEventFile(fileName);

            foreach (var item in file.EventStreams)
            {
                if (item.DataTypeName.Equals("Nui Depth"))
                {
                    state = "Depth";
                    
                    KStudioSeekableEventStream stream = item as KStudioSeekableEventStream;
                    this.frameCount = (int)stream.EventCount;
                    timing = new ushort[frameCount];

                    for (uint i = 0; i < frameCount; i++)
                    {
                        b.ReportProgress((int)((float)i / frameCount * 100));

                        Thread.Sleep(100);
                        var curr_event = stream.ReadEvent(i);
                        //unsafe
                        {
                            int size = outputData.Length * sizeof(ushort);
                            IntPtr ip = Marshal.AllocHGlobal(size);

                            uint bufferSize = 0;
                            curr_event.AccessUnderlyingEventDataBuffer(out bufferSize, out ip);

                            Copy(ip, outputData, 0, outputData.Length);
                        }
                        this.timing[i] = (ushort)curr_event.RelativeTime.TotalMilliseconds;
                        string filePath = Environment.CurrentDirectory + "/Xef2Mat_Output/DepthFrame" + i.ToString("D4") + ".mat";
                        MATWriter.ToMatFile("Dep" + i.ToString("D4"), filePath, outputData, HEIGHT, WIDTH);
                    }
                }
                if (item.DataTypeName.Equals("Nui IR"))
                {
                    state = "IR";

                    KStudioSeekableEventStream stream = item as KStudioSeekableEventStream;
                    this.frameCount = (int)stream.EventCount;
                    timing = new ushort[frameCount];

                    for (uint i = 0; i < frameCount; i++)
                    {
                        b.ReportProgress((int)((float)i / frameCount * 100));

                        var curr_event = stream.ReadEvent(i);
                        //unsafe
                        {
                            int size = outputData.Length * sizeof(ushort);
                            IntPtr ip = Marshal.AllocHGlobal(size);

                            uint bufferSize = 0;
                            curr_event.AccessUnderlyingEventDataBuffer(out bufferSize, out ip);

                            Copy(ip, outputData, 0, outputData.Length);
                        }
                        this.timing[i] = (ushort)curr_event.RelativeTime.TotalMilliseconds;
                        string filePath = Environment.CurrentDirectory + "/Xef2Mat_Output/IRFrame" + i.ToString("D4") + ".mat";
                        MATWriter.ToMatFile("IR" + i.ToString("D4"), filePath, outputData, HEIGHT, WIDTH);
                    }
                }
            }
            if (frameCount > 0)
            {
                state = "TimeStamp";
                b.ReportProgress(100);
                string filePath = Environment.CurrentDirectory + "/Xef2Mat_Output/TimeStamp.mat";
                MATWriter.ToMatFile("Time", filePath, this.timing, this.timing.Length, 1);
            }
        }
    }
}
