using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using scripthea.master;
using scripthea.viewer;
using scripthea.options;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.engineAPI
{
    /// <summary>
    /// Interaction logic for AddonGenUC.xaml
    /// </summary>
    public partial class AddonGenUC : UserControl, interfaceAPI
    {
        public AddonGenUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
        }
        public Dictionary<string, string> opts { get; set; }
        private Options optsRef; private DispatcherTimer dTimer;
        public void Init(ref Options _opts) 
        {
            optsRef = _opts;
            if (Directory.Exists(optsRef.iDutilities.AddonGenFolder)) lbTargetFolder.Text = "Addon Gen. folder: " + optsRef.iDutilities.AddonGenFolder;
            else { lbTargetFolder.Text = "Addon Gen.folder - not found"; optsRef.iDutilities.AddonGenFolder = @"d:\Scripthea\images\Reflection\"; }
            opts["addonFolder"] = optsRef.iDutilities.AddonGenFolder;

            testMode = false;
            dTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(1000), IsEnabled = false };
            dTimer.Tick += new EventHandler(dTimer_Tick);
        }
        public void Finish() 
        {
            if (opts.Count == 0) return; // it hasn't been init yet
            optsRef.iDutilities.AddonGenFolder = opts["addonFolder"]; 
        }
        public void Broadcast(string msg)
        {
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public event APIparamsHandler APIparamsEvent;
        protected Dictionary<string, object> OnAPIparams(bool? showIt)
        {
            return APIparamsEvent?.Invoke(showIt);
        }
        public bool isDocked { get { return false; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get { return true; } }

        private ImageInfo ii = null;
        private Process process = null;

        public bool GenerateImage(string prompt, string imageDepotFolder, ref ImageInfo iii)
        {
            Dictionary<string, object> apIn = OnAPIparams(null);
            ii = new ImageInfo(apIn); iii = ii;
            if (!Directory.Exists(opts["addonFolder"])) 
                { Utils.TimedMessageBox("Error: Addon folder <" + opts["addonFolder"] + "> - not found."); return false; }
            if (!Directory.Exists(imageDepotFolder))
                { Utils.TimedMessageBox("Error: image folder <" + imageDepotFolder + "> - not found."); return false; }
            opts["IDfolder"] = imageDepotFolder;
            running = true;
            string msgFile = Path.Combine(opts["addonFolder"], "msgback.txt");
            if (File.Exists(msgFile)) File.Delete(msgFile);

            ii.prompt = prompt;
            string paramFile = Path.Combine(opts["addonFolder"], "parameters.json");
            if (File.Exists(paramFile))
                if (IsFlReady(paramFile)) File.Delete(paramFile);
            File.WriteAllText(paramFile, ii.To_String());

            watchImageDir(opts["addonFolder"]);           
            string queryFile = Path.Combine(opts["addonFolder"], "text2image.bat");
            if (!File.Exists(queryFile)) { Utils.TimedMessageBox("Error: no file " + queryFile); return false; }
            process = Utils.RunBatchFile(queryFile);
            int tm = 0; int timeOut = 120; // 60 sec
            while (running && tm < timeOut) { Utils.Sleep(500); tm++; }
            if (tm == timeOut) { Utils.TimedMessageBox("Time out (60s) in addon image generation"); return false; }
            return true;
        }
        private bool running = false;
        FileSystemWatcher watcher;
        private void watchImageDir(string dir)
        {
            if (!running) return;
            if (!Directory.Exists(dir)) { Utils.TimedMessageBox("Error: wrong dir <"+dir+">"); return; }
            if (Utils.isNull(watcher))
            {
                watcher = new FileSystemWatcher(dir);
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.*"; 
                watcher.Created += new FileSystemEventHandler(OnChangedWatcher);
                //watcher.Changed += new FileSystemEventHandler(OnChangedWatcher);
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
        public bool IsFlReady(string filePath)
        {        
            bool isReady = false;
            int retries = 0;
            const int maxRetries = 10; // 10sec timeout
            const int retryInterval = 200; // milliseconds
            while (!isReady && retries<maxRetries)
            {
                try
                {
                    using (File.Open(filePath, FileMode.Open, FileAccess.Read))
                    {
                        isReady = true; // File can be opened, considered complete
                    }
                }
                catch (IOException)
                {
                    retries++;
                    Utils.Sleep(retryInterval);
                }
            }
            if (isReady)
            {
                // Process the completed file
                Console.WriteLine($"File '{filePath}' is ready for processing.");
            }
            else
            {
                Console.WriteLine($"Failed to verify file completion: '{filePath}'.");
            }
            return isReady;
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            dTimer.Stop();
            lbLastAddedFile.Text = pretext + Path.GetFileName(imgPath);
                    
            if (imgPath.Equals(string.Empty)) return;
            imgMonitor.Source = ImgUtils.UnhookedImageLoad(imgPath);
        }
        private string pretext, imgPath;
        private void ShowIt(string _pretext, string _imgPath)
        { 
            if (!IsVisible) return;
            dTimer.Start();
            pretext = _pretext; imgPath = _imgPath;
        }
        private void OnChangedWatcher(object source, FileSystemEventArgs e)
        {            
            if (e.Name.Equals("msgback.txt"))
            {
                string msg = File.ReadAllText(e.FullPath);
                ShowIt("Message from Addon gen. :" + msg, "");
                //if (msg.StartsWith("Error:")) return;
            }
            if (!Path.GetExtension(e.Name).Equals(".png")) return;
            if (IsFlReady(e.FullPath)) 
            { 
                //  + e.Name;
                Utils.Sleep(200); ii.filename = e.Name; 
                string targetImg = testMode ? e.FullPath : Path.Combine(opts["IDfolder"], ii.filename);
                ShowIt("Last created file: ", targetImg);
                if (!testMode) File.Move(e.FullPath, targetImg);
                Utils.Sleep(200); ii.MD5Checksum = Utils.GetMD5Checksum(targetImg); running = false;
            }
            else { ShowIt("Addon gen. time out !!!",""); }                        
                        
            string paramFile = Path.Combine(opts["addonFolder"], "parameters.json"); // clear params in case addon app left it
            if (File.Exists(paramFile)) 
                if (IsFlReady(paramFile)) File.Delete(paramFile);
            
            if (process != null)
                if (!process.HasExited)
                    if (!process.CloseMainWindow()) process.Kill();            
        }
        private bool testMode = false;
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            ii = new ImageInfo(); testMode = true;
            GenerateImage("Tonight the city is alive with lights that echo heaven's stars.", opts["IDfolder"], ref ii);
            testMode = false; ;
        }
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.InitialDirectory = imageFolder;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                opts["addonFolder"] = dialog.FileName; lbTargetFolder.Text = "Addon Gen. folder: " + opts["addonFolder"];
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
