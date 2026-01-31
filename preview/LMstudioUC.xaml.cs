using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using scripthea.options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
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
using Path = System.IO.Path;
using UtilsNS;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace scripthea.preview
{
    public class ChatCompletionResponse
    {
        public string id { get; set; }
        public string Object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public string SimpleReplay()
        {
            if (choices.Length == 0) return null;
            return choices[0].message.content;
        }
        public Usage usage { get; set; }

        public class Choice
        {
            public int index { get; set; }
            public Message message { get; set; }
            public string finish_reason { get; set; }
        }
        public class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
        }
        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }
    }
    public class OpenAIApi
    {
        public List<string> decomp2list(string txt, char separator = '\n') // the formating depends on the model, so not universal
        {
            List<string> ls = new List<string>();
            string tx = txt.Trim();
            if (tx.StartsWith("[") && tx.EndsWith("]")) 
                tx = tx.TrimStart('[').TrimEnd(']');
            if (!separator.Equals('\n')) tx = tx.Replace('\n', ' ');
            tx = tx.Replace('\"', ' ');
            string[] txa = tx.Split(separator);
            foreach (string ss in txa) ls.Add(ss.Trim());
            return ls;
        }
        public string similarityContext = "";
        public string connStatus { get; private set; }
        //private readonly HttpClient _httpClient;
        private readonly string strBaseAddress = "http://localhost:1234";
        private string _prefix = ""; // ### Instruction:\n";
        private string _suffix = ""; // \n### Response:";
        private readonly int ttl = 7200; 
        public OpenAIApi()
        {
        }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        public bool Muted { get; set; } = false; // future use
        public bool Connected { get; private set; } = false;
        public string localModel { get; private set; } = "local model";
        public string LoadedModel { get; private set; }        
        public void Disconnect() { Connected = false; }
        public void Finish()
        {
            
        }
        #region LM studio services
        public bool PingServer() // not working as such; use CheckConnection instead
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send(strBaseAddress, 3000); // 3-second timeout
                    return (reply.Status == IPStatus.Success);
                }
            }
            catch (Exception)
            {
                // If ping fails for any reason, consider the server unreachable
                return false;
            }
        }
        public async Task<List<string>> GetModelsAsync() // real call for models and confirming the connection
        {
            List<string> models = new List<string>();
            using (var httpClient = new HttpClient())
            {                
                try
                {
                    connStatus = "";
                    // Use a GET request to a known endpoint.
                    HttpResponseMessage response = await httpClient.GetAsync($"{strBaseAddress}/v1/models");
                    // response.EnsureSuccessStatusCode();

                    // You could check if the HTTP status code is successful (2xx).                   
                    if (response.IsSuccessStatusCode)
                    {
                        connStatus = "OK";
                        var json = await response.Content.ReadAsStringAsync();
                        var root = JObject.Parse(json);
                        // Response: { "data": [ { "id": "model-id", ... }, ... ] }
                        if (!root.ContainsKey("data")) throw new Exception("No data availble");
                        var data = (JArray)root["data"];
                        if (data is null)
                        {
                            Console.WriteLine("No models found in response.");
                            throw new Exception("No data");
                        }
                        foreach (var m in data)
                        {
                            var id = m?["id"]?.ToString();
                            if (!string.IsNullOrEmpty(id)) models.Add(id);
                        }
                    }
                    else { opts.Log($"Error: Connection failed. Status code: {response.StatusCode}"); return models; }
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception> {e.Message}"; opts.Log("Error: " + connStatus);
                    Connected = false; return models;
                }
                catch (Exception e)
                {
                    // Handle other possible exceptions.
                    connStatus = $"General exception> {e.Message}"; opts.Log("Error: " + connStatus);
                    Connected = false; return models;
                }
            }
            Connected = connStatus.Equals("OK") && models.Count > 0;
            return models;
        }
        public bool CheckConnection() // fake call for models
        {
            using (var httpClient = new HttpClient())
            {
                connStatus = "";
                try
                {
                    // Use a GET request to a known endpoint.
                    HttpResponseMessage response = httpClient.GetAsync($"{strBaseAddress}/v1/models")
                          .ConfigureAwait(false)
                          .GetAwaiter()
                          .GetResult();
                    // response.EnsureSuccessStatusCode();

                    // You could check if the HTTP status code is successful (2xx).                   
                    if (response.IsSuccessStatusCode) connStatus = "OK";
                    else connStatus = $"Connection failed. Status code: {response.StatusCode}";
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; //opts.Log("Error: " + connStatus);
                    Connected = false; return false;
                }
                catch (Exception e)
                {
                    // Handle other possible exceptions.
                    connStatus = $"General exception: {e.Message}"; //opts.Log("Error: " + connStatus);
                    Connected = false; return false;
                }
            }
            Connected = connStatus.Equals("OK");
            return Connected;
        }
        public async Task<bool> CheckConnectionAsync() // fake call for models
        {
            using (var httpClient = new HttpClient())
            {
                connStatus = "";
                try
                {
                    // Use a GET request to a known endpoint.
                    HttpResponseMessage response = await httpClient.GetAsync($"{strBaseAddress}/v1/models");
                    // response.EnsureSuccessStatusCode();

                    // You could check if the HTTP status code is successful (2xx).                   
                    if (response.IsSuccessStatusCode) connStatus = "OK";
                    else connStatus = $"Connection failed. Status code: {response.StatusCode}"; 
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; //opts.Log("Error: " + connStatus);
                    Connected = false; return false;
                }
                catch (Exception e)
                {
                    // Handle other possible exceptions.
                    connStatus = $"General exception: {e.Message}"; //opts.Log("Error: " + connStatus);
                    Connected = false; return false;
                }
            }
            Connected = connStatus.Equals("OK");
            return Connected;
        }
        public bool IsModelLoaded(string modelId)
        {
            LoadedModel = "";
            if (modelId.Equals("")) return false;
            using (var httpClient = new HttpClient())
            {
                var resp = httpClient.GetAsync($"{strBaseAddress}/api/v0/models/{modelId}")
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult(); 
                resp.EnsureSuccessStatusCode();
                string body = resp.Content.ReadAsStringAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                var json = JObject.Parse(body);
                if (json is null) return false; if (!json.ContainsKey("state")) return false;
                bool bb = string.Equals((string)json["state"], "loaded", StringComparison.OrdinalIgnoreCase);
                if (bb) LoadedModel = modelId;
                return bb;
            }
        }
        public async Task<bool> IsModelLoadedAsync(string modelId)
        {
            LoadedModel = "";
            if (modelId.Equals("")) return false;
            using (var httpClient = new HttpClient())
            {
                var resp = await httpClient.GetAsync($"{strBaseAddress}/api/v0/models/{modelId}"); //{Uri.EscapeDataString(
                resp.EnsureSuccessStatusCode();
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                if (json is null) return false; if (!json.ContainsKey("state")) return false;
                bool bb = string.Equals((string)json["state"], "loaded", StringComparison.OrdinalIgnoreCase);
                if (bb) LoadedModel = modelId;
                return bb;
            }
        }
        public bool LoadModel(string modelId)
        {
            LoadedModel = "";
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var payload = new JObject
                    {
                        ["model"] = modelId,   // your model id
                        ["ttl"] = ttl,                               // optional: unload after 5 min idle
                        ["messages"] = new JArray {
                            new JObject { ["role"]="user", ["content"]="ping" }
                        },
                        ["max_tokens"] = 1,                          // minimal work, just to trigger load
                        ["stream"] = false
                    };
                    _ = httpClient.PostAsync($"{strBaseAddress}/v1/chat/completions",
                        new StringContent(payload.ToString(), Encoding.UTF8, "application/json"));
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; return false;
                }
            }
            LoadedModel = modelId;
            return true;
        }
        public async Task<bool> LoadModelAsync(string modelId)
        {
            LoadedModel = "";
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var payload = new JObject
                    {
                        ["model"] = modelId,   // your model id
                        ["ttl"] = ttl,                               // optional: unload after 5 min idle
                        ["messages"] = new JArray {
                            new JObject { ["role"]="user", ["content"]="ping" }
                        },
                        ["max_tokens"] = 1,                          // minimal work, just to trigger load
                        ["stream"] = false
                    };
                    var resp = await httpClient.PostAsync($"{strBaseAddress}/v1/chat/completions",
                        new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    );
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; Connected = false; return false;
                }
            }
            LoadedModel = modelId;
            return true;
        }
        public class LmStudioModelsResponse
        {
            [JsonProperty("data")]
            public LmStudioModel[] Data { get; set; } = Array.Empty<LmStudioModel>();
        }
        public class LmStudioModel
        {
            [JsonProperty("id")]
            public string Id { get; set; } = "";

            // "vlm" (vision-language), "llm" (text), "embeddings", etc.
            [JsonProperty("type")]
            public string Type { get; set; } = "";

            // "loaded" / "not-loaded"
            [JsonProperty("state")]
            public string State { get; set; } = "";
        }
        public async Task<bool> IsLoadedModelVisionCapableAsync(string modelId)
        {
            using (var http = new HttpClient())
            {
                try
                {
                    var url = $"{strBaseAddress}/api/v0/models";
                    var json = await http.GetStringAsync(url);

                    var parsed = JsonConvert.DeserializeObject<LmStudioModelsResponse>(json)
                                 ?? new LmStudioModelsResponse();

                    var model = parsed.Data.FirstOrDefault(m =>
                        string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase));

                    if (model is null)
                        throw new InvalidOperationException($"Model '{modelId}' not found.");

                    // “loaded” ensures you’re checking the currently loaded model instance
                    var isLoaded = string.Equals(model.State, "loaded", StringComparison.OrdinalIgnoreCase);
                    var isVlm = string.Equals(model.Type, "vlm", StringComparison.OrdinalIgnoreCase);

                    return isLoaded && isVlm;
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; Connected = false; return false;
                }
            }
        }
        #endregion
        public async Task<string> SimilarityEvaluateAsync(string prompt, double _temperature = 0.0)
        {
            if (Muted) return "muted"; string rep = "";
            try
            {
                ChatCompletionResponse responseObject = await ComplexCompletionAsync(prompt, similarityContext, _temperature);
                rep = responseObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Connected = false; return null;
            }
            Connected = true; return rep;
        }        
        public async Task<string> SimpleCompletionAsync(string prompt, string context = "", double _temperature = 0.0, int _max_tokens = 30)
        {
            if (Muted) return "muted"; string rep = "";
            try
            {
                ChatCompletionResponse responseObject = await ComplexCompletionAsync(prompt, context, _temperature, _max_tokens);
                rep = responseObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Connected = false; return null;
            }
            Connected = true; return rep;
        }
        public async Task<ChatCompletionResponse> ComplexCompletionAsync(string prp, string context = "", double _temperature = 0.0, int _max_tokens = 30)
        {
            string contextPlus = "You are a helpful assistant. " + context.Trim();            
            string formattedPrompt = $"{_prefix}{prp}{_suffix}";
            //Console.WriteLine($"\nYour prompt: {prompt}\n");            
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var payload = new
                    {
                        model = localModel,
                        messages = new[]
                        {
                            new { role = "system", content = contextPlus },
                            new { role = "user", content = formattedPrompt }
                        },
                        temperature = _temperature,
                        max_tokens = _max_tokens
                    };

                    var response = await httpClient.PostAsync($"{strBaseAddress}/v1/chat/completions",
                        new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json")
                    );
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent);
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; Connected = false; return null;
                }
            }
        }
        public async Task<string> SimpleImageQueryAsync(string prompt, BitmapImage bitmap, string context = "", double _temperature = 0.0, int _max_tokens = 30)
        {
            if (Muted) return "Error: the engine is muted"; string rep = "";
            if (String.IsNullOrEmpty(prompt) || bitmap is null) return "Error: wrong parameters";
            List<BitmapImage> lbm = new List<BitmapImage>(); lbm.Add(bitmap);
            try
            {
                ChatCompletionResponse responseObject = await ImageQueryAsync(prompt, lbm, context, _temperature, _max_tokens);
                rep = responseObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Connected = false; return null;
            }
            Connected = true; return rep;
        }
        public async Task<string> MultiImageQueryAsync(string prompt, List<BitmapImage> bitmaps, string context = "", double _temperature = 0.0, int _max_tokens = 30)
        {
            if (Muted) return "Error: the engine is muted"; string rep = "";
            if (String.IsNullOrEmpty(prompt) || bitmaps.Count == 0) return "Error: wrong parameters";
            try
            {
                ChatCompletionResponse responseObject = await ImageQueryAsync(prompt, bitmaps, context, _temperature, _max_tokens);
                rep = responseObject.choices[0].message.content;
            }
            catch (Exception e)
            {
                Connected = false; return null;
            }
            Connected = true; return rep;
        }
        public static byte[] BitmapImageToBytes(BitmapImage bitmap)
        {
            if (bitmap is null)
                throw new ArgumentNullException(nameof(bitmap));

            // Pick encoder: PngBitmapEncoder, JpegBitmapEncoder, etc.
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();   // like File.ReadAllBytes(imagePath)
            }
        }
        public async Task<ChatCompletionResponse> ImageQueryAsync(string query, List<BitmapImage> bitmaps, string context = "", double _temperature = 0.0, int _max_tokens = 30)
        {
            string contextPlus = "You are a helpful assistant. " + opts.llm.LMScontext.Replace('\r', ' ');
            string formattedQuery = $"{_prefix}{query}{_suffix}";
            object[] contentExt = new object[bitmaps.Count + 1];
            contentExt[0] = new { type = "text", text = formattedQuery };
            for (int i = 0; i < bitmaps.Count; i++)
            {
                byte[] imageBytes = BitmapImageToBytes(bitmaps[i]);
                string base64image = Convert.ToBase64String(imageBytes);
                contentExt[i + 1] = new
                {
                    type = "image_url",
                    image_url = new
                    {
                        url = $"data:image/png;base64,{base64image}"
                        // change to image/png if needed
                    }
                };
            }
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var payload = new
                    {
                        model = localModel,
                        messages = new object[]
                        {
                            new { role = "system", content = contextPlus },
                            new
                            {
                                role = "user",
                                content = contentExt
                            }
                        },
                        temperature = _temperature,
                        max_tokens = _max_tokens
                    };
                    var response = await httpClient.PostAsync($"{strBaseAddress}/v1/chat/completions",
                        new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json")
                    );
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent);
                }
                catch (HttpRequestException e)
                {
                    // Handle any network-related error.
                    connStatus = $"Request exception: {e.Message}"; Connected = false; return null;
                }
            }
        }

    }
    /// <summary>
    /// Interaction logic for LMstudioUC.xaml
    /// </summary>
    public partial class LMstudioUC : UserControl
    {
        public LMstudioUC()
        {
            InitializeComponent();
        }
        protected Options opts; public OpenAIApi LMSclient;

        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
            LMSclient = new OpenAIApi(); LMSclient.Init(ref _opts);
            gbLMSLoc.Header = LocationHeader;
            tbLMScontext.Text = opts.llm.LMScontext;
        }
        public void Finish()
        {
            
        }
        public void UpdateVisuals()
        {
            tbLMSlocation.Text = opts.llm.LMSlocation; gbLMSmodel.Header = ModelHeader;
            if (Connected)
            {
                if (IsModelLoaded) gbLMSmodel.Header = ModelHeader + (IsModelLoaded ? "loaded" : "NOT loaded !") + "  "; 
                if (cbLMSmodels.Items.Count == 0)
                {
                    _ = ReadModels();
                }
            }
            cbLMSmodels.Text = opts.llm.LMSmodel;
            tbLMScontext.Text = opts.llm.LMScontext;
        }
        public bool Connected 
        {
            get
            {
                gbLMSLoc.Header = LocationHeader + (LMSclient.Connected ? "Connected <->" : "Disconnected -X-") + "  ";
                return LMSclient.Connected;
            }
            private set
            {
                if (value) LMSclient.CheckConnection();
                else LMSclient.Disconnect();
                gbLMSLoc.Header = LocationHeader + (LMSclient.Connected ? "Connected <->" : "Disconnected -X-") + "  ";
            } 
        }
        private string LocationHeader { get => "Location of LM Studio executable   Status:  "; }
        private string ModelHeader { get => "LM Studio model   Status:  "; }
        public bool IsModelLoaded 
        {
            get
            {
                if (LMSclient.LoadedModel is null) return false;                
                return LMSclient.IsModelLoaded(opts.llm.LMSmodel);
            }
        }
        public string CheclLMSerror() // return status when no proper reply
        {
            if (!LMSclient.CheckConnection()) { return "Error: connection to LM Studio is lost."; }
            if (!IsModelLoaded) { return "Error: LM Studio model is missing."; }
            return "Error: unspecified LM Studio problem.";
        }
        public bool IsReady(bool forced = false)
        {
            if (!forced) return Connected && IsModelLoaded;
            if (LMSclient.CheckConnection())
                LMSclient.IsModelLoaded(LMSclient.localModel);
            return Connected && IsModelLoaded;
        }
        public bool FullLaunch(bool incModel = true) // main LM studio launch
        {
            if (LMSclient is null || opts is null) return false;
            // launch           
            if (!LMSclient.CheckConnection())
            {
                opts.Log("Launching LM Studio...");
                if (!LaunchExe(opts.llm.LMSlocation)) { opts.Log("Error: unable to launch LM Studio. You may check the executable path and try again."); return false; }
                else { LMSclient.CheckConnection(); if (Connected) opts.Log("LM Studio is launched."); }
            }
            else if (Connected) opts.Log("LM Studio is running.");
            if (!incModel) return Connected;
            // model
            if (!LMSclient.IsModelLoaded(opts.llm.LMSmodel))
            {
                opts.Log("Loading LM Studio model: "+ opts.llm.LMSmodel);
                if (!LMSclient.LoadModel(opts.llm.LMSmodel)) 
                    { opts.Log("Error: unable to load LM Studio model: "+ opts.llm.LMSmodel); return false; };
            }  
            else opts.Log("Loaded LM Studio model: " + opts.llm.LMSmodel);
            return Connected && IsModelLoaded;
        }
        public async Task<bool> FullLaunchAsync(bool incModel = true, bool quiet = false) // main LM studio ASYNC launch
        {
            if (LMSclient is null || opts is null) return false;
            // launch            
            bool bb = await LMSclient.CheckConnectionAsync();
            if (!bb)
            {
                if (!quiet) { opts.Log("Launching LM Studio ..."); Utils.DoEvents(); }
                if (!LaunchExe(opts.llm.LMSlocation)) { opts.Log("Error: unable to launch LM Studio. You check exe path and may try again."); return false; }
                else
                {
                    await LMSclient.CheckConnectionAsync(); if (Connected && !quiet) opts.Log("> LM Studio is launched.");
                }
            }
            else if (Connected && !quiet) opts.Log("> LM Studio is launched.");
            if (!incModel) return Connected;
            // model
            bb = await LMSclient.IsModelLoadedAsync(opts.llm.LMSmodel);
            if (!bb)
            {
                if (!quiet) opts.Log("Loading LM Studio model: " + opts.llm.LMSmodel +" ...");
                bb = await LMSclient.LoadModelAsync(opts.llm.LMSmodel);
                if (!bb) { opts.Log("Error: unable to load LM Studio model: "+ opts.llm.LMSmodel); return false; }
                else if (!quiet) opts.Log("> "+opts.llm.LMSmodel + " model has been loaded");
            }
            else if (IsModelLoaded && !quiet) opts.Log("> " + opts.llm.LMSmodel + " model has been loaded");

            return Connected && IsModelLoaded;
        }
        private Process _lmStudioProcess = null;

        // low level stuff
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_SHOWMINNOACTIVE = 7;
        private const int SW_MINIMIZE = 6;

        public bool LaunchExe(string exe, string arguments = "--minimized --api --port 1234")
        {
            if (!File.Exists(exe)) return false;
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,              // <- inline parameters here
                WorkingDirectory = Path.GetDirectoryName(exe),
                UseShellExecute = true, // fine if you want normal window behavior
                WindowStyle = ProcessWindowStyle.Minimized
            };
            var proc = Process.Start(psi);
            if (proc is null) return false;
            for (int i = 0; i < 50; i++)       // ~5 seconds total @ 100ms
            {
                proc.Refresh();
                if (proc.HasExited) return false;

                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    // Try to minimize without activating
                    if (!ShowWindow(proc.MainWindowHandle, SW_SHOWMINNOACTIVE))
                    {
                        // Fallback
                        ShowWindow(proc.MainWindowHandle, SW_MINIMIZE);
                    }
                    break;
                }
                Thread.Sleep(100);
            }
            // Give it a moment to start
            Utils.Sleep(3000);
            proc.Refresh();
            // Even if this process exits (launcher pattern), we still keep the path
            psi.WindowStyle = ProcessWindowStyle.Minimized;
            _lmStudioProcess = proc;
            return true;
        }
        public bool CloseLmStudio()
        {
            bool bb = TryCloseLmStudio();
            if (bb)
            {
                gbLMSLoc.Header = "Location of LM Studio "; gbLMSmodel.Header = "LM Studio model "; 
                LMSclient.Disconnect(); _lmStudioProcess = null;
            }
            else opts.Log("Error: unable to close LM Studio from Scripthea. If you need to close LM Studio, do it from inside the app.");
            return bb;
        }
        public bool TryCloseLmStudio()
        {
            // If we still have a tracked process, try that first
            if (_lmStudioProcess is { HasExited: false } p)
            {
                // Ask the app to close nicely
                if (p.CloseMainWindow())
                {
                    if (p.WaitForExit(5000)) // wait up to 5s
                        return true;
                }
                // If it didn't exit, force kill
                try
                {
                    p.Kill(); //entireProcessTree: true
                    return p.WaitForExit(5000);
                }
                catch
                {
                    // fall through to name-based kill
                }
            }
            // Fallback: kill by process name (if launcher exited and you lost the handle)
            // Replace "LM Studio" with the actual process name from Task Manager, e.g. "LM Studio"
            foreach (var proc in Process.GetProcessesByName("LM Studio"))
            {
                if (!proc.HasExited)
                {
                    try
                    {
                        if (proc.CloseMainWindow())
                        {
                            if (proc.WaitForExit(5000))
                                continue;
                        }

                        proc.Kill(); //entireProcessTree: true
                        proc.WaitForExit(5000);
                    }
                    catch
                    {
                        // ignore failures per process
                    }
                }
            }
            return true; // you can tighten this to check if any are still running
        }
        public void LmStudioCloseMonitor(ref Process lmProcess) 
        {
            const string processName = "LM Studio";
            // Try to find a running LM Studio process
            var lmProcesses = Process.GetProcessesByName(processName);
            lmProcess = null;
            if (lmProcesses.Length == 0)
            {
                Console.WriteLine("LM Studio is not running.");
                return;
            }
            // Just monitor the first one found (or loop if you want all)
            lmProcess = lmProcesses.First();
            lmProcess.EnableRaisingEvents = true;
            Console.WriteLine($"Monitoring LM Studio (PID={lmProcess.Id}).");
        }
        #region local control
        private void btnLMSlocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (opts.llm.LMSlocation is null) opts.llm.LMSlocation = "";
            string folder = opts.llm.LMSlocation.Equals("") ? "" : Path.GetDirectoryName(opts.llm.LMSlocation);
            if (Directory.Exists(folder)) dialog.InitialDirectory = folder;
            dialog.Title = "Select LM Studio executable (e.g. LM Studio.exe)";
            dialog.IsFolderPicker = false; dialog.Multiselect = false;
            dialog.DefaultExtension = ".exe";
            dialog.Filters.Add(new CommonFileDialogFilter("Executable", "exe"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbLMSlocation.Text = dialog.FileName;                
            }            
            Focus();         // important
        }
        private void tbLMSlocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            VisualHelper.SetButtonEnabled(btnLMSlaunch, File.Exists(tbLMSlocation.Text.Trim()));            
        }
        private async Task<bool> ReadModels()
        {
            List<string> models = await LMSclient.GetModelsAsync();
            cbLMSmodels.Items.Clear();
            if (models.Count == 0) return false;
            int k = -1;
            foreach(string mdl in models)
            {
                int i = cbLMSmodels.Items.Add(mdl);
                if (string.Equals(mdl, opts.llm.LMSmodel, StringComparison.OrdinalIgnoreCase)) k = i;
            }
            if (k > -1) cbLMSmodels.SelectedIndex = k;
            return true;
        }
        private async void btnLMSlaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!btnLMSlaunch.IsEnabled) return;      
            if (!LMSclient.CheckConnection())
                Connected = LaunchExe(tbLMSlocation.Text.Trim());
            if (!Connected) { opts.Log("Error: unable to launch LM Studio. You may try again."); return; }
            // read models
            await ReadModels();
        }
        private async void btnLMSmodel_load_Click(object sender, RoutedEventArgs e)
        {            
            if (cbLMSmodels.Text == "") { opts.Log("Error: no model has been selected"); return; }
            gbLMSmodel.Header = ModelHeader+"...loading...  ";
            bool bb = await LMSclient.LoadModelAsync(cbLMSmodels.Text);
            gbLMSmodel.Header = ModelHeader + (IsModelLoaded ? "loaded" : "NOT loaded !") + "  ";                         
            if (!IsModelLoaded) opts.Log("Error: model not loaded"); return; }
        private void tbLMScontext_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (opts is null) return;            
        }
        private async void btnLMStest_Click(object sender, RoutedEventArgs e) //
        {            
            string prt = tbLMStest.Text;
            string rep = await LMSclient.SimpleCompletionAsync(prt);
            if (rep is null) { tbReply.Text = "Error: server problem"; return; }
            tbReply.Text = rep.Trim();
        }
        public void LmProcess_Exited()
        {
            opts.Log("LM Studio has closed!");            
        }
        #endregion
    }
}
