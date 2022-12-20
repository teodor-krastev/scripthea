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
            visualControl("Simulation", new SimulatorUC());
            //visualControl("DeepAI", new DeepAIUC()); Later plugin...
            visualControl("Craiyon", new CraiyonWebUC());
            visualControl("SDiffusion", new SDiffusionUC());
            _activeAPIname = "Simulation"; tabControl.SelectedIndex = 0;

            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.WorkerReportsProgress = true;
        }
        protected BackgroundWorker backgroundWorker1;
        public bool IsBusy { get { return backgroundWorker1.IsBusy; } }

        public delegate void APIEventHandler(string imageFilePath, bool success);
        public event APIEventHandler OnQueryComplete;
        protected void QueryComplete(string imageFilePath, bool success)
        {
            if ((OnQueryComplete != null)) OnQueryComplete(imageFilePath, success);
        }
        private void visualControl(string APIname, UserControl uc)
        {
            uc.Name = APIname.ToLower() + "UC"; interfaceAPIs[APIname] = (interfaceAPI)uc; 
            if (!interfaceAPIs[APIname].isDocked)
            {
                TabItem ti = new TabItem(); ti.Header = APIname;
                uc.Height = Double.NaN; uc.Width = Double.NaN; uc.Margin = new Thickness(0, 0, 0, 0);
                ti.Content = uc; ti.Visibility = Visibility.Collapsed; tabControl.Items.Add(ti);
            }
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
            if (activeAPIname.Equals("Craiyon"))
            {
                QueryComplete("", true); return;
            }
            if (File.Exists(imageFolder + imageName) && success)
            {
                using (StreamWriter sw = File.AppendText(imageFolder + "description.txt"))
                {
                    sw.WriteLine(imageName + "=" + prompt2api);
                }
                QueryComplete(imageFolder + imageName, true); // hooray !
            }          
            else QueryComplete(imageName, false); // sadly...           
        }
        public void Query(string prompt, string _imageDepoFolder) // fire event at the end
        {
            if (IsBusy) return;
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
