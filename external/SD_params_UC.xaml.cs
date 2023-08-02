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
using scripthea.viewer;
using UtilsNS;

namespace scripthea.external
{
    public static class SDside
    {
        // possible parameters
        public static readonly Dictionary<string, string> possParams = new Dictionary<string, string> // Value is obj.GetType().Name
        {   {"prompt", "String" }, {"negative_prompt", "String" }, {"seed", "Int64" }, {"width", "Int64" }, {"height", "Int64" },
            {"sampler_name", "String" }, {"cfg_scale", "Double" }, {"steps", "Int64" }, {"batch_size", "Int64" }, {"restore_faces", "Boolean" },
            {"sd_model_hash", "String" }, {"denoising_strength", "Int64" }, {"job_timestamp", "String" }
        };

        public static readonly List<string> Samplers = new List<string>
        {
            "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a","DPM++ 2Sa","DPM++ 2M", "DPM++ SDE", "DPM fast", "DPM adaptive",
            "LMS Karras", "DPM2 Karas", "DPM2 a Karas", "DPM++ 2Sa Karas","DPM++ 2M Karas", "DPM++ SDE Karas",
            "DDIM", "PLMS", "UniPC"
        };

        public static readonly List<string> curParams = new List<string>
        {
            "negative_prompt", "width", "height", "sampler_name", "restore_faces", "seed", "cfg_scale", "steps"
        };
    }
    public class SDsetting: Dictionary<string, object>
    {
        public SDsetting() { }
        public SDsetting(Dictionary<string, object> inDict) { GetFromDict(inDict); }
        public void GetFromDict(Dictionary<string, object> inDict) 
        {
            if (Utils.isNull(inDict)) return; 
            foreach (var pair in inDict) // validation
                if (SDside.possParams.ContainsKey(pair.Key)) this[pair.Key] = pair.Value;
        }
        public void GetFromImageInfo(ImageInfo ii)
        {
            if (Utils.isNull(ii)) return;
            this["negative_prompt"] = ii.negative_prompt;
            this["width"] = ii.width; this["height"] = ii.height;
            this["sampler_name"] = ii.sampler_name; this["restore_faces"] = ii.restore_faces; this["seed"] = ii.seed;
            this["cfg_scale"] = ii.cfg_scale; this["steps"] = ii.steps; 
        }
        public SDsetting Clone()
        {
            return new SDsetting(this);
        }
        public bool Compare2SDsetting(SDsetting sds)
        {
            foreach(string key in SDside.curParams)
            {
                if (!this.ContainsKey(key) || !sds.ContainsKey(key)) return false;
                if (this[key].ToString() != sds[key].ToString()) return false;
            }
            return true;
        }
    }
    public class SDlist: Dictionary<string,SDsetting> // change to ordered dictionary later
    {
        public string filename { get; private set; }
        public Dictionary<string,string> Header { get; private set; }
        public SDlist(string fn = "") 
        {
            filename = fn.Equals("") ? Path.Combine(Utils.configPath,"SD_params.cfg") : fn;
            List<string> ls = Utils.readList(filename, false);
            if (Utils.isNull(ls)) throw new Exception("file <" + filename + "> is missing");
            foreach (string ss in ls)
            {
                if (ss.StartsWith("#")) { Header = JsonConvert.DeserializeObject<Dictionary<string, string>>(ss.Substring(1)); continue; }
                string[] sa = ss.Split('='); if (!sa.Length.Equals(2)) throw new Exception("file <" + filename + "> wrong format");
                Add(sa[0], new SDsetting(JsonConvert.DeserializeObject<Dictionary<string, object>>(sa[1])));
            }
        }
        public void UpdateCombo(string LastSetting, ComboBox cb)
        {
            cb.Items.Clear(); cb.SelectedIndex = -1;
            foreach (var pair in this)
            {
                ComboBoxItem cbi = new ComboBoxItem() { Content = pair.Key };
                cb.Items.Add(cbi);
                if (LastSetting != null)
                    if (LastSetting.Equals(pair.Key)) cb.SelectedItem = cbi; 
            }
            if (cb.SelectedIndex == -1) cb.SelectedIndex = 0;
        }
        public void Save()
        {
            List<string> ls = new List<string>();
            if (Header == null) ls.Add("#{\"ImageGenerator\":\"StableDiffusion\",\"webui\":\"parameters\",\"application\":\"Scripthea 1.5.1.71\"}");
            else ls.Add("#" + JsonConvert.SerializeObject(Header));
            foreach (var pair in this)
                ls.Add(pair.Key + "=" + JsonConvert.SerializeObject(pair.Value));
            Utils.writeList(filename, ls);
        }        
    }
    /// <summary>
    /// Interaction logic for SD_API_UC.xaml
    /// </summary>
    public partial class SD_params_UC : UserControl
    {
        SDlist sdList;
        public SD_params_UC()
        {
            InitializeComponent();
        }
        private bool locked = true;
        protected Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            if (!locked) return; 
            nsWidth.Init("Width", 512, 64, 2048, 10); nsHeight.Init("Height", 512, 64, 2048, 10);
            foreach (string ss in SDside.Samplers)
                cbSampler.Items.Add(new ComboBoxItem(){ Content = ss });
            nsSamplingSteps.Init("Sampl.Steps", 20, 1, 150, 1);  nsCFGscale.Init("CFG Scale", 7, 1, 30, 1);
            
            sdList = new SDlist(); 
            sdList.UpdateCombo(opts.general.LastSDsetting, cbSettings); btnSetParams_Click(null, null);
            chkAutoRefresh.IsChecked = opts.general.AutoRefreshSDsetting;

            nsWidth.numBox.ValueChanged += new RoutedEventHandler(visual2prms); nsHeight.numBox.ValueChanged += new RoutedEventHandler(visual2prms);
            nsSamplingSteps.numBox.ValueChanged += new RoutedEventHandler(visual2prms); nsCFGscale.numBox.ValueChanged += new RoutedEventHandler(visual2prms);
            locked = false; visual2prms(null, null);
        }
        public void Finish()
        {
            if (chkAutoRefresh.IsChecked.Value) btnSetParams_Click(null, null);
            sdList.Save(); 
            opts.general.LastSDsetting = cbSettings.Text; opts.general.AutoRefreshSDsetting = chkAutoRefresh.IsChecked.Value;
        }
        private SDsetting _vPrms;
        public SDsetting vPrms
        {
            get 
            {
                return _vPrms;
            }
            set // prms2visual
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
        public void ImportImageInfo(string json, SolidColorBrush clr = null)
        {
            if (ActiveSetting == null) return;
            ActiveSetting.GetFromImageInfo(new ImageInfo(json)); 
            vPrms = ActiveSetting;
        }
        public SDsetting ActiveSetting // not to use while changing selected combo item
        { 
            get 
            {
                if (sdList.ContainsKey(cbSettings.Text)) return sdList[cbSettings.Text];
                else { Utils.TimedMessageBox("Unknown setting: " + cbSettings.Text); return null; }
            } 
        }
        protected void CheckDifference(SDsetting refSDs) // refSDs against the active SDsetting
        {
            if (Utils.isNull(refSDs)) return;
            grpSDsettings.Header = "SD parameters settings" + ((ActiveSetting.Compare2SDsetting(vPrms) || chkAutoRefresh.IsChecked.Value) ? "" : " *").ToString();
        }
        protected void visual2prms(object sender, RoutedEventArgs e) // live update
        {
            if (locked) return;
            _vPrms = new SDsetting();
            _vPrms["negative_prompt"] = tbNegativePrompt.Text; _vPrms["width"] = (long)nsWidth.Value; _vPrms["height"] = (long)nsHeight.Value;
            _vPrms["sampler_name"] = cbSampler.Text; _vPrms["restore_faces"] = chkRestoreFaces.IsChecked.Value; _vPrms["seed"] = Convert.ToInt64(tbSeed.Text);
            _vPrms["cfg_scale"] = (double)nsCFGscale.Value; _vPrms["steps"] = (long)nsSamplingSteps.Value;
            CheckDifference(vPrms);            
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
        private void btnSetParams_Click(object sender, RoutedEventArgs e) // set visuals from internal setting
        {
            SDsetting sds = ActiveSetting;
            vPrms = sds;           
        }
        private void btnGetParams_Click(object sender, RoutedEventArgs e)
        {
            ActiveSetting?.GetFromDict(vPrms); CheckDifference(vPrms);
        }
        private void cbSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!chkAutoRefresh.IsChecked.Value) return;
            if (sdList.ContainsKey(cbSettings.Text)) sdList[cbSettings.Text].GetFromDict(vPrms); // old one
            else Utils.TimedMessageBox("Unknown setting: " + cbSettings.Text);

            string newStr = "";
            if ((sender as ComboBox).SelectedItem == null) newStr = "";
            else newStr = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string; // new one
            if (sdList.ContainsKey(newStr)) vPrms = sdList[newStr];
            else Utils.TimedMessageBox("Unknown setting: " + newStr);
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value)
            {
                btnSetParams.Visibility = Visibility.Collapsed; btnGetParams.Visibility = Visibility.Collapsed;
                cbSettings.Width = 140+62; btnGetParams_Click(null, null); 
            }
            else
            {
                btnSetParams.Visibility = Visibility.Visible; btnGetParams.Visibility = Visibility.Visible;
                cbSettings.Width = 140;
            }
        }
        private void btnAddParams_Click(object sender, RoutedEventArgs e)
        {
            SDsetting sds = vPrms.Clone();
            if (!sdList.ContainsKey(cbSettings.Text)) { Utils.TimedMessageBox("Unknown setting: " + cbSettings.Text); return; }

            string newStg = new InputBox("Save current parameters in", cbSettings.Text, "").ShowDialog();
            if (newStg == "") return;
            sdList[newStg] = sds;
            if (newStg.Equals(cbSettings.Text))
            {
                if (chkAutoRefresh.IsChecked.Value) Utils.TimedMessageBox("same name: <" + newStg + "> - no action");
                else Utils.TimedMessageBox("<" + newStg + "> params setting updated"); 
            }
            else Utils.TimedMessageBox("<" + newStg + "> params setting added");
            bool ar = chkAutoRefresh.IsChecked.Value; chkAutoRefresh.IsChecked = false; // prevent cbSettings_SelectionChanged
            sdList.UpdateCombo(newStg, cbSettings); chkAutoRefresh.IsChecked = ar;
        }
        private void btnDelParams_Click(object sender, RoutedEventArgs e)
        {
            if (sdList.ContainsKey(cbSettings.Text)) sdList.Remove(cbSettings.Text);
            else { Utils.TimedMessageBox("Unknown setting: " + cbSettings.Text); return; }
            Utils.TimedMessageBox(cbSettings.Text + " params setting removed");

            bool ar = chkAutoRefresh.IsChecked.Value; chkAutoRefresh.IsChecked = false; // prevent cbSettings_SelectionChanged
            sdList.UpdateCombo(null, cbSettings); 
            if (ar) btnSetParams_Click(null, null); chkAutoRefresh.IsChecked = ar;
        }
    }
}
