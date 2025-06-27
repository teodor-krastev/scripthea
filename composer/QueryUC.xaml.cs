using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using scripthea.options;
using ExtCollMng;
using UtilsNS;
using Path = System.IO.Path;

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
        const string strScan = "S c a n"; protected DateTime sessionStart;
        protected Options opts;
        public ControlAPI API;
        public QueryUC()
        {
            InitializeComponent();
        }
        public void Init(ref Options _opts)
        {
            status = Status.Undefined;            
            opts = _opts; sessionStart = DateTime.Now;
            UpdateFromOptions();
            status = Status.Idle; 

            opts.Log("@_Header=loading cues files (*.cues)");              
            cuePoolUC.ExternalSelectionChanged += new SelectionChangedEventHandler(CuePoolSelectionChanged);
            Courier.CueSelectionHandler ChageCueRef = new Courier.CueSelectionHandler(ChangeCue);
            cuePoolUC.Init(ref opts, ref ChageCueRef);

            opts.Log("@_Header=loading modifiers files (*.mdfr)");
            modifiersUC.Init(ref opts);
            modifiersUC.OnChange += new RoutedEventHandler(ChangeModif);

            scanPreviewUC.Init(ref opts);
            scanPreviewUC.btnClose.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnScanChecked.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnQuerySelected.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);

            //tiMiodifiers.Visibility = Visibility.Collapsed; tiScanPreview.Visibility = Visibility.Collapsed;

            API = new ControlAPI(ref opts); 
            if (API.interfaceAPIs.ContainsKey("SDiffusion"))             
                API.interfaceAPIs["SDiffusion"].APIparamsEvent += new APIparamsHandler(OnAPIparams);
            if (API.interfaceAPIs.ContainsKey("AddonGen"))
                API.interfaceAPIs["AddonGen"].APIparamsEvent += new APIparamsHandler(OnAPIparams);

            cbActiveAPI_SelectionChanged(null, null);
            API.OnQueryComplete += new ControlAPI.APIEventHandler(QueryComplete);
            sd_params_UC.Init(ref opts);
            cuePoolUC.OnSDparams += new Utils.LogHandler(sd_params_UC.ImportImageInfo);
            
            if (opts.general.debug) 
            { 
                btnTest.Visibility = Visibility.Visible;
                cbActiveAPI.Items.Add(new ComboBoxItem() { Name = "cbiSimulation", Content = "Simulation" }); 
                //cbActiveAPI.SelectedIndex = 3;
            }
            else { btnTest.Visibility = Visibility.Collapsed; }
            
            extCollMng.Init(new Utils.LogHandler(opts.Log), Path.Combine(Utils.basePath,"cues"));
            extCollMng.OnChangeColls += new Action(cuePoolUC.UpdateCueMapFromDisk);
            cuePoolUC.btnExtColl.Click += new RoutedEventHandler(btnExtColl_Click);
            /*if (Utils.TheosComputer()) tiExtCollMng.Visibility = Visibility.Visible;
            else */ tiExtCollMng.Visibility = Visibility.Hidden;
            cuePoolUC.OnExtCollOff += new EventHandler(ExtCollInvisible);
        }
        public void Finish()
        {
            sd_params_UC.Finish();
            UpdateToOptions(null, null);
            opts.composer.SessionSpan += Convert.ToInt32((DateTime.Now - sessionStart).TotalMinutes);
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
                    case Status.Undefined:
                        break;
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
                    case Status.Scanning: 
                    case Status.Request2Cancel:
                        tiSingle.IsEnabled = false;
                        tiScan.IsEnabled = true;
                        tiOptions.IsEnabled = false;
                        if (!tcQuery.SelectedItem.Equals(tiScan)) tcQuery.SelectedItem = tiScan;
                        break;
                }
                _status = value; if (opts != null) opts.composer.QueryStatus = value;
            }
        }          
        protected Dictionary<string, object> OnAPIparams(bool? showIt)
        {
            if (showIt != null)
            {
                if ((bool)showIt && (API.activeAPIname.Equals("SDiffusion") || API.activeAPIname.Equals("AddonGen"))) tiSD_API.Visibility = Visibility.Visible;
                else tiSD_API.Visibility = Visibility.Collapsed;           
            }
            return sd_params_UC.vPrms;
        }
        private bool _showAPI;
        public bool showAPI
        {
            get { return _showAPI; }
            set 
            {
                if (value) rowAPI.Height = new GridLength(83);
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
        /*protected void ChangeCue(object sender, RoutedEventArgs e)
        {
            //opts.Log("conditions changed");
            if (opts.SingleAuto && cuePoolUC.radioMode) btnCompose_Click(sender,e);
        }*/
        protected void ChangeCue(List<string> selCues)
        {
            if (opts == null) return;
            if ((opts.composer.SingleAuto == Options.SingleAutoSet.cue || opts.composer.SingleAuto == Options.SingleAutoSet.both) && cuePoolUC.radioMode) 
                Compose(null, selCues, modifiersUC.Composite(), false);
        }
        protected void ChangeModif(object sender, RoutedEventArgs e)
        {
            if (opts == null) return;           
            if ((opts.composer.SingleAuto == Options.SingleAutoSet.modif || opts.composer.SingleAuto == Options.SingleAutoSet.both) && tcQuery.SelectedItem.Equals(tiSingle)) 
                Compose(sender,false);
        }
        private void SetbtnComposeEnabled(Options.SingleAutoSet af)
        {
            btnCompose.IsEnabled = af != Options.SingleAutoSet.both;
            switch (opts.composer.SingleAuto)
            {                                   
                case Options.SingleAutoSet.cue: 
                case Options.SingleAutoSet.modif:btnCompose.Content = "Complete a prompt";
                    break;
                case Options.SingleAutoSet.both:
                case Options.SingleAutoSet.none: btnCompose.Content = "Compose a prompt";
                    break;
            }

            if (btnCompose.IsEnabled) btnCompose.Foreground = Brushes.Black;
            else btnCompose.Foreground = Brushes.DarkGray;
        }
        private bool UpdatingOptions = false;
        private void UpdateFromOptions() // options to visual 
        {
            if (Utils.isNull(opts)) return;
            UpdatingOptions = true;
            pnlCue.Height = new GridLength(Utils.EnsureRange(opts.composer.QueryRowHeight, 150, 500));
            colQuery.Width = new GridLength(opts.composer.QueryColWidth);
            switch (opts.composer.SingleAuto)
            {
                case Options.SingleAutoSet.both: rbBoth.IsChecked = true;
                    break;
                case Options.SingleAutoSet.cue: rbCue.IsChecked = true;
                    break;
                case Options.SingleAutoSet.modif: rbModif.IsChecked = true;
                    break;
                case Options.SingleAutoSet.none: rbNone.IsChecked = true;
                    break;
            }
            SetbtnComposeEnabled(opts.composer.SingleAuto);              
            chkOneLineCue.IsChecked = opts.composer.OneLineCue;           

            cbActiveAPI.Text = opts.composer.API; 
            if (Directory.Exists(opts.composer.ImageDepotFolder)) tbImageDepot.Text = opts.composer.ImageDepotFolder;
            else
            {
                if (opts.composer.ImageDepotFolder != "") 
                    opts.Log("Directory <"+ opts.composer.ImageDepotFolder + "> does not exist. Setting to default directory :"+ SctUtils.defaultImageDepot);
                opts.composer.ImageDepotFolder = SctUtils.defaultImageDepot; tbImageDepot.Text = SctUtils.defaultImageDepot;
            }           
            UpdatingOptions = false;              
        }
        private void UpdateToOptions(object sender, RoutedEventArgs e) // visual to options
        {
            if (UpdatingOptions || Utils.isNull(opts)) return;
            int QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
            if (QueryRowHeight > 1) opts.composer.QueryRowHeight = QueryRowHeight;
            opts.composer.QueryColWidth = Convert.ToInt32(colQuery.Width.Value);
            if (rbBoth.IsChecked.Value) opts.composer.SingleAuto = Options.SingleAutoSet.both;
            if (rbCue.IsChecked.Value) opts.composer.SingleAuto = Options.SingleAutoSet.cue;
            if (rbModif.IsChecked.Value) opts.composer.SingleAuto = Options.SingleAutoSet.modif;
            if (rbNone.IsChecked.Value) opts.composer.SingleAuto = Options.SingleAutoSet.none;
            SetbtnComposeEnabled(opts.composer.SingleAuto);           
            opts.composer.OneLineCue = chkOneLineCue.IsChecked.Value;           

            opts.composer.API = cbActiveAPI.Text; 
            opts.composer.ImageDepotFolder = tbImageDepot.Text;           
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {            
            if (Directory.Exists(tbImageDepot.Text))
            {
                tbImageDepot.Foreground = Brushes.Black; 
                opts.composer.ImageDepotFolder = tbImageDepot.Text; opts.Log("@WorkDir");
            }                                
            else tbImageDepot.Foreground = Brushes.Red;            
        }
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = SctUtils.defaultImageDepot;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbImageDepot.Text = dialog.FileName;
            }
        }
        public string prompt
        {
            get { return Utils.stringFlatTextBox(tbCue, true) + Utils.stringFlatTextBox(tbModifier,true); }
        }
        public string PreviewCompose(List<string> selectedCue, string modifiers) 
        {
            string prt = "";
            string ComposeCue(List<string> selCue)
            {
                string rslt = "";
                foreach (string line in selCue)
                {
                    if (line.Equals("")) continue;
                    if (line.Length > 1)
                        if (line.StartsWith("##")) continue;
                    rslt += line + " ";
                }
                return rslt;
            }
            prt = ComposeCue(selectedCue) + modifiers;
            return prt;
        }
        public string Compose(object sender, List<string> selectedCue, string modifiers, bool forced)  // !forced - soft compose by SingleAuto mode; forced - disregard opts.SingleAuto
        { 
            void ComposeCue(List<string> selCue)
            {
                tbCue.Text = "";
                foreach (string line in selCue)
                {
                    if (line.Equals("")) continue;
                    if (line.Length > 1)
                        if (line.StartsWith("##")) continue;
                    tbCue.Text += line + (opts.composer.OneLineCue ? " " : Environment.NewLine);
                }
            }
            bool common = sender == rbBoth || sender == rbCue || sender == rbModif || sender == rbNone;
            common |= sender == null || sender == btnCompose || sender == tcQuery; 
            if (Utils.isNull(selectedCue)) return prompt; // ?? manual
            
            if (sender == cuePoolUC || common)
            {
                if (forced) ComposeCue(selectedCue);
                else
                {
                    if (opts.composer.SingleAuto == Options.SingleAutoSet.cue || opts.composer.SingleAuto == Options.SingleAutoSet.both)
                        ComposeCue(selectedCue);
                }
            }
            if (sender == modifiersUC || common)
            {
                if (forced) tbModifier.Text = modifiers;
                else
                {
                    if (opts.composer.SingleAuto == Options.SingleAutoSet.modif || opts.composer.SingleAuto == Options.SingleAutoSet.both)
                        tbModifier.Text = modifiers;
                }
            }
            return prompt;
        }
        public string Compose(object sender, bool forced)
        {
            if (Utils.isNull(cuePoolUC) || Utils.isNull(modifiersUC)) return "";
            if (Utils.isNull(cuePoolUC.activeCourier)) return "";
            return Compose(sender, cuePoolUC.activeCourier.SelectedCue(), modifiersUC.Composite(), forced);
        }
        public void btnCompose_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(cuePoolUC) || Utils.isNull(modifiersUC)) return;
            if (Utils.isNull(cuePoolUC.activeCourier)) return; 
            Compose(sender, cuePoolUC.activeCourier.SelectedCue(), modifiersUC.Composite(), true); 
        }
        private void QueryAPI(string prompt) // generated image path
        {   
            opts.Log("query -> "+ prompt, Brushes.DarkGreen);
            if (status.Equals(Status.Scanning)) opts.Log("@StartGeneration (" + (scanPromptIdx+1).ToString() + " / " + scanPrompts.Count.ToString() + ")");
            else opts.Log("@StartGeneration (single)");
            if (status.Equals(Status.Scanning) && scanPreviewUC.scanning) // move selection
                scanPreviewUC.selectByIndex(scanPromptIdx); //scanPreviewUC.selectByPropmt(prompt);
            API.Query(prompt, opts.composer.ImageDepotFolder); opts.Log("-=-=-", Brushes.DarkOrange);
            opts.composer.TotalImageCount++;
        }
        private string lastSingleImage = "";
        protected void QueryComplete(string imageFilePath, bool success)
        {
            if (!success)
            {
                if (imageFilePath == "") 
                { 
                    opts.Log("Error[887]: Problem with image generator/server! Look at the server terminal for details."); opts.Log("@StopRun"); status = Status.Idle; 
                }
                else opts.Log("Error with " + imageFilePath);
            }
            switch (status)
            {
                case Status.SingeQuery:
                case Status.Request2Cancel:
                    lastSingleImage = imageFilePath;
                    status = Status.Idle; opts.Log("@EndGeneration: " + imageFilePath); 
                    break;
                case Status.Scanning:                   
                    opts.Log("@EndGeneration: " + imageFilePath);                    
                    if (scanPromptIdx == (scanPrompts.Count-1)) // the end of it, back to init state
                    {
                        status = Status.Idle; 
                        opts.Log("This Scan resulted in " + scanPrompts.Count.ToString()+" images.", Brushes.DarkMagenta);
                        API.activeAPI.Broadcast("end.scan");
                    }                        
                    else // next image gen
                    {
                        scanPromptIdx++; QueryAPI(scanPrompts[scanPromptIdx]); 
                    }
                    break;
            }
            if (status == Status.Idle) // back
            {
                //if (!scanEnd) opts.Log("@EndGeneration");
                btnScan.Content = strScan; btnScan.Background = Brushes.MintCream;                        
                btnScanPreview.IsEnabled = true; scanPreviewUC.scanning = false; btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true;
            }
        }
        private List<string> scanPrompts = new List<string>();
        private int scanPromptIdx;
        private List<string> GetScanPrompts(bool PreviewScan = false)
        {        
            scanPrompts = new List<string>();
            if (cuePoolUC.activeCourier == null) { opts.Log("Error[145]: no cue is selected"); return scanPrompts; }
            List<List<string>> lls = cuePoolUC.activeCourier.GetCues();
            if (lls.Count.Equals(0)) { opts.Log("Error[96]: no cue is selected"); return scanPrompts; }
            List<string> ScanModifs = CombiModifs(modifiersUC.ModifItemsByType(ModifStatus.Scannable), opts.composer.ModifPrefix, Utils.EnsureRange(opts.composer.ModifSample, 1, 9));
            string fis = modifiersUC.FixItemsAsString(); 
            foreach (List<string> ls in lls)
            {
                if (ScanModifs.Count.Equals(0))
                {
                    scanPrompts.Add(Compose(null, ls, fis, true));                    
                }
                else
                {                
                    foreach (string sc in ScanModifs)
                    {
                        if (PreviewScan) scanPrompts.Add(PreviewCompose(ls, fis + (sc.Equals("") ? "" : opts.composer.ModifPrefix) + sc));
                        else scanPrompts.Add(Compose(null, ls, fis + (sc.Equals("") ? "" : opts.composer.ModifPrefix) + sc, true));
                    }
                }
            }
            return scanPrompts;
        }
        public List<string> CombiModifs(List<string> ScanModifs, string separator, int sample = 1) 
        {
            if (sample == 1) return ScanModifs; List<string> rslt = new List<string>();
            if (ScanModifs.Count == 0) return rslt;
            if ((ScanModifs.Count == 1) && ScanModifs[0].Equals("")) return rslt;
            if (sample >= ScanModifs.Count)
                { opts.Log("Error[364]: number of scannable modifiers: " + ScanModifs.Count.ToString() + " must be bigger than modifiers sample: " +sample.ToString()); return rslt; }
            List<string> line = new List<string>(); 
            IEnumerable<IEnumerable<int>> combinations = CombiIndexes(sample, ScanModifs.Count);
            foreach (var combination in combinations)
            {
                line.Clear();
                foreach (int i in combination)
                    if (!ScanModifs[i].Equals("")) line.Add(ScanModifs[i]);
                rslt.Add(string.Join(separator, line.ToArray()));
            }
            return rslt;
        }
        private IEnumerable<IEnumerable<int>> CombiIndexes(int sample, int total) 
        { 
            int BitCount(int x)
            {
                int count = 0;
                while (x != 0)
                {
                    count++;
                    x &= x - 1;
                }
                return count;
            }
            int[] indexes = Enumerable.Range(0, total).ToArray();
            
            return from subset in Enumerable.Range(0, 1 << indexes.Length)
                                where BitCount(subset) == sample
                                select
                                    from i in Enumerable.Range(0, indexes.Length)
                                    where (subset & (1 << i)) != 0
                                    select indexes[i];
         }
     
        public void Request2Cancel() 
        {
            if (Convert.ToString(btnScan.Content).Equals("Cancel") && status == Status.Scanning) // only if scanning
                btnScan_Click(btnScan, null);
        }
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            if (cuePoolUC.activeCourier.GetCues().Count == 0) { opts.Log("Error[97]: no cue is selected"); return; } 
            if (Convert.ToString(btnScan.Content).Equals(strScan))
            {
                if (API.IsBusy) { opts.Log("Error[11]: busy with previous query"); return; }
                switch (status) // if out of place
                {
                    case Status.SingeQuery:
                        opts.Log("Warning: API is busy, try again later..."); return;
                    case Status.Request2Cancel:
                        opts.Log("Warning: Your request for cancelation has been already accepted.", Brushes.Tomato); return;
                    case Status.Scanning:
                        opts.Log("Error[45]: internal error"); return;
                }
                status = Status.Scanning; btnScan.Content = "Cancel"; btnScan.Background = Brushes.Orange;
                btnScanPreview.IsEnabled = false; btnAppend2Preview.IsEnabled = false;
            }
            else // if button title is Cancel
            {
                if (status == Status.Scanning) opts.Log("Warning: User cancelation request!", Brushes.Tomato);
                status = Status.Request2Cancel; btnScan.Content = strScan; btnScan.Background = Brushes.MintCream;
                btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true; return;
            }
            GetScanPrompts();
            if (scanPrompts.Count == 0) { opts.Log("Error[141]: no prompt generated"); return; }
            scanPromptIdx = 0; QueryAPI(scanPrompts[0]);
        }
        private void chkAutoSingle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateToOptions(sender, e);
            Compose(sender, false);
        }
        private void cbActiveAPI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(API)) return;
            ComboBoxItem cbi = (ComboBoxItem)cbActiveAPI.SelectedItem;
            if (Utils.isNull(cbi)) API.activeAPIname = "Simulation";
            else
            {  
                var visName = (string)cbi.Content; opts.composer.API = visName;
                if (visName == "SD-A1111/Forge"|| visName == "SD-ComfyUI") visName = "SDiffusion";
                API.activeAPIname = visName;
            }
            showAPI = API.activeAPI.isDocked;
            if (showAPI) 
            { 
                gridAPI.Children.Clear(); gridAPI.Children.Add(API.activeAPI.userControl);
                API.activeAPI.Init(ref opts); sd_params_UC.Init(ref opts);
            }
            if (!Utils.isNull(e)) e.Handled = true;
            OnAPIparams(API.activeAPIname.Equals("SDiffusion") || API.activeAPIname.Equals("AddonGen"));
        }

        private void imgAPIdialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(API)) { opts.Log("Error[55]: no API is selected."); return; }
            if (Utils.isNull(API.activeAPI)) { opts.Log("Error[22]: no API is selected."); return; }           
            API.activeAPI.opts["IDfolder"] = opts.composer.ImageDepotFolder;
            API.about2Show(ref opts);
            API.ShowDialog();
        }
        private bool CheckAPIready()
        {
            if (Utils.isNull(API)) { opts.Log("Error[56]: no API is selected."); return false; }
            if (Utils.isNull(API.activeAPI)) { opts.Log("Error[21]: no API is selected."); return false; }
            if (API.IsBusy || status != Status.Idle)
                { Utils.TimedMessageBox("API is busy, try again later...", "Warning"); return false; }
            return true;
        }
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAPIready()) return;
            if (opts.composer.SingleAuto != Options.SingleAutoSet.none) Compose(null, false); status = Status.SingeQuery;
            string pro = Compose(sender, cuePoolUC.ActiveCueList?.selectedCues()[0].cueTextAsList(true), modifiersUC.Composite(), false); // TO BE CLEARED !!! selection from image depot problem
            if (pro.Trim().Equals(""))
            {
                if (!Utils.ConfirmationMessageBox("The prompt is empty! Continue anyway... ?")) { status = Status.Idle; return; }
            }
            QueryAPI(pro); 
        }
        private void tbCue_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnQuery.IsEnabled = !tbCue.Text.Trim().Equals("");
        }
        private void tcQuery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(tcQuery.SelectedItem)) return;
            if (sessionStart != null)
            {
                TimeSpan ts = DateTime.Now - sessionStart;
                ChangeModif(sender, e); lbSessionSpan.Content = "Session "+ts.ToString(@"hh\:mm")+" [hh:mm]";
            }
            if (tcQuery.SelectedItem.Equals(tiOptions))
            {
                opts?.Log("@ExplorerPart=100"); return;
            }    
            else opts?.Log("@ExplorerPart=0");
            if (!Utils.isNull(opts))
            {
                if (tcQuery.SelectedItem.Equals(tiSingle))
                {
                    gridPrompt.Visibility = Visibility.Visible; gridSplitCue.Visibility = Visibility.Visible;
                    pnlCue.Height = new GridLength(Utils.EnsureRange(opts.composer.QueryRowHeight, 150, 500));
                }
                else
                {
                    opts.composer.QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
                    gridPrompt.Visibility = Visibility.Collapsed; gridSplitCue.Visibility = Visibility.Collapsed;
                    pnlCue.Height = new GridLength(0);
                }
            }
            tbModifier.Text = "";
            if (Utils.isNull(cuePoolUC)) return;
            cuePoolUC.radioMode = tcQuery.SelectedItem.Equals(tiSingle);           
            modifiersUC.SetSingleScanMode(tcQuery.SelectedItem.Equals(tiSingle));
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private readonly string[] miTitles = { "Copy", "Cut", "Paste", "\"...\" synonyms", "\"...\" meaning", "\"...\" antonyms" };
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
            for (int i = 3; i < 6; i++)
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
            if (header.EndsWith("synonyms") || header.EndsWith("meaning") || header.EndsWith("antonyms")) Utils.AskTheWeb(header.Replace("\"", string.Empty));
        }
        private void btnScanPreview_Click(object sender, RoutedEventArgs e)
        {
            scanPreviewUC.lbCheckCount.Content = "processing..."; scanPreviewUC.lbCheckCount.Foreground = Brushes.Tomato;
            tcModScanPre.SelectedIndex = 1; tcModScanPre.UpdateLayout(); Utils.DoEvents(); //DateTime t0 = DateTime.Now;
            List<string> ls = (sender == btnAppend2Preview) ? new List<string>(scanPreviewUC.allPrompts) : new List<string>();
            
            GetScanPrompts(true); 
            if (scanPrompts.Count == 0) { opts.Log("Warning: No prompts generated"); scanPreviewUC.lbCheckCount.Content = ""; return; }
            ls.AddRange(scanPrompts); //opts.Log("time 2: " + ((DateTime.Now - t0).TotalSeconds).ToString("G3") + " [sec]");
            tiModifiers.Visibility = Visibility.Visible; tiScanPreview.Visibility = Visibility.Visible;
            scanPreviewUC.lbCheckCount.Content = "loading...";
            scanPreviewUC.LoadPrompts(ls); //opts.Log("time 1: " + ((DateTime.Now - t0).TotalSeconds).ToString("G3") + " [sec]"); 
        }
        private void btnScanPreviewProcs_Click(object sender, RoutedEventArgs e)
        {
            if (scanPreviewUC.btnScanChecked.Equals(sender)) 
            {
                if (scanPreviewUC.scanning) // cancel request
                {
                    scanPreviewUC.scanning = false; status = Status.Request2Cancel;
                    opts.Log("Warning: User cancelation request!", Brushes.Tomato); btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true;
                }
                else // scanning request
                {
                    scanPrompts = scanPreviewUC.checkedPrompts();
                    if (scanPrompts.Count == 0) { opts.Log("Error[285]: no prompts checked"); return; }
                    status = Status.Scanning; scanPreviewUC.scanning = true; btnScanPreview.IsEnabled = false; btnAppend2Preview.IsEnabled = false;
                    scanPromptIdx = 0; QueryAPI(scanPrompts[0]);
                }
            }
            if (scanPreviewUC.btnQuerySelected.Equals(sender))
            {               
                if (scanPreviewUC.selectedPrompt == "") { opts.Log("Error[74]: no prompt selected"); return; }
                status = Status.SingeQuery;
                QueryAPI(scanPreviewUC.selectedPrompt);
            }
        }
        public void CuePoolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != cuePoolUC.tabControl) { Utils.TimedMessageBox("wrong sender"); return; }
            TabItem ti = cuePoolUC.tabControl.SelectedItem as TabItem;
            if (ti == null) return;
            gridSingle.IsEnabled = ti.Equals(cuePoolUC.tiA_pool) || ti.Equals(cuePoolUC.tiB_pool) || ti.Equals(cuePoolUC.tiImageDepot);
            gridPrompt.IsEnabled = gridSingle.IsEnabled; gridScan.IsEnabled = gridSingle.IsEnabled;
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(prompt); 
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #region sMacro
        public List<string> SelectCues(int percentage, int idx)
        {
            tcQuery.SelectedItem = tiScan;
            return cuePoolUC.ActiveCueList.RandomSelect(percentage, idx);
        }
        public string Text2Image(string prmt = "")
        {
            if (!CheckAPIready()) return "Error[5]: API error (see log).";
            if (prmt == "")
            {
                if (!tcQuery.SelectedItem.Equals(tiSingle)) return "Error[4]: In Composer tab turn to Single and select a cue.";
                btnQuery_Click(btnQuery, null); Utils.DoEvents();
            }
            else
            {
                status = Status.SingeQuery;
                QueryAPI(prmt);
            }
            //asyncGenerateImage();
            while (status.Equals(Status.SingeQuery)) { Utils.Sleep(200); Utils.DoEvents(); }
            return lastSingleImage;
        }
        /*private async void asyncGenerateImage()
        {
            while (await GetBusyStatusAsync(Status.SingeQuery)) Utils.Sleep(200);
            Utils.Sleep(200);
        }*/
        private Task<bool> GetBusyStatusAsync(Status _status)
        {
            return Task.Run(() =>
            {
                return status == _status;
            });
            //return status == _status;
        }
        public bool mSetApply(string mSet, bool append)
        {
            if (mSet == string.Empty) return true;
            return modifiersUC.mSetStack.mSetApply(mSet, append);
        }
        public List<string> GetPreview(bool append)
        {            
            if (append) btnScanPreview_Click(btnAppend2Preview, null);
            else btnScanPreview_Click(btnScanPreview, null);
            return scanPreviewUC.GetPrompts(false);
        }
        public int SetPreview(List<string> prompts, bool append)
        {
            return scanPreviewUC.LoadPrompts(prompts);
        }
        public List<Tuple<string,string>> ScanImages(bool fromPreview)
        {
            if (!CheckAPIready()) { opts.Log("Error[5]: API error (see log)."); return null; }
            API.iiList = new List<Tuple<string, string>>();
            
            if (!tcQuery.SelectedItem.Equals(tiScan)) tcQuery.SelectedItem = tiScan;
            asyncScanImages(fromPreview);
            List<Tuple<string, string>> deepCopiedList = API.iiList.Select(tuple => new Tuple<string, string>(tuple.Item1, tuple.Item2)).ToList();
            return deepCopiedList;
        }
        public async void asyncScanImages(bool fromPreview)
        {
            if (fromPreview) btnScanPreviewProcs_Click(scanPreviewUC.btnScanChecked, null);
            else btnScan_Click(btnScan, null);
            while (await GetBusyStatusAsync(Status.Scanning)) { Utils.Sleep(300); } //Utils.DoEvents();           
        }        
        public List<Tuple<string, string>> PromptList2Image(List<string> prompts)
        {
            if (!CheckAPIready()) { opts.Log("Error[5]: API error (see log)."); return null; }
            var lt = new List<Tuple<string, string>>();
            foreach (string prompt in prompts)
            {
                string fp = Text2Image(prompt);
                lt.Add(new Tuple<string, string>(prompt, fp));
            }
            return lt;
        }
        public string ImageDepot(string command, string folder)
        {
            string newFolder = "";
            switch (command.ToLower())
            {
                default:
                case "get": return opts.composer.ImageDepotFolder;
                case "create": Directory.CreateDirectory(folder); Utils.Sleep(500);
                    if (Directory.Exists(folder)) newFolder = folder;
                    else { opts.Log("Error[64]: image depot create failed."); return ""; }
                    break;
                case "switch":
                    if (Directory.Exists(folder)) { newFolder = folder; opts.composer.ImageDepotFolder = newFolder; }
                    else { opts.Log("Error[65]: image depot switch failed."); return ""; }
                    break;
                case "setnext":
                    if (!Directory.Exists(opts.composer.ImageDepotFolder)) { opts.Log("Error[66]: work image depot folder - not found."); return ""; }
                    string mFolder = folder == string.Empty ? Utils.timeName() : folder;
                    newFolder = Path.Combine(Utils.GetParrentDirectory(opts.composer.ImageDepotFolder), mFolder);
                    if (!Directory.Exists(newFolder)) { Directory.CreateDirectory(newFolder); Utils.Sleep(500); } 
                    else { opts.Log("Error[67]: folder <"+newFolder+"> already exists"); return ""; }
                    break;
            }
            tbImageDepot.Text = newFolder;  
            return newFolder;
        }
        public string getStatus { get { return status.ToString(); } }
        protected void ExtCollInvisible(object sender, EventArgs e)
        {
            ExtCollMngVisibility = false;
        }
        protected bool _ExtCollMngVisibility;
        protected bool ExtCollMngVisibility
        {
            get { return _ExtCollMngVisibility; }
            set
            {
                if (_ExtCollMngVisibility == value) return;
                tiModifiers.IsEnabled = !value; tiScanPreview.IsEnabled = !value; tiSD_API.IsEnabled = !value;
                if (value) 
                { 
                    tcModScanPre.SelectedItem = tiExtCollMng;
                    tiModifiers.Visibility = Visibility.Hidden; tiScanPreview.Visibility = Visibility.Hidden; tiSD_API.Visibility = Visibility.Hidden;
                }
                else 
                { 
                    tcModScanPre.SelectedItem = tiModifiers;
                    tiModifiers.Visibility = Visibility.Visible; tiScanPreview.Visibility = Visibility.Visible; tiSD_API.Visibility = Visibility.Visible;
                }
                Utils.DoEvents(); _ExtCollMngVisibility = value;
            }
        }        
        private void btnExtColl_Click(object sender, RoutedEventArgs e)
        {
            ExtCollMngVisibility = true;
            if (cuePoolUC.extCollUC.IsCollectionFolder(cuePoolUC.cuesFolder)) extCollMng.UpdateCollInfo(cuePoolUC.cuesFolder);
            else extCollMng.UpdateCollInfo();
        }

        private void tcModScanPre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //opts?.Log(ExtCollMngVisibility.ToString()+ " / " + tcModScanPre.SelectedItem.ToString());  
        }

        public List<Tuple<string, string>> HelpList()
        {
            List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
            ls.Add(new Tuple<string, string>(
                "Text2Image(string prompt)", "Generate image with given prompt. If prompt is missing, it takes selected prompt from the composer which must be in Single mode. Returns the image filename"
                ));
            ls.Add(new Tuple<string, string>(
                "SelectCues(int percentage, int idx)", "Select precentage of cues in selected pool; if idx = 0 on selected tab; if idx = -1 on all the tabs in the pool; if idx > 0 on the idx tab in the pool. Returns a list of selected cues."
                )); 
            ls.Add(new Tuple<string, string>(
                "mSetApply(string mSet, bool append)", "Apply mSet to modifiers, in addition (append) or not to the checked already modifiers. Return True if successful."
                ));
            ls.Add(new Tuple<string, string>(
                "GetPreview(bool append)", "Generate prompts combining selected cues with checked modifiers. The composer must be in Scan mode and some cues must be selected. append is to add to or replace current selection of modifiers. If mSet = Reset all modifiers are deselected. Return a list of the generated prompts."
                ));
            ls.Add(new Tuple<string, string>(
                "SetPreview(List<string> prompts, bool append)", "Generate prompts combining selected cues with selected modifiers. The composer must be in Scan mode and some cues must be selected. If mSet is empty the current set of modifiers is used. append is to add to or replace current selection of modifiers. If mSet = 'Reset' all modifiers are deselected. Returns the total nunber of preview prompts."
                ));
            ls.Add(new Tuple<string, string>(
                "ScanImages(bool fromPreview)", "Generate series of images from prompts from the composer. fromPreview - if True a preview list is used, if False - the same as pressing the Scan button on the composer. The composer must be in Scan mode and some cues must be checked. Return list of tuples (prompt,filepath)"         
                ));
            ls.Add(new Tuple<string, string>(
                "PromptList2Image(List<string> prompts)", "Generate image with given prompts in a file. file format is the same as in save file in preview panel. Return list of tuples (prompt,filepath)"
                ));
            ls.Add(new Tuple<string, string>(
                "ImageDepot(string command, string folder)", "Command ->\nget: get working image depot folder \ncreate: create <folder> directory\nswitch: switch working image depot to <folder>\nsetNext: create and set working image-depot-folder next to current working IDF. If folder is empty it creates a time-stamp folder.\nReturn the absolute path to the new ImageDepot.\ne.g. ImageDepot('setnext','')   ImageDepot('get','')"
                ));
            ls.Add(new Tuple<string, string>(
                "getStatus", "Get the current status of the composer. [Idle, SingeQuery, Scanning, Request2Cancel]"                
                ));
            return ls;
        }
        #endregion
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog(); //dialog.InitialDirectory = ImgUtils.defaultImageDepot;
            dialog.Title = "Select an empty folder for the exported image depot";
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            string md = ""; string ext = "";
            switch (ImgUtils.GetImageType(dialog.FileName))
            {
                case ImgUtils.ImageType.Jpg: md = ImgUtils.GetJpgMetadata(dialog.FileName); ext = ".jtx";
                    break;
                case ImgUtils.ImageType.Png: ImgUtils.GetMetadataStringComfy(dialog.FileName, out md); ext = ".ptx";
                    break;
            }
            File.WriteAllText(Path.ChangeExtension(dialog.FileName, ext), md);


            //string md = File.ReadAllText(@"d:/meta1.txt");// ImgUtils.GetJpgMetadataExt(@"d:/ComfyUI_00310_.jpg");

            //ImgUtils.SetJpgMetadata(@"d:/24-11-09_10-18-56.jpg", md); //@"d:/test.jpg"
            
            opts.Log(md);
            /*List<string> files = new List<string>(Directory.GetFiles(tbImageDepot.Text, "*.png"));
            opts.Log(files.Count.ToString()+" files"); DateTime t0 = DateTime.Now;
            Dictionary<string, string> meta; int ns = 0;
            foreach (string fn in files)
            {
                if (ImgUtils.GetMetaDataItems(fn, out meta)) ns++;
            }
            double t = (DateTime.Now - t0).TotalSeconds;
            opts.Log("time taken = "+t.ToString("G3")+" [sec]  "+ns.ToString()+" files OK");
            opts.Log("time " + (t / ns).ToString("G3") + " [sec] per file");
            //CombiIndexes(3, 5);
           
            new MiniTimedMessage("===============").ShowDialog();*/

        }

    }
}
