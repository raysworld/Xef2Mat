using MicroMvvm;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using XEF2MATCore;

namespace Xef2MatUI
{
    public class ViewModel: ObservableObject
    {
        private double _progress;
        public double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => Set(ref _fileName, value);
        }
        private bool _isButtonEnabled;
        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set => Set(ref _isButtonEnabled, value);
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        Xef2MatCore XCore;

        public ViewModel()
        {
            Progress = 0;
            FileName = "";
            IsButtonEnabled = true;

            XCore = new Xef2MatCore();
            XCore.ProgressUpdated += XCore_ProgressUpdated;
            XCore.FileLoaded += XCore_FileLoaded;
            XCore.ExportFinished += XCore_ExportFinished;
        }
        
        private void XCore_FileLoaded()=> IsButtonEnabled = false;
        private void XCore_ProgressUpdated(double progress) => Progress = progress;
        private void XCore_ExportFinished()
        { 
            IsButtonEnabled = true; 
            Progress = 0; 
        }

        public IAsyncCommand SelectFileCommand { get => new AsyncCommand(UpdateFileSelectionExecute, CanFileSelectionExecute); }

        private bool CanFileSelectionExecute() => !IsBusy;

        private async Task UpdateFileSelectionExecute()
        {
            var folder_path = Environment.CurrentDirectory;
            if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.CurrentDirectory;
            openFileDialog.Filter = "Kinect Studio Data File|*.xef";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileName = openFileDialog.FileName;

                IsBusy = true;
                IsButtonEnabled = false;
                //XCore.Load(FileName);
                //await XCore.ExportAllAsync($"C:/Users/raysw/Downloads/20150622_125509_00");
                await Do_work();

                IsButtonEnabled = true;
                IsBusy = false;
            }
            else
            {
                return;
            }
        }

        private async Task Do_work()
        {
            await Task.Run(() =>
            {
                while (Progress < 100)
                {
                    Progress++;
                    Thread.Sleep(100);
                }                              
            });
        }
    }
}
