using System;
using System.Collections.Generic;
using System.IO;
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
using Newtonsoft.Json;
using Path = System.IO.Path;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for SD_API_UC.xaml
    /// </summary>
    public partial class SD_params_UC : UserControl
    {
        /*public readonly Dictionary<string, string> possParams = new Dictionary<string, string> // Value is obj.GetType().Name
        {   {"prompt", "String" }, {"negative_prompt", "String" }, {"seed", "Int64" }, {"width", "Int64" }, {"height", "Int64" },
            {"sampler_name", "String" }, {"cfg_scale", "Double" }, {"steps", "Int64" }, {"batch_size", "Int64" }, {"restore_faces", "Boolean" },
            {"sd_model_hash", "String" }, {"denoising_strength", "Int64" }, {"job_timestamp", "String" }
        };*/

        public static readonly List<string> Samplers = new List<string>
        {
            "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a","DPM++ 2Sa","DPM++ 2M", "DPM++ SDE", "DPM fast", "DPM adaptive",
            "LMS Karras", "DPM2 Karas", "DPM2 a Karas", "DPM++ 2Sa Karas","DPM++ 2M Karas", "DPM++ SDE Karas",
            "DDIM", "PLMS", "UniPC"
        };        

        string sd_params_file = "";
        public SD_params_UC()
        {
            InitializeComponent();
        }
        private bool locked = true; 
        public void Init()
        {
            if (!locked) return;
            nsWidth.Init("Width", 512, 64, 2048, 10); nsHeight.Init("Height", 512, 64, 2048, 10);
            foreach (string ss in Samplers)
                cbSampler.Items.Add(new ComboBoxItem(){ Content = ss });
            nsSamplingSteps.Init("Sampl.Steps", 20, 1, 150, 1);  nsCFGscale.Init("CFG Scale", 7, 1, 30, 1);
            sd_params_file = Path.Combine(Utils.configPath, "SD_params.cfg");
            if (File.Exists(sd_params_file)) sdp = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(sd_params_file));

            nsWidth.numBox.ValueChanged += new RoutedEventHandler(visual2prms); nsHeight.numBox.ValueChanged += new RoutedEventHandler(visual2prms);
            nsSamplingSteps.numBox.ValueChanged += new RoutedEventHandler(visual2prms); nsCFGscale.numBox.ValueChanged += new RoutedEventHandler(visual2prms);
            locked = false; visual2prms(null, null);
        }
        public void Finish()
        {
            File.WriteAllText(sd_params_file, JsonConvert.SerializeObject(sdp));
        }

        private Dictionary<string, object> prms;
        public Dictionary<string, object> sdp
        {
            get 
            {
                return prms;
            }
            set
            {
                if (value == null) return;
                if (value.ContainsKey("negative_prompt")) tbNegativePrompt.Text = Convert.ToString(value["negative_prompt"]);
                if (value.ContainsKey("width")) nsWidth.Value = Convert.ToInt32(value["width"]);
                if (value.ContainsKey("height")) nsHeight.Value = Convert.ToInt32(value["height"]);
                if (value.ContainsKey("sampler_name")) cbSampler.Text = Convert.ToString(value["sampler_name"]);
                if (value.ContainsKey("restore_faces")) chkRestoreFaces.IsChecked = Convert.ToBoolean(value["restore_faces"]);
                if (value.ContainsKey("seed")) tbSeed.Text = Convert.ToString(Convert.ToInt64(value["seed"]));
                if (value.ContainsKey("cfg_scale")) nsCFGscale.Value = Convert.ToInt32(value["cfg_scale"]);
                if (value.ContainsKey("steps")) nsSamplingSteps.Value = Convert.ToInt32(value["steps"]);
            }
        }
        protected void visual2prms(object sender, RoutedEventArgs e)
        {
            if (locked) return;
            prms = new Dictionary<string, object>();
            prms["negative_prompt"] = tbNegativePrompt.Text;
            prms["width"] = (long)nsWidth.Value; prms["height"] = (long)nsHeight.Value;
            prms["sampler_name"] = cbSampler.Text; prms["restore_faces"] = chkRestoreFaces.IsChecked.Value; prms["seed"] = Convert.ToInt64(tbSeed.Text);
            prms["cfg_scale"] = (double)nsCFGscale.Value; prms["steps"] = (long)nsSamplingSteps.Value;
        }
        private void tbNegativePrompt_TextChanged(object sender, TextChangedEventArgs e) // & seed
        {
            visual2prms(sender, e);
        }
        private void cbSampler_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            visual2prms(sender, e);
        }
        private void chkRestoreFaces_Checked(object sender, RoutedEventArgs e)
        {
            visual2prms(sender, e);
        }
    }
}
