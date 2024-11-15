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
using scripthea.options;
using Newtonsoft.Json.Linq;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for SD_API_UC.xaml
    /// </summary>
    public partial class SD_API_UC : UserControl
    {
        public string url 
        {
            get { return opts.composer.A1111 ? "http://127.0.0.1:7860" : "http://127.0.0.1:8188"; }
        }
        private HttpClient client;
        public SDoptionsWindow SDopts; ServerMonitor serverMonitor;
        private Options opts;
        public SD_API_UC()
        {
            InitializeComponent();
            client = new HttpClient();
        }       
        public void Init(ref SDoptionsWindow _SDopts, ref Options _opts ) // init and update visuals from opts
        {
            opts = _opts;  SDopts = _SDopts;
            lbServer.Content = opts.composer.A1111 ? "A1111" : "ComfyUI";
            if (_SDopts == null) _SDopts = new SDoptionsWindow(opts.composer.A1111); 
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
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
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
            {"model", "String" }, {"sd_model_hash", "String" }, {"denoising_strength", "Double" }, {"job_timestamp", "String" }
        };
        public bool GenerateImage(string imgFile, Dictionary<string, object> inParams, out Dictionary<string, object> outParams) // prompt is in inParams
        {
            string info = ""; outParams = null; long seed = 0;
            if (!Directory.Exists(Path.GetDirectoryName(imgFile))) { return false; }
            if (!CheckObjDict(inParams, possParams)) { return false; }
            bool rslt = false;
            Task task = Task.Run(async () =>
            {
                if (opts.composer.A1111)
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
                            ImgUtils.SaveBitmapImageToDisk((BitmapImage)ImgUtils.MemoryStream2BitmapSource(ms), imgFile);
                        }
                    }
                }
                else // ComfyUI
                {
                    ComfyUIclass clue = new ComfyUIclass(); 
                    Dictionary<string, object> cParams = SDside.Translate2Comfy(inParams);
                    string wf;
                    //wf = cUtils.Workflow("workflow_defaults.json", cParams);
                    wf = cUtils.actualWorkflow(opts.general.ComfyTemplate, cParams);
                    string wf_dir = Path.Combine(Utils.configPath, "workflows");
                    if (Directory.Exists(wf_dir)) File.WriteAllText(Path.Combine(wf_dir, Path.ChangeExtension(opts.general.ComfyTemplate, ".json")), wf);
                    rslt = cUtils.IsValidJson(wf);
                    if (rslt)
                    {
                        JObject historyOut = await clue.AwaitJobLive(wf, "0", output =>
                        { },  System.Threading.CancellationToken.None);

                        if (historyOut != null)
                        {
                            (string promptId, string filename, long seedOut, string subfolder, bool success) = cUtils.DeconOut(historyOut);

                            if (success)
                            {
                                if (!File.Exists(SDopts.opts.SDlocComfy)) Log("Error: ComfyUI bat file <" + SDopts.opts.SDlocComfy + "> is not found");
                                string folder = Path.Combine(Path.GetDirectoryName(SDopts.opts.SDlocComfy), "ComfyUI", "output");
                                if (!Directory.Exists(folder)) Log("Error: image output folder <" + folder + "> is missing");
                                string imagePath = Path.Combine(folder, filename);
                                if (File.Exists(imagePath)) File.Move(imagePath, imgFile);
                                else Log("Error: image file <" + imagePath + "> not found");
                                seed = seedOut;
                            }
                            rslt = success;
                        }
                        else rslt = false;
                     }
                    else Utils.TimedMessageBox("Error: wrong json format of the template.");
                }
            });
            try
            {
                task.Wait();
            }
            catch (Exception e) { Log("Error[9378]: processing problem! " + e.Message); return false; }
            if (rslt)
            {            
                if (opts.composer.A1111)
                {
                    if (!info.Equals(""))
                    {
                        outParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(info);
                        outParams["filename"] = Path.GetFileName(imgFile);
                        outParams["MD5Checksum"] = Utils.GetMD5Checksum(imgFile);
                    }
                }
                else //comfyUI
                {
                    outParams = new Dictionary<string, object>(inParams);
                    outParams["filename"] = Path.GetFileName(imgFile);
                    outParams["MD5Checksum"] = Utils.GetMD5Checksum(imgFile);
                    outParams["seed"] = seed;
                }
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
                string tp = pair.Value.GetType().Name;
                if (tp != possibles[pair.Key]) 
                    { return false; }
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
    public static class cUtils // comfyUI utils
    {
        public static bool IsValidJson(string jsonString)
        {
            try
            {
                JToken.Parse(jsonString); // This will throw an exception if the JSON is invalid.
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
        public enum prmKind { positive, negative, width, height, seed, steps, cfg, sampler_name, scheduler, model, denoise }
        public static Dictionary<prmKind, Type> prmTypes = new Dictionary<prmKind, Type>()
            { { prmKind.positive, typeof(string) }, { prmKind.negative, typeof(string) }, { prmKind.width, typeof(int) }, { prmKind.height, typeof(int) }, {prmKind.seed, typeof(long) }, {prmKind.steps, typeof(int) }, {prmKind.cfg, typeof(double) }, {prmKind.sampler_name, typeof(string) }, {prmKind.scheduler, typeof(string) },  {prmKind.model, typeof(string) }, {prmKind.denoise, typeof(double) }  };
        public static bool SetPrm(JObject workflow, prmKind kind, object val) // returns the new workflow
        {
            try
            {
                string KSampler = "";
                foreach (var item in workflow)
                {
                    if (item.Value["class_type"] != null)
                        if ((string)item.Value["class_type"] == "KSampler") KSampler = item.Key;
                }
                workflow[KSampler]["inputs"][kind.ToString()] = (dynamic)Convert.ChangeType(val, prmTypes[kind]);
                return true;
            }
            catch (System.NullReferenceException e) { return false; }
        }
        public static string Workflow(string template, Dictionary<string, object> prms) // return workflow, or start with Error:... if problem
        {
            if (prms.Count == 0) { Utils.TimedMessageBox("Error: no params to set"); return ""; };
            string defaults = File.ReadAllText(Path.Combine(Utils.configPath, template));
            try
            {
                JObject workflow = JObject.Parse(defaults);
                foreach (var pr in prms)
                {
                    switch (pr.Key)
                    {
                        case "positive":
                            if (workflow["6"]["class_type"].ToString() != "CLIPTextEncode") throw new System.NullReferenceException();
                            workflow["6"]["inputs"]["text"] = Convert.ToString(pr.Value);
                            break;
                        case "negative":
                            if (workflow["7"]["class_type"].ToString() != "CLIPTextEncode") throw new System.NullReferenceException();
                            workflow["7"]["inputs"]["text"] = Convert.ToString(pr.Value);
                            break;
                        case "model":
                        case "ckpt_name":
                            if (workflow["4"]["class_type"].ToString() != "CheckpointLoaderSimple") throw new System.NullReferenceException();
                            string mdl = (string)pr.Value;
                            if (!string.IsNullOrEmpty(mdl) && mdl != "<default>")
                                workflow["4"]["inputs"]["ckpt_name"] = Convert.ToString(pr.Value);
                            break;
                        case "width":
                        case "height":
                        case "batch_size":
                            if (workflow["5"]["class_type"].ToString() != "EmptyLatentImage") throw new System.NullReferenceException();
                            workflow["5"]["inputs"][pr.Key] = Convert.ToInt32(pr.Value);
                            break;                                                   
                        default:
                            bool success = Enum.TryParse(pr.Key, false, out prmKind resultType);
                            if (!success) return "Error: wrong parameter -> " + pr.Key;
                            if (!SetPrm(workflow, resultType, pr.Value)) throw new System.NullReferenceException();
                            break;
                    }
                }
                //string wf = actualWorkflow(template, prms);
                return JsonConvert.SerializeObject(workflow);
            }
            catch (System.NullReferenceException e) { return "Error[3641]: wrong parameter(s)"; }
        }
        public static string actualWorkflow(string template, Dictionary<string, object> cParams)
        {
            try
            {
                string tmpl = Path.Combine(Utils.configPath, template);
                if (!File.Exists(tmpl)) tmpl = Path.Combine(Utils.configPath, "workflow_default.cftm");
                if (!File.Exists(tmpl)) { Console.WriteLine("Error: no file <" + tmpl + ">"); return ""; }
                List<string> wf = actualWorkflowList(new List<string>(File.ReadAllLines(tmpl)), cParams);
                if (wf == null) { Console.WriteLine("Error: in workflow template"); return ""; }
                //Utils.writeList(Path.ChangeExtension(tmpl, ".json"), wf);
                return Convert.ToString(String.Join("\n", wf.ToArray())); //File.ReadAllText(Path.Combine(Utils.configPath,Path.ChangeExtension(template, ".json"))); 
            }
            catch (System.NullReferenceException e) { return "Error[3642]: wrong parameter(s)"; }
        }

        public static List<string> actualWorkflowList(List<string> workflow, Dictionary<string, object> fcts)
        {
            List<string> ls = new List<string>();
            foreach (string line in workflow)
            {
                string ss = Utils.skimRem(line);
                foreach (var fct in fcts)
                {
                    string fctName = '$' + fct.Key + '$';
                    if (line.IndexOf(fctName) == -1) continue;
                    string st = (fct.Value is string) ? '\"' + Convert.ToString(fct.Value) + '\"' : Convert.ToString(fct.Value);
                    ss = ss.Replace(fctName, st);
                }
                if (ss.IndexOf('$') > -1)
                {
                    Utils.TimedMessageBox("Missing parameter in " + line, "Error", 3500); return null;
                }
                ls.Add(ss);
            }
            return ls;
        }
        public static bool GetMetadataComfy(string imageFilePath, out Dictionary<string, string> itemMap)
        {
            itemMap = new Dictionary<string, string>();
            if (!Path.GetExtension(imageFilePath).ToLower().Equals(".png")) return false;
            var query = "/tEXt/{str=prompt}";
            try
            {
                using (Stream fileStream = File.Open(imageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    BitmapMetadata bitmapMetadata = decoder.Frames[0].Metadata as BitmapMetadata;
                    if (bitmapMetadata == null) return false;
                    var metadata = bitmapMetadata.GetQuery(query);
                    string md = metadata?.ToString(); if (md == null) { return false; }
                    Dictionary<string, object> dct = ExtractParams(md);
                    itemMap = Utils.dictObject2String(dct);
                }
            }
            catch (Exception e) { Utils.TimedMessageBox("Error (I/O): " + e.Message, "Error message", 3000); return false; }
            return true;
        }
        public static Dictionary<string, object> ExtractParams(string workflow_str)
        {
            Dictionary<string, object> dct = new Dictionary<string, object>();
            try
            {
                JObject workflow = JObject.Parse(workflow_str);
                foreach (var pr in prmTypes)
                {
                    switch (pr.Key)
                    {
                        case prmKind.positive:
                            if (workflow["6"]["class_type"].ToString() != "CLIPTextEncode") throw new System.NullReferenceException();
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["6"]["inputs"]["text"], pr.Value)); 
                            break;
                        case prmKind.negative:
                            if (workflow["7"]["class_type"].ToString() != "CLIPTextEncode") throw new System.NullReferenceException();
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["7"]["inputs"]["text"], pr.Value));
                            break;
                        case prmKind.model:
                            if (workflow["4"]["class_type"].ToString() != "CheckpointLoaderSimple") throw new System.NullReferenceException();
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["4"]["inputs"]["ckpt_name"], pr.Value));                            
                            break;
                        case prmKind.seed:
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["3"]["inputs"][pr.Key.ToString()], pr.Value));
                            break;
                        case prmKind.width:
                        case prmKind.height:                        
                            if (workflow["5"]["class_type"].ToString() != "EmptyLatentImage") throw new System.NullReferenceException();
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["5"]["inputs"][pr.Key.ToString()], pr.Value));
                            break;
                        default:
                            dct.Add(pr.Key.ToString(), Convert.ChangeType(workflow["3"]["inputs"][pr.Key.ToString()], pr.Value));                            
                            break;
                    }
                }
                return dct;
            }
            catch (System.NullReferenceException e) { return null; }
        }

        public static (string promptId, string filename, long seed, string subfolder, bool success) DeconOut(JObject historyOut)
        {
            try // assuming certain history structure !!!
            {
                if (historyOut == null) throw new Exception();
                if (historyOut.Count == 0) throw new Exception();
                string promptId = historyOut.Properties().First().Name;

                JToken pis = historyOut[promptId]["status"];
                bool success = (string)pis["status_str"] == "success" && (bool)pis["completed"];
                if (!historyOut.ContainsKey(promptId)) throw new Exception();
                if (historyOut[promptId]["outputs"] == null) throw new Exception();

                JToken piseed = null;
                try { piseed = historyOut[promptId]["prompt"][2]["3"]["inputs"]["seed"]; }
                catch(Exception ex) { }
                long seed = piseed != null ? (long)piseed: 0;
                
                JToken pid = historyOut[promptId]["outputs"].First.First;
                if (pid["images"] == null) throw new Exception();
                JToken pif = pid["images"][0];

                string filename = (string)pif["filename"];
                string subfolder = (string)pif["subfolder"]; // ?

                return (promptId, filename, seed, subfolder, success);
            }
            catch (System.NullReferenceException e)
            {
                return ("", "",0, "", false);
            }
        }
        // after JObject historyOut = await clue.SendGet<JObject>($"http://{clue.serverAddress}/history", CancellationToken.None);
        public static Dictionary<string, object> ExtractLastUsedPrms(JObject historyOut)
        {
            Dictionary<string, object> prms = new Dictionary<string, object>();

            return prms;
        }
    }
}

/*
 {
  "3": {
    "inputs": {
      "seed": 980499591786151,
      "steps": 21,
      "cfg": 6.7,
      "sampler_name": "dpmpp_3m_sde",
      "scheduler": "karras",
      "denoise": 1,
      "model": [
        "4",
        0],
      "positive": [
        "6",
        0],
      "negative": [
        "7",
        0],
      "latent_image": [
        "5",
        0]
    },
    "class_type": "KSampler",
    "_meta": {
      "title": "KSampler"
    }
  },
  "4": {
    "inputs": {
      "ckpt_name": "sd_xl_base_1.0.safetensors"
    },
    "class_type": "CheckpointLoaderSimple",
    "_meta": {
      "title": "Load Checkpoint"
    }
  },
  "5": {
    "inputs": {
      "width": 2000,
      "height": 694,
      "batch_size": 1
    },
    "class_type": "EmptyLatentImage",
    "_meta": {
      "title": "Empty Latent Image"
    }
  },
  "6": {
    "inputs": {
      "text": "beautiful scenery nature glass bottle landscape, , purple galaxy bottle,",
      "clip": [
        "4",
        1]
    },
    "class_type": "CLIPTextEncode",
    "_meta": {
      "title": "CLIP Text Encode (Prompt)"
    }
  },
  "7": {
    "inputs": {
      "text": "text, watermark",
      "clip": [
        "4",
        1]
    },
    "class_type": "CLIPTextEncode",
    "_meta": {
      "title": "CLIP Text Encode (Prompt)"
    }
  },
  "8": {
    "inputs": {
      "samples": [
        "3",
        0],
      "vae": [
        "4",
        2]
    },
    "class_type": "VAEDecode",
    "_meta": {
      "title": "VAE Decode"
    }
  },
  "9": {
    "inputs": {
      "filename_prefix": "ComfyUI",
      "images": [
        "8",
        0]
    },
    "class_type": "SaveImage",
    "_meta": {
      "title": "Save Image"
    }
  }
}
*/


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


