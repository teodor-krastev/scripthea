﻿using System;
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
        PyTcpListener server; int pCount = 0; NVidia nVidia;
        public SDiffusionUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
            server = new PyTcpListener();
            server.OnLog += new Utils.LogHandler(Log);
            server.OnReceive += new Utils.LogHandler(Receive);
            nVidia = new NVidia();
        }
        SDoptionsWindow SDopts;
        public Dictionary<string, string> opts { get; set; } // main (non this API specific) options 
        public void Init(string prompt) // init and update visuals from opts
        {
            lbStatus.Content = "COMM: closed"; server.Init();
            if (!nVidia.IsAvailable()) gridTmp.Visibility = Visibility.Collapsed;
            SDopts = new SDoptionsWindow(); 
        }
        public void Finish() 
        {
            if (Utils.isNull(server)) return;
            if (!server.status.Equals(PyTcpListener.Status.closed)) server.CloseSession();
            SDopts.keepOpen = false; SDopts.Close();
        }
        public bool isDocked { get { return true; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get ; } // connected and working (depends on the API)
        private void SimulatorImage(string filepath)
        {
            string imageSimulFolder = Utils.basePath + "\\images\\Simulator\\";
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageSimulFolder, "c*.png"));
            if (orgFiles.Count.Equals(0)) throw new Exception("Wrong simulator image folder ->" + imageSimulFolder);
            Random rnd = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
            string fn = orgFiles[rnd.Next(orgFiles.Count - 1)];
            File.Copy(fn, filepath);
        } 
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename) // returns the filename of saved/copied in ImageDepoFolder image 
        {
            filename = Utils.timeName();
            opts["folder"] = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\";
            server.SendFields(prompt, opts["folder"], filename);

            if (server == null) return false;

            //string data = server.GetFromClient();
            //string fn = System.IO.Path.ChangeExtension(filename,".sdj");
            /* add some custom fields
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            // add to dict HERE 
            data = JsonConvert.SerializeObject(dict);
             */
            //File.WriteAllText(System.IO.Path.Combine(opts["folder"], fn), data);            
            return true;// !data.Equals("");
        }
        protected void Log(String txt, SolidColorBrush clr = null)
        {
            if (txt.Length.Equals("")) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() =>
              {
                  if (txt.Substring(0, 1).Equals("@")) { lbStatus.Content = "COMM: "+txt.Substring(1); lbStatus.UpdateLayout(); return;  } 
                  tbAdvice.Text = "Log: " + txt + "\r"; tbAdvice.UpdateLayout();
              }));
        }
        protected void Receive(String txt, SolidColorBrush clr = null)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() =>
              {
                  tbAdvice.Text += "Rcv: "+txt+"\r"; tbAdvice.UpdateLayout();
              }));
        }
 
        private void chkSession_Checked(object sender, RoutedEventArgs e)
        {
            lbStatus.Content = "COMM: waiting"; lbStatus.UpdateLayout(); 
            tbAdvice.Text = "Start python client in SD-webUI\r"; tbAdvice.UpdateLayout(); Utils.DoEvents();
            
           
            server.OpenSession(); 
        }
        private void chkSession_Unchecked(object sender, RoutedEventArgs e)
        {
            server.CloseSession();
        }

        private void btnShoot_Click(object sender, RoutedEventArgs e)
        {
            pCount++;
            server.ToBeSent = "Little fairy town ; ink drawing ->"+Utils.randomString(5);
            if (server.status == PyTcpListener.Status.closed) server.OpenSession();
            else server.listen = true;
            //server.SendFields("Little fairy town ; ink drawing", Utils.basePath + "\\images\\", "imageName" );
            //tbAdvice.Text = server.GetFromClient()+"\r";
        }

        private void lb1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender.Equals(lb1)) Utils.AskTheWeb("Stable Diffusion");
            if (sender.Equals(lb2)) Utils.CallTheWeb("https://stability.ai/");
            if (sender.Equals(lb3)) Utils.CallTheWeb("http://127.0.0.1:7860/");
        }

        int currentTmp = -1; double averTmp = -1; int maxTmp = -1;
        List<int> tmpStack; int stackDepth = 15; 
        DispatcherTimer dTimer; 
        private void chkTemp_Checked(object sender, RoutedEventArgs e)
        {
            if (chkTemp.IsChecked.Value)
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
            else dTimer.Stop();
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (!nVidia.IsAvailable()) return;
            currentTmp = nVidia.GetGPUtemperature();
            chkTemp.Content = "GPU temp[°C] = " + currentTmp.ToString();
            tmpStack.Add(currentTmp);
            while (tmpStack.Count > stackDepth) tmpStack.RemoveAt(0);
            averTmp = tmpStack.ToArray().Average();
            maxTmp = -1;
            foreach (int t in tmpStack)
                maxTmp = Math.Max(t, maxTmp);
            lbTmpInfo.Content = "aver: " + averTmp.ToString("G3") + "  max: "+maxTmp.ToString();
        }
        int tmpThreshold = 60; // 0 -> not avail.
        private void tbThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(tbThreshold.Text, out tmpThreshold))
            {
                if (Utils.InRange(tmpThreshold, 35, 85)) tbThreshold.Foreground = Brushes.Black; // ALLOWED RANGE !
                else
                {
                    tbThreshold.Foreground = Brushes.Red; tmpThreshold = 0;
                }
            }               
            else tbThreshold.Foreground = Brushes.Red;
        }
        private void ibtnOpts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SDopts.ShowDialog();
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

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    //ASYNC https://csharp.hotexamples.com/examples/-/TcpClient/ConnectAsync/php-tcpclient-connectasync-method-examples.html
    public class PyTcpListener
    {
        public PyTcpListener()
        {
            status = Status.closed;
            
            dTimer = new DispatcherTimer();
            dTimer.Tick += new EventHandler(dTimer_Tick);
            dTimer.Interval = new TimeSpan(2000 * 10000); // 2 [sec]
        }
        protected DispatcherTimer dTimer;
        private TcpListener server = null;
        public enum Status { closed, waiting, connected, imageReceived, promptSent }
        private Status _status;
        public void Init()
        {
            OpenServer();
        }
        public Status status
        {
            get
            {
                if (server == null) _status = Status.closed;
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
        } 
        //private TcpClient client; 
        public void OpenServer(Int32 port = 5344)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1"); // not used

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(IPAddress.Any, port);
                status = Status.closed;
                //Console.WriteLine("Waiting for a connection... "); 
            }
            catch (SocketException e)
            {
                Log(String.Format("Error: SocketException: {0}", e));
            }
        }
        TcpClient tcpClient;
        public void OpenSession()
        {
            try
            {
                // Start listening for client requests.               
                server.Start();                
                dTimer.Start(); // start polling
                status = Status.waiting;                
            }
            catch (SocketException e)
            {
                Log(String.Format("Error: SocketException: {0}", e));
            }
        }
        public string ToBeSent { get; set; }
        public bool listen { get { return dTimer.IsEnabled; } set { dTimer.IsEnabled = value; } }
        
        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (Utils.isNull(server)) return;
            if (!server.Pending())
            {
                Log("Waiting to connect");
            }
            else
            {
                //Accept the pending client connection and return a TcpClient object initialized for communication.
                tcpClient = server.AcceptTcpClient();

                // Using the RemoteEndPoint property.
                // Console.WriteLine("I am listening for connections on " +
                //    IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
                //    "on port number " + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());
                
                if (!Utils.isNull(tcpClient)) 
                {
                    // Get a stream object for reading and writing
                    NetworkStream stream = tcpClient.GetStream();
                    string dt = GetFromClient(stream);
                    if (dt.Equals("@next.prompt"))
                    {
                        if (!ToBeSent.Equals(""))
                        {
                            dTimer.Stop();
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(ToBeSent);
                            stream.Write(msg, 0, msg.Length);
                        }    
                        status = Status.connected; 
                    }
                    if (dt.Equals("@image.ready"))
                    {
                        status = Status.imageReceived; 
                    }                    
                    else Log("Err: rly:" + dt + " out:" + ToBeSent);
                }
                // Close the tcpListener and tcpClient instances
                // tcpClient.Close(); 
                
            }
        }
        private string GetFromClient(NetworkStream stream, string filepath = "") // write received text if not empty
        {
            try
            {           
                Byte[] bytes = new Byte[4096];
                // Loop to receive all the data sent by the client.
                //while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                int i = stream.Read(bytes, 0, bytes.Length);                                      
                // Translate data bytes to a ASCII string.
                String data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                //if (data.Equals("@ping"))  if (!SendToClient("@pong")) { status = Status.closed; Console.WriteLine("Broken COMM !");  return ""; }
                status = Status.imageReceived;
                if (filepath.Equals("")) Console.WriteLine("Received: {0}", data);
                else File.WriteAllText(filepath, data);
                Receive(data);
                return data;
            }
            catch (SocketException e)
            {
                Log(String.Format("Error: SocketException: {0}", e)); return "";
            }
        }
        public bool SendFields(string prompt, string folder, string filename, int sampler_index = -1, int seed = -2)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("prompt", prompt); 
            dict.Add("folder", folder); // with trailing '\'
            dict.Add("filename", filename); // no ext
            dict.Add("sampler", sampler_index); // 0 based index; -1 - from UI
            dict.Add("seed", seed); // -2 - from UI
            return SendDict(dict);
        }
        public bool SendDict(Dictionary<string, object> dict)
        {
            return SendToClient(JsonConvert.SerializeObject(dict));
        }
        public bool SendToClient(string txt)
        {
            try
            {
                byte[] msg = System.Text.Encoding.ASCII.GetBytes(txt);
                // Send back a response to the client
                //stream.Write(msg, 0, msg.Length);

                TcpClient tcpClient = server.AcceptTcpClient();

                // Using the RemoteEndPoint property.
                // Console.WriteLine("I am listening for connections on " +
                //    IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
                //    "on port number " + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());

                if (!Utils.isNull(tcpClient))
                {
                    // Get a stream object for reading and writing
                    NetworkStream stream = tcpClient.GetStream();
                    stream.Write(msg, 0, msg.Length);
                }
                //Close the tcpListener and tcpClient instances
                tcpClient.Close();

                status = Status.promptSent; //Console.WriteLine("Sent: {0}", txt);
            }
            catch { return false; }
            return true;
        }
        /*public bool CheckCOMM()
        {            
            if (!SendToClient("@ping")) return false;
            return GetFromClient().Equals("@pong");
        }*/
        public void CloseSession() // Shutdown and end the connection
        {
            SendToClient("@close.session"); server.Stop(); status = Status.closed;
        }
    }
}
/* JSON massage both ways is dictionary based on StableDiffusionProcessing
 * 
 * from ..\stable-diffusion-webui\modules\processing.py
 
class StableDiffusionProcessing :
    def __init__(self, sd_model=None, outpath_samples=None, outpath_grids=None, prompt="", styles=None, seed=-1, subseed=-1, subseed_strength=0, seed_resize_from_h=-1, 
seed_resize_from_w=-1, seed_enable_extras=True, sampler_index=0, batch_size=1, n_iter=1, steps=50, cfg_scale=7.0, width=512, height=512, restore_faces=False, 
tiling=False, do_not_save_samples=False, do_not_save_grid=False, extra_generation_params=None, overlay_images=None, negative_prompt=None, eta=None):

        self.sd_model = sd_model
        self.outpath_samples: str = outpath_samples
        self.outpath_grids: str = outpath_grids
        self.prompt: str = prompt
        self.prompt_for_display: str = None
        self.negative_prompt: str = (negative_prompt or "")
        self.styles: list = styles or[]
        self.seed: int = seed
        self.subseed: int = subseed
        self.subseed_strength: float = subseed_strength
        self.seed_resize_from_h: int = seed_resize_from_h
        self.seed_resize_from_w: int = seed_resize_from_w
        self.sampler_index: int = sampler_index
        self.batch_size: int = batch_size
        self.n_iter: int = n_iter
        self.steps: int = steps
        self.cfg_scale: float = cfg_scale
        self.width: int = width
        self.height: int = height
        self.restore_faces: bool = restore_faces
        self.tiling: bool = tiling
        self.do_not_save_samples: bool = do_not_save_samples
        self.do_not_save_grid: bool = do_not_save_grid
        self.extra_generation_params: dict = extra_generation_params or { }
self.overlay_images = overlay_images
        self.eta = eta
        self.paste_to = None
        self.color_corrections = None
        self.denoising_strength: float = 0
        self.sampler_noise_scheduler_override = None
        self.ddim_discretize = opts.ddim_discretize
        self.s_churn = opts.s_churn
        self.s_tmin = opts.s_tmin
        self.s_tmax = float('inf')  # not representable as a standard ui option
        self.s_noise = opts.s_noise

        if not seed_enable_extras:
    self.subseed = -1
            self.subseed_strength = 0
            self.seed_resize_from_h = 0
            self.seed_resize_from_w = 0

    def init(self, all_prompts, all_seeds, all_subseeds):
        pass

    def sample(self, conditioning, unconditional_conditioning, seeds, subseeds, subseed_strength):
        raise NotImplementedError()
*/