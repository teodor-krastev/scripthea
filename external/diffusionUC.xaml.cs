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
using System.Net.Sockets;
using Newtonsoft.Json;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for diffusionUC.xaml
    /// </summary>
    public partial class diffusionUC : UserControl, interfaceAPI
    {
        PyTcpListener server; int pCount = 0;
        public diffusionUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
            server = new PyTcpListener();
        }
        public Dictionary<string, string> opts { get; set; } // visual adjustable options to that particular API, keep it in synchro with the visuals 
        public void Init(string prompt) // init and update visuals from opts
        {
            server.OnLog += new PyTcpListener.LogHandler(Log);
            server.OnReceive += new PyTcpListener.LogHandler(Receive);
            lbStatus.Content = "COMM: closed"; server.Init(); 
        }
        public void Finish() 
        {
            if (Utils.isNull(server)) return;
            if (!server.status.Equals(PyTcpListener.Status.closed)) server.CloseSession(); 
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
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename) // returns the filename of saved in ImageDepoFolder image 
        {
            filename = Utils.timeName();
            opts["folder"] = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\";
            server.SendFields(prompt, opts["folder"], filename);
            string data = server.GetFromClient();
            string fn = System.IO.Path.ChangeExtension(filename,".sdj");
            /* add some custom fields
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            // add to dict HERE 
            data = JsonConvert.SerializeObject(dict);
             */
            File.WriteAllText(System.IO.Path.Combine(opts["folder"], fn), data);            
            return !data.Equals("");
        }
        protected void Log(String txt)
        {
            if (txt.Length.Equals("")) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() =>
              {
                  if (txt.Substring(0, 1).Equals("@")) { lbStatus.Content = "COMM: "+txt.Substring(1); lbStatus.UpdateLayout(); return;  } 
                  tbAdvice.Text = "Log: " + txt + "\r"; tbAdvice.UpdateLayout();
              }));
        }
        protected void Receive(String txt)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() =>
              {
                  tbAdvice.Text += "Received: "+txt+"\r"; tbAdvice.UpdateLayout();
              }));
        }
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            lbStatus.Content = "COMM: waiting"; lbStatus.UpdateLayout(); 
            tbAdvice.Text = "Start python client in SD webUI\r"; tbAdvice.UpdateLayout(); Utils.DoEvents();
            server.OpenSession(); 
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            server.CloseSession();
        }
        private void btnShoot_Click(object sender, RoutedEventArgs e)
        {
            pCount++;
            server.SendFields("Little fairy town ; ink drawing", Utils.basePath + "\\images\\", "imageName" );
            tbAdvice.Text = server.GetFromClient()+"\r";
        }

        private void lb1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender.Equals(lb1)) Utils.AskTheWeb("Stable Diffusion");
            if (sender.Equals(lb2)) Utils.CallTheWeb("https://stability.ai/");
            if (sender.Equals(lb3)) Utils.CallTheWeb("http://127.0.0.1:7860/");
        }
    }

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    public class PyTcpListener
    {
        public PyTcpListener()
        {
            
        }
        private TcpListener server = null;
        public enum Status { closed, waiting, connected, imageReceived, promptSent }
        private Status _status;
        public void Init()
        {
            status = Status.closed;
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

        public delegate void LogHandler(string txt);
        public event LogHandler OnLog;
        protected void Log(string txt)
        {
            if (OnLog != null) OnLog(txt);
        }
        public event LogHandler OnReceive;
        protected void Receive(String txt)
        {
            if (OnReceive != null) OnReceive(Convert.ToString(txt));
        } 
        private TcpClient client; NetworkStream stream;
        public void OpenSession(Int32 port = 5344)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1"); // not used

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(IPAddress.Any, port);

                status = Status.waiting; Console.WriteLine("Waiting for a connection... "); 

                // Start listening for client requests.               
                server.Start();

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                client = server.AcceptTcpClient();

                // Get a stream object for reading and writing
                stream = client.GetStream();
                
                if (GetFromClient().Equals("@start"))
                {
                    status = Status.connected; Console.WriteLine("Connected!");
                }                                   
            }
            catch (SocketException e)
            {
                Log(String.Format("Error: SocketException: {0}", e));
            }
        }
        public string GetFromClient(string filepath = "")
        {
            try
            {           
                Byte[] bytes = new Byte[4096];
                // Loop to receive all the data sent by the client.
                //while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                int i = stream.Read(bytes, 0, bytes.Length);                                      
                // Translate data bytes to a ASCII string.
                String data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                if (data.Equals("@ping"))
                    if (!SendToClient("@pong")) { status = Status.closed; Console.WriteLine("Broken COMM !");  return ""; }
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
                stream.Write(msg, 0, msg.Length);
                status = Status.promptSent; //Console.WriteLine("Sent: {0}", txt);
            }
            catch { return false; }
            return true;
        }
        public bool CheckCOMM()
        {            
            if (!SendToClient("@ping")) return false;
            return GetFromClient().Equals("@pong");
        }
        public void CloseSession() // Shutdown and end the connection
        {
            SendToClient("@end"); client.Close(); server.Stop(); server = null;
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