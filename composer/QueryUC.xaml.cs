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
        const string strScan = "S c a n";
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
            cuePoolUC.OnLog += new Utils.LogHandler(Log);
            cuePoolUC.Init(ref opts);
            foreach (Courier cr in cuePoolUC.couriers)
                cr.OnCueSelection += new Courier.CueSelectionHandler(ChangeCue);            
            
            Log("@_Header=loading modifiers files (*.mdfr)");
            modifiersUC.OnChange += new RoutedEventHandler(ChangeModif);
            modifiersUC.OnLog += new Utils.LogHandler(Log);
            modifiersUC.Init(ref opts);

            scanPreviewUC.OnLog += new Utils.LogHandler(Log);
            scanPreviewUC.btnClose.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnScanChecked.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnQuerySelected.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);

            //tiMiodifiers.Visibility = Visibility.Collapsed; tiScanPreview.Visibility = Visibility.Collapsed;

            API = new ControlAPI(ref opts); 
            if (API.interfaceAPIs.ContainsKey("SDiffusion"))             
                API.interfaceAPIs["SDiffusion"].APIparamsEvent += new APIparamsHandler(OnAPIparams);
            cbActiveAPI_SelectionChanged(null, null);
            API.OnQueryComplete += new ControlAPI.APIEventHandler(QueryComplete);
            API.OnLog += new Utils.LogHandler(Log);
            sd_params_UC.Init(ref opts);
            cuePoolUC.OnSDparams += new Utils.LogHandler(sd_params_UC.ImportImageInfo);
            
            if (Utils.TheosComputer() && Utils.isInVisualStudio) { btnTest.Visibility = Visibility.Visible; }
            else { btnTest.Visibility = Visibility.Collapsed; }              
        }
        public void Finish()
        {
            sd_params_UC.Finish();
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
                    case Status.Scanning: 
                    case Status.Request2Cancel:
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
       
        protected Dictionary<string, object> OnAPIparams(bool? showIt)
        {
            if (showIt != null)
            {
                if ((bool)showIt) tiSD_API.Visibility = Visibility.Visible;
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
        /*protected void ChangeCue(object sender, RoutedEventArgs e)
        {
            //Log("conditions changed");
            if (opts.SingleAuto && cuePoolUC.radioMode) btnCompose_Click(sender,e);
        }*/

        protected void ChangeCue(List<string> selCues)
        {
            //Log("conditions changed");
            if (opts.composer.SingleAuto && cuePoolUC.radioMode) Compose(null, selCues, modifiersUC.Composite());
        }
        protected void ChangeModif(object sender, RoutedEventArgs e)
        {
            //Log("conditions changed");           
            if (opts.composer.SingleAuto && tcQuery.SelectedItem.Equals(tiSingle)) btnCompose_Click(sender,e);
        }

        private bool UpdatingOptions = false;
        private void UpdateFromOptions() // internal to visual options
        {
            if (Utils.isNull(opts)) return;
            UpdatingOptions = true;
            pnlCue.Height = new GridLength(Utils.EnsureRange(opts.composer.QueryRowHeight, 150, 500));
            colQuery.Width = new GridLength(opts.composer.QueryColWidth);
            chkAutoSingle.IsChecked = opts.composer.SingleAuto; btnCompose.IsEnabled = !opts.composer.SingleAuto;              
            chkOneLineCue.IsChecked = opts.composer.OneLineCue;           

            cbActiveAPI.Text = opts.composer.API; 
            if (Directory.Exists(opts.composer.ImageDepotFolder)) tbImageDepot.Text = opts.composer.ImageDepotFolder;
            else
            {
                Log("Directory <"+tbImageDepot.Text+"> does not exist. Setting to default directory :"+ ImgUtils.defaultImageDepot);
                opts.composer.ImageDepotFolder = ImgUtils.defaultImageDepot; tbImageDepot.Text = ImgUtils.defaultImageDepot;
            }           
            UpdatingOptions = false;              
        }
        private void UpdateToOptions(object sender, RoutedEventArgs e) // visual to internal options
        {
            if (UpdatingOptions || Utils.isNull(opts)) return;
            int QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
            if (QueryRowHeight > 1) opts.composer.QueryRowHeight = QueryRowHeight;
            opts.composer.QueryColWidth = Convert.ToInt32(colQuery.Width.Value);
            opts.composer.SingleAuto = chkAutoSingle.IsChecked.Value; btnCompose.IsEnabled = !opts.composer.SingleAuto;           
            opts.composer.OneLineCue = chkOneLineCue.IsChecked.Value;           

            opts.composer.API = cbActiveAPI.Text; 
            opts.composer.ImageDepotFolder = tbImageDepot.Text;           
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {            
            if (Directory.Exists(tbImageDepot.Text))
            {
                tbImageDepot.Foreground = Brushes.Black; 
                opts.composer.ImageDepotFolder = tbImageDepot.Text; Log("@WorkDir");
            }                                
            else tbImageDepot.Foreground = Brushes.Red;            
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
        public string prompt
        {
            get { return Utils.flattenTextBox(tbCue, true) + Utils.flattenTextBox(tbModifier,true); }
        }
        public string Compose(object sender, List<string> selectedCue, string modifiers) // , bool OneLineCue = true ->redundant 
        {
            if (Utils.isNull(selectedCue)) return prompt;
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == cuePoolUC || sender == chkAutoSingle)
            {
                tbCue.Text = "";
                foreach (string line in selectedCue)
                {
                    if (line.Equals("")) continue;
                    if (line.Length > 1)
                        if (line.Substring(0, 2).Equals("##")) continue;
                    tbCue.Text += line + (opts.composer.OneLineCue ? ' ' : '\r');
                }
            }
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == modifiersUC || sender == chkAutoSingle)
                tbModifier.Text = modifiers;
            return prompt;
        }
        public void btnCompose_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(cuePoolUC) || Utils.isNull(modifiersUC)) return;
            if (Utils.isNull(cuePoolUC.activeCourier)) return; 
            Compose(sender, cuePoolUC.activeCourier.SelectedCue(), modifiersUC.Composite()); 
        }
        private void QueryAPI(string prompt)
        {   
            Log("query -> "+ prompt, Brushes.DarkGreen);
            if (status.Equals(Status.Scanning)) Log("@StartGeneration (" + (scanPromptIdx+1).ToString() + " / " + scanPrompts.Count.ToString() + ")");
            else Log("@StartGeneration (single)");
            if (status.Equals(Status.Scanning) && scanPreviewUC.scanning) // move selection
                scanPreviewUC.selectByPropmt(prompt);
            API.Query(prompt, opts.composer.ImageDepotFolder); Log("---", Brushes.DarkOrange); 
        }
        protected void QueryComplete(string imageFilePath, bool success)
        {
            if (!success)
            {
                if (imageFilePath == "") { Log("Stable Diffusion is not connected!", Brushes.Red); status = Status.Idle; }
                else Log("Error with " + imageFilePath);
            }
            bool scanEnd = false;
            switch (status)
            {
                case Status.SingeQuery:
                case Status.Request2Cancel:
                    status = Status.Idle; Log("@EndGeneration: " + imageFilePath);
                    break;
                case Status.Scanning:                   
                    Log("@EndGeneration: " + imageFilePath);                    
                    if (scanPromptIdx == (scanPrompts.Count-1)) // the end of it, back to init state
                    {
                        status = Status.Idle; scanEnd = true;
                        Log("This Scan resulted in " + scanPrompts.Count.ToString()+" images.", Brushes.DarkMagenta);
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
                //if (!scanEnd) Log("@EndGeneration");
                btnScan.Content = strScan; btnScan.Background = Brushes.MintCream;                        
                btnScanPreview.IsEnabled = true; scanPreviewUC.scanning = false; btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true;
            }
        }
        private List<string> scanPrompts = new List<string>();
        private int scanPromptIdx;
        private void GetScanPrompts()
        {        
            scanPrompts = new List<string>(); 
            if (cuePoolUC.activeCourier == null) { Log("Err: no cue is selected (err.code:12)"); return; }
            List<List<string>> lls = cuePoolUC.activeCourier.GetCues();
            if (lls.Count.Equals(0)) { Log("Err: no cue is selected (err.code:96)"); return; }
            List<string> ScanModifs = CombiModifs(modifiersUC.ModifItemsByType(ModifStatus.Scannable), opts.composer.ModifPrefix, Utils.EnsureRange(opts.composer.ModifSample, 1, 9));
            foreach (List<string> ls in lls)
            {
                if (ScanModifs.Count.Equals(0))
                {
                    scanPrompts.Add(Compose(null, ls, modifiersUC.FixItemsAsString()));                    
                }
                else
                    foreach (string sc in ScanModifs)
                    {
                        scanPrompts.Add(Compose(null, ls, modifiersUC.FixItemsAsString() + (sc.Equals("") ? "" : opts.composer.ModifPrefix) + sc));                        
                    }
            }            
        }
        public List<string> CombiModifs(List<string> ScanModifs, string separator, int sample = 1) 
        {
            if (sample == 1) return ScanModifs; List<string> rslt = new List<string>();
            if (ScanModifs.Count == 0) return rslt;
            if ((ScanModifs.Count == 1) && ScanModifs[0].Equals("")) return rslt;
            if (sample >= ScanModifs.Count)
                { Log("Error: number of scannable modifiers: " + ScanModifs.Count.ToString() + " must be bigger than modifiers sample: " +sample.ToString()); return rslt; }
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
            if (Convert.ToString(btnScan.Content).Equals(strScan))
            {
                if (API.IsBusy) { Log("Err: busy with previous query"); return; }
                switch (status) // if out of place
                {
                    case Status.SingeQuery:
                        Log("Warning: API is busy, try again later..."); return;
                    case Status.Request2Cancel:
                        Log("Warning: Your request for cancelation has been already accepted.", Brushes.Tomato); return;
                    case Status.Scanning:
                        Log("Err: internal error #45"); return;
                }
                status = Status.Scanning; btnScan.Content = "Cancel"; btnScan.Background = Brushes.Coral;
                btnScanPreview.IsEnabled = false; btnAppend2Preview.IsEnabled = false;
            }
            else
            {
                if (status == Status.Scanning) Log("Warning: User cancelation!", Brushes.Tomato);
                status = Status.Request2Cancel; btnScan.Content = strScan; btnScan.Background = Brushes.MintCream;
                btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true; return;
            }
            GetScanPrompts();
            if (scanPrompts.Count == 0) { Log("Err: no prompt generated"); return; }
            scanPromptIdx = 0; QueryAPI(scanPrompts[0]);
        }
        private void chkAutoSingle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateToOptions(sender, e);
            btnCompose_Click(sender, e);
        }
        private void cbActiveAPI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(API)) return;
            ComboBoxItem cbi = (ComboBoxItem)cbActiveAPI.SelectedItem;
            if (Utils.isNull(cbi)) API.activeAPIname = "Simulation";
            else API.activeAPIname = (string)cbi.Content; 
            opts.composer.API = API.activeAPIname; showAPI = API.activeAPI.isDocked;
            if (showAPI) 
            { 
                gridAPI.Children.Clear(); gridAPI.Children.Add(API.activeAPI.userControl);
                API.activeAPI.Init(ref opts);
            }
            if (!Utils.isNull(e)) e.Handled = true;
        }

        private void imgAPIdialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected. (55)"); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (22)"); return; }           
            API.activeAPI.opts["folder"] = opts.composer.ImageDepotFolder;
            API.about2Show(ref opts);
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
            if (opts.composer.SingleAuto) btnCompose_Click(null, null); status = Status.SingeQuery;
            string pro = Compose(sender, cuePoolUC.ActiveCueList?.selectedCues()[0].cueTextAsList(true), modifiersUC.Composite()); // TO BE CLEARED !!! selection from image depot problem
            QueryAPI(pro); 
        }
        private void tbCue_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnQuery.IsEnabled = !tbCue.Text.Trim().Equals("");
        }

        private void imgCopy_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            
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
                if (tcQuery.SelectedItem.Equals(tiSingle)) pnlCue.Height = new GridLength(Utils.EnsureRange(opts.composer.QueryRowHeight, 150,500));
                else
                {
                    opts.composer.QueryRowHeight = Convert.ToInt32(pnlCue.Height.Value);
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
            if (header.EndsWith("synonyms") || header.EndsWith("meaning")) Utils.AskTheWeb(header.Replace("\"", string.Empty));
        }
        private void btnScanPreview_Click(object sender, RoutedEventArgs e)
        {
            List<string> ls = (sender == btnAppend2Preview) ? new List<string>(scanPreviewUC.allPrompts) : new List<string>();
            GetScanPrompts();
            if (scanPrompts.Count == 0) { Log("Warning: No prompts generated"); return; };
            ls.AddRange(scanPrompts);

            tiModifiers.Visibility = Visibility.Visible; tiScanPreview.Visibility = Visibility.Visible; 
            tcModScanPre.SelectedIndex = 1; Utils.DoEvents();
            scanPreviewUC.LoadPrompts(ls);
        }
        private void btnScanPreviewProcs_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(scanPreviewUC.btnScanChecked))
            {
                if (scanPreviewUC.scanning)
                {
                    scanPreviewUC.scanning = false; status = Status.Request2Cancel;
                    Log("Warning: User cancelation!", Brushes.Tomato); btnScanPreview.IsEnabled = true; btnAppend2Preview.IsEnabled = true;
                }
                else
                {
                    scanPrompts = scanPreviewUC.checkedPrompts();
                    if (scanPrompts.Count == 0) { Log("Err: no prompts checked"); return; }
                    status = Status.Scanning; scanPreviewUC.scanning = true; btnScanPreview.IsEnabled = false; btnAppend2Preview.IsEnabled = false;
                    scanPromptIdx = 0; QueryAPI(scanPrompts[0]);
                }
            }
            if (sender.Equals(scanPreviewUC.btnQuerySelected))
            {               
                if (scanPreviewUC.selectedPrompt == "") { Log("Err: no prompt selected"); return; }                
                QueryAPI(scanPreviewUC.selectedPrompt);
            }
        }        
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(prompt);
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            /*List<string> files = new List<string>(Directory.GetFiles(tbImageDepot.Text, "*.png"));
            Log(files.Count.ToString()+" files"); DateTime t0 = DateTime.Now;
            Dictionary<string, string> meta; int ns = 0;
            foreach (string fn in files)
            {
                if (ImgUtils.GetMetaDataItems(fn, out meta)) ns++;
            }
            double t = (DateTime.Now - t0).TotalSeconds;
            Log("time taken = "+t.ToString("G3")+" [sec]  "+ns.ToString()+" files OK");
            Log("time " + (t / ns).ToString("G3") + " [sec] per file");*/
            //CombiIndexes(3, 5);
        }

    }
}
