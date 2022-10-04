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
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using scripthea.viewer;
using scripthea.composer;
using UtilsNS;

namespace scripthea
{
    public class Options
    {
        public int Left;
        public int Top;
        public int Height;
        public int Width;
        public int LogColWidth;
        public bool LogColWaveSplit;
        public int QueryColWidth;
        public int ViewColWidth;       

        public bool SingleAuto;
        public bool OneLineCue;

        public string ImageDepotFolder;
        public string ModifPrefix;
        public string API;

        public int ThumbZoom;
        public bool ThumbCue;
        public bool ThumbFilename;
    }

    /// <summary>
    /// Scripthea is a prompt composer as utility for text-to-image AI generators
    /// </summary>
    public partial class MainWindow : Window
    {
        AboutWin aboutWin;
        public MainWindow()
        {
            aboutWin = new AboutWin();
            aboutWin.Show();

            InitializeComponent();           
        }       
        string optionsFile;
        public Options opts;

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            optionsFile = Utils.configPath + "options.json";
            if (File.Exists(optionsFile))
            {
                string json = System.IO.File.ReadAllText(optionsFile);
                opts = JsonConvert.DeserializeObject<Options>(json);
                if (opts.ImageDepotFolder.Equals("<default.image.depot>")) opts.ImageDepotFolder = queryUC.defaultImageFolder;
            }
            else opts = new Options();

            queryUC.OnLog += new QueryUC.LogHandler(Log);
            viewerUC.OnLog += new ViewerUC.LogHandler(Log);
            craiyonImportUC.OnLog += new CraiyonImportUC.LogHandler(Log);

            queryUC.Init(ref opts);
            viewerUC.Init(ref opts);
            craiyonImportUC.Init();

            Left = opts.Left;
            Top = opts.Top;
            Width = opts.Width;
            Height = opts.Height;
            Width = opts.Width;
            pnlLog.Width = new GridLength(opts.LogColWidth);
            gridSplitLeft_MouseDoubleClick(null, null);
            pnlLogImage.Height = new GridLength(opts.LogColWidth); 

            oldTab = tiComposer;
            Title = "Scripthea - text-to-image prompt composer v" + Utils.getAppFileVersion + "  ";  
            Log("> Welcome to Scrpithea"); Log("");
            if (opts.SingleAuto) queryUC.btnCompose_Click(null, null);

            aboutWin.Hide();
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {            
            opts.Left = Convert.ToInt32(Left);
            opts.Top = Convert.ToInt32(Top);
            opts.Width = Convert.ToInt32(Width);
            opts.Height = Convert.ToInt32(Height);
            opts.LogColWidth = Convert.ToInt32(pnlLog.ActualWidth);
            queryUC.Finish();
            viewerUC.Finish();
            if (!Utils.isNull(aboutWin)) aboutWin.Close();
 
            string json = JsonConvert.SerializeObject(opts);
            System.IO.File.WriteAllText(optionsFile, json);
        }
        private DispatcherTimer dTimer;
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLogger.Document.Blocks.Clear();
        }
        public void Log(string msg, SolidColorBrush clr = null)
        {
            try 
            { 
                if (msg.Length > 7)
                    switch (msg.Substring(0,8))
                    {
                        case "@StartPr": // StartProc
                            if (Utils.isNull(dTimer))
                            {
                                dTimer = new DispatcherTimer();
                                dTimer.Tick += new EventHandler(dTimer_Tick);
                                dTimer.Interval = new TimeSpan(200*10000);
                            }
                            dti = 0; dTimer.Start(); return;
                        
                        case "@EndProc":
                            if (Utils.isNull(dTimer)) return;
                            dTimer.Stop(); lbProcessing.Content = "";
                            string fn = msg.Substring(9);
                            if (File.Exists(fn)) // success
                            {
                                imgLast.Source = (new BitmapImage(new Uri(fn))).Clone();
                            } return;
                        case "@WorkDir":
                            if (Directory.Exists(opts.ImageDepotFolder))
                                tbImageDepot.Text = "working image depot -> " + opts.ImageDepotFolder;
                            return;
                    }
                if (chkLog.IsChecked.Value) Utils.log(tbLogger, msg, clr);
            }
            finally { Utils.DoEvents(); }
        }
        int dti;
        private void dTimer_Tick(object sender, EventArgs e)
        {
            string ch = "";
            switch (dti % 4)
            {
                case 0: ch = "--";
                    break;
                case 1: ch = " \\";
                    break;
                case 2: ch = " |";
                    break;
                case 3: ch = " /";
                    break;
            }
            dti++;
            lbProcessing.Content = ch;
        }
        TabItem oldTab;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != tabControl) return;
            if (tabControl.SelectedItem.Equals(tiViewer))
            {
                if (oldTab.Equals(tiComposer)) viewerUC.tbImageDepot.Text = queryUC.tbImageDepot.Text;
                if (oldTab.Equals(tiUtils)) viewerUC.tbImageDepot.Text = craiyonImportUC.imageFolder;
            }
            oldTab = (TabItem)tabControl.SelectedItem;
        }

        private void imgAbout_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(aboutWin)) aboutWin = new AboutWin();
            aboutWin.ShowDialog();            
        }
        private void gridSplitLeft_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!Utils.isNull(sender)) opts.LogColWaveSplit = !opts.LogColWaveSplit;
            if (opts.LogColWaveSplit)
            {
                gridSplitLeft.Visibility = Visibility.Visible;
                gridSplitLeft2.Visibility = Visibility.Collapsed;
                tabControl.Margin = new Thickness(17, 0, 0, 0);
            }
            else
            {
                gridSplitLeft.Visibility = Visibility.Collapsed;
                gridSplitLeft2.Visibility = Visibility.Visible;
                tabControl.Margin = new Thickness(5, 0, 0, 0);
            }
        }
        private void MainWindow1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.F1)) Utils.CallTheWeb("https://scripthea.sicyon.com");
        }
    }
}
