using MicroMvvm;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XEF2MATCore;

namespace Xef2MatUI
{
    public class ViewModel: ObservableObject
    {
        private int _progress;
        public int Progress
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

        public Xef2MatCore XCore { get; private set; }
        

        public ViewModel()
        {
            XCore = new Xef2MatCore();
            XCore.ProgressUpdated += XCore_ProgressUpdated;
            XCore.FileLoaded += XCore_FileLoaded;
            XCore.ExportFinished += XCore_ExportFinished;
        }

        private void XCore_FileLoaded()=> IsButtonEnabled = false;
        private void XCore_ProgressUpdated(int progress) => Progress = progress;
        private void XCore_ExportFinished()
        { IsButtonEnabled = true; Progress = 0; }

        public ICommand SelectFileCommand { get => new RelayCommand(UpdateFileSelectionExecute, CanFileSelectionExecute); }

        private bool CanFileSelectionExecute() => true;

        private void UpdateFileSelectionExecute()
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
            }
            else
            {
                return;
            }
        }
    }
}
