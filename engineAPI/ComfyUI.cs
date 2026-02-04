using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Web;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using scripthea.options;

namespace scripthea.engineAPI
{
    #region Network
    public static class NetworkUtils
    {
        /// <summary>Parses an <see cref="HttpResponseMessage"/> into a JSON object result.</summary>
        /// <exception cref="InvalidOperationException">Thrown when the server returns invalid data (error code or other non-JSON).</exception>
        /// <exception cref="NotImplementedException">Thrown when an invalid JSON type is requested.</exception>
        public static async Task<JType> Parse<JType>(HttpResponseMessage message) where JType : class
        {
            string content = await message.Content.ReadAsStringAsync();
            if (content.StartsWith("500 Internal Server Error"))
            {
                throw new InvalidOperationException($"Server turned 500 Internal Server Error, something went wrong: {content}");
            }
            try
            {
                Type type = typeof(JType);

                // Using if-else statements
                if (type == typeof(JObject))
                {
                    return JObject.Parse(content) as JType;
                }
                else if (type == typeof(JArray))
                {
                    return JArray.Parse(content) as JType;
                }
                else if (type == typeof(string))
                {
                    return content as JType;
                }
                else
                {
                    throw new NotImplementedException($"Invalid JSON type requested: {type}");
                }
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException($"Failed to read JSON '{content}' with message: {ex.Message}");
            }
        }
        public static async Task<JObject> PostJSONString(this HttpClient client, string route, string input, CancellationToken interrupt)
        {
            HttpContent hc = new StringContent(input, Encoding.UTF8, "application/json");
            byte[] data = await hc.ReadAsByteArrayAsync();

            // Write the data to a file
            //File.WriteAllBytes("bytes-out.json", data);

            return await Parse<JObject>(await client.PostAsync(route, hc, interrupt));
        }
        public static async Task<byte[]> ReceiveData(this WebSocket socket, int maxBytes, CancellationToken limit)
        {
            byte[] bbuffer = new byte[8192];
            ArraySegment<byte> abuffer = new ArraySegment<byte>(bbuffer);
            MemoryStream ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(abuffer, limit);
                ms.Write(abuffer.Array, 0, result.Count);
                if (ms.Length > maxBytes)
                {
                    throw new IOException($"Received too much data! (over {maxBytes} bytes)");
                }
            }
            while (!result.EndOfMessage);
            return ms.ToArray();
        }
    }
    #endregion NetworkUtils

    public class ComfyUIclass
    {
        public readonly string serverAddress;
        private HttpClient httpClient;
        public ServerMonitor serverMonitor;
        private Options opts;

        public ComfyUIclass(ref Options _opts)
        {
            opts = _opts;
            serverAddress = "127.0.0.1:8188";
            httpClient = new HttpClient();
        }
        public void Finish()
        {
            httpClient.Dispose();
        }
        public void log(string txt)
        {
            Console.WriteLine(txt);
        }

        public JObject ParseToJson(string input)
        {
            try
            {
                return JObject.Parse(input);
            }
            catch (JsonReaderException ex)
            {
                throw new JsonReaderException($"Failed to parse JSON `{input.Replace("\n", "  ")}`: {ex.Message}");
            }
        }
        public async Task<JType> SendGet<JType>(string url, CancellationToken token) where JType : class
        {
            HttpResponseMessage hrm = await httpClient.GetAsync(url, token);
            return await NetworkUtils.Parse<JType>(hrm);
        }
        private long seed(int n)
        {
            Random rnd = new Random();
            string ss = ""; int j;
            for (int i = 0; i < n; i++) ss += rnd.Next(1, 10);
            return Convert.ToInt64(ss);
        }
        /// <summary>Runs a job with live feedback (progress updates, previews, etc.)</summary>
        /// <param name="workflow">The workflow JSON to use.</param>
        /// <param name="batchId">Local batch-ID for this generation.</param>
        /// <param name="takeOutput">Takes an output object: Image for final images, JObject for anything else.</param>
        /// <param name="user_input">Original user input data.</param>
        /// <param name="interrupt">Interrupt token to use.</param>
        public async Task<JObject> AwaitJobLive(string workflow, string batchId, Action<object> takeOutput, CancellationToken interrupt)
        {
            if (interrupt.IsCancellationRequested || workflow == "Error: wrong parameter(s)")
            {
                return null;
            }
            log("Will await a job, do parse...");

            JObject workflowJson = ParseToJson(workflow);

            Dictionary<string, JObject> prompt = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(workflow); string KSampler = "";
            foreach (var item in prompt)
            {
                if (item.Value.ContainsKey("class_type"))
                    if ((string)item.Value["class_type"] == "KSampler") KSampler = item.Key;
            }
            JToken KSseed = null;
            if (KSampler != "")
            {
                try 
                { 
                    KSseed = workflowJson[KSampler]["inputs"]["seed"]; 
                    if (workflowJson.ContainsKey(KSampler))
                        if ((long)(KSseed) <= 0) 
                            workflowJson[KSampler]["inputs"]["seed"] = Convert.ToString(seed(15));
                }
                catch (Exception ex) { Console.WriteLine("Error in workflow json: " + KSampler); } 
            }

            log("JSON parsed.");
            int expectedNodes = workflowJson.Count;
            string id = Guid.NewGuid().ToString(); 
            ClientWebSocket socket = null;
            if (socket is null)
            {
                log("Need to connect a websocket...");
                id = Guid.NewGuid().ToString();
                socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri($"ws://{serverAddress}/ws?clientId={id}"), CancellationToken.None);
                log("Connected.");
            }
            int nodesDone = 0;
            float curPercent = 0;
            void yieldProgressUpdate(string txt = "")
            {
                if (txt != "") takeOutput(txt);
                else
                {
                    takeOutput(new JObject()
                    {
                        ["batch_index"] = batchId,
                        ["overall_percent"] = nodesDone / (float)expectedNodes,
                        ["current_percent"] = curPercent
                    });
                }
            }
            try
            {
                string wf = JsonConvert.SerializeObject(workflowJson);
                workflow = $"{{\"prompt\": {wf}, \"client_id\": \"{id}\"}}";

                JObject promptResult = await NetworkUtils.PostJSONString(httpClient, $"http://{serverAddress}/prompt", workflow, interrupt);

                if (promptResult.ContainsKey("error"))
                {
                    log($"Error came from prompt: {JObject.Parse(workflow).ToString()}");
                    throw new InvalidDataException($"ComfyUI errored: {promptResult}");
                }
                string promptId = $"{promptResult["prompt_id"]}";
                long firstStep = 0;
                bool hasInterrupted = false;
                bool isReceivingOutputs = false;
                bool isExpectingVideo = false;
                string currentNode = "";
                bool isMe = false;
                while (true)
                {
                    if (interrupt.IsCancellationRequested && !hasInterrupted && false)
                    {
                        hasInterrupted = true;
                        log("ComfyUI Interrupt requested");
                        await httpClient.PostAsync($"http://{serverAddress}/interrupt", new StringContent(""));
                    }
                    byte[] output = await NetworkUtils.ReceiveData(socket, 100 * 1024 * 1024, CancellationToken.None);
                    if (output != null)
                    {
                        string strOut = Encoding.ASCII.GetString(output);
                        if (Encoding.ASCII.GetString(output, 0, 8) == "{\"type\":")
                        {
                            JObject json = ParseToJson(Encoding.UTF8.GetString(output));
                            log($"ComfyUI Websocket {batchId} said (isMe={isMe}): {json.ToString(Formatting.None)}");
                            string type = $"{json["type"]}";
                            if (!isMe)
                            {
                                if (type == "execution_start")
                                {
                                    if ($"{json["data"]["prompt_id"]}" == promptId)
                                    {
                                        isMe = true;
                                    }
                                }
                                else continue;
                            }
                            switch (type)
                            {
                                case "executing":
                                    string nodeId = $"{json["data"]["node"]}";
                                    if (nodeId == "") // Not true null for some reason, so, ... this.
                                    {
                                        goto endloop;
                                    }
                                    currentNode = nodeId;
                                    goto case "execution_cached";
                                case "execution_cached":
                                    nodesDone++;
                                    curPercent = 0;
                                    hasInterrupted = false;
                                    yieldProgressUpdate();
                                    break;
                                case "progress":
                                    int max = json["data"].Value<int>("max");
                                    curPercent = json["data"].Value<float>("value") / max;
                                    isReceivingOutputs = max == 12345 || max == 12346;
                                    isExpectingVideo = max == 12346;
                                    yieldProgressUpdate();
                                    break;
                                case "executed":
                                    nodesDone = expectedNodes;
                                    curPercent = 0;
                                    yieldProgressUpdate();
                                    break;
                                case "execution_start":
                                    if (firstStep == 0)
                                    {
                                        firstStep = Environment.TickCount;
                                    }
                                    break;
                                case "status": // queuing
                                    break;
                                default:
                                    log($"Ignore type {json["type"]}");
                                    break;
                            }
                        }
                        else
                        {
                            yieldProgressUpdate("else...");
                        }
                    }
                    if (socket.CloseStatus.HasValue)
                    {
                        return null;
                    }
                }
            endloop:
                JObject historyOut = await SendGet<JObject>($"http://{serverAddress}/history/{promptId}", interrupt);
                //await ClearComfyCache();
                return historyOut;
            }
            catch (Exception e)
            {
                log("Error: " + e.Message); return null;
            }
            finally
            {
                socket.Dispose();
            }
            async Task<JObject> ClearComfyCache(bool unloadModels = true, bool freeMemory = true) //?
            {
                Dictionary<string, bool> dct = new Dictionary<string, bool>() { { "unloadModels", unloadModels }, { "freeMemory", freeMemory } };
                string ss = JsonConvert.SerializeObject(dct);
                JObject jObject = await NetworkUtils.PostJSONString(httpClient, $"http://{serverAddress}/free", ss, CancellationToken.None);
                return jObject;
            }
        }
    }
    
}
/*foreach (Image image in await GetAllImagesForHistory(, interrupt))
{
    if (Program.ServerSettings.AddDebugData)
    {
        user_input.ExtraMeta["debug_backend"] = new JObject()
        {
            ["backend_type"] = BackendData.BackType.Name,
            ["backend_id"] = BackendData.ID,
            ["debug_internal_prompt"] = user_input.Get(T2IParamTypes.Prompt),
            ["backend_usages"] = BackendData.Usages,
            ["comfy_output_history_prompt_id"] = promptId
        };
    }
    takeOutput(new T2IEngine.ImageOutput() { Img = image, IsReal = true, GenTimeMS = firstStep == 0 ? -1 : (Environment.TickCount64 - firstStep) }); "status": {\r\n      "status_str": "success",\r\n      "completed": true,
{{\r\n  "523e41ea-08b5-4d42-9e8f-5446ffd27422": {\r\n    "prompt": [\r\n      1,\r\n      "523e41ea-08b5-4d42-9e8f-5446ffd27422",\r\n      {\r\n        "3": {\r\n          "inputs": {\r\n            "seed": 789561352176156,\r\n            "steps": 21,\r\n            "cfg": 6.7,\r\n            "sampler_name": "dpmpp_3m_sde",\r\n            "scheduler": "karras",\r\n            "denoise": 1.0,\r\n            "model": [\r\n              "4",\r\n              0\r\n            ],\r\n            "positive": [\r\n              "6",\r\n              0\r\n            ],\r\n            "negative": [\r\n              "7",\r\n              0\r\n            ],\r\n            "latent_image": [\r\n              "5",\r\n              0\r\n            ]\r\n          },\r\n          "class_type": "KSampler",\r\n          "_meta": {\r\n            "title": "KSampler"\r\n          }\r\n        },\r\n        "4": {\r\n          "inputs": {\r\n            "ckpt_name": "sd_xl_base_1.0.safetensors"\r\n          },\r\n          "class_type": "CheckpointLoaderSimple",\r\n          "_meta": {\r\n            "title": "Load Checkpoint"\r\n          }\r\n        },\r\n        "5": {\r\n          "inputs": {\r\n            "width": 2000,\r\n            "height": 694,\r\n            "batch_size": 1\r\n          },\r\n          "class_type": "EmptyLatentImage",\r\n          "_meta": {\r\n            "title": "Empty Latent Image"\r\n          }\r\n        },\r\n        "6": {\r\n          "inputs": {\r\n            "text": "(beautiful woman:1.3) sitting on a desk in a nice restaurant with a (glass of wine and plate with salat:0.9), (candlelight dinner atmosphere:1.1), (wearing a red evening dress:1.2), dimmed lighting, cinema, high detail",\r\n            "clip": [\r\n              "4",\r\n              1\r\n            ]\r\n          },\r\n          "class_type": "CLIPTextEncode",\r\n          "_meta": {\r\n            "title": "CLIP Text Encode (Prompt)"\r\n          }\r\n        },\r\n        "7": {\r\n          "inputs": {\r\n            "text": "text, watermark",\r\n            "clip": [\r\n              "4",\r\n              1\r\n            ]\r\n          },\r\n          "class_type": "CLIPTextEncode",\r\n          "_meta": {\r\n            "title": "CLIP Text Encode (Prompt)"\r\n          }\r\n        },\r\n        "8": {\r\n          "inputs": {\r\n            "samples": [\r\n              "3",\r\n              0\r\n            ],\r\n            "vae": [\r\n              "4",\r\n              2\r\n            ]\r\n          },\r\n          "class_type": "VAEDecode",\r\n          "_meta": {\r\n            "title": "VAE Decode"\r\n          }\r\n        },\r\n        "9": {\r\n          "inputs": {\r\n            "filename_prefix": "ComfyUI",\r\n            "images": [\r\n              "8",\r\n              0\r\n            ]\r\n          },\r\n          "class_type": "SaveImage",\r\n          "_meta": {\r\n            "title": "Save Image"\r\n          }\r\n        }\r\n      },\r\n      {\r\n        "client_id": "1db76dc3-fa13-48df-a94f-36b21e90fca4"\r\n      },\r\n      [\r\n        "9"\r\n      ]\r\n    ],\r\n    "outputs": {\r\n      "9": {\r\n        "images": [\r\n          {\r\n            "filename": "ComfyUI_00158_.png",\r\n            "subfolder": "",\r\n            "type": "output"\r\n          }\r\n        ]\r\n      }\r\n    },\r\n    "status": {\r\n      "status_str": "success",\r\n      "completed": true,\r\n      "messages": [\r\n        [\r\n          "execution_start",\r\n          {\r\n            "prompt_id": "523e41ea-08b5-4d42-9e8f-5446ffd27422"\r\n          }\r\n        ],\r\n        [\r\n          "execution_cached",\r\n          {\r\n            "nodes": [\r\n              "6",\r\n              "7",\r\n              "4",\r\n              "5"\r\n            ],\r\n            "prompt_id": "523e41ea-08b5-4d42-9e8f-5446ffd27422"\r

}  */


