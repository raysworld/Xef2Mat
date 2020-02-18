using KinectMLConnect;
using Microsoft.Kinect.Tools;
using Microsoft.Kinect;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;

namespace XEF2MATCore
{
    /// <summary>
    /// The types of Kinect streams
    /// </summary>
    public enum KinectStreamType { Depth, IR, Opaque, Body}
    /// <summary>
    /// The intrisic parameters of the Kinect sensor
    /// </summary>
    public class KinectParameter
    {
        public const int WIDTH = 512;          // Width of the depth image
        public const int HEIGHT = 424;         // Height of the depth image
    }
    /// <summary>
    /// The core class that implements the conversion function
    /// </summary>
    public class Xef2MatCore
    {
        /// <value>
        /// Gets the file path where the .xef file is placed
        /// </value>
        public string Path { get; private set; }
        /// <value>
        /// The depth stream extracted from the .xef file
        /// </value>
        public KStudioEventStream DepthStream { get; private set; }
        /// <value>
        /// The IR stream extracted from the .xef file
        /// </value>
        public KStudioEventStream IRStream { get; private set; }
        /// <value>
        /// The flag that indicates if the streams have been loaded
        /// </value>
        public Boolean IsStreamLoaded { get; private set; } = false;

        /// <summary>
        /// The delegate that fires when a .xef file is loaded
        /// </summary>
        public delegate void FileLoadedHandler();
        /// <summary>
        /// The event that is related to <see cref="Xef2MatCore.FileLoadedHandler"/>
        /// </summary>
        public event FileLoadedHandler FileLoaded;
        /// <summary>
        /// The method that fires after the .xef is successfully loaded
        /// </summary>
        public void OnFileLoaded()
        {
            IsStreamLoaded = true; 
            FileLoaded?.Invoke();
        }

        /// <summary>
        /// The delegate that fires every time a frame of the stream is processed
        /// </summary>
        /// <param name="name">The name of the stream (e.g. "Depth", "IR")</param>
        /// <param name="progress">The value of the overall process (0.0~100.0)</param>
        public delegate void ConvertProgressHandler(string name, double progress);
        /// <summary>
        /// The event that is related to <see cref="Xef2MatCore.ConvertProgressHandler"/>
        /// </summary>
        public event ConvertProgressHandler ProgressUpdated;
        /// <summary>
        /// The method that fires every time a frame of the stream is processed (progress updated)
        /// </summary>
        /// <param name="name">The name of the stream (e.g. "Depth", "IR")</param>
        /// <param name="progress">The value of the overall process (0.0~100.0)</param>
        public void OnProgressUpdated(string name, double progress) => ProgressUpdated?.Invoke(name, progress);

        /// <summary>
        /// The delegate that fires when a .xef file is finished to export
        /// </summary>
        public delegate void ExportHandler();
        /// <summary>
        /// The event that is related to <see cref="Xef2MatCore.ExportHandler"/>
        /// </summary>
        public event ExportHandler ExportFinished;
        /// <summary>
        /// The method that fires when a .xef file is finished to export
        /// </summary>
        public void OnExportFinished() => ExportFinished?.Invoke();

        /// <summary>
        /// The method loads a .xef file locates at <paramref name="path"/>, finds the depth/IR stream in the file, 
        /// and then export the converted data into <paramref name="out_path"/>.
        /// </summary>
        /// <param name="path">The path to the .xef file</param>
        /// <param name="out_path">The output path to store the exported data</param>
        public void Load(string path, string out_path)
        {
            IsStreamLoaded = false;

            if (string.IsNullOrEmpty(path)) return;

            Path = path;
            using (KStudioClient client = KStudio.CreateClient())
            {
                var file = client.OpenEventFile(Path, KStudioEventFileFlags.None);
                if (file != null && file.EventStreams != null)
                {
                    DepthStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui Depth"));
                    IRStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui IR"));
                    
                    OnFileLoaded();

                    string folder_path = string.IsNullOrWhiteSpace(out_path) 
                        ? $"{Environment.CurrentDirectory}/output" 
                        : out_path;
                                        
                    if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);
                    
                    StreamToMat(DepthStream, "Depth", folder_path);
                    StreamToMat(IRStream, "IR", folder_path);
                }
            }
            OnExportFinished();
        }

        /// <summary>
        /// The async version of the method that loads a .xef file locates at <paramref name="path"/>, finds the depth/IR stream in the file, 
        /// and then export the converted data into the current working directory.
        /// </summary>
        /// <param name="path">The path to the .xef file</param>
        public async Task LoadAsync(string path)
        {
            IsStreamLoaded = false;

            if (string.IsNullOrEmpty(path)) return;

            Path = path;
            using (KStudioClient client = KStudio.CreateClient())
            {
                var file = client.OpenEventFile(Path, KStudioEventFileFlags.None);
                if (file != null && file.EventStreams != null)
                {
                    DepthStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui Depth"));
                    IRStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui IR"));

                    OnFileLoaded();

                    var folder_path = $"{Environment.CurrentDirectory}/output";
                    if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);
                    await StreamToMatAsync(DepthStream, "Depth", folder_path);
                    await StreamToMatAsync(IRStream, "IR", folder_path);
                }
            }
            OnExportFinished();
        }

        /// <summary>
        /// The method converts the stream <paramref name="kstream"/> into a .mat file 
        /// with the variable name <paramref name="name"/> and saves the .mat files to <paramref name="file_path"/>
        /// </summary>
        /// <param name="kstream">The stream to convert</param>
        /// <param name="name">The name of the converted variable</param>
        /// <param name="file_path">The folder to store the .mat files</param>
        private void StreamToMat(KStudioEventStream kstream, string name, string file_path)
        {
            var stream = kstream as KStudioSeekableEventStream;
            var outputData = new ushort[KinectParameter.WIDTH * KinectParameter.HEIGHT]; // Storage the data of the frames
            var frame_count = (int)stream.EventCount;
            var timing = new ushort[frame_count];

            for (uint i = 0; i < frame_count; i++)
            {
                var progress = (float)(i+1) / frame_count * 100;
                OnProgressUpdated(name, progress);

                var curr_event = stream.ReadEvent(i);

                //unsafe
                {
                    int size = outputData.Length * sizeof(ushort);
                    IntPtr ip = Marshal.AllocHGlobal(size);

                    uint bufferSize = 0;
                    curr_event.AccessUnderlyingEventDataBuffer(out bufferSize, out ip);

                    Copy(ip, outputData, 0, outputData.Length);
                }
                timing[i] = (ushort)curr_event.RelativeTime.TotalMilliseconds;

                var frame_path = $"{file_path}/{name}_{i.ToString("D5")}.mat";
                MATWriter.ToMatFile(
                    name,
                    frame_path,
                    outputData,
                    KinectParameter.HEIGHT, KinectParameter.WIDTH);
            }

            if (frame_count > 0)
            {
                var timing_path = $"{file_path}/TimeSteps.mat";
                MATWriter.ToMatFile(
                    "Time",
                    timing_path,
                    timing,
                    timing.Length, 1);
                OnProgressUpdated("TimeSpan", 100);
            }
        }

        /// <summary>
        /// The async version of the method that converts the stream <paramref name="kstream"/> into a .mat file 
        /// with the variable name <paramref name="name"/> and saves the .mat files to <paramref name="file_path"/>
        /// </summary>
        /// <param name="kstream">The stream to convert</param>
        /// <param name="name">The name of the converted variable</param>
        /// <param name="file_path">The folder to store the .mat files</param>
        private async Task StreamToMatAsync(KStudioEventStream kstream, string name, string file_path)
        {
            var stream = kstream as KStudioSeekableEventStream;
            var outputData = new ushort[KinectParameter.WIDTH * KinectParameter.HEIGHT]; // Storage the data of the frames
            var frame_count = (int)stream.EventCount;
            var timing = new ushort[frame_count];

            for (uint i = 0; i < frame_count; i++)
            {
                var progress = (float)i / frame_count * 100;
                OnProgressUpdated(name, progress);

                await Task.Run(()=>
                {
                    var curr_event = stream.ReadEvent(i);

                    //unsafe
                    {
                        int size = outputData.Length * sizeof(ushort);
                        IntPtr ip = Marshal.AllocHGlobal(size);

                        uint bufferSize = 0;
                        curr_event.AccessUnderlyingEventDataBuffer(out bufferSize, out ip);

                        Copy(ip, outputData, 0, outputData.Length);
                    }
                    timing[i] = (ushort)curr_event.RelativeTime.TotalMilliseconds;

                    var frame_path = $"{file_path}/{name}_{i.ToString("D5")}.mat";
                    MATWriter.ToMatFile(
                        name,
                        frame_path,
                        outputData,
                        KinectParameter.HEIGHT, KinectParameter.WIDTH);
                });                
            }

            if (frame_count > 0)
            {
                await Task.Run(() =>
                {
                    var timing_path = $"{file_path}/TimeSteps.mat";
                    MATWriter.ToMatFile(
                        "Time",
                        timing_path,
                        timing,
                        timing.Length, 1);
                });
                OnProgressUpdated("TimeSpan", 100);
            }
        }        

        /// <summary>
        /// The method copies the data from <paramref name="source"/> in memory to an array <paramref name="destination"/>
        /// from the index <paramref name="startIndex"/> with length <paramref name="length"/>
        /// </summary>
        /// <param name="source">The pointer to the source data</param>
        /// <param name="destination">The array to store the source data</param>
        /// <param name="startIndex">The start index</param>
        /// <param name="length">The length to copy</param>
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
    }
}
