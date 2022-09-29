using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for SimulatorUC.xaml
    /// </summary>
    public partial class SimulatorUC : UserControl, interfaceAPI
    {
        private string imageSimulFolder
        {
            get
            {               
                return Utils.basePath + "\\images\\Simulator\\";
            }
        }        
        public SimulatorUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();                    
        }

        public Dictionary<string, string> opts { get; set; } 

        public void ShowAccess(string prompt) // updsate visual from opts
        {
                      
        }
        public bool isEnabled
        {
            get { return true; }
        }
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename)
        {
            opts["folder"] = imageDepotFolder; Utils.Sleep(4000);
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageSimulFolder, "c*.png"));
            if (orgFiles.Count.Equals(0)) throw new Exception("Wrong simulator image folder ->" + imageSimulFolder);
            Random rnd = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
            string fn = orgFiles[rnd.Next(orgFiles.Count-1)];
            filename = Utils.timeName() + ".png"; 
            File.Copy(fn, imageDepotFolder + filename);       
            return true;
        }        

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageSimulFolder, "c*.png"));
            if (orgFiles.Count.Equals(0)) throw new Exception("Wrong simulator image folder ->" + imageSimulFolder);
            Random rnd = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
            string fn = orgFiles[rnd.Next(orgFiles.Count - 1)];
            if (File.Exists(fn))
            {
                var bitmap = new BitmapImage(new Uri(fn));
                imgSimul.Source = bitmap;
            }
        }
    }
}
