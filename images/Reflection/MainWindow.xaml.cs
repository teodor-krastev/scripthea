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
using UtilsNS;
using Path = System.IO.Path;

namespace Reflection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool addon_type2 = false; // default is type 1 
        public MainWindow()
        {
            InitializeComponent();
            string[] arguments = Environment.GetCommandLineArgs();
            
            if (arguments.Length == 0) return;
            if (arguments.Length > 0)
            { 
                exePath = Path.GetDirectoryName(arguments[0]);
                if (!Directory.Exists(exePath)) exePath = Path.GetDirectoryName(Utils.appFullPath);
            }
            if (arguments.Length == 2)
                addon_type2 = arguments[1] == "--type2";
            Title += " - Scripthea addon type " + (addon_type2 ? "2" : "1");
        }
        private void wndReflection_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout(); Utils.DoEvents(); 
            btnMakeImage_Click(sender, null);
            if (addon_type2) 
            {
                watchImageDir(exePath);                 
            }
            else // type 1
            {
                Close();
            }
        }
        private string exePath; 
        Random rnd = new Random();
        private void btnMakeImage_Click(object sender, RoutedEventArgs e)
        {
            string paramFile = Path.Combine(exePath, "parameters.json");
            if (!File.Exists(paramFile)) { Utils.TimedMessageBox(paramFile + " is missing"); return; }
            else
            {
                if (!IsFlReady(paramFile)) { Utils.TimedMessageBox(paramFile + " is locked by another process"); return; }
            }
            string prmsTxt = File.ReadAllText(paramFile); 
            Utils.Sleep(500); if (addon_type2) File.Delete(paramFile);
            tbLog.Text = "Input Parameters \n"+prmsTxt;
            string imgCollection = Path.Combine(Utils.GetBaseLocation(Utils.BaseLocation.oneUp), "simulator");
            tbLog.Text += "\n Image source folder: "+ imgCollection;
            List<string> files = new List<string>(Directory.GetFiles(imgCollection, "*.png"));
            string newImage = Path.Combine(exePath,Path.ChangeExtension(Utils.timeName(), ".png"));
            tbLog.Text += "\n\n Generating";
            for (int j = 0; j < 10; j++) { Utils.DoEvents(); Utils.Sleep(100); tbLog.Text += " ."; }
            File.Copy(files[rnd.Next(files.Count - 1)],  newImage);
            tbLog.Text += "\n Generated image: " + newImage;
        }
        FileSystemWatcher watcher;
        private void watchImageDir(string dir)
        {
            //if (!running) return;
            if (!Directory.Exists(dir)) { Utils.TimedMessageBox("Error: wrong dir <" + dir + ">"); return; }
            if (Utils.isNull(watcher))
            {
                watcher = new FileSystemWatcher(dir);
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.json"; 
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
        public bool IsFlReady(string filePath)
        {
            bool isReady = false;
            int retries = 0;
            const int maxRetries = 10; // 10sec timeout
            const int retryInterval = 200; // milliseconds
            while (!isReady && retries < maxRetries)
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
        private void OnChangedWatcher(object source, FileSystemEventArgs e)
        {
            //if (!System.IO.Path.GetExtension(e.Name).Equals(".sis")) return;
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(
                    delegate ()
                    {
                        if (!e.Name.Equals("parameters.json",StringComparison.InvariantCultureIgnoreCase)) return;
                        if (addon_type2) btnMakeImage_Click(null, null);
                    }
                )
            );
        }
    }
}
