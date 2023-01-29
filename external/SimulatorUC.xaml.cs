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
using Path = System.IO.Path;
using scripthea.master;

namespace scripthea.external
{
    public static class SimulFolder
    {
        public static string imageSimulFolder
        {
            get { return Path.Combine(ImgUtils.defaultImageDepot,"Simulator"); }
        }           
        public static string RandomImageFile
        {
            get 
            { 
                List<string> orgFiles = new List<string>(Directory.GetFiles(imageSimulFolder, "*.png"));
                if (orgFiles.Count.Equals(0)) throw new Exception("Wrong simulator image folder ->" + imageSimulFolder);
                Random rnd = new Random(Convert.ToInt32(DateTime.Now.TimeOfDay.TotalSeconds));
                return orgFiles[rnd.Next(orgFiles.Count - 1)];                 
            }
        }
    }
    /// <summary>
    /// Interaction logic for SimulatorUC.xaml
    /// </summary>
    public partial class SimulatorUC : UserControl, interfaceAPI
    {
        public SimulatorUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();                    
        }
        public Dictionary<string, string> opts { get; set; } 

        public void Init(string prompt) { }
        public void Finish() { }
        public bool isDocked { get { return false; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get { return true; } }
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename)
        {
            if (Directory.Exists(imageDepotFolder)) opts["folder"] = imageDepotFolder;
            else opts["folder"] = ImgUtils.defaultImageDepot;

            Utils.Sleep(10000);

            filename = Path.ChangeExtension(Utils.timeName(), ".png");
            File.Copy(SimulFolder.RandomImageFile, Path.Combine(opts["folder"], filename));       
            return true;
        }        
        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            imgSimul.Source = ImgUtils.UnhookedImageLoad(SimulFolder.RandomImageFile);
        }
    }
}
