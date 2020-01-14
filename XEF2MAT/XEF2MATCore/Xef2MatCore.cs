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
    public enum KinectStreamType { Depth, IR, Opaque, Body}
    public class KinectParameter
    {
        public const int WIDTH = 512;          // Width of the depth image
        public const int HEIGHT = 424;         // Height of the depth image
    }
    public class Xef2MatCore
    {
        public string Path { get; private set; }
        public KStudioEventStream DepthStream { get; private set; }
        public KStudioEventStream IRStream { get; private set; }
        public Boolean IsStreamLoaded { get; private set; } = false;

        public delegate void FileLoadedHandler();
        public event FileLoadedHandler FileLoaded;
        public void OnFileLoaded()
        {
            IsStreamLoaded = true; 
            FileLoaded?.Invoke();
        }

        public delegate void ConvertProgressHandler(string name, double progress);
        public event ConvertProgressHandler ProgressUpdated;
        public void OnProgressUpdated(string name, double progress) => ProgressUpdated?.Invoke(name, progress);
                          
        public delegate void ExportHandler();
        public event ExportHandler ExportFinished;
        public void OnExportFinished() => ExportFinished?.Invoke();


        public Xef2MatCore()
        {            
        }

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
