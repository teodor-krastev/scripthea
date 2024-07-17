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
using scripthea.viewer;
using scripthea.options;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for diffusionUC.xaml
    /// </summary>
    public partial class SDiffusionUC : UserControl, interfaceAPI
    {
        public SDiffusionUC()
        {
            InitializeComponent(); 
            opts = new Dictionary<string, string>();
        }
        SDoptionsWindow SDopts; 
        public Dictionary<string, string> opts { get; set; } // interfaceAPI: main (non API specific) options 
        private Process process4batch1111 = null; private Process process4batchComfy = null;
        private Options genOpts;
        public void Init(ref Options _opts) // init and update visuals from opts
        {
            genOpts = _opts; 
            btnRunServer.ToolTip = genOpts.composer.A1111 ? "Open local SD A1111/Forge server" : "Open local SD ComfyUI server";

            sd_api_uc.btnSDoptions.Click -= btnSDoptions_Click; sd_api_uc.btnSDoptions.Click += btnSDoptions_Click;
            sd_api_uc.ActiveEvent -= OnActiveEvent; sd_api_uc.ActiveEvent += OnActiveEvent;
            sdScriptUC.btnSDoptions.Click -= btnSDoptions_Click; sdScriptUC.btnSDoptions.Click += btnSDoptions_Click;

            if (SDopts == null) SDopts = new SDoptionsWindow(genOpts.composer.A1111);
            tempRegulator.Init(ref SDopts.opts); SDopts.nVidiaHwAvailable = tempRegulator.nVidiaHWAvailable;
            opts2Visual(true);
        }
        private void OnActiveEvent(object sender, EventArgs e)
        {
            PossibleRunServer();
        }
        private bool lastAPIcom1111 = false; //A1111 -> api or py
        private void opts2Visual(bool first)  // to add comfyUI
        {
            bool changeAPIcom1111 = lastAPIcom1111 != SDopts.opts.APIcomm1111;
            if (first || changeAPIcom1111)
            {
                if (SDopts.opts.APIcomm1111) // 
                {
                    if (changeAPIcom1111)
                        if (sdScriptUC.IsConnected) sdScriptUC.reStartServers(true);
                    sd_api_uc.Visibility = Visibility.Visible; sdScriptUC.Visibility = Visibility.Collapsed;
                    sd_api_uc.Init(ref SDopts, ref genOpts); sd_api_uc.OnLog += new Utils.LogHandler(Log);
                }
                else
                {
                    if (changeAPIcom1111) sd_api_uc.Finish();
                    sdScriptUC.Visibility = Visibility.Visible; sd_api_uc.Visibility = Visibility.Collapsed;                    
                    sdScriptUC.Init(ref SDopts); 
                }
                lastAPIcom1111 = SDopts.opts.APIcomm1111;
                var ap = OnAPIparams(SDopts.opts.APIcomm1111);
            }
            tempRegulator.opts2Visual();
            PossibleRunServer();
        }
        private bool PossibleRunServer()
        {
            if (!sd_api_uc.active)
            {
                if (genOpts.composer.A1111)
                {
                    btnRunServer.IsEnabled = SDopts.IsSDloc1111(SDopts.opts.SDloc1111, false);
                }
                else 
                {
                    btnRunServer.IsEnabled = SDopts.IsSDlocComfy(SDopts.opts.SDlocComfy, false); 
                }
            }
            else btnRunServer.IsEnabled = false; // the server is running
            if (btnRunServer.IsEnabled) { btnRunServer.Foreground = Brushes.DarkRed; btnRunServer.BorderBrush = Brushes.DarkRed; }
            else { btnRunServer.Foreground = Brushes.DarkGray; btnRunServer.BorderBrush = Brushes.DarkGray; }
            return btnRunServer.IsEnabled;
        }
        public void Finish()
        {
            tempRegulator.Finish();
            sd_api_uc.Finish(); sdScriptUC.Finish();
            if (SDopts.opts.autoCloseCmd && genOpts.general.AppTerminating) closeProcess();
            SDopts.keepOpen = false; SDopts.Close(); SDopts = null;
        }
        public void Broadcast(string msg)
        {
            if (SDopts.opts.APIcomm1111) return;
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
                if (SDopts.opts.APIcomm1111) return sd_api_uc.active;
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
            if (SDopts.opts.APIcomm1111) // API
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
                ii = new ImageInfo() { filename = filename, imageGenerator = ImageInfo.ImageGenerator.StableDiffusion };
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
            bool bb = SDopts.opts.APIcomm1111;
            SDopts.opts2Visual(); SDopts.ShowDialog();
            opts2Visual(bb != SDopts.opts.APIcomm1111);
        }
        private void closeProcess()
        {
            try
            {
                if (process4batch1111 != null)
                {
                    if (process4batch1111.HasExited)
                    {
                        Console.WriteLine($"Process 1111 exited with code: {process4batch1111.ExitCode}");
                        Console.WriteLine($"Process 1111 exit time: {process4batch1111.ExitTime}");
                        return;
                    }                   
                    if (!process4batch1111.CloseMainWindow())
                    {
                        //If the main window cannot be closed, kill the process
                        process4batch1111.Kill();
                    }
                }
                if (process4batchComfy != null)
                {
                    if (process4batchComfy.HasExited)
                    {
                        Console.WriteLine($"Process Comfy exited with code: {process4batchComfy.ExitCode}");
                        Console.WriteLine($"Process Comfy exit time: {process4batchComfy.ExitTime}");
                        return;
                    }
                    if (!process4batchComfy.CloseMainWindow())
                    {
                        //If the main window cannot be closed, kill the process
                        process4batchComfy.Kill();
                    }
                }
            }
            catch (Exception ex) { Log("Error: SD server -> " + ex.Message); } 
        }
        private void btnRunServer_Click(object sender, RoutedEventArgs e)
        {
            if (genOpts.composer.A1111) 
            { 
                if (!SDopts.IsSDloc1111(SDopts.opts.SDloc1111, true)) return; 
                process4batch1111 = Utils.RunBatchFile(SDopts.opts.SDloc1111);
                if (process4batch1111.HasExited)
                {
                    Console.WriteLine($"Process exited with code: {process4batch1111.ExitCode}");
                    Console.WriteLine($"Process exit time: {process4batch1111.ExitTime}");                
                }
            }
            else 
            { 
                if (!SDopts.IsSDlocComfy(SDopts.opts.SDlocComfy, true)) return;
                process4batchComfy = Utils.RunBatchFile(SDopts.opts.SDlocComfy);
                        
                if (process4batchComfy.HasExited)
                {
                    Console.WriteLine($"Process Comfy exited with code: {process4batchComfy.ExitCode}");
                    Console.WriteLine($"Process Comfy exit time: {process4batchComfy.ExitTime}");
                    return;
                }
            }
        }
    }
}

