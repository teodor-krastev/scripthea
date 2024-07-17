using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using scripthea.options;
using UtilsNS;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for DepotStastsUC.xaml
    /// </summary>
    public partial class DepotStastsUC : UserControl
    {
        public DepotStastsUC()
        {
            InitializeComponent();
        }
        protected Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        protected ImageDepot iDepot;
        public void Refresh(string imageFolder)
        {
            string iFolder = opts.composer.ImageDepotFolder;
            if (Directory.Exists(imageFolder)) iFolder = imageFolder;
            if (!Directory.Exists(iFolder)) { Utils.TimedMessageBox("Error[792]: folder <" + iFolder + "> - not found."); iDepot = null; }
            iDepot = new ImageDepot(iFolder);
            Refresh(ref iDepot);
        }
        public void Clear()
        {

        }
        protected double StdDev(double[] numbers)
        {
            double mean = numbers.Average();
            double sumOfSquaresOfDifferences = numbers.Select(val => (val - mean) * (val - mean)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / numbers.Length);
        }
        public void Refresh(ref ImageDepot _iDepot)
        {
            Clear(); 
            iDepot = _iDepot; if (iDepot == null) return; 
        }
        public List<Tuple<string, double>> FolderStats()
        {
            List<Tuple<string, double>> ls = new List<Tuple<string, double>>();
            // folder size kB and Mb

            // number of image files and iDepot images

            // 
            return ls;
        }
        public List<Tuple<string, double, double, double, double>> ImagesStats()
        {
            List<Tuple<string, double, double, double, double>> ls = new List<Tuple<string, double, double, double, double>>();
            // image size in kB

            // image size in pixels

            // rating

            // prompt size

            // steps
        
            // cfg_scale
           
            // denoising_strength


            return ls;
        }
        // potentially 

    }
}
