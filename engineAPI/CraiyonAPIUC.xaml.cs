using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Net.Http;
using Newtonsoft.Json;

namespace scripthea.engineAPI
{
    /// <summary>
    /// Interaction logic for CraiyonAPIUC.xaml
    /// </summary>
    public partial class CraiyonAPIUC : UserControl
    {
        public CraiyonAPIUC()
        {
            InitializeComponent();
        }
        public class GeneratedImages
        {
            public List<string> Images { get; set; }
            public string Description { get; set; }
            public string Model { get; set; }
        }
        
        public class Craiyon
        {
            private readonly string baseUrl = "https://api.craiyon.com";
            private readonly string drawApiEndpoint = "/v3";
            private readonly string modelVersion;
            private readonly string apiToken;
            private static readonly HttpClient client = new HttpClient();

            public Craiyon(string apiToken = null, string modelVersion = "c4ue22fb7kb6wlac")
            {
                this.modelVersion = modelVersion;
                this.apiToken = apiToken;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36 Edg/120.0.0.0");
                client.DefaultRequestHeaders.Referrer = new Uri("https://www.craiyon.com");
            }

            public async Task<GeneratedImages> GenerateAsync(string prompt, string negativePrompt = "", string modelType = "none")
            {
                var url = baseUrl + drawApiEndpoint;
                var requestBody = new
                {
                    prompt,
                    negative_prompt = negativePrompt,
                    model = modelType,
                    token = this.apiToken,
                    version = this.modelVersion
                };
                var json = JsonConvert.SerializeObject(requestBody);
                var response = await client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                var images = new List<string>();

                foreach (var item in (Newtonsoft.Json.Linq.JArray)result["images"])
                {
                    images.Add($"https://img.craiyon.com/{item.ToString()}");
                }

                return new GeneratedImages
                {
                    Images = images,
                    Description = result["next_prompt"].ToString(),
                    Model = "v3"
                };
            }
        }

    }
}

