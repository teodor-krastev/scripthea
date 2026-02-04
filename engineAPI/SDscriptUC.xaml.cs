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
using Path = System.IO.Path;
using System.IO.Pipes;
using scripthea.options;
using UtilsNS;

namespace scripthea.engineAPI
{
    /// <summary>
    /// Interaction logic for SDscriptUC.xaml
    /// </summary>
    public partial class SDscriptUC : UserControl
    {
        public SDscriptUC()
        {
            InitializeComponent(); localDebug = Utils.isInVisualStudio;
            opts = new Dictionary<string, string>();
        }
        private PipeServer2S server2s; private PipeServer2C server2c;
        SDoptionsWindow SDopts; private bool localDebug = true; 
        public Dictionary<string, string> opts { get; set; } // interfaceAPI: main (non API specific) options 
        public void Init(ref SDoptionsWindow _SDopts) // init and update visuals from opts
        {
            if (_SDopts is null) _SDopts = new SDoptionsWindow(true);
            SDopts = _SDopts;
            if (!SDopts.IsSDloc1111(SDopts.opts.SDloc1111, false)) { Utils.DelayExec(3000, () => { Utils.TimedMessageBox("Go to SD Options and select your webUI SD instalation folder.", "Solution", 6000); }); return; }
            reStartServers();
        }
        public void reStartServers(bool forced = false)
        {
            if (forced) Finish(); 
            if (!Utils.isNull(server2s)) return;
            lbStatus.Content = "COMM: closed";
            // (re)create servers          
            server2s = new PipeServer2S("scripthea_pipe2s");
            server2s.OnLog += inLog;
            server2s.TextReceived += Server_TextReceived; // solely for debug
            server2s.OnStatusChange += new PipeServer2S.StatusChangeHandler(StatusChange);
            server2s.status = SDServer.Status.waiting;

            server2c = new PipeServer2C("scripthea_pipe2c");
        }
        public void CloseServer()
        {
            if (!Utils.isNull(server2c)) server2c.CloseSession(); // close server 
        }
        public void Finish()
        {
            CloseServer(); Utils.Sleep(500);
            if (!Utils.isNull(server2s))
            {
                server2s.reader.Close(); server2s.reader = null; GC.Collect();
                server2s.Dispose();
            }
            server2c?.Dispose(); //inLog("Scripthea server restarting...", Brushes.Red);
            Utils.Sleep(500);
            lbStatus.Content = "COMM: closed"; server2s = null; server2c = null; GC.Collect(); GC.WaitForPendingFinalizers();
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public bool isDocked { get { return true; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool IsConnected
        {
            get
            {
                if (server2s is null || server2c is null) return false;
                return server2s.IsConnected && server2c.IsConnected;  // connected and working (depends on the comm. type)
            } 
        }
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
            filename = Utils.timeName(); // target image 
            if (Utils.isNull(server2s) || Utils.isNull(server2c)) { inLog("Error[159]: communication issue"); return false; }
            string folder = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\"; opts["IDfolder"] = folder;
            string fullFN = Path.Combine(folder, Path.ChangeExtension(filename, ".png"));
            if (File.Exists(fullFN))
            {
                fullFN = Utils.AvoidOverwrite(fullFN); // new name
                filename = Path.ChangeExtension(Path.GetFileName(fullFN), "");
            }

            int k = 0; // wait for expect state
            while (!server2s.status.Equals(SDServer.Status.promptExpect) && (k < (2 * SDopts.opts.TimeOutImgGen))) { Thread.Sleep(500); k++; }
            if (!server2s.status.Equals(SDServer.Status.promptExpect)) { inLog("Error[35]: time-out at promptExpect"); return false; }

            Dictionary<string, string> jsn = new Dictionary<string, string>();
            jsn.Add("prompt", prompt); jsn.Add("IDfolder", folder); jsn.Add("filename", filename);
            filename = Path.ChangeExtension(filename, ".png");
            if (!server2c.Send(JsonConvert.SerializeObject(jsn))) { inLog("Error[471]: fail to send a message to the client"); return false; }

            k = 0; bool bir = imageReady; // wait for image gen.
            while (!bir && (k < (5 * SDopts.opts.TimeOutImgGen))) { Thread.Sleep(200); k++; if (!bir) bir = imageReady; }
            if (!bir) { inLog("Error[842]: time-out at imageReady"); return false; }

            //if (server.debug && bir) SimulatorImage(fullFN);
            if (!File.Exists(fullFN)) { inLog("Error[14]: image file <" + fullFN + "> not found"); return false; }

            return bir;
        }
        private void StatusChange(SDServer.Status previous, SDServer.Status current)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                  new Action(() =>
                  {
                      lbStatus.Content = "COMM: " + current.ToString(); lbStatus.UpdateLayout();
                      VisualHelper.SetButtonEnabled(btnReset, current == SDServer.Status.promptExpect);

                      switch (current)
                      {
                          case SDServer.Status.closed:
                              BorderBrush = Brushes.White; // before server opens 
                                                           //if (!previous.Equals(SDServer.Status.closed)) RestartServer();
                              break;
                          case SDServer.Status.waiting:
                              BorderBrush = Brushes.Silver; // waiting the client to call
                              break;
                          case SDServer.Status.connected:
                              BorderBrush = Brushes.Gold; // ... the client called from the Scripthea script   
                              server2c.OnLog += inLog;
                              break;
                          case SDServer.Status.promptExpect:
                              BorderBrush = Brushes.RoyalBlue; // the script for the next prompt
                              break;
                          case SDServer.Status.promptSent:
                              BorderBrush = Brushes.Coral; // prompt sent, image is expected 
                              break;
                          case SDServer.Status.imageReady:
                              BorderBrush = Brushes.LightSeaGreen; // image has been generated
                              break;
                      }
                  }));
            }
            catch { }
        }
        protected void inLog(String txt, SolidColorBrush clr = null)
        {
            if (txt.Length.Equals(0)) return;
            if (Application.Current is null) return;
            Utils.log(rtbSDlog, txt.Trim(), clr);
        }
        private void Server_TextReceived(object sender, string txt)
        {
            if (localDebug) inLog("> " + txt);
        }
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Log("@CancelRequest");
            reStartServers(true);
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
                if (pipeServer is null) _status = Status.closed;
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
            if (txt is null) return;
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
                case "exit":
                    status = Status.closed;
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
                if (message is null)
                {
                    log("session close by client(SD)");
                    status = Status.closed;
                    break;
                }
                Receive(message);
                TextReceived?.Invoke(this, message);
                if (message.Trim() == "exit")
                {
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
                if (writer is null) { writer = new StreamWriter(pipeServer); }
                writer.Write(message);
                if (IsConnected) writer.Flush();
            }
            catch (Exception e) { log("problem flushing the write buffer (" + e.Message + ")"); return false; }
            return IsConnected;
        }
        public override void AddAction()
        {
            while (IsConnected && !cts.IsCancellationRequested) { Utils.Sleep(200); }
        }
        public bool CloseSession()
        {
            return Send("@close.session");
        }
    }
    public class SDServer : IDisposable
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
        public bool IsConnected { get { if (pipeServer is null) return false; return pipeServer.IsConnected; } }
        protected string pipeName;
        protected NamedPipeServerStream pipeServer;
        public Task task;
        protected CancellationTokenSource cts;

        public SDServer(string _pipeName, PipeDirection direction)
        {
            pipeName = _pipeName;
            pipeServer = new NamedPipeServerStream(pipeName, direction, 1);
            cts = new CancellationTokenSource();
            task = Task.Run(() => PipeServer(), cts.Token);
        }
        public void Stop()
        {
            if (pipeServer.IsConnected) pipeServer.Disconnect();
            cts.Cancel(); 
            try { task.Wait(); }
            catch (AggregateException ex) { Utils.TimedMessageBox(ex.Message); }
            //task.Dispose();
        }
        public void Dispose()
        {
            if (pipeServer.IsConnected) pipeServer.Disconnect();
            cts.Cancel();
            pipeServer.Close(); pipeServer.Dispose(); pipeServer = null;
            GC.Collect(); GC.WaitForPendingFinalizers();
            //try { task.Wait(); }
            //catch (AggregateException ex) { Utils.TimedMessageBox(ex.Message); }
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
            log(pipeName + " session");
        }
        public void Close()
        {
            if (task.Status.Equals(TaskStatus.Running) || task.Status.Equals(TaskStatus.RanToCompletion))
            {
                cts.Cancel(); task.Wait(3000);
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
            if (pipeServer.IsConnected) pipeServer.Disconnect();
            pipeServer.Dispose();
        }
    }
    #endregion PipeServer


}
