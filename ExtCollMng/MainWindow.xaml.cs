using System;
using System.Collections.Generic;
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
using UtilsNS;

namespace ExtCollMng
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
        // command line option, to start from some collection --coll:<coll.name>
        private void ExtCollMngMain_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "Scripthea External Collections Manager v" + Utils.getAppFileVersion;
            string[] args = Environment.GetCommandLineArgs();
            string coll = "";
            if (args.Length > 1) // Check if any arguments were provided
            {
                for (int i = 1; i < args.Length; i++)
                {
                    string argument = args[i];
                    if (argument.StartsWith("--coll:")) coll = argument.Substring(7); // set active collection
                }
            }
            ecmUC.Init(new Utils.LogHandler(Log), ""); ecmUC.Mute = !chkLog.IsChecked.Value;
            ecmUC.UpdateCollInfo();
        }
        public void Log(string txt, SolidColorBrush clr = null)
        {
            Utils.log(rtbLog, txt, clr);
        }
        private void chkLog_Checked(object sender, RoutedEventArgs e)
        {
            if (ecmUC is null) return;
            ecmUC.Mute = !chkLog.IsChecked.Value;
        }
    }
}
