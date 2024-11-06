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
            {"model", "String" }, {"sd_model_hash", "String" }, {"denoising_strength", "Double" }, {"job_timestamp", "String" }
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
            "euler_ancestral", "euler", "lms", "heun", "heunpp2","dpm_2", "dpm_2_ancestral",
            "dpm_fast", "dpm_adaptive", "dpmpp_2s_ancestral", "dpmpp_sde", "dpmpp_sde_gpu",
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
        public static string FindMatch(string smp, bool toComfy, bool selfIncl = true) // true - return Comfy syntax; false - return A1111 syntax
        {
            if (toComfy) 
            {
                if (comfySamplers.IndexOf(smp) > -1 && selfIncl) return smp; 
                foreach(var t in SamplersMatch)
                    if (smp.Equals(t.Item2)) return t.Item3; 
            }
            else // to1111
            {
                if (a1111Samplers.IndexOf(smp) > -1 && selfIncl) return smp;
                foreach (var t in SamplersMatch)
                    if (smp.Equals(t.Item3)) return t.Item2;
            }
            return "";
        }
        // working parameters -> names from possParams
        private static readonly List<string> A1111Params = new List<string>
        {
            "prompt", "negative_prompt", "width", "height", "sampler_name", "restore_faces", "seed", "cfg_scale", "steps", "denoising_strength", "model"
        };

        private static readonly List<string> ComfyParams = new List<string> // in internal names
        {
            "prompt", "negative_prompt", "width", "height", "sampler_name", "seed", "cfg_scale", "steps", "denoising_strength", "model"//, "template"
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
                    case "prompt":
                        string ss = Convert.ToString(p.Value).Replace('\"', '`').Replace('\'', '`');
                        t.Add("positive", ss);
                        break;
                    case "negative_prompt": t.Add("negative", p.Value);
                        break;
                    case "cfg_scale": t.Add("cfg", p.Value);
                        break;
                    case "denoising_strength": t.Add("denoise", p.Value);
                        break;
                    case "sampler_name":
                        string sn = FindMatch((string)p.Value, true);
                        if (sn != "") t.Add(p.Key, sn);
                        else t.Add(p.Key, SamplersMatch[0].Item3); // not-found go for default
                        break;
                    case "seed":
                        int sd = Convert.ToInt32(p.Value);
                        t.Add(p.Key, sd == -1? 0 : sd);
                        break;
                    case "width":
                    case "height":
                    case "steps":
                    //case "template":
                    case "model": t.Add(p.Key, p.Value);
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
            this["steps"] = ii.steps; this["cfg_scale"] = ii.cfg_scale; this["model"] = ii.model; this["denoising_strength"] = ii.denoising_strength;
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
    public class SDlist: Dictionary<string, SDsetting> // change to ordered dictionary later
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
        public int UpdateCombo(string LastSetting, ComboBox cb)
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
            return cb.SelectedIndex;
        }
        public void Save()
        {
            List<string> ls = new List<string>();
            ls.Add("#{\"ImageGenerator\":\"StableDiffusion\",\"webui\":\"parameters\",\"application\":\"Scripthea "+Utils.getAppFileVersion+"\"}");
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
        private bool A1111
        {
            get { return opts.composer.A1111; }
            set
            {
                SDside.A1111 = A1111;
                if (A1111)
                {
                    chkRestoreFaces.Visibility = Visibility.Visible; //stpModel.Visibility = Visibility.Collapsed;
                    gbComfyTemplates.Visibility = Visibility.Collapsed;
                }
                else
                {
                    chkRestoreFaces.Visibility = Visibility.Collapsed; stpModel.Visibility = Visibility.Visible;
                    gbComfyTemplates.Visibility = Visibility.Visible;
                }
            }
        }
        public void Init(ref Options _opts)
        {
            opts = _opts; A1111 = opts.composer.A1111; // update vis.
            cbSampler.Items.Clear();
            foreach (string ss in SDside.Samplers)
            {
                FontFamily fm;
                if (SDside.FindMatch(ss, A1111, false) != "") fm = new System.Windows.Media.FontFamily("Segoe UI Semibold");
                else fm = new System.Windows.Media.FontFamily("Segoe UI");

                ComboBoxItem cbi = new ComboBoxItem() { Content = ss, FontFamily = fm };
                cbSampler.Items.Add(cbi);
            }

            //if (locked) { locked = false; return; }              
            if (sdList == null) // only the first time
            {
                // template
                cbTemplates.Items.Clear(); int idx = -1;
                foreach (string tmpl in GetTemplates())
                { int i = cbTemplates.Items.Add(tmpl); if (tmpl.Equals(opts.general.ComfyTemplate, StringComparison.InvariantCultureIgnoreCase)) idx = i; }
                if (idx > -1) cbTemplates.SelectedIndex = idx;
                else cbTemplates.SelectedIndex = 0;
                // visual arrangments
                nsWidth.Init("Width", 512, 64, 2048, 10); nsWidth.lbTitle.Foreground = chkKeepRatio.Foreground;
                nsHeight.Init("Height", 512, 64, 2048, 10); nsHeight.lbTitle.Foreground = chkKeepRatio.Foreground;
                nsSamplingSteps.Init("Sample.steps", 20, 1, 150, 1);  nsCFGscale.Init("CFG.scale", 7, 1, 30, 0.1); nsDenoise.Init("Denoise", 1, 0, 1, 0.01);
                // sd settings
                sdList = new SDlist(); 
                int k = sdList.UpdateCombo(opts.general.LastSDsetting, cbSettings); 
                if (sdList.ContainsKey(opts.general.LastSDsetting)) _vPrms = new SDsetting(sdList[opts.general.LastSDsetting]);
                else
                {
                    string sd = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
                    if (sd != "") _vPrms = new SDsetting(sdList[sd]);
                    else opts.Log("Error[4896]: unknown setting." + opts.general.LastSDsetting);
                }
                chkAutoSynch.IsChecked = opts.general.AutoRefreshSDsetting;               
                // events
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
                    string sn1 = SDside.FindMatch(sn, !A1111);
                    if (sn1 == "")
                    {
                        opts.Log("Error[5556]: sampler <" + sn + "> not found. Assume default");
                        sn = SDside.Samplers[0];
                    }
                    else sn = sn1;
                    if (!VisualHelper.SelectItemInCombo(cbSampler, sn)) { opts.Log("Error[555]: internal issue!"); sn = ""; }  
                    if (sn != "") value["sampler_name"] = sn;
                }
                if (value.ContainsKey("restore_faces")) chkRestoreFaces.IsChecked = Convert.ToBoolean(value["restore_faces"]);
                if (value.ContainsKey("seed"))
                {
                    long li = Convert.ToInt64(value["seed"]);
                    if (A1111) { if (li == 0) li = -1;} // adjust randomizing code
                    else { if (li < 0) li = 0; }
                    tbSeed.Text = Convert.ToString(li);
                }                        
                if (value.ContainsKey("steps")) nsSamplingSteps.Value = Convert.ToInt32(value["steps"]);
                if (value.ContainsKey("cfg_scale")) nsCFGscale.Value = Convert.ToDouble(value["cfg_scale"]);
                if (value.ContainsKey("model")) tbModel.Text = Convert.ToString(value["model"]).Trim();                
                if (value.ContainsKey("denoising_strength")) nsDenoise.Value = Convert.ToDouble(value["denoising_strength"]);
            }
        }
        public bool set(string prmName, dynamic prmValue) 
        {
            if (!SDside.curParams.Contains(prmName)) return false; 
            vPrms = new SDsetting(new Dictionary<string, object>() { { prmName, (object)prmValue } });
            return true;
        }
        public dynamic get(string prmName) 
        {
            if (!SDside.curParams.Contains(prmName)) return null;
            return vPrms[prmName];
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
                if (cbSettings == null) return null;
                string selText = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
                if (sdList.ContainsKey(selText)) return sdList[selText];
                else { opts.Log("Unknown setting: " + selText); return null; }
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
            /*if (locked) return;
            _vPrms["cfg_scale"] = nsCFGscale.Value;
            _vPrms["denoising_strength"] = nsDenoise.Value;*/
            visual2prms(sender, null); //CheckDifference(vPrms); 
        }
        protected void visual2prms(object sender, RoutedEventArgs e) // live update
        {
            if (locked || cbSampler == null || _vPrms == null) return;            
            _vPrms["negative_prompt"] = tbNegativePrompt.Text; _vPrms["width"] = (long)nsWidth.Value; _vPrms["height"] = (long)nsHeight.Value;
            if (cbSampler.Items.Count > 0)
            {
                if (cbSampler.SelectedIndex == -1) cbSampler.SelectedIndex = 0;
                string sampler = (cbSampler.SelectedItem as ComboBoxItem).Content as string;
                _vPrms["sampler_name"] = sampler; // vPrms.sampler is current syntax                
            }
             _vPrms["restore_faces"] = chkRestoreFaces.IsChecked.Value;
            if (long.TryParse(tbSeed.Text, out long li))
            {              
                if (A1111) { if (li == 0) li = -1; } // adjust randomizing code
                else { if (li < 0) li = 0; }                
                _vPrms["seed"] = li;
            }
            _vPrms["steps"] = (long)nsSamplingSteps.Value;
            _vPrms["cfg_scale"] = nsCFGscale.Value;
            _vPrms["model"] = tbModel.Text.Trim();
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
            else opts.Log("Error: unknown setting: " + cbSettings.Text);

            string newStr = "";
            if ((sender as ComboBox).SelectedItem != null) 
                newStr = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string; // new one
            if (sdList.ContainsKey(newStr)) { vPrms = sdList[newStr]; chkKeepRatio_Checked(sender, null); }
            else opts.Log("Error: unknown setting: " + newStr);
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoSynch.IsChecked.Value)
            {
                btnSetParams.Visibility = Visibility.Collapsed; btnGetParams.Visibility = Visibility.Collapsed;
                cbSettings.Width = 150+62; btnGetParams_Click(null, null); 
            }
            else
            {
                btnSetParams.Visibility = Visibility.Visible; btnGetParams.Visibility = Visibility.Visible;
                cbSettings.Width = 150;
            }
        }
        private void btnAddParams_Click(object sender, RoutedEventArgs e)
        {
            SDsetting sds = vPrms.Clone();
            string selText = (cbSettings.SelectedItem as ComboBoxItem).Content as string;
            if (!sdList.ContainsKey(selText)) { opts.Log("Error: unknown setting: " + selText); return; }

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
            else { opts.Log("Error: unknown setting: " + selText); return; }
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
                "get(string prmName)", "Get SD parameter.\rPossible parameter names are:\r negative_prompt: string,\r width: integer,\r height: integer,\r sampler_name: string (Euler a, Euler, LMS, Heun, DPM2, DPM2 a,DPM++ 2Sa,DPM++ 2M, DPM++ SDE, DPM fast, DPM adaptive, LMS Karras, DPM2 Karas, DPM2 a Karas, DPM++ 2Sa Karas, DPM++ 2M Karas, DPM++ SDE Karas, DDIM, PLMS, UniPC),\r restore_faces: boolean,\r seed: integer,\r cfg_scale: double,\r steps: integer.\nReturns parameter value"
                ));
            ls.Add(new Tuple<string, string>(
                "set(string prmName, dynamic prmValue)", "Set SD parameter, returns True/False.\rPossible parameter names are:\r negative_prompt: string,\r width: integer,\r height: integer,\r sampler_name: string (Euler a, Euler, LMS, Heun, DPM2, DPM2 a,DPM++ 2Sa,DPM++ 2M, DPM++ SDE, DPM fast, DPM adaptive, LMS Karras, DPM2 Karas, DPM2 a Karas, DPM++ 2Sa Karas, DPM++ 2M Karas, DPM++ SDE Karas, DDIM, PLMS, UniPC),\r restore_faces: boolean,\r seed: integer,\r cfg_scale: double,\r steps: integer."
                ));
            return ls;
        }
        private void chkDefaultModel_Checked(object sender, RoutedEventArgs e) // later maybe
        {
            /*if (tbModel == null) return;
            if (chkDefaultModel.IsChecked.Value) tbModel.Text = "<default>";
            else tbModel.Text = "";
            tbModel.IsReadOnly = chkDefaultModel.IsChecked.Value;*/
        }
        private void tbModel_TextChanged(object sender, TextChangedEventArgs e)
        {
            visual2prms(sender, e);
        }
        private List<string> GetTemplates()
        {
            List<string> tmpl = new List<string>(); string[] arr = Directory.GetFiles(Utils.configPath, "*.cftm");
            foreach (string tm in arr)
                tmpl.Add(Path.GetFileName(tm));
            return tmpl;
        }
        private void cbTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (opts == null) return;
            opts.general.ComfyTemplate = cbTemplates.SelectedItem as string; //as ComboBoxItem
        }
    }
}
