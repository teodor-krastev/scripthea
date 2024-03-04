using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using Newtonsoft.Json;
using Path = System.IO.Path;
using System.IO.Pipes;
using UtilsNS;
using scripthea.viewer;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for diffusionUC.xaml
    /// </summary>
    public partial class SDiffusionUC : UserControl, interfaceAPI
    {
        public SDiffusionUC()
        {
            InitializeComponent(); localDebug = Utils.isInVisualStudio;
            opts = new Dictionary<string, string>();
        }
        SDoptionsWindow SDopts; private bool localDebug = true;
        public Dictionary<string, string> opts { get; set; } // interfaceAPI: main (non API specific) options 
        private Process process4batch = null;
        private Options genOpts;
        public void Init(ref Options _opts) // init and update visuals from opts
        {
            genOpts = _opts;
            sd_api_uc.btnSDoptions.Click -= btnSDoptions_Click; sd_api_uc.btnSDoptions.Click += btnSDoptions_Click;
            sd_api_uc.ActiveEvent -= OnActiveEvent; sd_api_uc.ActiveEvent += OnActiveEvent;
            sdScriptUC.btnSDoptions.Click -= btnSDoptions_Click; sdScriptUC.btnSDoptions.Click += btnSDoptions_Click;

            if (SDopts == null) SDopts = new SDoptionsWindow();
            tempRegulator.Init(ref SDopts.opts); SDopts.nVidiaHwAvailable = tempRegulator.nVidiaHWAvailable;
            opts2Visual(true);
        }
        private void OnActiveEvent(object sender, EventArgs e)
        {
            PossibleRunServer();
        }
        private bool lastAPIcomm = false;
        private void opts2Visual(bool first)
        {
            bool changeAPIcomm = lastAPIcomm != SDopts.opts.APIcomm;
            if (first || changeAPIcomm)
            {
                if (SDopts.opts.APIcomm)
                {
                    if (changeAPIcomm)
                        if (sdScriptUC.IsConnected) sdScriptUC.reStartServers(true);
                    sd_api_uc.Visibility = Visibility.Visible; sdScriptUC.Visibility = Visibility.Collapsed;
                    sd_api_uc.Init(ref SDopts);
                }
                else
                {
                    if (changeAPIcomm) sd_api_uc.Finish();
                    sdScriptUC.Visibility = Visibility.Visible; sd_api_uc.Visibility = Visibility.Collapsed;                    
                    sdScriptUC.Init(ref SDopts); 
                }
                lastAPIcomm = SDopts.opts.APIcomm;
                var ap = OnAPIparams(SDopts.opts.APIcomm);
            }
            tempRegulator.opts2Visual();
            PossibleRunServer();
        }
        private bool PossibleRunServer()
        {
            btnRunServer.IsEnabled = SDopts.IsSDlocation(SDopts.opts.SDlocation, false);
            if (SDopts.opts.APIcomm)
            {
                btnRunServer.IsEnabled &= !sd_api_uc.active;
            }
            else { }
            if (btnRunServer.IsEnabled) { btnRunServer.Foreground = Brushes.DarkRed; btnRunServer.BorderBrush = Brushes.DarkRed; }
            else { btnRunServer.Foreground = Brushes.DarkGray; btnRunServer.BorderBrush = Brushes.DarkGray; }
            return btnRunServer.IsEnabled;
        }
        public void Finish()
        {
            tempRegulator.Finish();
            sd_api_uc.Finish(); sdScriptUC.Finish();
            if (SDopts.opts.autoCloseCmd) closeProcess();
            SDopts.keepOpen = false; SDopts.Close(); SDopts = null;
        }
        public void Broadcast(string msg)
        {
            if (SDopts.opts.APIcomm) return;
            if (msg.Equals("end.scan", StringComparison.OrdinalIgnoreCase) && SDopts.opts.closeAtEndOfScan)
            {
                sdScriptUC.reStartServers(true); Log("SD comm. has been reset.", Brushes.Tomato);
            }                
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public event APIparamsHandler APIparamsEvent;
        protected Dictionary<string, object> OnAPIparams(bool? showIt)
        {
            return APIparamsEvent?.Invoke(showIt);
        }
        public bool isDocked { get { return true; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled  // connected and working (depends on the API)
        {
            get
            {
                if (SDopts.opts.APIcomm) return sd_api_uc.active;
                else return sdScriptUC.IsConnected;
            }  
        }
        private void SimulatorImage(string filepath) // copy random simulator file to filepath
        {
            File.Copy(SimulFolder.RandomImageFile, filepath);
        }
        public bool GenerateImage(string prompt, string imageDepotFolder, ref ImageInfo ii) // returns the filename of saved/copied in ImageDepoFolder image 
        {
            if (!isEnabled) { ii = null; return false; } 
            tempRegulator.tempRegulate(); bool rslt = false; 
            if (SDopts.opts.APIcomm) // API
            {
                string filename = Utils.timeName(); // target image 
                string folder = imageDepotFolder.EndsWith("\\") ? imageDepotFolder : imageDepotFolder + "\\"; opts["IDfolder"] = folder;
                string fullFN = Path.Combine(folder, Path.ChangeExtension(filename, ".png"));
                if (File.Exists(fullFN))
                {
                    fullFN = Utils.AvoidOverwrite(fullFN); // new name
                    filename = Path.ChangeExtension(Path.GetFileName(fullFN), "");
                }
                Dictionary<string, object> apIn = OnAPIparams(null); Dictionary<string, object> apOut;
                apIn["prompt"] = prompt;
                rslt = sd_api_uc.GenerateImage(fullFN, apIn, out apOut);
                
                ii = new ImageInfo(apOut);
            }
            else // named-pipe server here <-> client in python script
            {
                string filename;
                rslt = sdScriptUC.GenerateImage(prompt, imageDepotFolder, out filename);
                ii = new ImageInfo(Path.Combine(imageDepotFolder, filename), ImageInfo.ImageGenerator.StableDiffusion, true);
            }
            return rslt;
        }
        private void lb1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender.Equals(lb1)) Utils.AskTheWeb("Stable+Diffusion+text-to-image+generator");
            if (sender.Equals(lb2)) Utils.CallTheWeb("https://stability.ai/");            
            //if (sender.Equals(btnRunServer)) Utils.CallTheWeb("http://127.0.0.1:7860/");
        }        
        private void btnSDoptions_Click(object sender, RoutedEventArgs e)
        {
            bool bb = SDopts.opts.APIcomm;
            SDopts.opts2Visual(); SDopts.ShowDialog();
            opts2Visual(bb != SDopts.opts.APIcomm);
        }
        private void closeProcess()
        {
            try
            {
                if (process4batch == null) return;
                if (process4batch.HasExited)
                {
                    Console.WriteLine($"Process exited with code: {process4batch.ExitCode}");
                    Console.WriteLine($"Process exit time: {process4batch.ExitTime}");
                    return;
                }                   
                if (!process4batch.CloseMainWindow())
                {
                    //If the main window cannot be closed, kill the process
                    process4batch.Kill();
                }
            }
            catch (Exception ex) { Log("Error: SD server -> " + ex.Message); } 
        }
        private void btnRunServer_Click(object sender, RoutedEventArgs e)
        {
            if (!SDopts.IsSDlocation(SDopts.opts.SDlocation, true)) return;
            process4batch = Utils.RunBatchFile(Path.Combine(SDopts.opts.SDlocation, "webui-user.bat"));
            if (process4batch.HasExited)
            {
                Console.WriteLine($"Process exited with code: {process4batch.ExitCode}");
                Console.WriteLine($"Process exit time: {process4batch.ExitTime}");                
            }
        }
    }
}

