using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using System.Net.Http;
using Newtonsoft.Json;
using Path = System.IO.Path;
using UtilsNS;
using System.Text;
using scripthea.viewer;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for SD_API_UC.xaml
    /// </summary>
    public partial class SD_API_UC : UserControl
    {
        public string url { get; private set; }
        private HttpClient client;
        public SDoptionsWindow SDopts; ServerMonitor serverMonitor;
        public SD_API_UC()
        {
            InitializeComponent();
            url = "http://127.0.0.1:7860"; client = new HttpClient();
        }
        public void Init(ref SDoptionsWindow _SDopts) // init and update visuals from opts
        {            
            if (_SDopts == null) _SDopts = new SDoptionsWindow();
            SDopts = _SDopts;
            if (serverMonitor == null)
            {
                serverMonitor = new ServerMonitor(url); 
                serverMonitor.ChangeAwakeness -= serverAwakeness; serverMonitor.ChangeAwakeness += serverAwakeness;
                serverMonitor.StartMonitoring();
            }
        }
        public void Finish()
        {
            serverMonitor = null; GC.Collect(); GC.WaitForPendingFinalizers();
        }
        public event EventHandler ActiveEvent;
        protected virtual void OnActiveEvent()
        {
            // Check if there are any subscribers
            ActiveEvent?.Invoke(this, EventArgs.Empty);
        }
        private bool _active;
        public bool active 
        {
            get { return _active; } 
            set 
            { 
                _active = value;
                if (value) { lbStatus.Content = "API: active"; lbStatus.Foreground = Brushes.Green; }
                else { lbStatus.Content = "API: not active"; lbStatus.Foreground = Brushes.DarkRed; }
                OnActiveEvent();
            }
        }
        public void serverAwakeness()
        {
            if (!Dispatcher.CheckAccess())
            {
                // If not, we need to invoke this method on the UI thread
                Dispatcher.Invoke(new Action(serverAwakeness), new object[] { });
            }
            else
            {
                // If we're on the UI thread, update the UI directly
                if (!Utils.isNull(serverMonitor))
                    active = (bool)serverMonitor.isServerAwake;
            }
        }

        //"infotexts": ["puppy dog\nSteps: 5, Sampler: Euler, CFG scale: 7.0, Seed: 1492195626, Size: 512x512, Model hash: e1441589a6, Model: v1-5-pruned, Seed resize from: -1x-1, Denoising strength: 0"]
        public readonly Dictionary<string, string> possParams = new Dictionary<string, string> // Value is obj.GetType().Name
        {   {"prompt", "String" }, {"negative_prompt", "String" }, {"seed", "Int64" }, {"width", "Int64" }, {"height", "Int64" },
            {"sampler_name", "String" }, {"cfg_scale", "Double" }, {"steps", "Int64" }, {"batch_size", "Int64" }, {"restore_faces", "Boolean" },
            {"sd_model_hash", "String" }, {"denoising_strength", "Int64" }, {"job_timestamp", "String" }
        };
        public bool GenerateImage(string imgFile, Dictionary<string, object> inParams, out Dictionary<string, object> outParams) // prompt is in inParams
        {
            string info = ""; outParams = null;
            if (!Directory.Exists(Path.GetDirectoryName(imgFile))) { return false; }
            if (!CheckObjDict(inParams, possParams)) { return false; }
            bool rslt = false;
            Task task = Task.Run(async () =>
            {
                var content = new StringContent(JsonConvert.SerializeObject(inParams), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{url}/sdapi/v1/txt2img", content);
                rslt = response.IsSuccessStatusCode;
                if (rslt)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(result);
                    info = data.info.Value;

                    string base64Image = data.images[0].Value.ToString();
                    byte[] imageBytes = Convert.FromBase64String(base64Image);

                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        System.Drawing.Image imageSrc = System.Drawing.Image.FromStream(ms);
                        imageSrc.Save(imgFile);
                    }
                }
            });
            task.Wait();
            if (!info.Equals("") && rslt)
            {
                outParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(info);
                outParams["filename"] = Path.GetFileName(imgFile);
            }
            return rslt;
        }
        public Dictionary<string, object> options
        {
            get
            {
                string info = "";
                Task task = Task.Run(async () =>
                {
                    var response = await client.GetAsync($"{url}/sdapi/v1/options");
                    if (response.IsSuccessStatusCode)
                    {
                        info = await response.Content.ReadAsStringAsync();
                    }
                });
                task.Wait();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(info);
            }
            set
            {
                Task task = Task.Run(async () =>
                {
                    var content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{url}/sdapi/v1/options", content);
                });
                task.Wait();
            }
        }
        public Dictionary<string, string> ObjDict2StrDict(Dictionary<string, object> dop)
        {
            Dictionary<string, string> sod = new Dictionary<string, string>();
            foreach (var pair in dop)
            {
                object obj = pair.Value;
                if (obj == null) continue;
                if ((obj is int) || (obj is long) || (obj is double) || (obj is string) || (obj is bool))
                    sod.Add(pair.Key, obj.ToString()); // obj.GetType().Name
            }
            return sod;
        }
        public bool CheckObjDict(Dictionary<string, object> dop, Dictionary<string, string> possibles)
        {
            foreach (var pair in dop)
            {
                if (!possibles.ContainsKey(pair.Key)) { return false; }
                if (pair.Value.GetType().Name != possibles[pair.Key]) { return false; }
            }
            return true;
        }
        public void SetParamsInImageInfo(Dictionary<string, object> prms, ref ImageInfo ii)
        {

        }

    }

    public class ServerMonitor
    {
        private readonly HttpClient _client;
        private readonly string _url;
        public bool? isServerAwake { get; private set; }

        public event Action ChangeAwakeness;

        public ServerMonitor(string url)
        {
            _client = new HttpClient();
            _url = url;
            isServerAwake = null;
        }

        public void StartMonitoring()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    bool? prevSrvAwake = isServerAwake;
                    try
                    {
                        var response = await _client.GetAsync(_url);
                        isServerAwake = response.IsSuccessStatusCode;
                    }
                    catch (Exception)
                    {
                        isServerAwake = false;
                    }
                    if (prevSrvAwake != isServerAwake) ChangeAwakeness?.Invoke();
                    // Sleep for 5 seconds before the next check.
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });
        }
    }
}

/*===========================================================================================================
prompt=String
all_prompts = JArray
negative_prompt=String
all_negative_prompts = JArray
seed=Int64
all_seeds = JArray
subseed=Int64
all_subseeds = JArray
subseed_strength=Int64
width = Int64
height=Int64
sampler_name = String
cfg_scale=Double
steps = Int64
batch_size=Int64
restore_faces = Boolean
sd_model_hash=String
seed_resize_from_w = Int64
seed_resize_from_h=Int64
denoising_strength = Int64
extra_generation_params=JObject
index_of_first_image = Int64
infotexts=JArray
styles = JArray
job_timestamp=String
clip_skip = Int64
is_using_inpainting_conditioning=Boolean
---------------------------------------------------------------------
{
  "enable_hr": false,
  "denoising_strength": 0,
  "firstphase_width": 0,
  "firstphase_height": 0,
  "hr_scale": 2,
  "hr_upscaler": "string",
  "hr_second_pass_steps": 0,
  "hr_resize_x": 0,
  "hr_resize_y": 0,
  "prompt": "",
  "styles": [
    "string"
  ],
  "seed": -1,
  "subseed": -1,
  "subseed_strength": 0,
  "seed_resize_from_h": -1,
  "seed_resize_from_w": -1,
  "sampler_name": "string",
  "batch_size": 1,
  "n_iter": 1,
  "steps": 50,
  "cfg_scale": 7,
  "width": 512,
  "height": 512,
  "restore_faces": false,
  "tiling": false,
  "do_not_save_samples": false,
  "do_not_save_grid": false,
  "negative_prompt": "string",
  "eta": 0,
  "s_churn": 0,
  "s_tmax": 0,
  "s_tmin": 0,
  "s_noise": 1,
  "override_settings": {},
  "override_settings_restore_afterwards": true,
  "script_args": [],
  "sampler_index": "Euler",
  "script_name": "string",
  "send_images": true,
  "save_images": false,
  "alwayson_scripts": {}
}

 */


