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
using UtilsNS;
using OpenHWMonitor;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for diffusionUC.xaml
    /// </summary>
    public partial class SDiffusionUC : UserControl, interfaceAPI
    {
        AsyncSocketListener server; NVidia nVidia;
        public SDiffusionUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
            nVidia = new NVidia();
        }
        SDoptionsWindow SDopts;
        public Dictionary<string, string> opts { get; set; } // main (non this API specific) options 
        Thread listenerThread = null; 
        public void Init(string prompt) // init and update visuals from opts
        {
            SDopts = new SDoptionsWindow(); lbStatus.Content = "COMM: closed"; 
            bool nVidiaAvailable = nVidia.IsAvailable();
            SDopts.groupGPUtmpr.IsEnabled = nVidiaAvailable;
            gridTmpr.IsEnabled = nVidiaAvailable;
            if (!nVidia.IsAvailable()) SDopts.opts.showGPUtemp = false;
            opts2Visual(SDopts.opts);

            // server.Init();
            server = new AsyncSocketListener(); 
            server.OnLog += new Utils.LogHandler(Log);
            server.OnReceive += new Utils.LogHandler(Receive);
            
            ThreadStart listenerThreadStart = new ThreadStart(server.StartListening);
            listenerThread = new Thread(listenerThreadStart);
            listenerThread.Start();
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
            if (Utils.isNull(server)) return;
            btnReset_Click(null, null); // close session
            server.Close(); // close server 
            listenerThread.Abort(); // clean up the server treading
            SDopts.keepOpen = false; SDopts.Close();
        }
        public bool isDocked { get { return true; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get; } // connected and working (depends on the API)
        private void SimulatorImage(string filepath)
        {
            string imageSimulFolder = Utils.basePath + "\\images\\Simulator\\";
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageSimulFolder, "c*.png"));
            if (orgFiles.Count.Equals(0)) throw new Exception("Wrong simulator image folder ->" + imageSimulFolder);
            Random rnd = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
            string fn = orgFiles[rnd.Next(orgFiles.Count - 1)];
            File.Copy(fn, filepath);
        }
        private bool imageReady = false; 
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename) // returns the filename of saved/copied in ImageDepoFolder image 
        {
            if (SDopts.opts.GPUtemperature && dTimer.IsEnabled)
            {
                while ((currentTmp > SDopts.opts.GPUThreshold) || (currentTmp == -1)) { Thread.Sleep(500); }
            }
            filename = Utils.timeName();
            opts["folder"] = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\";
            if (server.status.Equals(AsyncSocketListener.Status.promptExpect) && !Utils.isNull(server.workSocket))
            { 
                server.Send(server.workSocket, prompt); //, Utils.basePath + "\\images\\", "imageName"
                imageReady = false; //iTimer.Start();
                int k = 0; 
                while (!imageReady && (k < (2*SDopts.opts.TimeOutImgGen))) { Thread.Sleep(500); k++; }
            }

            //server.SendFields(prompt, opts["folder"], filename);

            

            //string data = server.GetFromClient();
            //string fn = System.IO.Path.ChangeExtension(filename,".sdj");
            /* add some custom fields
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            // add to dict HERE 
            data = JsonConvert.SerializeObject(dict);
             */
            //File.WriteAllText(System.IO.Path.Combine(opts["folder"], fn), data);            
            return imageReady;// !data.Equals("");
        }
        private void iTimer_Tick(object sender, EventArgs e)
        {
        }       
        private void OnChangeStatus()
        {
            switch (server.status)
            {
                case AsyncSocketListener.Status.closed: BorderBrush = Brushes.White; // before server opens 
                    break;
                case AsyncSocketListener.Status.waiting: BorderBrush = Brushes.Silver; // waiting the client to call
                    break;
                case AsyncSocketListener.Status.connected: BorderBrush = Brushes.Gold; // ... the client called from the Scripthea script   
                    break;
                case AsyncSocketListener.Status.promptExpect: BorderBrush = Brushes.RoyalBlue; // the script for the next prompt
                    break;
                case AsyncSocketListener.Status.promptSent: BorderBrush = Brushes.LightSeaGreen; // prompt sent, image is expected 
                    break;
                case AsyncSocketListener.Status.imageReady: imageReady = true; BorderBrush = Brushes.LimeGreen; // image has been generated
                    break;
            }
            btnReset.IsEnabled = server.status == AsyncSocketListener.Status.promptExpect;
        }
        protected void Log(String txt, SolidColorBrush clr = null)
        {
            if (txt.Length.Equals(0)) return;
            if (Application.Current == null) return;
            try
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                  new Action(() =>
                  {
                      if (txt.StartsWith("@"))
                      {
                          lbStatus.Content = "COMM: " + txt.Substring(1); lbStatus.UpdateLayout();
                          OnChangeStatus(); // secondary actions
                      }
                      else Utils.log(tbSDlog, txt);
                  }));
            }
            catch { }
        }
        protected void Receive(String txt, SolidColorBrush clr = null)
        {
            if (server.debug)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        Utils.log(tbSDlog, ">"+txt);
                    }));
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (server.status.Equals(AsyncSocketListener.Status.promptExpect) && !Utils.isNull(server.workSocket))
            {
                server.Send(server.workSocket, "@close.session"); 
            }
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
            currentTmp = nVidia.GetGPUtemperature();
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
    }

    /// https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services
    /// </summary>
    public class AsyncSocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsyncSocketListener()
        {
            status = Status.closed;
        }

        public bool debug = false;
        public void Init()
        {
            
        }
        public enum Status 
        { 
            closed,         // before server opens 
            waiting,        // waiting the client to call
            connected,      // ... the client called from the Scripthea script          
            promptExpect, // the script for the next prompt
            promptSent,     // prompt sent, image is expected 
            imageReady      // image has been generated
        }
        private Status _status;
        public Status status
        {
            get
            {
                if (listener == null) _status = Status.closed;
                return _status;
            }
            private set { _status = value; Log("@" + Convert.ToString(status)); }
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public event Utils.LogHandler OnReceive;
        protected void Receive(string txt, SolidColorBrush clr = null)
        {
            if (OnReceive != null) OnReceive(Convert.ToString(txt));
            switch (txt.Trim())
            {
                case "@next.prompt": status = Status.promptExpect;
                    break;
                case "@image.ready": status = Status.imageReady;
                    break;
                case "@close.session": status = Status.waiting;
                    break;
            }            
        }
        private Socket listener;
        public Socket GetSocket()
        {
            return this.listener;
        }
        public void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(IPAddress.Parse("127.0.0.1")); //Dns.GetHostEntry(Dns.GetHostName()); 
            IPAddress ipAddress = ipHostInfo.AddressList[3];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5344); //11000

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                status = Status.waiting; Log("waiting SD webUI script");
                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();
                    // Start an asynchronous socket to listen for connections.  
                    //Log("Waiting for Scrpthea script in SD-webui");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    
                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Log("Err: " + e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();
            if (status.Equals(Status.waiting) || status.Equals(Status.closed))
            {
                status = Status.connected; Log("client connected");
            }
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            ServerStateObject state = new ServerStateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }
        public Socket workSocket = null;

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;
            if (closing) return;
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            ServerStateObject state = (ServerStateObject)ar.AsyncState;
            Socket handler = state.workSocket; workSocket = state.workSocket; ;
            try
            {            
                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read
                    // more data.  
                    content = state.sb.ToString();
                    if (content.Contains("\n"))
                    {
                        // All the data has been read from the
                        // client. Display it on the console.  
                        //Log("justIn: "+content);
                        Receive(content);
                        // Echo the data back to the client.  
                        //this.Send(handler, content);
                        content = string.Empty;
                        state.sb.Clear();
                        handler.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, ServerStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (Exception e) { Log("client force closed -> "+e.Message);  status = Status.closed; }
        }

        public void Send(Socket handler, String data)
        {
            if (handler == null) return;
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
            if (data.Equals("@close.session")) status = Status.waiting;
            else status = Status.promptSent;
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                if (debug) Log(">" + bytesSent.ToString()+" bytes sent");
            }
            catch (Exception e)
            {
                Log("Err: "+e.ToString());
            }
        }
        private bool closing = false;
        public void Close() // complete
        {
            closing = true;
            if (!Utils.isNull(workSocket))
            {
                if (status.Equals(Status.promptExpect))
                {
                    Send(workSocket, "@close.session"); //, Utils.basePath + "\\images\\", "imageName"
                }
                if (workSocket.Connected)
                {
                    workSocket.Shutdown(SocketShutdown.Both);
                    workSocket.Close();
                }
                workSocket.Dispose();
            }
        }
    }

    // State object for reading client data asynchronously  
    public class ServerStateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 4096;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }
}

/* Err: System.Threading.ThreadAbortException: Thread was being aborted.
   at System.Threading.WaitHandle.WaitOneNative(SafeHandle waitableSafeHandle, UInt32 millisecondsTimeout, Boolean hasThreadAffinity, Boolean exitContext)
   at System.Threading.WaitHandle.InternalWaitOne(SafeHandle waitableSafeHandle, Int64 millisecondsTimeout, Boolean hasThreadAffinity, Boolean exitContext)
   at System.Threading.WaitHandle.WaitOne(Int32 millisecondsTimeout, Boolean exitContext)
   at System.Threading.WaitHandle.WaitOne()
   at scripthea.external.AsyncSocketListener.StartListening() in F:\Projects\Scripthea\external\SDiffusionUC.xaml.cs:line 351
*/