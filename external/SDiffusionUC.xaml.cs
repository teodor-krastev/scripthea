using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json;
using OpenHWMonitor;
using Path = System.IO.Path;
using System.IO.Pipes;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for diffusionUC.xaml
    /// </summary>
    public partial class SDiffusionUC : UserControl, interfaceAPI
    {
        PipeServer2S server2s; PipeServer2C server2c; NVidia nVidia;
        public SDiffusionUC()
        {
            InitializeComponent(); localDebug = Utils.isInVisualStudio;
            opts = new Dictionary<string, string>();
            nVidia = new NVidia();
        }
        SDoptionsWindow SDopts;  private bool localDebug = true;
        public Dictionary<string, string> opts { get; set; } // main (non this API specific) options 
        
        public void Init(string prompt) // init and update visuals from opts
        {
            lbStatus.Content = "COMM: closed"; server2s = null; server2c = null;
            if (SDopts == null)
            {
                SDopts = new SDoptionsWindow(); 
                bool nVidiaAvailable = nVidia.IsAvailable();
                SDopts.groupGPUtmpr.IsEnabled = nVidiaAvailable;
                gridTmpr.IsEnabled = nVidiaAvailable;
                if (!nVidia.IsAvailable()) SDopts.opts.showGPUtemp = false;
                opts2Visual(SDopts.opts);
            }
            // (re)create servers          
            server2s = new PipeServer2S("scripthea_pipe2s");
            server2s.OnLog += inLog;
            server2s.TextReceived += Server_TextReceived; // solely for debug
            server2s.OnStatusChange += new PipeServer2S.StatusChangeHandler(StatusChange);
            server2s.status = SDServer.Status.waiting;

            server2c = new PipeServer2C("scripthea_pipe2c");            
        }
        private void opts2Visual(SDoptions opts)
        {
            if (opts.showCommLog) gridSDlog.Visibility = Visibility.Visible;
            else gridSDlog.Visibility = Visibility.Collapsed;
            if (opts.showGPUtemp) gridTmpr.Visibility = Visibility.Visible;
            else gridTmpr.Visibility = Visibility.Collapsed;
            chkTmpr.IsChecked = opts.GPUtemperature;
            numGPUThreshold.Value = opts.GPUThreshold;
        }
        public void Finish()
        {
            if (Utils.isNull(server2c)) return;
            
            server2c.CloseSession(); // close server 
            server2s = null; server2c = null;
            SDopts.keepOpen = false; SDopts.Close();
        }       
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public bool isDocked { get { return true; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get; } // connected and working (depends on the API)
        private void SimulatorImage(string filepath) // copy random simulator file to filepath
        {
            File.Copy(SimulFolder.RandomImageFile, filepath);
        }
        public bool imageReady 
        { 
            get 
            {
                if (Utils.isNull(server2s) || Utils.isNull(server2c)) return false;
                return server2s.status.Equals(SDServer.Status.imageReady);                
            } 
        }
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename) // returns the filename of saved/copied in ImageDepoFolder image 
        {
            if (SDopts.opts.GPUtemperature && dTimer.IsEnabled)
            {
                while ((currentTmp > SDopts.opts.GPUThreshold) || (currentTmp == -1)) { Thread.Sleep(500); }
            }
            filename = Utils.timeName(); // target image 
            if (Utils.isNull(server2s) || Utils.isNull(server2c)) { inLog("Err: communication issue"); return false; }
            string folder = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\";  opts["folder"] = folder; 
            string fullFN = Path.Combine(folder,Path.ChangeExtension(filename, ".png"));
            if (File.Exists(fullFN))
            {
                fullFN = Utils.AvoidOverwrite(fullFN); // new name
                filename = Path.ChangeExtension(Path.GetFileName(fullFN), "");
            }
            
            int k = 0; // wait for expect state
            while (!server2s.status.Equals(SDServer.Status.promptExpect) && (k < (2 * SDopts.opts.TimeOutImgGen))) { Thread.Sleep(500); k++; }
            if (!server2s.status.Equals(SDServer.Status.promptExpect)) { inLog("Err: time-out at promptExpect"); return false; }

            Dictionary<string, string> jsn = new Dictionary<string, string>();
            jsn.Add("prompt", prompt); jsn.Add("folder", folder); jsn.Add("filename", filename);
            filename = Path.ChangeExtension(filename, ".png");
            if (!server2c.Send(JsonConvert.SerializeObject(jsn))) { inLog("Err: fail to send a message to the client"); return false; }

            k = 0; bool bir = imageReady; // wait for image gen.
            while (!bir && (k < (5 * SDopts.opts.TimeOutImgGen))) {  Thread.Sleep(200); k++; if (!bir) bir = imageReady; }                       
            if (!bir) { inLog("Err: time-out at imageReady"); return false; }

            //if (server.debug && bir) SimulatorImage(fullFN);
            if (!File.Exists(fullFN)) { inLog("Err: image file <" + fullFN + "> not found"); return false; }
            
            return bir;
        }
        private void iTimer_Tick(object sender, EventArgs e)
        {
        }
        private void StatusChange(SDServer.Status previous, SDServer.Status current)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                  new Action(() =>
                  {
                        lbStatus.Content = "COMM: " + current.ToString(); lbStatus.UpdateLayout();        
                        btnReset.IsEnabled = current == SDServer.Status.promptExpect;            
             
                        switch (current)
                        {
                            case SDServer.Status.closed: BorderBrush = Brushes.White; // before server opens 
                                break;
                            case SDServer.Status.waiting: BorderBrush = Brushes.Silver; // waiting the client to call
                                break;
                            case SDServer.Status.connected: BorderBrush = Brushes.Gold; // ... the client called from the Scripthea script   
                                server2c.OnLog += inLog;
                                break;
                            case SDServer.Status.promptExpect: BorderBrush = Brushes.RoyalBlue; // the script for the next prompt
                                break;
                            case SDServer.Status.promptSent: BorderBrush = Brushes.Coral; // prompt sent, image is expected 
                                break;
                            case SDServer.Status.imageReady: BorderBrush = Brushes.LightSeaGreen; // image has been generated
                                break;
                        }           
                  }));
            }
            catch { }
        }
        protected void inLog(String txt, SolidColorBrush clr = null)
        {
            if (txt.Length.Equals(0)) return;
            if (Application.Current == null) return;
            Utils.log(rtbSDlog, txt.Trim(), clr);
        }
        private void Server_TextReceived(object sender, string txt)
        {
            if (localDebug) inLog("> "+txt);
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Log("@CancelRequest");
            inLog("closing session in client", Brushes.IndianRed);
            if (!Utils.isNull(server2c))
                if (server2c.IsConnected) server2c.CloseSession();
            inLog("to start another session restart Scripthea application", Brushes.Red);

            return;
            if (!Utils.isNull(server2s)) //&& false
                if (server2s.IsConnected)
                    { server2s.reader.Dispose(); server2s.Close(); } //
            if (!Utils.isNull(server2c))
                if (server2c.IsConnected) 
                    {  Utils.Sleep(100); server2c.writer.Dispose(); server2c.Close(); }
            Utils.Sleep(100);

            Init("");
        }
        private void lb1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender.Equals(lb1)) Utils.AskTheWeb("Stable Diffusion");
            if (sender.Equals(lb2)) Utils.CallTheWeb("https://stability.ai/");
            if (sender.Equals(lb3)) Utils.CallTheWeb("http://127.0.0.1:7860/");
        }
        int currentTmp = -1; double averTmp = -1; int maxTmp = -1;
        List<int> tmpStack; 
        DispatcherTimer dTimer;
        private void chkTemp_Checked(object sender, RoutedEventArgs e)
        {
            if (chkTmpr.IsChecked.Value)
            {
                if (Utils.isNull(dTimer))
                {
                    dTimer = new DispatcherTimer();
                    dTimer.Tick += new EventHandler(dTimer_Tick);
                    dTimer.Interval = new TimeSpan(2000 * 10000); // 2 [sec]
                    tmpStack = new List<int>();
                }
                dTimer.Start();
            }
            else { dTimer.Stop(); chkTmpr.Foreground = Brushes.Black; }
            SDopts.opts.GPUtemperature = chkTmpr.IsChecked.Value;
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (!nVidia.IsAvailable()) return;
            int? primeTmp = nVidia.GetGPUtemperature();
            if (Utils.isNull(primeTmp)) return;
            currentTmp = (int)primeTmp;
            if (currentTmp < SDopts.opts.GPUThreshold) chkTmpr.Foreground = Brushes.Blue;
            else chkTmpr.Foreground = Brushes.Red;
            chkTmpr.Content = "GPU temp[°C] = " + currentTmp.ToString();
            tmpStack.Add(currentTmp);
            while (tmpStack.Count > SDopts.opts.GPUstackDepth) tmpStack.RemoveAt(0);
            averTmp = tmpStack.ToArray().Average();
            maxTmp = -1;
            foreach (int t in tmpStack)
                maxTmp = Math.Max(t, maxTmp);
            lbTmpInfo.Content = "aver: " + averTmp.ToString("G3") + "  max: " + maxTmp.ToString();
        }
        private void numGPUThreshold_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (SDopts != null)
                SDopts.opts.GPUThreshold = numGPUThreshold.Value;
        }
        private void ibtnOpts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SDopts.opts2Visual(); SDopts.ShowDialog(); 
            opts2Visual(SDopts.opts);
        }
        #region Watcher
        // Watcher ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        FileSystemWatcher watcher;

        private void watchImageDir(string dir)
        {
            if (Utils.isNull(watcher))
            {
                watcher = new FileSystemWatcher(dir);
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.*";
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
            if (!System.IO.Path.GetExtension(e.Name).Equals(".sis")) return;
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(
                    delegate ()
                    {
                        /*if (!LineMode) return;
                        procStage = 2;
                        while (!IsFileReady(e.FullPath)) { DoEvents(); }
                        UpdateFileList(e.Name);*/
                    }
                )
            );
        }
        #endregion Watcher
    }
    #region PipeServers
    public class PipeServer2S : SDServer // incoming
    {
        public StreamReader reader = null;
        public event EventHandler<string> TextReceived;
        private Status _status;
        public Status status
        {
            get
            {
                if (pipeServer == null) _status = Status.closed;
                return _status;
            }
            set { Status previous = _status; _status = value; StatusChange(previous, value); }
        }
        public delegate void StatusChangeHandler(Status previous, Status current);
        public event StatusChangeHandler OnStatusChange;
        public void StatusChange(Status previous, Status current)
        {
            if (OnStatusChange != null) OnStatusChange(previous, current);
        }

        protected void Receive(string txt)
        {
            if (txt == null) return;
            switch (txt.Trim())
            {
                case "@next.prompt":
                    status = Status.promptExpect;
                    break;
                case "@image.ready":
                    status = Status.imageReady;
                    break;
                case "@image.failed":
                    status = Status.imageFailed;
                    break;
                case "@close.session":
                    status = Status.waiting;
                    break;
            }
        }
        public PipeServer2S(string pipeName) : base(pipeName, PipeDirection.In)
        {
            reader = new StreamReader(pipeServer);
        }
        public override void AddAction()
        {
            while (IsConnected && !cts.IsCancellationRequested)
            {
                string message = reader.ReadLine();
                if (message == null)
                {
                    log("session cut-off by client");
                    status = Status.closed;
                    break;
                }
                Receive(message);
                TextReceived?.Invoke(this, message);
                if (message.Trim() == "@close.session")
                {
                    log("session closed by client");
                    status = Status.closed;
                    break;
                }
            }
        }
    }
    public class PipeServer2C : SDServer // outgoing
    {
        public StreamWriter writer = null;
        public PipeServer2C(string pipeName) : base(pipeName, PipeDirection.Out)
        {
            writer = new StreamWriter(pipeServer);          
        }
        public bool Send(string message)
        {            
            try
            {
                if (!IsConnected) return false; ;
                if (writer == null) { writer = new StreamWriter(pipeServer); }
                writer.Write(message);
                if (IsConnected) writer.Flush();
            }
            catch(Exception e) { log("problem flushing the write buffer ("+e.Message+")"); return false; }
            return IsConnected;
        }
        public override void AddAction()
        {
            //while (IsConnected && !cts.IsCancellationRequested)  { Utils.Sleep(100); }
        }
        public void CloseSession()
        {
            Send("@close.session");
        }
    }
    public class SDServer
    {
        public enum Status
        {
            closed,         // before server opens 
            waiting,        // waiting the client to call
            connected,      // ... the client called from the Scripthea script          
            promptExpect,   // the script for the next prompt
            promptSent,     // prompt sent, image is expected 
            imageReady,     // image has been generated
            imageFailed     // image has failed to be generated
        }
        public bool IsConnected { get { if (pipeServer == null) return false; return pipeServer.IsConnected; } }
        protected NamedPipeServerStream pipeServer;
        public Task task;
        protected CancellationTokenSource cts;
        protected CancellationToken token;
        public SDServer(string pipeName, PipeDirection direction)
        {
            pipeServer = new NamedPipeServerStream(pipeName, direction, 1);
            cts = new CancellationTokenSource(); token = cts.Token;
            task = Task.Run(() => PipeServer(), token);
        }
        public event Utils.LogHandler OnLog;
        protected void log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Console.WriteLine(txt);
        }
        public virtual void AddAction()
        {
        }
        protected void PipeServer()
        {
            log("Waiting to connect...");
            pipeServer.WaitForConnection();
            log("open session");

            AddAction();
        }
        public void Close()
        {                     
            if (task.Status.Equals(TaskStatus.Running)  || task.Status.Equals(TaskStatus.RanToCompletion))
            {
                cts.Cancel(); task.Wait(TimeSpan.FromSeconds(3)); 
               /* try
                {
                    if (!task.Wait(TimeSpan.FromSeconds(1)))
                    {
                        throw new TaskCanceledException("Task did not complete within the timeout.");
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Task timed out. Terminating...");
                    task.Kill();
                }*/                            
            }
            if (pipeServer.IsConnected) 
                { pipeServer.Disconnect(); pipeServer.Dispose(); }
        }
    }
    #endregion PipeServer
}

