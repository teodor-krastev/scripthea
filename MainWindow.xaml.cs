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
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using scripthea.viewer;
using scripthea.composer;
using scripthea.external;
using scripthea.master;
using UtilsNS;
using Path = System.IO.Path;
using Color = System.Drawing.Color;

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
            InitializeComponent();
        }
        string optionsFile;
        public Options opts;
        public PreferencesWindow prefsWnd;
        private Bitmap penpic;

        public FocusControl focusControl;
        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            optionsFile = Path.Combine(Utils.configPath, "options.json");
            if (!Directory.Exists(Utils.configPath)) throw new Exception("Fatal error: Directory <" + Utils.configPath + "> does not exist.");
            if (File.Exists(optionsFile))
            {
                string json = System.IO.File.ReadAllText(optionsFile);
                opts = JsonConvert.DeserializeObject<Options>(json);
                if (opts.ImageDepotFolder.Equals("<default.image.depot>")) opts.ImageDepotFolder = ImgUtils.defaultImageDepot;
            }
            else opts = new Options();
            opts.debug = Utils.isInVisualStudio || Utils.localConfig;
            prefsWnd = new PreferencesWindow();
            Title = "Scripthea - options loaded";
            dirTreeUC.Init();
            dirTreeUC.OnActive += new DirTreeUC.SelectHandler(Active);

            queryUC.OnLog += new Utils.LogHandler(Log); queryUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            viewerUC.OnLog += new Utils.LogHandler(Log); viewerUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            importUtilUC.OnLog += new Utils.LogHandler(Log); importUtilUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);

            Title = "Scripthea - loading text files...";            
            viewerUC.Init(ref opts);
            depotMaster.Init(ref opts);
            importUtilUC.Init();
            exportUtilUC.Init(ref opts);

            oldTab = tiComposer;
            Log("> Welcome to Scripthea" + "  " + (Utils.isInVisualStudio ? "(in VS)" : "")); Log("");
            Utils.DelayExec(2000, new Action(() => { aboutWin.Hide(); })); 
            queryUC.Init(ref opts);            
            
            Left = opts.Left;
            Top = opts.Top;
            Width = opts.Width;
            Height = opts.Height;
            Width = opts.Width;
            pnlLog.Width = new GridLength(opts.LogColWidth);
            gridSplitLeft_MouseDoubleClick(null, null);
            rowLogImage.Height = new GridLength(1);

            if (opts.SingleAuto) queryUC.btnCompose_Click(null, null);

            focusControl = new FocusControl();
            focusControl.Register(importUtilUC);
            focusControl.Register(exportUtilUC.iPicker);
            focusControl.Register(queryUC);
            focusControl.Register(viewerUC);
            focusControl.Register(depotMaster.iPickerA);
            focusControl.Register(depotMaster.iPickerB);
            string penpicFile = Path.Combine(Utils.configPath, "penpic1.png");
            if (!File.Exists(penpicFile)) throw new Exception(penpicFile +" file is missing");
            penpic = new Bitmap(penpicFile); imgAbout.Source = ImgUtils.BitmapToBitmapImage(penpic, System.Drawing.Imaging.ImageFormat.Png);
            SwitchExplorer(false);
            if (!opts.debug) imgPreferences.Visibility = Visibility.Collapsed;
            Title = "Scripthea - text-to-image prompt composer v" + Utils.getAppFileVersion;           
        }
        private void SwitchExplorer(bool on) // 
        {
            if (on)
            {
                if (ExplorerPart == 0) { ExplorerPart = 100; return; } // on/off situation
                if (ExplorerPart < 70) { ExplorerPart = 70; return; } // more fluid case; if it's small increase size
            }
            else ExplorerPart = 0;
        }
        public int ExplorerPart // from 0 to 100% directory tree
        {
            get { return (int)(100 * rowExplorer.Height.Value / (rowLog.Height.Value + rowExplorer.Height.Value)); } 
            set
            {
                int vl = Utils.EnsureRange(value, 0, 100);
                //_ExplorerPart = vl;

                rowLog.Height = new GridLength(100 - vl, GridUnitType.Star);
                rowExplorer.Height = new GridLength(vl, GridUnitType.Star);
                if (vl.Equals(0) || vl.Equals(100)) gridSplitLeft2.Visibility = Visibility.Collapsed;
                else gridSplitLeft2.Visibility = Visibility.Visible;
                if (vl != 100 && vl != 0 && !Utils.isNull(opts))
                {
                    if (vl > 70) //partly shown
                    {
                        gridSplitLog.Visibility = Visibility.Collapsed; rowLogImage.Height = new GridLength(1);
                    }
                    else
                    {
                        gridSplitLeft.Visibility = Visibility.Visible; //rowLogImage.Height = new GridLength(Utils.EnsureRange(opts.LogColWidth, 1, gridLog.Height * 0.66));
                    }
                }
                ExplorerPartChanging();
            }
        }
        private void ExplorerPartChanging()
        {
            chkLog.Visibility = Visibility.Visible; btnClear.Visibility = Visibility.Visible; btnRefresh.Visibility = Visibility.Visible;
            if (ExplorerPart.Equals(0)) { btnRefresh.Visibility = Visibility.Collapsed; }
            if (ExplorerPart.Equals(100)) { chkLog.Visibility = Visibility.Collapsed; btnClear.Visibility = Visibility.Collapsed; }
        }
        private void gridSplitLog2_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ExplorerPartChanging();
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
            dirTreeUC.Finish();
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
                string txt = msg.Trim();
                if (txt.Length > 7)
                    switch (txt.Substring(0, 8))
                    {
                        case "@StartGe": // StartGeneration
                            if (Utils.isNull(dTimer))
                            {
                                dTimer = new DispatcherTimer();
                                dTimer.Tick += new EventHandler(dTimer_Tick);
                                dTimer.Interval = new TimeSpan(200 * 10000);
                            }
                            dti = 0; dTimer.Start(); txt = msg.Substring(1);
                            break;
                        case "@EndGene": // EndGeneration
                            if (Utils.isNull(dTimer)) { Utils.TimedMessageBox("Err: broken timer", "Warning", 3500); return; }
                            dTimer.Stop();  lbProcessing.Content = "";
                            imgAbout.Source = ImgUtils.BitmapToBitmapImage(penpic, System.Drawing.Imaging.ImageFormat.Png); 
                            string fn = txt.Substring(15).Trim();
                            if (rowLogImage.Height.Value < 2) rowLogImage.Height = new GridLength(pnlLog.ActualWidth);
                            if (File.Exists(fn)) imgLast.Source = ImgUtils.UnhookedImageLoad(fn); // success
                            else imgLast.Source = ImgUtils.file_not_found;
                            txt = msg.Substring(1);
                            break;
                        case "@CancelR": // CancelRequest 
                            queryUC.Request2Cancel();
                            break;
                        case "@WorkDir":
                            if (Directory.Exists(opts.ImageDepotFolder))
                            {
                                dirTreeUC.CatchAFolder(opts.ImageDepotFolder);
                                tbImageDepot.Text = "working image depot -> " + opts.ImageDepotFolder;
                            }
                            else tbImageDepot.Text = "working image depot -> <NOT SET>";
                            break; 
                        case "@Explore":
                            string[] sa = txt.Split('='); if (sa.Length != 2) Utils.TimedMessageBox("Error(#458)");
                            ExplorerPart = Convert.ToInt32(sa[1]);
                            break; 
                        case "@_Header":
                            string[] sb = txt.Split('='); if (sb.Length != 2) Utils.TimedMessageBox("Error(#459)");
                            Title = "Scripthea - "+sb[1];
                            break; 
                    }
                if (chkLog.IsChecked.Value)
                {                
                    if (txt.StartsWith("@") && !opts.debug) return; // skips internal messages if not debug    
                    if (ExplorerPart.Equals(100)) { if (!txt.StartsWith("@")) Utils.TimedMessageBox(txt, "Warning", 3500); }
                    else Utils.log(tbLogger, txt, clr);
                }
            }
            finally
            {
                
            }
        }
        int dti;
        private void dTimer_Tick(object sender, EventArgs e)
        {
            string ch = "";
            switch (dti % 6)
            {
                /*case 0: ch = "--";
                    break;
                case 1: ch = " \\";
                    break;
                case 2: ch = " |";
                    break;
                case 3: ch = " /";
                    break;*/
                case 0: ch = ". ....";
                    break;
                case 1: ch = ".. ...";
                    break;
                case 2: ch = "... ..";
                    break;
                case 3: ch = ".... .";
                    break;
                case 4: ch = "... ..";
                    break;
                case 5: ch = ".. ...";
                    break;
            }
            dti++;
            lbProcessing.Content = ch;
           
            Bitmap clrBitmap = ImgUtils.ChangeColor(penpic, ImgUtils.ColorFromHue((dti % 20) * 18));
            imgAbout.Source = ImgUtils.BitmapToBitmapImage(clrBitmap, System.Drawing.Imaging.ImageFormat.Png);
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
                if (ImgUtils.checkImageDepot(fld) > 0) dirTreeUC.CatchAFolder(fld);
            }
        }
        private void imgPreferences_MouseDown(object sender, MouseButtonEventArgs e)
        {
            prefsWnd.ShowDialog();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string sPath = dirTreeUC.selectedPath;
            dirTreeUC.refreshTree(); dirTreeUC.CatchAFolder(sPath);
        }

    }
}
