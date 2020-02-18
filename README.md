# Xef2Mat
WPF-based converter for converting Kinect Studio data file (`.xef`) to Matlab data file (`.mat`)  

### Author
Rui LI 

### Date
2016-11-07  (Last update: 2020-01-14)

### Instructions
This app is used for converting Kinect Studio data file (`.xef`) to Matlab data file (`.mat`).
The project is based on the following references:  

- `Microsoft.Kinect` (You may find it by installing [Microsoft Kinect SDK 2.0](https://www.microsoft.com/en-us/download/details.aspx?id=44561))  

- `Microsoft.Kinect.Tools` (You may also find it by installing Microsoft Kinect SDK 2.0)  

- `MATWriter` (Written by [SergentMT]( http://www.codeproject.com/Tips/819613/Kinect-Version-Depth-Frame-to-mat-File-Exporter))  

### Notes:
It seems that this app only works on **x64** platforms in **debug** mode  (For the latest version debug/release both work)

### How-to-use:

- install Microsoft Kinect SDK 2.0
- `git clone` and then build the repo with Visual Studio 2017 or later, or download the binary from [here](https://github.com/raysworld/Xef2Mat/releases/download/v0.2/xef2mat_dbg_x64.7z).
- see `demo/demo_import_data.m` a demo script for importing the data into Matlab.

##### Run with GUI

Simply run xef2mat.exe, select the `.xef` file by clicking on the 'select' button. The output files will be at the same folder of the app.  

##### Run without GUI

```powershell
./xef2mat.exe --no-gui [source_file_path] [dest_folder_path]
```

### Related links:
You may visit https://github.com/Isaac-W/KinectXEFTools to find a .NET framework based `xef` reader.
