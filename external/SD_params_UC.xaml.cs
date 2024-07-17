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
using scripthea.options;
using UtilsNS;

namespace scripthea.external
{
    public static class SDside
    {
        public static bool A1111 = true;
        // possible parameters for A1111 and internally
        public static readonly Dictionary<string, string> possParams = new Dictionary<string, string> // Value is obj.GetType().Name
        {   {"prompt", "String" }, {"negative_prompt", "String" }, {"seed", "Int64" }, {"width", "Int64" }, {"height", "Int64" },
            {"sampler_name", "String" }, {"cfg_scale", "Double" }, {"steps", "Int64" }, {"batch_size", "Int64" }, {"restore_faces", "Boolean" },
            {"sd_model_hash", "String" }, {"denoising_strength", "Double" }, {"job_timestamp", "String" }
        };

        public static readonly List<string> a1111Samplers = new List<string>
        {
            "Euler a", "Euler", "LMS", "Heun", "DPM2", "DPM2 a","DPM++ 2Sa","DPM++ 2M", "DPM++ SDE", "DPM fast", "DPM adaptive",
            "LMS Karras", "DPM2 Karas", "DPM2 a Karas", "DPM++ 2Sa Karas","DPM++ 2M Karas", "DPM++ SDE Karas",
            "DDIM", "PLMS", "UniPC"
        };
        // in ComfyUI py code: "euler", "euler_cfg_pp", "euler_ancestral", "euler_ancestral_cfg_pp", "heun", "heunpp2","dpm_2", "dpm_2_ancestral",
        //    "lms", "dpm_fast", "dpm_adaptive", "dpmpp_2s_ancestral", "dpmpp_sde", "dpmpp_sde_gpu",
        //    "dpmpp_2m", "dpmpp_2m_sde", "dpmpp_2m_sde_gpu", "dpmpp_3m_sde", "dpmpp_3m_sde_gpu", "ddpm", "lcm",
        //    "ipndm", "ipndm_v", "deis" 
        public static readonly List<string> comfySamplers = new List<string>
        {
            "euler", "euler_ancestral", "heun", "heunpp2","dpm_2", "dpm_2_ancestral",
            "lms", "dpm_fast", "dpm_adaptive", "dpmpp_2s_ancestral", "dpmpp_sde", "dpmpp_sde_gpu",
            "dpmpp_2m", "dpmpp_2m_sde", "dpmpp_2m_sde_gpu", "dpmpp_3m_sde", "dpmpp_3m_sde_gpu", "ddpm", "lcm",
            "ddim", "uni_pc", "uni_pc_bh2"
        };
        public static List<string> Samplers { get { return A1111 ? a1111Samplers : comfySamplers; } }
        public static readonly List<(int, string, string)> SamplersMatch = new List<(int, string, string)>
        {            
            (1, "Euler a", "euler_ancestral"),
            (2, "Euler", "euler"),
            (3, "LMS", "lms"),
            (4, "Heun", "heun"),
            (5, "DPM2", "dpm_2"),
            (6, "DPM2 a", "dpm_2_ancestral"),
            (7, "DPM fast", "dpm_fast"),
            (8, "DPM adaptive","dpm_adaptive"),
            (9, "DPM++ 2Sa","dpmpp_2s_ancestral"),
            (10, "DPM++ SDE","dpmpp_sde"),
            (11, "DPM++ 2M", "dpmpp_2m"),
            (12, "DDIM", "ddim"),
            (13, "LMS Karras", "lcm"),
            (14, "UniPC", "uni_pc")            
        };
        public static string FindMatch(string smp, bool of1111)
        {
            string found = "";
            foreach(var t in SamplersMatch)
            {
                if (of1111)
                {
                    if (smp.Equals(t.Item3)) { found = t.Item2; break; }
                }
                else
                {
                    if (smp.Equals(t.Item2)) { found = t.Item3; break; }
                }
            }
            return found;
        }

        // working parameters -> names from possParams
        private static readonly List<string> A1111Params = new List<string>
        {
            "prompt", "negative_prompt", "width", "height", "sampler_name", "restore_faces", "seed", "cfg_scale", "steps", "denoising_strength"
        };

        private static readonly List<string> ComfyParams = new List<string> // in internal names
        {
            "prompt", "negative_prompt", "width", "height", "sampler_name", "seed", "cfg_scale", "steps", "denoising_strength"
        };
        public static List<string> curParams { get { return A1111 ? A1111Params : ComfyParams; } }
        public static bool CheckParam(string nm)
        {
            return curParams.IndexOf(nm) > -1;
        }
        // translate from internal (A1111) to Comfy syntax
        public static Dictionary<string, object> Translate2Comfy(Dictionary<string, object> prms)
        {
            Dictionary<string, object> t = new Dictionary<string, object>();
            foreach (var p in prms)
            {
                //if (ComfyParams.IndexOf(p.Key) == -1) Utils.TimedMessageBox("Error: in translation, unknown parameter: " + p.Key);
                switch (p.Key)
                {
                    case "prompt": t.Add("positive", p.Value);
                        break;
                    case "negative_prompt": t.Add("negative", p.Value);
                        break;
                    case "cfg_scale": t.Add("cfg", p.Value);
                        break;
                    case "denoising_strength": t.Add("denoise", p.Value);
                        break;
                    case "sampler_name":
                        string sn = FindMatch((string)p.Value, true);
                        if (sn != "") t.Add("sampler_name", sn);
                        break;
                    case "width":
                    case "height":
                    case "seed":
                    case "steps": t.Add(p.Key, p.Value);
                        break;
                }
            }
            return t;
        }       
    }
    public class SDsetting: Dictionary<string, object>
    {
        public SDsetting() { }
        public SDsetting(Dictionary<string, object> inDict) { GetFromDict(inDict); }
        public void GetFromDict(Dictionary<string, object> inDict) 
        {
            if (Utils.isNull(inDict)) return; 
            foreach (var pair in inDict) // validation
                if (SDside.possParams.ContainsKey(pair.Key) && (pair.Key != "prompt")) this[pair.Key] = pair.Value;
        }
        public void GetFromImageInfo(ImageInfo ii)
        {
            if (Utils.isNull(ii)) return;
            this["negative_prompt"] = ii.negative_prompt;
            this["width"] = ii.width; this["height"] = ii.height;
            this["sampler_name"] = ii.sampler_name; this["restore_faces"] = ii.restore_faces; this["seed"] = ii.seed;
            this["steps"] = ii.steps; this["cfg_scale"] = ii.cfg_scale; this["denoising_strength"] = ii.denoising_strength;
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
            if (Header == null) ls.Add("#{\"ImageGenerator\":\"StableDiffusion\",\"webui\":\"parameters\",\"application\":\"Scripthea 1.9.5\"}");
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
        private bool locked = false; // lock params from visuals
        protected Options opts;
        private bool A1111 = true;
        public void Init(ref Options _opts)
        {
            opts = _opts; A1111 = opts.composer.A1111; SDside.A1111 = A1111;
            if (A1111)
            {
                chkRestoreFaces.Visibility = Visibility.Visible;
            }
            else
            {
                chkRestoreFaces.Visibility = Visibility.Collapsed;
                
            }
            cbSampler.Items.Clear();
            if (!A1111) cbSampler.Items.Add(new ComboBoxItem() { Content = "<default>"});
            foreach (string ss in SDside.Samplers)
            {
                FontFamily fm;
                if (SDside.FindMatch(ss, !A1111) != "") fm = new System.Windows.Media.FontFamily("Segoe UI Semibold");
                else fm = new System.Windows.Media.FontFamily("Segoe UI");

                ComboBoxItem cbi = new ComboBoxItem() { Content = ss, FontFamily = fm };
                cbSampler.Items.Add(cbi);
            }
            //if (locked) { locked = false; return; }              
            if (sdList == null) // first time
            {
                nsWidth.Init("Width", 512, 64, 2048, 10); nsWidth.lbTitle.Foreground = chkKeepRatio.Foreground;
                nsHeight.Init("Height", 512, 64, 2048, 10); nsHeight.lbTitle.Foreground = chkKeepRatio.Foreground;
                nsSamplingSteps.Init("Sampl.Steps", 20, 1, 150, 1);  nsCFGscale.Init("CFG Scale", 7, 1, 30, 0.1); nsDenoise.Init("Denoise", 1, 0, 1, 0.01);

                sdList = new SDlist(); 
                sdList.UpdateCombo(opts.general.LastSDsetting, cbSettings); 
                chkAutoSynch.IsChecked = opts.general.AutoRefreshSDsetting;               

                nsWidth.OnValueChanged += new NumericSliderUC.ValueChangedHandler(SizeAdjust); nsHeight.OnValueChanged += new NumericSliderUC.ValueChangedHandler(SizeAdjust);
                nsSamplingSteps.dblBox.ValueChanged += new RoutedEventHandler(visual2prms); 
                nsCFGscale.OnValueChanged += new DoubleSliderUC.ValueChangedHandler(CfgAdjust);  nsDenoise.OnValueChanged += new DoubleSliderUC.ValueChangedHandler(CfgAdjust);
            }            
            btnSetParams_Click(null, null); // update visual from setting/prms
        }
        public void Finish()
        {
            if (chkAutoSynch.IsChecked.Value) btnGetParams_Click(null, null);
            sdList.Save(); 
            opts.general.LastSDsetting = (cbSettings.SelectedItem as ComboBoxItem).Content as string; ; opts.general.AutoRefreshSDsetting = chkAutoSynch.IsChecked.Value;
        }
        private SDsetting _vPrms;
        public SDsetting vPrms
        {
            get 
            {
                return _vPrms;
            }
            set // prms2visualOn
            {
                if (value == null) return;
                if (value.ContainsKey("negative_prompt")) tbNegativePrompt.Text = Convert.ToString(value["negative_prompt"]);
                if (value.ContainsKey("width")) nsWidth.Value = Convert.ToInt32(value["width"]);
                if (value.ContainsKey("height")) nsHeight.Value = Convert.ToInt32(value["height"]);
                if (value.ContainsKey("sampler_name"))
                {
                    string sn = Convert.ToString(value["sampler_name"]);
                    if (!A1111) { sn = SDside.FindMatch(sn, A1111); if (sn == "") sn = "<default>"; }
                    cbSampler.Text = sn;
                }
                else cbSampler.Text = A1111 ? "" : "<default>";
                if (value.ContainsKey("restore_faces")) chkRestoreFaces.IsChecked = Convert.ToBoolean(value["restore_faces"]);
                if (value.ContainsKey("seed")) tbSeed.Text = Convert.ToString(Convert.ToInt64(value["seed"]));
                if (value.ContainsKey("steps")) nsSamplingSteps.Value = Convert.ToInt32(value["steps"]);
                if (value.ContainsKey("cfg_scale")) nsCFGscale.Value = Convert.ToDouble(value["cfg_scale"]);
                if (value.ContainsKey("denoising_strength")) nsDenoise.Value = Convert.ToDouble(value["denoising_strength"]);
            }
        }
        public bool stSet(string prmName, dynamic prmValue) // ?
        {
            if (!SDside.curParams.Contains(prmName)) return false; 
            vPrms = new SDsetting(new Dictionary<string, object>() { { prmName, (object)prmValue } });
            return true;
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
                string selText = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
                if (sdList.ContainsKey(selText)) return sdList[selText];
                else { Utils.TimedMessageBox("Unknown setting: " + selText); return null; }
            } 
        }
        protected void CheckDifference(SDsetting refSDs) // refSDs against the active SDsetting
        {
            if (Utils.isNull(refSDs) || Utils.isNull(ActiveSetting)) return;
            grpSDsettings.Header = "SD parameters settings" + ((ActiveSetting.Compare2SDsetting(vPrms) || chkAutoSynch.IsChecked.Value) ? "" : " *").ToString();
        }
        protected void SizeAdjust(object sender, double value)
        {
            if (locked) return;
            if (chkKeepRatio.IsChecked.Value && WxHratio > 0)
            {
                bool bb = locked;
                locked = true;
                if (sender == nsWidth) nsHeight.Value = Convert.ToInt32(nsWidth.Value / WxHratio);
                if (sender == nsHeight) nsWidth.Value = Convert.ToInt32(nsHeight.Value * WxHratio);
                locked = bb;
            }
            visual2prms(sender, null);
        }
        protected void CfgAdjust(object sender, double value)
        {
            if (locked) return;
            _vPrms["cfg_scale"] = nsCFGscale.Value;
            _vPrms["denoising_strength"] = nsDenoise.Value;
            CheckDifference(vPrms);
        }
        protected void visual2prms(object sender, RoutedEventArgs e) // live update
        {
            if (locked || cbSampler == null) return;
            _vPrms = new SDsetting();
            _vPrms["negative_prompt"] = tbNegativePrompt.Text; _vPrms["width"] = (long)nsWidth.Value; _vPrms["height"] = (long)nsHeight.Value;
            if (cbSampler.Items.Count > 0)
            {
                if (cbSampler.SelectedIndex == -1) cbSampler.SelectedIndex = 0;
                string sampler = (cbSampler.SelectedItem as ComboBoxItem).Content as string;
                if (A1111) _vPrms["sampler_name"] = sampler; 
                else
                {
                    if (sampler != "<default>")
                    {                
                        string sn = SDside.FindMatch(sampler, true);
                        if (sn == "") sn = SDside.a1111Samplers[0]; // if nothing set to default
                        _vPrms["sampler_name"] = sn;
                    }
                }
            }
             _vPrms["restore_faces"] = chkRestoreFaces.IsChecked.Value;
            if (long.TryParse(tbSeed.Text, out long result)) _vPrms["seed"] = result;
            _vPrms["steps"] = (long)nsSamplingSteps.Value;
            _vPrms["cfg_scale"] = nsCFGscale.Value;
            _vPrms["denoising_strength"] = nsDenoise.Value;

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
            vPrms = sds; chkKeepRatio_Checked(sender, null);
        }
        private void btnGetParams_Click(object sender, RoutedEventArgs e) 
        {
            ActiveSetting?.GetFromDict(vPrms); CheckDifference(vPrms);
        }
        private void cbSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!chkAutoSynch.IsChecked.Value) return;
            string oldSel = cbSettings.Text; // before the change
            if (sdList.ContainsKey(oldSel)) { visual2prms(null, null); sdList[oldSel].GetFromDict(vPrms); } // old one
            else Utils.TimedMessageBox("Unknown setting: " + cbSettings.Text);

            string newStr = "";
            if ((sender as ComboBox).SelectedItem != null) 
                newStr = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string; // new one
            if (sdList.ContainsKey(newStr)) { vPrms = sdList[newStr]; chkKeepRatio_Checked(sender, null); }
            else Utils.TimedMessageBox("Unknown setting: " + newStr);
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoSynch.IsChecked.Value)
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
            string selText = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
            if (!sdList.ContainsKey(selText)) { Utils.TimedMessageBox("Unknown setting: " + selText); return; }

            string newStg = new InputBox("Save current parameters in", selText, "").ShowDialog();
            if (newStg == "") return;
            sdList[newStg] = sds;
            if (newStg.Equals(selText))
            {
                if (chkAutoSynch.IsChecked.Value) Utils.TimedMessageBox("same name: <" + newStg + "> - no action");
                else Utils.TimedMessageBox("<" + newStg + "> params setting updated"); 
            }
            else Utils.TimedMessageBox("<" + newStg + "> params setting added");
            bool ar = chkAutoSynch.IsChecked.Value; chkAutoSynch.IsChecked = false; // prevent cbSettings_SelectionChanged
            sdList.UpdateCombo(newStg, cbSettings); chkAutoSynch.IsChecked = ar;
        }
        private void btnDelParams_Click(object sender, RoutedEventArgs e)
        {
            string selText = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
            if (sdList.ContainsKey(selText)) sdList.Remove(selText);
            else { Utils.TimedMessageBox("Unknown setting: " + selText); return; }
            Utils.TimedMessageBox(selText + " params setting removed");

            bool ar = chkAutoSynch.IsChecked.Value; chkAutoSynch.IsChecked = false; // prevent cbSettings_SelectionChanged
            sdList.UpdateCombo(null, cbSettings); 
            if (ar) btnSetParams_Click(null, null); chkAutoSynch.IsChecked = ar;
        }
        double WxHratio; 
        private void chkKeepRatio_Checked(object sender, RoutedEventArgs e)
        {
            WxHratio = chkKeepRatio.IsChecked.Value? (double)nsWidth.Value / (double)nsHeight.Value : -1;
        }      
        public List<Tuple<string, string>> HelpList()
        {
            List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
            ls.Add(new Tuple<string, string>(
                "stSet(string prmName, dynamic prmValue)", "Set SD parameter, returns True/False.\rPossible parameter names are:\r negative_prompt: string,\r width: integer,\r height: integer,\r sampler_name: string (Euler a, Euler, LMS, Heun, DPM2, DPM2 a,DPM++ 2Sa,DPM++ 2M, DPM++ SDE, DPM fast, DPM adaptive, LMS Karras, DPM2 Karas, DPM2 a Karas, DPM++ 2Sa Karas, DPM++ 2M Karas, DPM++ SDE Karas, DDIM, PLMS, UniPC),\r restore_faces: boolean,\r seed: integer,\r cfg_scale: integer,\r steps: integer."
                ));
            return ls;
        }
    }
}
