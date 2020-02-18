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

namespace XEF2MAT_UI
{
    public class ViewModel: ObservableObject
    {
        private double _progress;
        public double Progress
        {
            get => _progress;
            set => Set(ref _progress, value);
        }

        public string _message;
        public string Message
        {
            get => _message;
            set => Set(ref _message, value);
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
            Message = "Ready";
            FileName = "";
            IsButtonEnabled = true;            
        }

        private void XCore_FileLoaded()
        {
            IsButtonEnabled = false;
        }
        private void XCore_ProgressUpdated(string name, double progress)
        {
            Progress = progress;
            Message = $"[{name}] - [ {progress.ToString("F2")}% ]";
        }
        private void XCore_ExportFinished()
        { 
            IsButtonEnabled = true; 
            Progress = 0;
            Message = $"Proceeded";
        }

        public IAsyncCommand SelectFileCommandAsync { get => new AsyncCommand(UpdateFileSelectionExecuteAsync, CanFileSelectionExecute); }
        private bool CanFileSelectionExecute() => !IsBusy;
        private async Task UpdateFileSelectionExecuteAsync()
        {

            var folder_path = Environment.CurrentDirectory;
            if (!Directory.Exists(folder_path)) Directory.CreateDirectory(folder_path);

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Environment.CurrentDirectory,
                Filter = "Kinect Studio Data File|*.xef",
                RestoreDirectory = true,
                FilterIndex = 1
            };

            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                FileName = openFileDialog.FileName;

                IsBusy = true;

                XCore = new Xef2MatCore();
                XCore.FileLoaded += XCore_FileLoaded;
                XCore.ProgressUpdated += XCore_ProgressUpdated;
                XCore.ExportFinished += XCore_ExportFinished;

                await XCore.LoadAsync(FileName);

                IsBusy = false;
            }
            else
            {
                return;
            }
        }
    }
}
