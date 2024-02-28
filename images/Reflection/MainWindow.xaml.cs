using System;
using System.Collections.Generic;
using System.IO;
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
using UtilsNS;
using Path = System.IO.Path;

namespace Reflection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        string exePath = Path.GetDirectoryName(Utils.appFullPath);
        private void btnMakeImage_Click(object sender, RoutedEventArgs e)
        {
            string prmsFile = Path.Combine(exePath, "parameters.json");
            if (!File.Exists(prmsFile)) { Utils.TimedMessageBox(prmsFile + " is missing"); return; }
            string prmsTxt = File.ReadAllText(prmsFile);
            tbLog.Text = "Input Parameters \n\n"+prmsTxt;
            string imgCollection = Path.Combine(Utils.GetBaseLocation(Utils.BaseLocation.oneUp), "simulator");
            tbLog.Text += "\n Image collection folder: "+ imgCollection;
            List<string> files = new List<string>(Directory.GetFiles(imgCollection, "*.png"));
            Random rnd = new Random();  int i = rnd.Next(files.Count - 1);
            string newImage = Path.Combine(exePath,Path.ChangeExtension(Utils.timeName(), ".png"));
            File.Copy(files[i],  newImage);
            tbLog.Text += "\n\n New image: " + newImage;
        }
    }
}
