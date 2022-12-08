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
}
