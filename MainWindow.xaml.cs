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
        public int QueryRowHeight;
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
                if (opts.ImageDepotFolder.Equals("<default.image.depot>")) opts.ImageDepotFolder = ImgUtils.defaultImageDepot;
            }
            else opts = new Options();

            dirTreeUC.Init();
            dirTreeUC.OnActive += new DirTreeUC.SelectHandler(Active);

            queryUC.OnLog += new QueryUC.LogHandler(Log); queryUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            viewerUC.OnLog += new ViewerUC.LogHandler(Log); viewerUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            craiyonImportUC.OnLog += new CraiyonImportUC.LogHandler(Log); craiyonImportUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);

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
            rowLogImage.Height = new GridLength(opts.LogColWidth);

            oldTab = tiComposer;
            Title = "Scripthea - text-to-image prompt composer v" + Utils.getAppFileVersion;
            Log("> Welcome to Scripthea" + "  " + (Utils.isInVisualStudio ? "(within VS)" : "")); Log("");
            if (opts.SingleAuto) queryUC.btnCompose_Click(null, null);

            Log("@ExplorerPart=0");
            aboutWin.Hide();
        }
        private int _ExplorerPart;
        public int ExplorerPart // from 0 to 100%
        {
            get { return _ExplorerPart; }
            set
            {
                int vl = Utils.EnsureRange(value, 0, 100);
                _ExplorerPart = vl;
                rowLog.Height = new GridLength(100 - vl, GridUnitType.Star);
                rowExplorer.Height = new GridLength(vl, GridUnitType.Star);
                if (vl.Equals(0) || vl.Equals(100)) gridSplitLeft2.Visibility = Visibility.Collapsed;
                else gridSplitLeft2.Visibility = Visibility.Visible;
                if (vl != 100 && vl != 0 && !Utils.isNull(opts))
                {
                    if (vl > 70) //partly shown
                    {
                        gridSplitLog.Visibility = Visibility.Collapsed; rowLogImage.Height = new GridLength(0);
                    }
                    else
                    {
                        gridSplitLeft.Visibility = Visibility.Visible; rowLogImage.Height = new GridLength(Utils.EnsureRange(opts.LogColWidth, 100, gridLog.Height * 0.66));
                    }
                }
            }
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
                    switch (msg.Substring(0, 8))
                    {
                        case "@StartPr": // StartProc
                            if (Utils.isNull(dTimer))
                            {
                                dTimer = new DispatcherTimer();
                                dTimer.Tick += new EventHandler(dTimer_Tick);
                                dTimer.Interval = new TimeSpan(200 * 10000);
                            }
                            dti = 0; dTimer.Start(); return;

                        case "@EndProc":
                            if (Utils.isNull(dTimer)) return;
                            dTimer.Stop(); lbProcessing.Content = "";
                            string fn = msg.Substring(9);
                            if (File.Exists(fn)) // success
                            {
                                imgLast.Source = (new BitmapImage(new Uri(fn))).Clone();
                            }
                            return;
                        case "@WorkDir":
                            if (ImgUtils.checkImageDepot(opts.ImageDepotFolder,false))
                            {
                                dirTreeUC.CatchAFolder(opts.ImageDepotFolder);
                                tbImageDepot.Text = "working image depot -> " + opts.ImageDepotFolder;
                            }
                            return;
                        case "@Explore":
                            string[] sa = msg.Split('='); if (sa.Length != 2) Utils.TimedMessageBox("Error(#458)");
                            ExplorerPart = Convert.ToInt32(sa[1]);
                            return;

                    }
                if (chkLog.IsChecked.Value)
                    if (ExplorerPart.Equals(100)) Utils.TimedMessageBox(msg);
                    else Utils.log(tbLogger, msg, clr);
            }
            finally
            {
                if (msg.Length > 0 && msg.Substring(0, 1).Equals("@") && Utils.isInVisualStudio && !ExplorerPart.Equals(100)) Utils.log(tbLogger, msg, clr);
                Utils.DoEvents();
            }
        }
        int dti;
        private void dTimer_Tick(object sender, EventArgs e)
        {
            string ch = "";
            switch (dti % 4)
            {
                case 0:
                    ch = "--";
                    break;
                case 1:
                    ch = " \\";
                    break;
                case 2:
                    ch = " |";
                    break;
                case 3:
                    ch = " /";
                    break;
            }
            dti++;
            lbProcessing.Content = ch;
        }
        TabItem oldTab;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != tabControl) return;
            if (tabControl.SelectedItem.Equals(tiComposer))
            {
                if (queryUC.tcQuery.SelectedItem.Equals(queryUC.tiOptions))
                {
                    ExplorerPart = 100; dirTreeUC.CatchAFolder(queryUC.tbImageDepot.Text);
                }
                else ExplorerPart = 0;
            }
            if (tabControl.SelectedItem.Equals(tiViewer))
            {
                if (oldTab.Equals(tiComposer)) viewerUC.tbImageDepot.Text = queryUC.tbImageDepot.Text;
                if (oldTab.Equals(tiUtils)) viewerUC.tbImageDepot.Text = craiyonImportUC.imageFolder;
                ExplorerPart = 100; dirTreeUC.CatchAFolder(viewerUC.tbImageDepot.Text);
            }
            if (tabControl.SelectedItem.Equals(tiUtils))
            {
                ExplorerPart = 100; dirTreeUC.CatchAFolder(craiyonImportUC.imageFolder);
            }
            oldTab = (TabItem)tabControl.SelectedItem;

        }
        protected void Active(string path)
        {
            if (tabControl.SelectedItem.Equals(tiComposer))
            {
                queryUC.tbImageDepot.Text = path;
            }
            if (tabControl.SelectedItem.Equals(tiViewer))
            {
                viewerUC.tbImageDepot.Text = path;
            }
            if (tabControl.SelectedItem.Equals(tiUtils))
            {
                craiyonImportUC.imageFolder = path; craiyonImportUC.btnNewFolder_Click(null, null);
            }
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
            if (sender.Equals(MainWindow1))
                if (e.Key.Equals(Key.F1)) Utils.CallTheWeb("https://scripthea.sicyon.com");
            if (e.Key.Equals(Key.Enter))
            {
                string fld = "";
                if (sender.Equals(queryUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiComposer)) fld = queryUC.tbImageDepot.Text;
                if (sender.Equals(viewerUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiViewer)) fld = viewerUC.tbImageDepot.Text;
                if (sender.Equals(craiyonImportUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiUtils)) fld = craiyonImportUC.tbImageDepot.Text;
                if (ImgUtils.checkImageDepot(fld)) 
                    dirTreeUC.CatchAFolder(fld);
            }
        }
    }
}
