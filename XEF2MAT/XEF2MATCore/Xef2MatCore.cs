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
        public KStudioClient KinectClient { get; private set; }
        public string Path { get; private set; }
        public KStudioEventStream DepthStream { get; private set; }
        public KStudioEventStream IRStream { get; private set; }
        public Boolean IsStreamLoaded { get; private set; } = false;

        public delegate void FileLoadedHandler();
        public event FileLoadedHandler FileLoaded;
        public void OnFileLoaded() => FileLoaded?.Invoke();

        public delegate void ConvertProgressHandler(double progress);
        public event ConvertProgressHandler ProgressUpdated;
        public void OnProgressUpdated(double progress) => ProgressUpdated?.Invoke(progress);

        public delegate void ExportHandler();
        public event ExportHandler ExportFinished;
        public void OnExportFinished() => ExportFinished?.Invoke();


        public Xef2MatCore()
        {
            //var kc = KStudio.CreateClient();
            //KinectClient = KStudio.CreateClient();
        }

        public void Load(string path)
        {
            IsStreamLoaded = false;

            if (string.IsNullOrEmpty(path)) return;

            Path = path;
            var client = KStudio.CreateClient();
            var file = client.OpenEventFile(Path);

            if (file != null && file.EventStreams != null)
            {
                DepthStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui Depth"));
                IRStream = file.EventStreams.First(t => t.DataTypeName.Equals("Nui IR"));

                IsStreamLoaded = true;

                OnFileLoaded();
            }
        }

        private void StreamToMat(KStudioSeekableEventStream stream, string name, string file_path)
        {
            var outputData = new ushort[KinectParameter.WIDTH * KinectParameter.HEIGHT]; // Storage the data of the frames
            var frame_count = (int)stream.EventCount;
            var timing = new ushort[frame_count];
            
            for (uint i = 0; i < frame_count; i++)
            {
                var progress = (float)i / frame_count * 100;
                OnProgressUpdated(progress);

                //try
                //{
                var curr_event = stream.ReadEvent(i);
                //}
                //catch (Exception e)
                //{
                //    var s = e.Message;
                //}


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

        public void SaveToMat(KinectStreamType type, string path = null)
        {
            if (!IsStreamLoaded) return;
            if (string.IsNullOrWhiteSpace(path))
                path = $"{Environment.CurrentDirectory}/output";
            else
                path = $"{path}/output";
           
            Directory.CreateDirectory(path); 

            switch (type)
            {
                case KinectStreamType.Depth: 
                    {
                        StreamToMat(DepthStream as KStudioSeekableEventStream, "Depth", path);
                        break;
                    }
                case KinectStreamType.IR:
                    {
                        StreamToMat(IRStream as KStudioSeekableEventStream, "IR", path);
                        break;
                    }
                case KinectStreamType.Opaque:
                case KinectStreamType.Body:
                default:
                    break;
            }
        }

        public void ExportAll(string path)
        {
            if (!IsStreamLoaded) return;

            SaveToMat(KinectStreamType.Depth, path);
            SaveToMat(KinectStreamType.IR, path);

            OnExportFinished();
        }

        public async Task ExportAllAsync(string path)
        {
            if (!IsStreamLoaded) return;

            await Task.Run(() =>
            {
                SaveToMat(KinectStreamType.Depth, path);
                SaveToMat(KinectStreamType.IR, path);

                OnExportFinished();
            });            
        }
    }
}
