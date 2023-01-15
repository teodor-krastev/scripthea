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
using UtilsNS;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace scripthea.external
{
    public class SDoptions
    {
        public SDoptions()
        {

        }
        // General
        public bool closeAtEndOfScan;
        public int TimeOutImgGen;
        public bool showCommLog;
        public bool showGPUtemp;
        public bool GPUtemperature;
        public int GPUThreshold;
        public int GPUstackDepth;        
        // Initial settings
        public string SDlocation;
        public bool ValidScript;
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
        public bool keepOpen = true;
        /// <summary>
        /// dialog box constructor; reads from file or creates new options object
        /// </summary>
        public SDoptionsWindow()
        {
            InitializeComponent();         
            if (File.Exists(configFilename))
            {
                string fileJson = File.ReadAllText(configFilename);
                opts = JsonConvert.DeserializeObject<SDoptions>(fileJson);                
            }
            else opts = new SDoptions();
        }
        public string configFilename = Path.Combine(Utils.configPath, "StableDiffusion.cfg");  
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public SDoptions opts;
        private void Visual2opts()
        {
            opts.closeAtEndOfScan = chkAutoCloseSession.IsChecked.Value;
            opts.TimeOutImgGen = numTimeOutImgGen.Value;
            opts.showCommLog = chkCommLog.IsChecked.Value;
            opts.showGPUtemp = chkGPUtemp.IsChecked.Value;
            opts.GPUtemperature = chkGPUtemperature.IsChecked.Value;
            opts.GPUThreshold = numGPUThreshold.Value;
            opts.GPUstackDepth = numGPUstackDepth.Value;
        
            opts.SDlocation = tbSDlocation.Text;
            opts.ValidScript = chkValidScript.IsChecked.Value;
        }
        public void opts2Visual()
        {
            chkAutoCloseSession.IsChecked = opts.closeAtEndOfScan;
            numTimeOutImgGen.Value = opts.TimeOutImgGen;
            chkCommLog.IsChecked = opts.showCommLog;
            chkGPUtemp.IsChecked = opts.showGPUtemp;
            chkGPUtemperature.IsChecked = opts.GPUtemperature;
            numGPUThreshold.Value = opts.GPUThreshold;
            numGPUstackDepth.Value = opts.GPUstackDepth;

            if (Directory.Exists(opts.SDlocation)) tbSDlocation.Text = opts.SDlocation;
            else Utils.TimedMessageBox("SD-WebUI directory <" + opts.SDlocation + "> does not exist.", "Warning", 3000);
            chkValidScript.IsChecked = opts.ValidScript;
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

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Directory.Exists(tbSDlocation.Text) ? tbSDlocation.Text : Utils.basePath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbSDlocation.Text = dialog.FileName;
            }
            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();         // important
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
