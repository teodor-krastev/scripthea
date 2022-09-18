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
using UtilsNS;

namespace scripthea
{
    public enum Status
    {
        Idle, SingeQuery, Scanning, Request2Cancel 
    }
    /// <summary>
    /// Interaction logic for QueryUC.xaml
    /// </summary>
    public partial class QueryUC : UserControl
    {
        string defaultImageFolder = Utils.basePath + "\\images\\";
        public ControlAPI API;
        public Options opts;
        public QueryUC()
        {
            InitializeComponent();
        }
        public void Init(ref Options _opts)
        {
            status = Status.Idle;
            opts = _opts;
            UpdateFromOptions();

            seedListUC.Init(); seedListUC.OnChange += new RoutedEventHandler(ChangeCue);
            modifiersUC.Init(); modifiersUC.OnChange += new RoutedEventHandler(ChangeModif);

            API = new ControlAPI(); cbActiveAPI_SelectionChanged(null, null);
            API.OnQueryComplete += new ControlAPI.APIEventHandler(QueryComplete);

            if (Utils.TheosComputer()) cbiCraiyon.Visibility = Visibility.Visible;
            else cbiCraiyon.Visibility = Visibility.Collapsed;
        }
        public void Finish()
        {
            UpdateToOptions(null, null);
            if (!Utils.isNull(API))
            {
               API.eCancel = false; API.Close();
            }
        }
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
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
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
            if (opts.SingleAuto && seedListUC.radioMode) btnCompose_Click(sender,e);
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
            colQuery.Width = new GridLength(opts.QueryColWidth);
            chkAutoSingle.IsChecked = opts.SingleAuto; btnCompose.IsEnabled = !opts.SingleAuto;              
            chkOneLineCue.IsChecked = opts.OneLineCue;           

            cbActiveAPI.Text = opts.API; tbModifPrefix.Text = opts.ModifPrefix; 
            if (Directory.Exists(opts.ImageDepotFolder)) tbImageDepot.Text = opts.ImageDepotFolder;
            else
            {
                Log("Directory <"+tbImageDepot.Text+"> does not exist. Setting to default directory :"+ defaultImageFolder);
                opts.ImageDepotFolder = defaultImageFolder; tbImageDepot.Text = defaultImageFolder;
            }           
            UpdatingOptions = false;              
        }
        private void UpdateToOptions(object sender, RoutedEventArgs e) // visual to internal options
        {
            if (UpdatingOptions || Utils.isNull(opts)) return;
            opts.QueryColWidth = Convert.ToInt32(colQuery.Width.Value);
            opts.SingleAuto = chkAutoSingle.IsChecked.Value; btnCompose.IsEnabled = !opts.SingleAuto;           
            opts.OneLineCue = chkOneLineCue.IsChecked.Value;           

            opts.API = cbActiveAPI.Text; opts.ModifPrefix = tbModifPrefix.Text;
            opts.ImageDepotFolder = tbImageDepot.Text;           
        }
        private void tbImageDepo_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateToOptions(sender, null); Log("@WorkDir");
        }
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = defaultImageFolder;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbImageDepot.Text = dialog.FileName;
            }
        }
        public string fullCue
        {
            get { return tbCue.Text + tbModifier.Text; }
        }

        public string Compose(object sender,  SeedItemUC selectedSeed, string modifiers, bool OneLineCue = true)
        {
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == seedListUC)
            {
                tbCue.Text = "";
                List<string> ls = selectedSeed.seedTextAsList(true);
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
            return fullCue;
        }
        public void btnCompose_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(seedListUC) || Utils.isNull(modifiersUC)) return;
            if (Utils.isNull(seedListUC.allSeeds) || Utils.isNull(modifiersUC.catModifs)) return;
            List<SeedItemUC> selectedSeed = seedListUC.selectedSeeds;
            if (Utils.isNull(selectedSeed)) { Log("Err: no cue is selected"); return; }
            if (selectedSeed.Count.Equals(0)) { Log("Err: no cue is selected"); return; }
            Compose(sender, selectedSeed[0], modifiersUC.Composite());
        }
        private void QueryAPI(string cue)
        {   
            Log("query -> "+cue, Brushes.DarkGreen); Log("@StartProc");
            API.Query(cue, opts.ImageDepotFolder);
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
                    if (scanCuesIdx > (scanCues.Count - 1))
                    {
                        status = Status.Idle; btnScan.Content = "S c a n"; btnScan.Background = Brushes.MintCream;
                        Log("This Scan resulted in " + scanCues.Count.ToString()+" images.", Brushes.DarkMagenta);
                    }                        
                    else
                    {
                        QueryAPI(scanCues[scanCuesIdx]); scanCuesIdx++;
                    }
                    break;
            }
        }
        private List<string> scanCues;
        private int scanCuesIdx;
        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            List<SeedItemUC> selectedSeeds = seedListUC.selectedSeeds;
            if (Convert.ToString(btnScan.Content).Equals("S c a n"))
            {
                if (Utils.isNull(selectedSeeds)) { Log("Err: no cue is selected"); return; }
                if (selectedSeeds.Count.Equals(0)) { Log("Err: no cue is selected"); return; }
                if (API.IsBusy) { Log("Err: busy with previous query"); return; }
                switch (status)
                {
                    case Status.SingeQuery:
                        Utils.TimedMessageBox("API is busy, try again later..."); return;
                    case Status.Request2Cancel:
                        Log("Your request for cancelation has been already accepted.", Brushes.Tomato); return;
                    case Status.Scanning:
                        Log("Err: internal error #45"); return;
                }
                status = Status.Scanning; btnScan.Content = "Cancel"; btnScan.Background = Brushes.Coral;
            } 
            else
            {
                if (status == Status.Scanning) Log("User cancelation!", Brushes.Tomato);
                status = Status.Request2Cancel; btnScan.Content = "S c a n"; btnScan.Background = Brushes.MintCream; return;
            } 
            List<string> ScanModifs = modifiersUC.ScanItems(); scanCues = new List<string>(); 
            foreach (SeedItemUC ssd in selectedSeeds)
            {
                if (ScanModifs.Count.Equals(0))
                {
                    scanCues.Add(Compose(null, ssd, ""));                    
                }
                else
                    foreach (string sc in ScanModifs)
                    {
                        scanCues.Add(Compose(null, ssd, opts.ModifPrefix + sc));                        
                    }
            }
            if (scanCues.Count == 0) { Log("Err: no cues is selected"); return; };
            scanCuesIdx = 1; QueryAPI(scanCues[0]);
        }
        private void chkAutoSingle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateToOptions(sender, e);
            btnCompose_Click(sender, e);
        }
        private void tbModifSepar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.ModifPrefix = tbModifPrefix.Text;
            if (!Utils.isNull(modifiersUC))  modifiersUC.separator = tbModifPrefix.Text;
        }

        private void cbActiveAPI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(API)) return;
            ComboBoxItem cbi = (ComboBoxItem)cbActiveAPI.SelectedItem;
            API.activeAPIname = (string)cbi.Content; opts.API = API.activeAPIname;
        }

        private void imgAPIdialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected."); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (2)"); return; }           
            API.activeAPI.opts["folder"] = opts.ImageDepotFolder;
            API.about2Show(fullCue);
            API.ShowDialog();
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected."); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (2)"); return; }
            if (API.IsBusy || status != Status.Idle)
            {
                Utils.TimedMessageBox("API is busy, try again later...", "Warning"); return;
            }            
            btnCompose_Click(null, null); status = Status.SingeQuery;
            QueryAPI(Compose(null, seedListUC.selectedSeeds[0], modifiersUC.Composite()));
        }

        private void tbCue_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnQuery.IsEnabled = !tbCue.Text.Trim().Equals("");
        }

        private void imgCopy_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(fullCue);
        }
        private void tcQuery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utils.isNull(tcQuery.SelectedItem)) return;
            if (tcQuery.SelectedItem.Equals(tiOptions)) return;
            tbModifier.Text = "";
            if (Utils.isNull(seedListUC)) return;
            seedListUC.radioMode = tcQuery.SelectedItem.Equals(tiSingle);
            ChangeModif(sender, e);
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
    }
}
