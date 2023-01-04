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
using scripthea.external;
using scripthea.master;
using UtilsNS;

namespace scripthea
{
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
            //Utils.baseLocation = Utils.BaseLocation.appData;
            InitializeComponent();
        }
        string optionsFile;
        public Options opts;
        public PreferencesWindow prefsWnd;

        public FocusControl focusControl;
        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            optionsFile = Utils.configPath + "options.json";
            if (!Directory.Exists(Utils.configPath)) throw new Exception("Fatal error: Directory <" + Utils.configPath + "> does not exist.");
            if (File.Exists(optionsFile))
            {
                string json = System.IO.File.ReadAllText(optionsFile);
                opts = JsonConvert.DeserializeObject<Options>(json);
                if (opts.ImageDepotFolder.Equals("<default.image.depot>")) opts.ImageDepotFolder = ImgUtils.defaultImageDepot;
            }
            else opts = new Options();
            prefsWnd = new PreferencesWindow();
            Title = "Scripthea - options loaded";
            dirTreeUC.Init();
            dirTreeUC.OnActive += new DirTreeUC.SelectHandler(Active);

            queryUC.OnLog += new Utils.LogHandler(Log); queryUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            viewerUC.OnLog += new Utils.LogHandler(Log); viewerUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            importUtilUC.OnLog += new Utils.LogHandler(Log); importUtilUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);

            Title = "Scripthea - loading text files...";
            queryUC.Init(ref opts);
            Title = "Scripthea - text files loaded";
            viewerUC.Init(ref opts);
            depotMaster.Init(ref opts);
            importUtilUC.Init();
            exportUtilUC.Init(ref opts);

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

            focusControl = new FocusControl();
            focusControl.Register(importUtilUC);
            focusControl.Register(exportUtilUC.iPicker);
            focusControl.Register(queryUC);
            focusControl.Register(viewerUC);
            focusControl.Register(depotMaster.iPickerA);
            focusControl.Register(depotMaster.iPickerB);

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
            if (!Utils.isNull(prefsWnd))
            {
                prefsWnd.keepOpen = false; prefsWnd.Close();
            }
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
                            if (Directory.Exists(opts.ImageDepotFolder))
                            {
                                dirTreeUC.CatchAFolder(opts.ImageDepotFolder);
                                tbImageDepot.Text = "working image depot -> " + opts.ImageDepotFolder;
                            }
                            else tbImageDepot.Text = "working image depot -> <NOT SET>";
                            return;
                        case "@Explore":
                            string[] sa = msg.Split('='); if (sa.Length != 2) Utils.TimedMessageBox("Error(#458)");
                            ExplorerPart = Convert.ToInt32(sa[1]);
                            return;
                        case "@_Header":
                            string[] sb = msg.Split('='); if (sb.Length != 2) Utils.TimedMessageBox("Error(#459)");
                            Title = "Scripthea - "+sb[1];
                            return;
                    }
                if (chkLog.IsChecked.Value)
                    if (ExplorerPart.Equals(100)) Utils.TimedMessageBox(msg,"Warning",3500);
                    else Utils.log(tbLogger, msg, clr);
            }
            finally
            {
                if (msg.Length > 0 && msg.Substring(0, 1).Equals("@") && Utils.isInVisualStudio && !ExplorerPart.Equals(100)) Utils.log(tbLogger, msg, clr);
                
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
                if (oldTab.Equals(tiComposer)) viewerUC.ShowImageDepot(queryUC.tbImageDepot.Text);
                if (oldTab.Equals(tiUtils)) viewerUC.ShowImageDepot(importUtilUC.imageFolder);
                ExplorerPart = 100; dirTreeUC.CatchAFolder(viewerUC.tbImageDepot.Text);
            }
            if (tabControl.SelectedItem.Equals(tiDepotMaster))
            {
                ExplorerPart = 100;
            }
            if (tabControl.SelectedItem.Equals(tiUtils))
            {
                ExplorerPart = 100; dirTreeUC.CatchAFolder(importUtilUC.imageFolder);
            }
            oldTab = (TabItem)tabControl.SelectedItem;
            e.Handled = true;
        }
        protected void Active(string path)
        {
            focusControl.ifc.textFolder.Text = path;            
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
                if (e.Key.Equals(Key.F1)) Utils.CallTheWeb("https://scripthea.com");
            if (e.Key.Equals(Key.Enter))
            {
                string fld = "";
                if (sender.Equals(queryUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiComposer)) fld = queryUC.tbImageDepot.Text;
                if (sender.Equals(viewerUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiViewer)) fld = viewerUC.tbImageDepot.Text;
                if (sender.Equals(importUtilUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiUtils)) fld = importUtilUC.tbImageDepot.Text;
                if (ImgUtils.checkImageDepot(fld) > 0) 
                    dirTreeUC.CatchAFolder(fld);
            }
        }
        private void imgPreferences_MouseDown(object sender, MouseButtonEventArgs e)
        {
            prefsWnd.ShowDialog();
        }

    }
}
