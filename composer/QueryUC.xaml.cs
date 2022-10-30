﻿using System;
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

namespace scripthea.composer
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
        //public string defaultImageFolder = Utils.basePath + "\\images\\";
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

            cueListUC.Init(); cueListUC.OnChange += new RoutedEventHandler(ChangeCue);
            modifiersUC.Init(); modifiersUC.OnChange += new RoutedEventHandler(ChangeModif);
            scanPreviewUC.btnClose.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnScanChecked.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);
            scanPreviewUC.btnQuerySelected.Click += new RoutedEventHandler(btnScanPreviewProcs_Click);

            tiMiodifiers.Visibility = Visibility.Collapsed; tiScanPreview.Visibility = Visibility.Collapsed;

            API = new ControlAPI(); cbActiveAPI_SelectionChanged(null, null);
            API.OnQueryComplete += new ControlAPI.APIEventHandler(QueryComplete);           

            if (Utils.TheosComputer()) cbiDiffusion.Visibility = Visibility.Visible;
            else cbiDiffusion.Visibility = Visibility.Collapsed;
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
        private bool _showAPI;
        public bool showAPI
        {
            get { return _showAPI; }
            set 
            {
                if (value) rowAPI.Height = new GridLength(60);
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
            if (opts.SingleAuto && cueListUC.radioMode) btnCompose_Click(sender,e);
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
            if (ImgUtils.checkImageDepot(tbImageDepot.Text, false) > 0)
            {
                UpdateToOptions(sender, null);
                tbImageDepot.Foreground = Brushes.Black;
                Log("@WorkDir");
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
        public string propmt
        {
            get { return tbCue.Text + tbModifier.Text; }
        }

        public string Compose(object sender,  CueItemUC selectedSeed, string modifiers, bool OneLineCue = true)
        {
            if (sender == null || sender == btnCompose || sender == tcQuery || sender == cueListUC)
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
            if (Utils.isNull(cueListUC) || Utils.isNull(modifiersUC)) return;
            if (Utils.isNull(cueListUC.allSeeds) || Utils.isNull(modifiersUC.modifLists)) return;
            List<CueItemUC> selectedSeed = cueListUC.selectedSeeds();
            if (Utils.isNull(selectedSeed)) { Log("Err: no cue is selected"); return; }
            if (selectedSeed.Count.Equals(0)) { Log("Err: no cue is selected"); return; }
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
            List<CueItemUC> selectedSeeds = cueListUC.selectedSeeds(); scanPrompts = new List<string>(); 
            if (Utils.isNull(selectedSeeds)) { Log("Err: no cue is selected"); return; }
            if (selectedSeeds.Count.Equals(0)) { Log("Err: no cue is selected"); return; }
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
                        scanPrompts.Add(Compose(null, ssd, modifiersUC.FixItemsAsString() + opts.ModifPrefix + sc));                        
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
                        Utils.TimedMessageBox("API is busy, try again later..."); return;
                    case Status.Request2Cancel:
                        Log("Your request for cancelation has been already accepted.", Brushes.Tomato); return;
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
            if (!Utils.isNull(modifiersUC))  modifiersUC.separator = tbModifPrefix.Text;
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
        }

        private void imgAPIdialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(API)) { Log("Err: no API is selected."); return; }
            if (Utils.isNull(API.activeAPI)) { Log("Err: no API is selected. (2)"); return; }           
            API.activeAPI.opts["folder"] = opts.ImageDepotFolder;
            API.about2Show(propmt);
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
            QueryAPI(Compose(null, cueListUC.selectedSeeds()[0], modifiersUC.Composite()));
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
            if (Utils.isNull(cueListUC)) return;
            cueListUC.radioMode = tcQuery.SelectedItem.Equals(tiSingle);
            modifiersUC.SetSingleScanMode(tcQuery.SelectedItem.Equals(tiSingle));
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
    }
}