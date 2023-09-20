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
using Path = System.IO.Path;
using Color = System.Drawing.Color;
using PyCodeLib;

namespace scripthea
{
    /// <summary>
    /// Scripthea is a prompt composer as utility for text-to-image AI generators
    /// </summary>
    public partial class MainWindow : Window
    {
        public AboutWin aboutWin;       
        public MainWindow()
        {
            aboutWin = new AboutWin();
            aboutWin.Show();
            InitializeComponent();
        }
        string optionsFile;
        public Options opts;
        public PreferencesWindow preferencesWindow;
        private System.Drawing.Bitmap penpic;

        public FocusControl focusControl;
        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            optionsFile = Path.Combine(Utils.configPath, "Scripthea.cfg");
            if (!Directory.Exists(Utils.configPath))
            {
                string msg = "Fatal error: Directory <" + Utils.configPath + "> does not exist.";
                Title = msg; Utils.Sleep(2000);
                throw new Exception(msg);
            }                   
            if (File.Exists(optionsFile))
            {
                string json = System.IO.File.ReadAllText(optionsFile);
                opts = JsonConvert.DeserializeObject<Options>(json);
                if (opts.composer.ImageDepotFolder == null) opts.composer.ImageDepotFolder = "";
                if (opts.composer.ImageDepotFolder.Equals("<default.image.depot>")) opts.composer.ImageDepotFolder = ImgUtils.defaultImageDepot; 
            }
            else opts = new Options();
            opts.general.debug = Utils.isInVisualStudio || Utils.localConfig; aboutWin.Init(ref opts);
            if (opts.general.UpdateCheck) Check4Update(null,null);
            else
            {
                if (opts.general.NewVersion == null) opts.general.NewVersion = "";
                if (!opts.general.NewVersion.Equals(""))
                {
                    aboutWin.lbMessage.Foreground = System.Windows.Media.Brushes.Green; aboutWin.lbMessage.Content = "New release (" + opts.general.NewVersion + ") is available at Scripthea.com !";
                }
            }
            preferencesWindow = new PreferencesWindow(); preferencesWindow.Init(ref opts); preferencesWindow.btnCheck4Update.Click += new RoutedEventHandler(Check4Update);
            
            Title = "Scripthea - options loaded";
            dirTreeUC.Init(ref opts);
            dirTreeUC.OnActive += new DirTreeUC.SelectHandler(Active);
            dirTreeUC.OnLog += new Utils.LogHandler(Log);

            queryUC.OnLog += new Utils.LogHandler(Log); queryUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            viewerUC.OnLog += new Utils.LogHandler(Log); viewerUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
            importUtilUC.OnLog += new Utils.LogHandler(Log); importUtilUC.tbImageDepot.KeyDown += new KeyEventHandler(MainWindow1_KeyDown);
           
            Title = "Scripthea - loading text files...";            
            viewerUC.Init(ref opts);
            depotMaster.Init(ref opts);
            importUtilUC.Init();
            exportUtilUC.Init(ref opts);

            oldTab = tiComposer;
            Log("> Welcome to Scripthea" + "  " + (opts.general.debug ? "(in debug mode)" : "")); Log("");
            Utils.DelayExec(2500, new Action(() => { aboutWin.Hide(); })); 
            queryUC.Init(ref opts);            
            Left = opts.layout.Left;
            Top = opts.layout.Top;
            Width = Math.Abs(opts.layout.Width);
            Height = opts.layout.Height;
            if (opts.layout.Maximazed) WindowState = WindowState.Maximized; 
            pnlLog.Width = new GridLength(opts.layout.LogColWidth);            
            rowLogImage.Height = new GridLength(1);
            colMasterWidth.Width = new GridLength(opts.iDutilities.MasterWidth);
            colImportWidth.Width = new GridLength(opts.iDutilities.ImportWidth);
            colExportWidth.Width = new GridLength(opts.iDutilities.ExportWidth);

            if (opts.composer.SingleAuto) queryUC.btnCompose_Click(null, null);
            // pyCode Init
            
            if (Utils.TheosComputer() && Utils.isInVisualStudio) pyCode.Init(@"C:\Software\Python\Python310\python310.dll");
            if (!pyCode.IsEnabled) tiSMacro.Visibility = Visibility.Collapsed;
            else
            {
                colSMacroFull.Width = new GridLength(opts.sMacro.FullWidth);
                pyCode.colCodeWidth = opts.sMacro.CodeWidth; pyCode.colLogWidth = opts.sMacro.LogWidth;
                if (pyCode.st != null) pyCode.st.OnLog += new Utils.LogHandler(Log);
                // modules registration in pyCode
                pyCode.Register("query",queryUC, queryUC.HelpList());
                pyCode.Register("sdPrms", queryUC.sd_params_UC, queryUC.sd_params_UC.HelpList());
            }                      
            focusControl = new FocusControl();
            focusControl.Register("import",importUtilUC);
            focusControl.Register("export",exportUtilUC.iPicker);
            focusControl.Register("query",queryUC);
            focusControl.Register("viewer",viewerUC);
            focusControl.Register("idmA",depotMaster.iPickerA);
            focusControl.Register("idmB",depotMaster.iPickerB);
            //focusControl.Register("idfX", queryUC.cuePoolUC.iPickerX);
            string penpicFile = Path.Combine(Utils.configPath, "penpic1.png");
            if (!File.Exists(penpicFile)) throw new Exception(penpicFile +" file is missing");
            penpic = new System.Drawing.Bitmap(penpicFile); imgAbout.Source = ImgUtils.BitmapToBitmapImage(penpic, System.Drawing.Imaging.ImageFormat.Png);
            ExplorerPart = 0;
            gridSplitLeft_MouseDoubleClick(null, null);
            Title = "Scripthea - text-to-image prompt composer v" + Utils.getAppFileVersion;   
            if (opts.layout.Width < 0)
            {
                Log("Assuming that you run Scripthea for the first time:", Brushes.Blue);
                Log("1. Check and modify (if needed) the default preferences (three bar button above).", Brushes.Blue);
                Log("2. If you have Stable Diffusion WebUI installed go to SD panel (here top/right), open Options and on the second tab point the Stable Diffusion WebUI locaton", Brushes.Blue);
                Log(""); Log("Press F1 for Scripthea online help.", Brushes.Green);
            }
        }
        public void Check4Update(object sender, RoutedEventArgs e)
        {
            int k = sender == null ? opts.general.LastUpdateCheck : -1;
            aboutWin.Check4Updates(k);
            if (k == -1)
            {
                if (opts.general.NewVersion.Equals("")) preferencesWindow.lbNewVer.Content = "Your version is up to date";
                else preferencesWindow.lbNewVer.Content = "New version: " + opts.general.NewVersion;
            }
        }
        public int ExplorerPart // from 0 to 100% directory tree
        {
            get { return (int)(100 * rowExplorer.Height.Value / (rowLog.Height.Value + rowExplorer.Height.Value)); } 
            set
            {
                int vl = Utils.EnsureRange(value, 0, 100);
 
                rowLog.Height = new GridLength(100 - vl, GridUnitType.Star);
                rowExplorer.Height = new GridLength(vl, GridUnitType.Star);
                if (vl != 100 && vl != 0 && !Utils.isNull(opts))
                {
                    if (vl > 70) //partly shown
                    {
                        gridSplitLog.Visibility = Visibility.Collapsed; rowLogImage.Height = new GridLength(1);
                    }
                    else
                    {
                        gridSplitLeft.Visibility = Visibility.Visible; 
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
            opts.layout.Left = Convert.ToInt32(Left);
            opts.layout.Top = Convert.ToInt32(Top);
            opts.layout.Width = Convert.ToInt32(Width);
            opts.layout.Height = Convert.ToInt32(Height);
            opts.layout.Maximazed = WindowState == WindowState.Maximized;
            opts.layout.LogColWidth = Convert.ToInt32(pnlLog.Width.Value);
            opts.iDutilities.MasterWidth = Convert.ToInt32(colMasterWidth.Width.Value);
            opts.iDutilities.ImportWidth = Convert.ToInt32(colImportWidth.Width.Value);
            opts.iDutilities.ExportWidth = Convert.ToInt32(colExportWidth.Width.Value);
            opts.sMacro.FullWidth = Convert.ToInt32(colSMacroFull.Width.Value);
            opts.sMacro.CodeWidth = Convert.ToInt32(pyCode.colCodeWidth);
            opts.sMacro.LogWidth = Convert.ToInt32(pyCode.colLogWidth);

            pyCode.Finish();
            queryUC.Finish();
            viewerUC.Finish();
            dirTreeUC.Finish();
            if (!Utils.isNull(aboutWin)) { aboutWin.closing = true; aboutWin.Close(); }

            string json = JsonConvert.SerializeObject(opts);
            System.IO.File.WriteAllText(optionsFile, json);
            if (!Utils.isNull(preferencesWindow))
            {
                preferencesWindow.keepOpen = false; preferencesWindow.Close();
            }
        }
        private DispatcherTimer dTimer;
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLogger.Document.Blocks.Clear(); imgLast.Source = null;
        }
        public void Log(string msg, SolidColorBrush clr = null)
        {
            try
            {
                string txt = msg.Trim(); bool skipLog = false;
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
                            string fn = msg.Equals("@EndGeneration") ? "" : txt.Substring(15).Trim();
                            if (rowLogImage.Height.Value < 2) rowLogImage.Height = new GridLength(pnlLog.ActualWidth);
                            if (File.Exists(fn)) imgLast.Source = ImgUtils.UnhookedImageLoad(fn); // success
                            else { imgLast.Source = ImgUtils.file_not_found; Log("Error: file not found " + fn); } 
                            txt = msg.Substring(1);
                            break;
                        case "@CancelR": // CancelRequest 
                            queryUC.Request2Cancel();
                            break;
                        case "@WorkDir":
                            if (Directory.Exists(opts.composer.ImageDepotFolder))
                            {
                                dirTreeUC.CatchAFolder(opts.composer.ImageDepotFolder);
                                tbImageDepot.Text = "working image depot -> " + opts.composer.ImageDepotFolder;
                            }
                            else tbImageDepot.Text = "working image depot -> <NOT THERE>";
                            break; 
                        case "@Explore":
                            string[] sa = txt.Split('='); if (sa.Length != 2) Utils.TimedMessageBox("Error(#458)");
                            ExplorerPart = Convert.ToInt32(sa[1]); skipLog = true;
                            break;
                        case "query ->":
                            if ((tabControl.SelectedIndex > 0) && (ExplorerPart > 95)) ExplorerPart = 50;
                            break;
                        case "@_Header":
                            string[] sb = txt.Split('='); if (sb.Length != 2) Utils.TimedMessageBox("Error(#459)");
                            Title = "Scripthea - "+sb[1]; skipLog = true;
                            break; 
                    }
                if (chkLog.IsChecked.Value && !skipLog)
                {                
                    if (txt.StartsWith("@") && !opts.general.debug) return; // skips internal messages if not debug    
                    if (ExplorerPart.Equals(100)) 
                    { 
                        if (!txt.StartsWith("@") && !txt.StartsWith("StartGe") && !txt.Equals("---")) 
                            Utils.TimedMessageBox(txt, "Warning", 2500); 
                    }
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

            System.Drawing.Bitmap clrBitmap = ImgUtils.ChangeColor(penpic, ImgUtils.ColorFromHue((dti % 20) * 18));
            imgAbout.Source = ImgUtils.BitmapToBitmapImage(clrBitmap, System.Drawing.Imaging.ImageFormat.Png);
        }
        TabItem oldTab;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string nextDepotFolder(string firstGuess)
            {
                if (ImgUtils.checkImageDepot(firstGuess) > -1) return firstGuess;
                if (ImgUtils.checkImageDepot(opts.composer.ImageDepotFolder) > -1) return opts.composer.ImageDepotFolder;
                return ImgUtils.defaultImageDepot;
            }
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
                bool bb = false;
                if (oldTab.Equals(tiComposer))
                    bb |= viewerUC.ShowImageDepot(nextDepotFolder(queryUC.tbImageDepot.Text));
                if (oldTab.Equals(tiUtils)) 
                    bb |= viewerUC.ShowImageDepot(nextDepotFolder(importUtilUC.imageFolder));
                if (!bb) bb |= viewerUC.ShowImageDepot(nextDepotFolder(""));
                ExplorerPart = 100; dirTreeUC.CatchAFolder(viewerUC.tbImageDepot.Text); 
                focusControl.GotTheFocus(viewerUC, null); // unknown why it needs to be done from code 
            }
            if (tabControl.SelectedItem.Equals(tiDepotMaster))
            {
                ExplorerPart = 100;
            }
            if (tabControl.SelectedItem.Equals(tiUtils))
            {
                ExplorerPart = 100;
                if (focusControl.ifc != null)
                {
                    if (focusControl.ifcName.Equals("import")) 
                        dirTreeUC.CatchAFolder(importUtilUC?.imageFolder);
                    if (focusControl.ifcName.Equals("export")) 
                        dirTreeUC.CatchAFolder(exportUtilUC?.iPicker?.tbImageDepot.Text);
                }
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
            if (!Utils.isNull(sender)) opts.layout.LogColWaveSplit = !opts.layout.LogColWaveSplit;
            if (opts.layout.LogColWaveSplit)
            {
                gridSplitLeft.Visibility = Visibility.Visible;
                gridSplitLeft2.Visibility = Visibility.Collapsed;
                tabControl.Margin = new Thickness(17, 0, 0, 0);
            }
            else
            {
                gridSplitLeft2.Visibility = Visibility.Visible;
                gridSplitLeft.Visibility = Visibility.Collapsed;               
                tabControl.Margin = new Thickness(7, 0, 0, 0);
            }
        }
        private void MainWindow1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1: 
                    if (sender.Equals(MainWindow1)) Utils.CallTheWeb("https://scripthea.com");
                    break;
                case Key.Enter: 
                    string fld = "";
                    if (sender.Equals(queryUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiComposer)) fld = queryUC.tbImageDepot.Text;
                    if (sender.Equals(viewerUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiViewer)) fld = viewerUC.tbImageDepot.Text;
                    if (sender.Equals(importUtilUC.tbImageDepot) && tabControl.SelectedItem.Equals(tiUtils)) fld = importUtilUC.tbImageDepot.Text;
                    if (sender.Equals(exportUtilUC.iPicker.tbImageDepot) && tabControl.SelectedItem.Equals(tiUtils)) fld = exportUtilUC.iPicker.tbImageDepot.Text; 
                    if (ImgUtils.checkImageDepot(fld) > 0) dirTreeUC.CatchAFolder(fld);
                    break;
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            string sPath = dirTreeUC.selectedPath;
            dirTreeUC.refreshTree(); dirTreeUC.CatchAFolder(sPath);
        }
        private void MainWindow1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (tabControl.SelectedItem.Equals(tiViewer))
            {
                if (viewerUC == null) return;
                if (e.Key.Equals(Key.F11)) viewerUC.animation = true;
                if (e.Key.Equals(Key.Escape)) viewerUC.animation = false;
            }
        }
        private void btnPreferences_Click(object sender, RoutedEventArgs e)
        {
            preferencesWindow.ShowWindow(tabControl.SelectedIndex);
        }       
    }
}
