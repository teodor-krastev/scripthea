using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
using scripthea.viewer;
using scripthea.master;
using scripthea.options;
using UtilsNS;

namespace scripthea.external
{
    public delegate Dictionary<string, object> APIparamsHandler(bool? showIt);

    /// <summary>
    /// Common interface to all APIs
    /// </summary>
    public interface interfaceAPI
    {
        Dictionary<string, string> opts { get; set; } // visual adjustable options to that particular API, keep it in synchro with the visuals 
        void Init(ref Options _opts); // coupled with Finish
        void Finish(); // coupled with Init (not a destructor)
        void Broadcast(string msg);
        event Utils.LogHandler OnLog; // @ is for internal comm
        event APIparamsHandler APIparamsEvent;
        bool isDocked { get; }
        UserControl userControl { get; }
        bool isEnabled { get; } // connected and working (depends on the API)
        bool GenerateImage(string prompt, string imageDepotFolder, ref ImageInfo ii); // returns ImageInfo of saved in imageDepoFolder image 
    }
    /// <summary>
    /// Interaction logic for controlAPI.xaml
    /// </summary>
    public partial class ControlAPI : Window
    {
        public Dictionary<string, interfaceAPI> interfaceAPIs;
        protected Options opts;
        public ControlAPI(ref Options _opts)
        {
            InitializeComponent(); opts = _opts;
            interfaceAPIs = new Dictionary<string, interfaceAPI>();
            visualControl("Simulation", new SimulatorUC()).OnLog += new Utils.LogHandler(Log);
            visualControl("AddonGen", new AddonGenUC()).OnLog += new Utils.LogHandler(Log);
            visualControl("Craiyon", new CraiyonWebUC()).OnLog += new Utils.LogHandler(Log);
            visualControl("SDiffusion", new SDiffusionUC()).OnLog += new Utils.LogHandler(Log); 
            _activeAPIname = "AddonGen"; tabControl.SelectedIndex = 0;

            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.WorkerReportsProgress = true;
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }

        protected BackgroundWorker backgroundWorker1;
        public bool IsBusy { get { return backgroundWorker1.IsBusy; } }

        public delegate void APIEventHandler(string imageFilePath, bool success);
        public event APIEventHandler OnQueryComplete;
        protected void QueryComplete(string imageFilePath, bool success)
        {
            if ((OnQueryComplete != null)) OnQueryComplete(imageFilePath, success);
        }
        private interfaceAPI visualControl(string APIname, UserControl uc) // add iAPI
        {
            uc.Name = APIname.ToLower() + "UC"; interfaceAPIs.Add(APIname, (interfaceAPI)uc); 
            if (!interfaceAPIs[APIname].isDocked)
            {
                TabItem ti = new TabItem(); ti.Header = APIname;
                uc.Height = Double.NaN; uc.Width = Double.NaN; uc.Margin = new Thickness(0, 0, 0, 0);
                ti.Content = uc; ti.Visibility = Visibility.Collapsed; tabControl.Items.Add(ti);
            }
            return interfaceAPIs[APIname];
        }
        private string _activeAPIname;
        public string activeAPIname
        {
            get { return _activeAPIname; }
            set
            {
                if (!interfaceAPIs.ContainsKey(value)) { Utils.TimedMessageBox("No API: " + value); return; }                
                interfaceAPIs[_activeAPIname]?.Finish(); 
                _activeAPIname = value; interfaceAPIs[_activeAPIname]?.Init(ref opts);
            }
        }
        public interfaceAPI activeAPI { get { return interfaceAPIs[activeAPIname]; } }

        private string prompt2api, imageFolder; bool success = false; ImageInfo iInfo;     

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            iInfo = new ImageInfo();
            success = activeAPI.GenerateImage(prompt2api, imageFolder, ref iInfo); // calling API
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (activeAPIname)
            {
                case "Simulation":
                    QueryComplete(Path.Combine(imageFolder, iInfo.filename), true);
                    break;
                case "Craiyon":
                    QueryComplete("", true);
                    break;
                case "AddonGen":
                    //QueryComplete(Path.Combine(imageFolder, iInfo.filename), success);
                    //break;
                case "SDiffusion": // take imageFolder, imageName (filename) and success
                    if (success)
                    {
                        if (!Directory.Exists(imageFolder)) { Log("Error[348]: folder not found"); return; }
                        if (File.Exists(Path.Combine(imageFolder, iInfo.filename)))
                        {
                            string desc = Path.Combine(imageFolder, SctUtils.descriptionFile);
                            if (!File.Exists(desc)) // create an empty iDepot 
                            {
                                ImageDepot df = new ImageDepot(imageFolder, activeAPIname.Equals("SDiffusion") ? ImageInfo.ImageGenerator.StableDiffusion : ImageInfo.ImageGenerator.AddonGen, true);
                                df.Save(true); df = null;
                            }
                            using (StreamWriter sw = File.AppendText(desc))
                            {
                                bool bb = iInfo != null;
                                if (bb) bb &= iInfo.IsEnabled(); 
                                if (bb) sw.WriteLine(iInfo.To_String());
                                else Log("Error[342]: wrong image file");                                
                            }
                            QueryComplete(Path.Combine(imageFolder, iInfo.filename), true); // hooray ;)
                        }
                        else { Log("Error[195]: image file lost"); return; } 
                    }
                    else  // sadly :(   
                    {
                        if (Utils.isNull(iInfo)) { QueryComplete("", false); return; }
                        if (Utils.isNull(iInfo.filename)) { QueryComplete("", false); return; }
                        QueryComplete(Path.Combine(imageFolder, iInfo.filename), false);
                    }
                    break;
            }        
        }
        public void Query(string prompt, string _imageDepoFolder) // fire event at the end
        {
            if (IsBusy) { Log("...I'm busy"); return; }
            if (Directory.Exists(_imageDepoFolder)) imageFolder = _imageDepoFolder.EndsWith("\\") ? _imageDepoFolder : _imageDepoFolder + "\\";
            else Utils.TimedMessageBox("No directory: " + _imageDepoFolder);
            prompt2api = prompt;
            backgroundWorker1.RunWorkerAsync();
         }
        public void about2Show(ref Options _opts)
        {
            Dictionary<string, string> tempOpts = new Dictionary<string, string>(activeAPI.opts);
            activeAPI.Init(ref _opts); 
            foreach (TabItem ti in tabControl.Items)
            {               
                if (ti.Header.Equals(activeAPIname))
                {
                    tabControl.SelectedItem = ti;
                    Title = "Text-to-image generator: " + activeAPIname;
                    break;
                }
            }
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
        public bool eCancel = true;
        private void controlAPIwin_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            opts.general.AppTerminating = true;
            foreach(var api in interfaceAPIs)
            {
                api.Value?.Finish();
            }           
            e.Cancel = eCancel; Hide();
        }
    }

}
