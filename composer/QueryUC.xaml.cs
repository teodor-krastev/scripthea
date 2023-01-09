using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using scripthea.external;
using scripthea.master;
using UtilsNS;

namespace scripthea.composer
{
    public enum Status
    {
        Undefined, Idle, SingeQuery, Scanning, Request2Cancel 
    }
    /// <summary>
    /// Interaction logic for QueryUC.xaml
    /// </summary>
    public partial class QueryUC : UserControl, iFocusControl
    {
        //public string defaultImageFolder = Utils.basePath + "\\images\\";
        protected Options opts;
        public ControlAPI API;
        public QueryUC()
        {
            InitializeComponent();
        }
        public void Init(ref Options _opts)
        {
            status = Status.Undefined;            
            opts = _opts;
            UpdateFromOptions();
            status = Status.Idle;

            Log("@_Header=loading cues files (*.cues)");            
            cuePoolUC.OnChange += new RoutedEventHandler(ChangeCue);            
            cuePoolUC.OnLog += new Utils.LogHandler(Log);
            cuePoolUC.Init(ref opts);
            
            Log("@_Header=loading modifiers files (*.mdfr)");
            modifiersUC.OnChange += new RoutedEventHandler(ChangeModif);
            modifiersUC.OnLog += new Utils.LogHandler(Log);
            modifiersUC.Init(ref opts);

            scanPreviewUC.OnLog += new Utils.LogHandler(Log);
            scanPreviewUC.btnClose.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnScanChecked.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnQuerySelected.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);

            tiMiodifiers.Visibility = Visibility.Collapsed; tiScanPreview.Visibility = Visibility.Collapsed;

            API = new ControlAPI(); cbActiveAPI_SelectionChanged(null, null);
            API.OnQueryComplete += new ControlAPI.APIEventHandler(QueryComplete);

            if (Utils.TheosComputer()) { cbiDiffusion.Visibility = Visibility.Visible; btnTest.Visibility = Visibility.Visible; }
            else { cbiDiffusion.Visibility = Visibility.Collapsed; btnTest.Visibility = Visibility.Collapsed; }

            
        }
        public void Finish()
        {
            UpdateToOptions(null, null);
            cuePoolUC.Finish(); modifiersUC.Finish();
            if (!Utils.isNull(API))
            {
               API.eCancel = false; API.Close();
            }
        }
        public UserControl parrent { get { return this; } }
        public GroupBox groupFolder { get { return gbFolder; } }
        public TextBox textFolder { get { return tbImageDepot; } }

        private Status _status;
        public Status status
        {
            get { return _status; }
            set
            {
                switch (value)
                {
                    case Status.Idle:
                        tiSingle.IsEnabled = true;
                        tiScan.IsEnabled = true;
                        tiOptions.IsEnabled = true;                      
                        break;
                    case Status.SingeQuery:
                        tiSingle.IsEnabled = true;
                        tiScan.IsEnabled = false;
                        tiOptions.IsEnabled = false;
                        break;
                    case Status.Scanning: case Status.Request2Cancel:
                        tiSingle.IsEnabled = false;
                        tiScan.IsEnabled = true;
                        tiOptions.IsEnabled = false;
                        break;
                }
                _status = value;
            }
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        private bool _showAPI;
        public bool showAPI
        {
            get { return _showAPI; }
            set 
            {
                if (value) rowAPI.Height = new GridLength(68);
                else rowAPI.Height = new GridLength(1);
                if (value) imgAPIdialog.Visibility = Visibility.Hidden;
                else imgAPIdialog.Visibility = Visibility.Visible;
                _showAPI = value;  
            }
        }
        //public event RoutedEventHandler OnChange;
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected void ChangeCue(object sender, RoutedEventArgs e)
        {
            //Log("conditions changed");
            if (opts.SingleAuto && cuePoolUC.radioMode) btnCompose_Click(sender,e);
        }
        protected void ChangeModif(object sender, RoutedEventArgs e)
        {
            //Log("conditions changed");           
            if (opts.SingleAuto && tcQuery.SelectedItem.Equals(tiSingle)) btnCompose_Click(sender,e);
        }

        private bool UpdatingOptions = false;
        private void UpdateFromOptions() // internal to visual options
        {
            if (Utils.isNull(opts)) return;
            UpdatingOptions = true;
            pnlCue.Height = new GridLength(Utils.EnsureRange(opts.QueryRowHeight, 150, 500));
            colQuery.Width = new GridLength(opts.QueryColWidth);
            chkAutoSingle.IsChecked = opts.SingleAuto; btnCompose.IsEnabled = !opts.SingleAuto;              
            chkOneLineCue.IsChecked = opts.OneLineCue;           

            cbActiveAPI.Text = opts.API; tbModifPrefix.Text = opts.ModifPrefix; 
            if (Directory.Exists(opts.ImageDepotFolder)) tbImageDepot.Text = opts.ImageDepotFolder;
            else
            {
                Log("Directory <"+tbImageDepot.Text+"> does not exist. Setting to default directory :"+ ImgUtils.defaultImageDepot);
                opts.ImageDepotFolder = ImgUtils.defaultImageDepot; tbImageDepot.Text = ImgUtils.defaultImageDepot;
            }           
            UpdatingOptions = false;              
        }
        private void UpdateToOptions(object sender, RoutedEventArgs e) // visual to internal options
        {
            if (UpdatingOptions || Utils.isNull(opts)) return;
            int QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
            if (QueryRowHeight > 1) opts.QueryRowHeight = QueryRowHeight;
            opts.QueryColWidth = Convert.ToInt32(colQuery.Width.Value);
            opts.SingleAuto = chkAutoSingle.IsChecked.Value; btnCompose.IsEnabled = !opts.SingleAuto;           
            opts.OneLineCue = chkOneLineCue.IsChecked.Value;           

            opts.API = cbActiveAPI.Text; opts.ModifPrefix = tbModifPrefix.Text;
            opts.ImageDepotFolder = tbImageDepot.Text;           
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {
            opts.ImageDepotFolder = tbImageDepot.Text;
            if (Directory.Exists(tbImageDepot.Text)) tbImageDepot.Foreground = Brushes.Black;                
            else tbImageDepot.Foreground = Brushes.Red;
            Log("@WorkDir");
        }
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = ImgUtils.defaultImageDepot;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbImageDepot.Text = dialog.FileName;
            }
        }
        public string propmt
        {
            get { return tbCue.Text + tbModifier.Text; }
        }

        public string Compose(object sender,  CueItemUC selectedSeed, string modifiers, bool OneLineCue = true)
        {
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == cuePoolUC)
            {
                tbCue.Text = "";
                List<string> ls = selectedSeed.cueTextAsList(true);
                foreach (string line in ls)
                {
                    if (line.Equals("")) continue;
                    if (line.Length > 1)
                        if (line.Substring(0, 2).Equals("##")) continue;
                    tbCue.Text += line + (opts.OneLineCue ? ' ' : '\r');
                }
            }
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == modifiersUC)
                tbModifier.Text = modifiers;
            return propmt;
        }
        public void btnCompose_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(cuePoolUC) || Utils.isNull(modifiersUC)) return;
            if (Utils.isNull(cuePoolUC.ActiveCueList) && !status.Equals(Status.Undefined)) { Log("Err: no active cue pool."); return; }
            if (Utils.isNull(cuePoolUC.ActiveCueList?.allCues) || Utils.isNull(modifiersUC.modifLists)) return;
            List<CueItemUC> selectedSeed = cuePoolUC?.ActiveCueList?.selectedCues();
            if (Utils.isNull(selectedSeed)) { Log("Err: no cue is selected. (35)"); return; }
            if (selectedSeed.Count.Equals(0)) { /*Log("Err: no cue is selected. (58)");*/ return; }
            Compose(sender, selectedSeed[0], modifiersUC.Composite());
        }
        private void QueryAPI(string prompt)
        {   
            Log("query -> "+ prompt, Brushes.DarkGreen); Log("@StartProc");
            API.Query(prompt, opts.ImageDepotFolder);
            if (status.Equals(Status.Scanning) && scanPreviewUC.scanning)
                scanPreviewUC.selectByPropmt(prompt);
        }
        protected void QueryComplete(string imageFilePath, bool success)
        {
            if (success)
            {
                Log("image -> " + imageFilePath, Brushes.Navy); Log("---", Brushes.DarkOrange);                
            }
            else Log("Error(API)-> "+ imageFilePath);
            Log("@EndProc " + imageFilePath);
            switch (status)
            {
                case Status.SingeQuery:
                case Status.Request2Cancel:
                    status = Status.Idle;
                    break;
                case Status.Scanning:                   
                    if (scanPromptIdx > (scanPrompts.Count - 1))
                    {
                        status = Status.Idle; btnScan.Content = "S c a n"; btnScan.Background = Brushes.MintCream;
                        Log("This Scan resulted in " + scanPrompts.Count.ToString()+" images.", Brushes.DarkMagenta);
                        btnScanPreview.IsEnabled = true; scanPreviewUC.scanning = false; btnScanPreview.IsEnabled = true; 
                    }                        
                    else
                    {
                        QueryAPI(scanPrompts[scanPromptIdx]); scanPromptIdx++;
                    }
                    break;
            }
        }
        private List<string> scanPrompts = new List<string>();
        private int scanPromptIdx;

        private void GetScanPrompts()
        {        
            List<CueItemUC> selectedSeeds = cuePoolUC?.ActiveCueList?.selectedCues(); scanPrompts = new List<string>(); 
            if (Utils.isNull(selectedSeeds)) { Log("Err: no cue is selected (12)"); return; }
            if (selectedSeeds.Count.Equals(0)) { Log("Err: no cue is selected (96)"); return; }
            List<string> ScanModifs = modifiersUC.ModifItemsByType(ModifStatus.Scannable); 
            foreach (CueItemUC ssd in selectedSeeds)
            {
                if (ScanModifs.Count.Equals(0))
                {
                    scanPrompts.Add(Compose(null, ssd, modifiersUC.FixItemsAsString()));                    
                }
                else
                    foreach (string sc in ScanModifs)
                    {
                        scanPrompts.Add(Compose(null, ssd, modifiersUC.FixItemsAsString() + (sc.Equals("") ? "" : opts.ModifPrefix) + sc));                        
                    }
            }            
        }
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            if (Convert.ToString(btnScan.Content).Equals("S c a n"))
            {
                if (API.IsBusy) { Log("Err: busy with previous query"); return; }
                switch (status)
                {
                    case Status.SingeQuery:
                        Log("Warning: API is busy, try again later..."); return;
                    case Status.Request2Cancel:
                        Log("Warning: Your request for cancelation has been already accepted.", Brushes.Tomato); return;
                    case Status.Scanning:
                        Log("Err: internal error #45"); return;
                }
                status = Status.Scanning; btnScan.Content = "Cancel"; btnScan.Background = Brushes.Coral;
                btnScanPreview.IsEnabled = false;
            }
            else
            {
                if (status == Status.Scanning) Log("Warning: User cancelation!", Brushes.Tomato);
                status = Status.Request2Cancel; btnScan.Content = "S c a n"; btnScan.Background = Brushes.MintCream;
                btnScanPreview.IsEnabled = true; return;
            }
            GetScanPrompts();
            if (scanPrompts.Count == 0) { Log("Err: no prompt generated"); return; }
            scanPromptIdx = 1; QueryAPI(scanPrompts[0]);
        }
        private void chkAutoSingle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateToOptions(sender, e);
            btnCompose_Click(sender, e);
        }
        private void tbModifSepar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.ModifPrefix = tbModifPrefix.Text;
        }
        private void cbActiveAPI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(API)) return;
            ComboBoxItem cbi = (ComboBoxItem)cbActiveAPI.SelectedItem;
            API.activeAPIname = (string)cbi.Content; opts.API = API.activeAPIname;
            showAPI = API.activeAPI.isDocked;
            if (showAPI) 
            { 
                gridAPI.Children.Clear(); gridAPI.Children.Add(API.activeAPI.userControl);
                API.activeAPI.Init("");
            }
            if (!Utils.isNull(e)) e.Handled = true;
        }

        private void imgAPIdialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected. (55)"); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (22)"); return; }           
            API.activeAPI.opts["folder"] = opts.ImageDepotFolder;
            API.about2Show(propmt);
            API.ShowDialog();
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected. (56)"); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (21)"); return; }
            if (API.IsBusy || status != Status.Idle)
            {
                Utils.TimedMessageBox("API is busy, try again later...", "Warning"); return;
            }            
            btnCompose_Click(null, null); status = Status.SingeQuery;
            QueryAPI(Compose(null, cuePoolUC.ActiveCueList?.selectedCues()[0], modifiersUC.Composite()));
        }

        private void tbCue_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnQuery.IsEnabled = !tbCue.Text.Trim().Equals("");
        }

        private void imgCopy_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(propmt);
        }
        private void tcQuery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(tcQuery.SelectedItem)) return;
            if (tcQuery.SelectedItem.Equals(tiOptions))
            {
                Log("@ExplorerPart=100"); return;
            }    
            else Log("@ExplorerPart=0");
            if (!Utils.isNull(opts))
            {
                if (tcQuery.SelectedItem.Equals(tiSingle)) pnlCue.Height = new GridLength(Utils.EnsureRange(opts.QueryRowHeight, 150,500));
                else
                {
                    opts.QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
                    pnlCue.Height = new GridLength(1);
                }
            }
            tbModifier.Text = "";
            if (Utils.isNull(cuePoolUC)) return;
            cuePoolUC.radioMode = tcQuery.SelectedItem.Equals(tiSingle);
            modifiersUC.SetSingleScanMode(tcQuery.SelectedItem.Equals(tiSingle));
            ChangeModif(sender, e);
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private readonly string[] miTitles = { "Copy", "Cut", "Paste", "\"...\" synonyms", "\"...\" meaning" };
        private void tbCue_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            cmCue.Items.Clear(); bool isSel = !tbCue.SelectedText.Trim().Equals("");
            for (int i = 0; i < 3; i++)
            {
                MenuItem mi = new MenuItem(); mi.Header = miTitles[i];
                if (i==0 || i==1) mi.IsEnabled = isSel; 
                if (i == 2) mi.IsEnabled = Clipboard.ContainsText();
                mi.Click += mi_Click;
                cmCue.Items.Add(mi);
            }
            if (!isSel) return;
            cmCue.Items.Add(new Separator());
            for (int i = 3; i < 5; i++)
            {
                MenuItem mi = new MenuItem(); 
                mi.Header = miTitles[i].Replace("...", tbCue.SelectedText.Trim()); 
                mi.Click += mi_Click;
                cmCue.Items.Add(mi);
            }           
        }
        void mi_Click(object sender, RoutedEventArgs e)
        {
            bool isSel = !tbCue.SelectedText.Trim().Equals("");
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            switch (header)
            {
                case "Copy": Clipboard.SetText(tbCue.SelectedText.Trim());
                    return;
                case "Cut": Clipboard.SetText(tbCue.SelectedText.Trim()); tbCue.SelectedText = "";
                    return;
                case "Paste": tbCue.SelectedText = Clipboard.GetText();
                    return;
            }
            if (header.EndsWith("synonyms") || header.EndsWith("meaning")) Utils.AskTheWeb(header);
        }

        private void btnScanPreview_Click(object sender, RoutedEventArgs e)
        {
            GetScanPrompts();
            if (scanPrompts.Count == 0) { return; };
            List<string> ls = new List<string>(scanPrompts);

            tcModScanPre.SelectedIndex = 1; Utils.DoEvents();
            scanPreviewUC.LoadPrompts(ls);
            btnScan.IsEnabled = false;
        }

        private void btnScanPreviewProcs_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(scanPreviewUC.btnScanChecked))
            {
                if (scanPreviewUC.scanning)
                {
                    scanPreviewUC.scanning = false; status = Status.Request2Cancel;
                    Log("Warning: User cancelation!", Brushes.Tomato); btnScanPreview.IsEnabled = true;
                }
                else
                {
                    scanPrompts = scanPreviewUC.checkedPrompts();
                    if (scanPrompts.Count == 0) { Log("Err: no prompts checked"); return; }
                    status = Status.Scanning; scanPreviewUC.scanning = true; btnScanPreview.IsEnabled = false;
                    scanPromptIdx = 1; QueryAPI(scanPrompts[0]);
                }
            }
            if (sender.Equals(scanPreviewUC.btnQuerySelected))
            {               
                if (scanPreviewUC.selectedPrompt == "") { Log("Err: no prompt selected"); return; }                
                QueryAPI(scanPreviewUC.selectedPrompt);
            }
            if (sender.Equals(scanPreviewUC.btnClose))
            {
                tcModScanPre.SelectedIndex = 0;
                btnScan.IsEnabled = true;
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            List<string> files = new List<string>(Directory.GetFiles(tbImageDepot.Text, "*.png"));
            Log(files.Count.ToString()+" files"); DateTime t0 = DateTime.Now;
            Dictionary<string, string> meta; int ns = 0;
            foreach (string fn in files)
            {
                if (ImgUtils.GetMetaDataItems(fn, out meta)) ns++;
            }
            double t = (DateTime.Now - t0).TotalSeconds;
            Log("time taken = "+t.ToString("G3")+" [sec]  "+ns.ToString()+" files OK");
            Log("time " + (t / ns).ToString("G3") + " [sec] per file");
        }

    }
}
