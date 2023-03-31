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
using UtilsNS;


namespace scripthea.external
{
    /// <summary>
    /// Common interface to all APIs
    /// </summary>
    public interface interfaceAPI
    {
        Dictionary<string, string> opts { get; set; } // visual adjustable options to that particular API, keep it in synchro with the visuals 
        void Init(string prompt);
        void Finish();
        event Utils.LogHandler OnLog;
        bool isDocked { get; }
        UserControl userControl { get; }
        bool isEnabled { get; } // connected and working (depends on the API)
        bool GenerateImage(string prompt, string imageDepotFolder, out string filename); // returns the filename of saved in _ImageDepoFolder image 
    }
    /// <summary>
    /// Interaction logic for controlAPI.xaml
    /// </summary>
    public partial class ControlAPI : Window
    {
        Dictionary<string, interfaceAPI> interfaceAPIs;
        public ControlAPI()
        {
            InitializeComponent();
            interfaceAPIs = new Dictionary<string, interfaceAPI>();
            visualControl("Simulation", new SimulatorUC()).OnLog += new Utils.LogHandler(Log);
            //visualControl("DeepAI", new DeepAIUC()); Later plugin...
            visualControl("Craiyon", new CraiyonWebUC()).OnLog += new Utils.LogHandler(Log); 
            visualControl("SDiffusion", new SDiffusionUC()).OnLog += new Utils.LogHandler(Log); 
            _activeAPIname = "Simulation"; tabControl.SelectedIndex = 0;

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
        private interfaceAPI visualControl(string APIname, UserControl uc)
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
                if (interfaceAPIs.ContainsKey(value)) _activeAPIname = value;
                else Utils.TimedMessageBox("No API: " + value);
            }
        }
        public interfaceAPI activeAPI { get { return interfaceAPIs[activeAPIname]; } }

        private string prompt2api, imageFolder, imageName;  bool success = false;      

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            success = activeAPI.GenerateImage(prompt2api, imageFolder, out imageName); // calling API
        }
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (activeAPIname)
            {
                case "Simulation":
                    QueryComplete(Path.Combine(imageFolder, imageName), true);
                    break;
                case "Craiyon":
                    QueryComplete("", true);
                    break;
                case "SDiffusion": // take imageFolder, imageName (filename) and success
                    if (success)
                    {
                        if (!Directory.Exists(imageFolder)) { Log("Error: folder not found"); return; }
                        if (File.Exists(Path.Combine(imageFolder, imageName)))
                        {
                            string desc = Path.Combine(imageFolder, ImgUtils.descriptionFile);
                            if (!File.Exists(desc)) // create an empty iDepot 
                            {
                                ImageDepot df = new ImageDepot(imageFolder, ImageInfo.ImageGenerator.StableDiffusion, true);
                                df.Save(true); df = null;
                            }
                            using (StreamWriter sw = File.AppendText(desc))
                            {
                                ImageInfo ii = new ImageInfo(Path.Combine(imageFolder, imageName), ImageInfo.ImageGenerator.StableDiffusion, true);
                                if (ii.IsEnabled())sw.WriteLine(ii.To_String());
                                else Log("Error: wrong image file");                                
                            }
                            QueryComplete(Path.Combine(imageFolder, imageName), true); // hooray ;)
                        }
                        else { Log("Error: image file lost"); return; } 
                    }
                    else QueryComplete(Path.Combine(imageFolder, imageName), false); // sadly :(   
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
        public void about2Show(string prompt)
        {
            Dictionary<string, string> tempOpts = new Dictionary<string, string>(activeAPI.opts);
            activeAPI.Init(prompt); 
            foreach (TabItem ti in tabControl.Items)
            {               
                if (ti.Header.Equals(activeAPIname))
                {
                    tabControl.SelectedItem = ti; break;
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
            if (!Utils.isNull(activeAPI)) activeAPI.Finish();
            e.Cancel = eCancel; Hide();
        }
    }

}
