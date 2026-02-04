using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using scripthea.options;
using UtilsNS;

namespace scripthea.engineAPI
{
    public class SDoptions // configuration only (the main state/switch is in opts)
    {
        public SDoptions()
        {
        }
        // common
        public int TimeOutImgGen;
        public bool autoCloseCmd;
        // A1111/ComfyUI
        public string SDloc1111;
        public bool APIcommSD; // if APIcommSD then API vs pyScript switch 
        public bool closeAtEndOfScan;
        public bool measureGPUtemp;
        public bool ValidateScript;
        public bool ValidateAPI;

        public string SDlocComfy;
        // GRU temperature
        public bool GPUtemperature;
        public int GPUThreshold;
        public int GPUstackDepth;        

        public void save(string configFilename)
        {
            File.WriteAllText(configFilename, JsonConvert.SerializeObject(this));
        }
    }
    /// <summary>
    /// Interaction logic, load & save for GeneralOptions genOptions
    /// </summary>
    public partial class SDoptionsWindow : Window
    {
        protected string configFilename = Path.Combine(Utils.configPath, "StableDiffusion.cfg");  
        public bool keepOpen = true;
        public bool? ValidScript { get; private set; } = null; // unvalidated
        public bool? ValidAPI { get; private set; } = null; // unvalidated
        /// <summary>
        /// dialog box constructor; reads from file or creates new options object
        /// </summary>
        public SDoptionsWindow(bool _A1111)
        {
            InitializeComponent();
            numTimeOutImgGen.Maximum = 1000; numTimeOutImgGen.Minimum = 10;
            numGPUThreshold.Maximum = 100; numGPUThreshold.Minimum = 2;
            numGPUstackDepth.Maximum = 100; numGPUstackDepth.Minimum = 2;
            if (File.Exists(configFilename))
            {
                string fileJson = File.ReadAllText(configFilename);
                opts = JsonConvert.DeserializeObject<SDoptions>(fileJson);
            }
            else { opts = new SDoptions(); opts.ValidateScript = true; opts.ValidateAPI = true; }
            A1111 = _A1111;
            if (A1111)
            {
                if (opts.ValidateAPI && opts.APIcommSD) ValidateAPI1111();
                if (opts.ValidateScript && !opts.APIcommSD) ValidatePyScript();
                //Title += "  (A1111/Forge mode)";
                tabCtrl.SelectedItem = tiForge; //tiForge.IsEnabled = true; 
            }
            else 
            { 
                //Title += "  (ComfyUI mode)";
                tabCtrl.SelectedItem = tiComfyUI; //tiForge.IsEnabled = false;
            }
            //tiComfyUI.IsEnabled = !tiForge.IsEnabled;
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt, "Warning", 3500);
        }
        public bool nVidiaHwAvailable { get; set; }
        
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public SDoptions opts;
        private void Visual2opts()
        {
            opts.SDloc1111 = tbSDloc1111.Text;
            opts.ValidateScript = chkValidateScript.IsChecked.Value;
            opts.ValidateAPI = chkValidateAPI.IsChecked.Value;
            opts.APIcommSD = rbAPIcomm.IsChecked.Value;
            opts.closeAtEndOfScan = chkAutoCloseSession.IsChecked.Value;

            opts.SDlocComfy = tbSDlocComfy.Text;

            opts.measureGPUtemp = chkMeasureGPUtemp.IsChecked.Value;
            opts.GPUtemperature = chkGPUtemperature.IsChecked.Value;
            opts.GPUThreshold = numGPUThreshold.Value;
            opts.GPUstackDepth = numGPUstackDepth.Value;
        
            opts.TimeOutImgGen = numTimeOutImgGen.Value;
            opts.autoCloseCmd = chkAutoCloseCmd.IsChecked.Value;
        }
        private bool A1111;
        public void opts2Visual()
        {
            if (IsSDloc1111(opts.SDloc1111)) tbSDloc1111.Text = opts.SDloc1111;
            chkValidateScript.IsChecked = opts.ValidateScript || !ValidatePyScript();
            chkValidateAPI.IsChecked = opts.ValidateAPI || !ValidateAPI1111();
            chkAutoCloseSession.IsChecked = opts.closeAtEndOfScan;
            rbAPIcomm.IsChecked = opts.APIcommSD;

            if (IsSDlocComfy(opts.SDlocComfy)) tbSDlocComfy.Text = opts.SDlocComfy;

            numTimeOutImgGen.Value = opts.TimeOutImgGen;
            chkAutoCloseCmd.IsChecked = opts.autoCloseCmd;

            chkMeasureGPUtemp.IsChecked = opts.measureGPUtemp;
            chkGPUtemperature.IsChecked = opts.GPUtemperature;
            numGPUThreshold.Value = opts.GPUThreshold; 
            numGPUstackDepth.Value = opts.GPUstackDepth;
            chkMeasureGPUtemp_Checked(null, null);
        }
        /// <summary>
        /// Accepting and saving the changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e) // visual to internal 
        {
            Visual2opts();
            opts.save(configFilename);
            Hide();
        }
        /// <summary>
        /// Cancel without modifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
        private void wndSDOptions_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = keepOpen; Hide();
        }
        public bool IsSDloc1111(string bat, bool warning = true) 
        {
            bool bb = File.Exists(bat); if (!bb) return false;
            string folder = bb ? Path.GetDirectoryName(bat) : "";
            if (bb) bb &=  Directory.Exists(Path.Combine(folder, "scripts")) && Directory.Exists(Path.Combine(folder, "modules"));
            if (!bb && warning && !folder.Equals("")) Utils.TimedMessageBox("The folder <" + folder + "> does not look like a Stable Diffusion A1111/Forge installation.", "Problem", 5000);
            if (bb) gbSDloc1111.BorderBrush = Brushes.Silver;
            else gbSDloc1111.BorderBrush = Brushes.OrangeRed;
            return bb;
        }
        public bool IsSDlocComfy(string bat, bool warning = true)
        {
            bool bb = File.Exists(bat); if (!bb) return false;
            string folder = bb ? Path.GetDirectoryName(bat) : "";
            if (bb) bb &= Directory.Exists(Path.Combine(folder, "ComfyUI")) && Directory.Exists(Path.Combine(folder, "python_embeded"));
            if (!bb && warning && !folder.Equals("")) Utils.TimedMessageBox("The folder <" + folder + "> does not look like a Stable Diffusion ComfyUI installation.", "Problem", 5000);
            if (bb) gbSDloc1111.BorderBrush = Brushes.Silver;
            else gbSDloc1111.BorderBrush = Brushes.OrangeRed;
            return bb;
        }
        public bool ValidatePyScript()
        {            
            if (!A1111 || opts.APIcommSD) return true;
            ValidScript = false;
            string pyScript = "prompts_from_scripthea_1_5.py";
            string orgLoc = Path.Combine(Utils.configPath, pyScript);
            if (!File.Exists(orgLoc)) { Log("Error[784]: file " + orgLoc + " is missing."); return false; }
            if (!IsSDloc1111(opts.SDloc1111)) return false; string folder = Path.GetDirectoryName(opts.SDloc1111);
            string sdLoc = Path.Combine(folder, "scripts", pyScript);
            if (Utils.GetMD5Checksum(orgLoc) == Utils.GetMD5Checksum(sdLoc)) { ValidScript = true; return true; }
            if (MessageBox.Show("Scripthea python script is missing (or old) from scripts folder of SD\r\r Copy <"+pyScript+"> to SD script folder?\r Yes - recommended", "",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return false;
            File.Copy(orgLoc, sdLoc, true); Utils.Sleep(200);
            ValidScript = Utils.GetMD5Checksum(orgLoc) == Utils.GetMD5Checksum(sdLoc);
            return Convert.ToBoolean(ValidScript);
        }
        public bool ValidateAPI1111()
        {
            if (!A1111 || !opts.APIcommSD) return true;
            ValidAPI = false;
            if (!IsSDloc1111(opts.SDloc1111)) return false;
            string batLoc = opts.SDloc1111;
            List<string> ls = Utils.readList(batLoc, false); 
            bool found = false; int j = -1;
            for (int i = 0; i < ls.Count; i++)
            {
                if (!ls[i].StartsWith("set COMMANDLINE_ARGS")) continue;
                string ss = ls[i].Remove(0,20); j = i;
                string[] sa = ss.Split(' ');                
                foreach (string sb in sa)
                {
                    found = sb.Equals("--api",StringComparison.InvariantCultureIgnoreCase);
                    if (found) { ValidAPI = true; return true; }
                }                    
            }
            if (MessageBox.Show("Command line parameter(--api) in webui-user.bat is missing.\r Correct webui-user.bat for API access to Stable Diffusion?\r\r Yes - recommended", "",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return false;
            if (j == -1) return false;
            ls[j] = ls[j] + " --api";
            Utils.writeList(batLoc, ls);
            ValidAPI = true; return true; 
        }
        // C:\Software\stable-diffusion-webui-forge\webui-user.bat - OK
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = File.Exists(tbSDloc1111.Text) ? Path.GetDirectoryName(tbSDloc1111.Text) : Utils.basePath;
            dialog.DefaultExtension = ".bat"; dialog.Filters.Add(new CommonFileDialogFilter("batch file", "bat"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (IsSDloc1111(dialog.FileName)) 
                {
                    tbSDloc1111.Text = dialog.FileName; opts.SDloc1111 = dialog.FileName; // ValidatePyScript();
                }
            }
            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();         // important
        }
        private void btnBrowseComfy_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = File.Exists(tbSDlocComfy.Text) ? Path.GetDirectoryName(tbSDlocComfy.Text) : Utils.basePath;
            dialog.DefaultExtension = ".bat"; dialog.Filters.Add(new CommonFileDialogFilter("batch file", "bat"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (IsSDlocComfy(dialog.FileName))
                {
                    tbSDlocComfy.Text = dialog.FileName; opts.SDloc1111 = dialog.FileName; 
                }
            }
            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();         // important
        }
        private void chkMeasureGPUtemp_Checked(object sender, RoutedEventArgs e)
        {
            if (nVidiaHwAvailable && chkMeasureGPUtemp.IsChecked.Value) // same controls different labels
            {
                groupGPUtmpr.Header = "      nVidia GPU temperature feedack"; lbGPUvalue.Content = "Threshold [°C]";
                lbGPUvalueDepth.Visibility = Visibility.Visible; numGPUstackDepth.Visibility = Visibility.Visible;
            }
            else
            {
                groupGPUtmpr.Header = "      non-nVidia GPU temperature control"; lbGPUvalue.Content = "Delay ";
                lbGPUvalueDepth.Visibility = Visibility.Collapsed; numGPUstackDepth.Visibility = Visibility.Collapsed;
            }
        }
        private void rbAPIcomm_Checked(object sender, RoutedEventArgs e)
        {
            if (chkValidateAPI is null) return;
            if (rbAPIcomm.IsChecked.Value)
            {
                chkValidateAPI.Foreground = Brushes.Black;
                chkValidateScript.Foreground = Brushes.Gray;
                chkAutoCloseSession.Foreground = Brushes.Gray;
            }
            else
            {
                chkValidateAPI.Foreground = Brushes.Gray;
                chkValidateScript.Foreground = Brushes.Black;
                chkAutoCloseSession.Foreground = Brushes.Black;
            }
        }
    }

    public class SDformat
    {        
        public Dictionary<string, Type> args;
        public SDformat()
        {
            args = new Dictionary<string, Type>();
            args.Add("sd_model", null);
            args.Add("outpath_samples", typeof(string));
            args.Add("outpath_grids", typeof(string));
            args.Add("prompt_for_display", typeof(string));
            args.Add("prompt", typeof(string));
            args.Add("negative_prompt", typeof(string));
            args.Add("styles", typeof(string));
            args.Add("seed", typeof(int));
            args.Add("subseed_strength", typeof(double));
            args.Add("subseed", typeof(int));
            args.Add("seed_resize_from_h", typeof(int));
            args.Add("seed_resize_from_w", typeof(int));
            args.Add("sampler_index", typeof(int));
            args.Add("batch_size", typeof(int));
            args.Add("n_iter", typeof(int));
            args.Add("steps", typeof(int));
            args.Add("cfg_scale", typeof(double));
            args.Add("width", typeof(int));
            args.Add("height", typeof(int));
            args.Add("restore_faces", typeof(bool));
            args.Add("tiling", typeof(bool));
            args.Add("do_not_save_samples", typeof(bool));
            args.Add("do_not_save_grid", typeof(bool));
        }
    }
}
