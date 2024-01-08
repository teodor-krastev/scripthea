using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using scripthea.master;
using scripthea.viewer;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for sPluginUC.xaml
    /// </summary>
    public partial class sPluginUC : UserControl, interfaceAPI
    {
        public sPluginUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
        }
        string tFolder = @"d:\Projects\Scripthea\sPlugin\"; // target folder
        public Dictionary<string, string> opts { get; set; }

        public void Init(ref Options _opts) { lbTargetFolder.Content = "Target folder: " + tFolder; }
        public void Finish() { }
        public void Broadcast(string msg)
        {

        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public event APIparamsHandler APIparamsEvent;
        public bool isDocked { get { return false; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get { return true; } }
        public bool GenerateImage(string prompt, string imageDepotFolder, out ImageInfo ii)
        {
            running = true; ii = new ImageInfo();

            watchImageDir(tFolder);           
            string queryFile = Path.Combine(tFolder, "query.bat");
            if (!File.Exists(queryFile)) { Utils.TimedMessageBox("Error: no file " + queryFile); return false; }
            //System.Diagnostics.Process.Start(queryFile);
            Utils.RunBatchFile(queryFile);
            running = false;
            return true;
        }
        private bool running = false;
        FileSystemWatcher watcher;
        private void watchImageDir(string dir)
        {
            //if (!running) return;
            if (!Directory.Exists(dir)) { Utils.TimedMessageBox("Error: wrong dir"); return; }
            if (Utils.isNull(watcher))
            {
                watcher = new FileSystemWatcher(dir);
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.png";
                watcher.Created += new FileSystemEventHandler(OnChangedWatcher);
            }
            else { watcher.Path = dir; }
            watcher.EnableRaisingEvents = true;
        }
        public bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private void OnChangedWatcher(object source, FileSystemEventArgs e)
        {
            //if (!System.IO.Path.GetExtension(e.Name).Equals(".sis")) return;
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(
                    delegate ()
                    {                      
                        while (!IsFileReady(e.FullPath)) { Utils.DoEvents(); }
                        lbLastAddedFile.Content = "Last added file: "+e.Name;
                    }
                )
            );
        }
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage("something", "uiytuiytuyt", out ImageInfo ii);
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.InitialDirectory = imageFolder;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tFolder = dialog.FileName; lbTargetFolder.Content = "Target folder: " + tFolder;
            }
        }
    }
}
/*  **Here's how to run a BAT file from C#:**

**1. Use the `Process` class:**

```csharp
using System.Diagnostics;

// Replace with the actual path to your BAT file
string batchFilePath = @"C:\path\to\your\batchfile.bat";

Process process = new Process();
process.StartInfo.FileName = "cmd.exe";  // Use cmd.exe to execute the BAT file
process.StartInfo.Arguments = "/c " + batchFilePath;
process.StartInfo.UseShellExecute = false; // Avoid opening a command prompt window
process.StartInfo.CreateNoWindow = true;  // Hide the command prompt window
process.Start();

// Wait for the process to finish (optional)
process.WaitForExit();
```

**Explanation:**

- **`Process.StartInfo`:** Configures how the process will be started.
- **`FileName`:** Sets the executable to run (cmd.exe in this case).
- **`Arguments`:** Specifies the command to execute (`/c` tells cmd.exe to run a command and then exit).
- **`UseShellExecute`:** Set to `false` to prevent opening a command prompt window.
- **`CreateNoWindow`:** Set to `true` to hide the command prompt window.

**2. Redirecting output (optional):**

```csharp
process.StartInfo.RedirectStandardOutput = true;
process.Start();

string output = process.StandardOutput.ReadToEnd();
Console.WriteLine(output);  // Example of handling the output
```

**3. Waiting for the process to finish (optional):**

```csharp
process.WaitForExit();
```

**Additional considerations:**

- **File paths:** Ensure correct paths to the BAT file.
- **Permissions:** Your C# application might need proper permissions to run the BAT file.
- **Administrative rights:** If the BAT file requires elevated privileges, you might need to run your C# application as an administrator.*/
