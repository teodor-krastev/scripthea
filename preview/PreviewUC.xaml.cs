using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Diagnostics;

using scripthea.master;
using scripthea.options;
using UtilsNS;
using System.Windows.Threading;

namespace scripthea.preview
{
    public interface iPreview
    {
        void Init(ref Options _opts);
        void Finish();
        bool scanningFlag { get; set; }
        bool IsReadOnly { get; set; }
        int LoadPrompts(List<Tuple<string, string>> prompts);
        int AppendPrompts(List<Tuple<string, string>> prompts); // return total count
        int selectByIndex(int idx); // idx -1 for the next valid item, if none return -1
        int selectedIdx { get; }
        bool IsValid(int idx);
        List<int> GetValidList();
        Tuple<string, string> selectedPrompt { get; }
        List<Tuple<string, string>> GetPrompts(bool onlyChecked);
        void Clear();
        void MenuCommand(string cmd);
    }
    public partial class PreviewUC : UserControl
    {
        public PreviewUC()
        {
            InitializeComponent();
        }
        protected Options opts;
        public iPreview active
        {
            get { if (tcPreviews.SelectedIndex == 0) return scanPreviewUC; else return previewListUC; }
        }
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
            scanPreviewUC.Init(ref opts); previewListUC.Init(ref opts);
            scanPreviewUC.OnSelectionChanged += new RoutedEventHandler(SelectionChanged); scanPreviewUC.OnItemChanged += new RoutedEventHandler(ItemChanged);
            previewListUC.OnSelectionChanged += new RoutedEventHandler(SelectionChanged); previewListUC.OnItemChanged += new RoutedEventHandler(ItemChanged);
            fashionUC.Init();
            fashionUC.rbNone.Checked += new RoutedEventHandler(rbNone_Checked);
            fashionUC.rbContext.Checked += new RoutedEventHandler(rbNone_Checked);
            fashionUC.rbAskLLM.Checked += new RoutedEventHandler(rbNone_Checked);           
            fashionUC.fashionMode = (FashioningUC.FashionModeTypes)opts.composer.FashionMode; rbNone_Checked(null, null);
            chkShowBoth.IsChecked = opts.llm.ShowBoth; chkAutoAsk.IsEnabled = false; chkAutoAsk.IsChecked = opts.llm.AutoAsk; chkAutoAsk.IsEnabled = true;
            IsLLMEnabled = false;
            dblTemperature.IsEnabled = false;
            dblTemperature.Minimum = 0; dblTemperature.Maximum = 1; dblTemperature.Interval = 0.1; dblTemperature.DoubleFormat = "F1"; dblTemperature.Value = opts.llm.LMStemperature;
            dblTemperature.IsEnabled = true;
            intMaxTokens.Minimum = 3; intMaxTokens.Maximum = 300; intMaxTokens.Value = opts.llm.LMSmax_tokens;
            promptsBuffer = new List<Tuple<string, string>>();
        }
        public void Finish()
        {
            scanPreviewUC.Finish(); previewListUC.Finish();
            fashionUC.Finish(); Clear();
        }
        protected List<Tuple<string, string>> promptsBuffer;
        private List<Tuple<string, string>> TrimSeparator(List<Tuple<string, string>> prompts)
        {
            List<Tuple<string, string>> rslt = new List<Tuple<string, string>>();
            foreach (Tuple<string, string> tpl in prompts)
                rslt.Add(new Tuple<string, string>(tpl.Item1.Replace(opts.composer.ModifPrefix,", "), tpl.Item2));
            return rslt;
        }
        public int LoadPrompts(List<Tuple<string, string>> prompts)
        {
            Clear(); promptsBuffer.Clear();
            return AppendPrompts(TrimSeparator(prompts));
        }
        public int AppendPrompts(List<Tuple<string, string>> prompts)
        {
            promptsBuffer.AddRange(prompts);  
            int k = active.AppendPrompts(prompts);
            if (fashionUC.fashionMode == FashioningUC.FashionModeTypes.ask_llm && chkAutoAsk.IsChecked.Value)
            {
                Utils.DoEvents(); Utils.Sleep(500);
                _ = LMMScan(); 
            }
            return k;
        }
        public void SelectionChanged(object sender, RoutedEventArgs e)
        {           
            Tuple<string, string> si = active.selectedPrompt;
            VisualHelper.SetButtonEnabled(btnQuerySelected,!si.Item1.Trim().Equals(""));
        }
        public void ItemChanged(object sender, RoutedEventArgs e)
        {
            UpdateCounter();
        }
        public List<Tuple<string, string>> GetPrimePrompts(bool onlyChecked)
        {
            switch (opts.composer.FashionMode)
            {
                case (int)FashioningUC.FashionModeTypes.none:
                    return active.GetPrompts(onlyChecked);

                case (int)FashioningUC.FashionModeTypes.context:
                    return active.GetPrompts(onlyChecked);

                case (int)FashioningUC.FashionModeTypes.ask_llm:
                    return previewListUC.GetFixedPrompts(onlyChecked, false);
                default: return null;
            }
        }
        public void rbNone_Checked(object sender, RoutedEventArgs e)
        {
            fashionUC.SwitchMode();
            int k = opts.composer.FashionMode; List<Tuple<string, string>> lst = new List<Tuple<string, string>>(GetPrimePrompts(false));
            opts.composer.FashionMode = (int)fashionUC.fashionMode;
            rowFashion.Height = opts.composer.FashionMode == 0 ? new GridLength(30) : new GridLength(130);
            tcPreviews.SelectedIndex = opts.composer.FashionMode == 2 ? 1 : 0;
            if (promptsBuffer == null) return;
            if ((k < 2 && tcPreviews.SelectedIndex == 1) || (k == 2 && tcPreviews.SelectedIndex == 0) && promptsBuffer.Count > 0)
                LoadPrompts(lst);
            UpdateCounter();
        }
        public int selectByIndex(int idx) // idx -1 for the next valid item, if none return -1
        {
            return active.selectByIndex(idx);
        }
        public int selectedIdx { get => active.selectedIdx; }
        public bool IsValid(int idx) { return active.IsValid(idx); }
        public List<int> GetValidList() { return active.GetValidList(); }
        public List<string> selectedListPrompt // selected prompt as List for generation
        {
            get
            {
                List<string> cntx;
                switch (fashionUC.fashionMode)
                {
                    case FashioningUC.FashionModeTypes.none:
                        return new List<string>(new string[] { scanPreviewUC.selectedPromptAsStr }); 

                    case FashioningUC.FashionModeTypes.context:
                        cntx = fashionUC.getContext();
                        cntx.Add(scanPreviewUC.selectedPromptAsStr);
                        return cntx;

                    case FashioningUC.FashionModeTypes.ask_llm:
                        cntx = new List<string>(); 
                        if (previewListUC.selectedItem == null) return cntx; 
                        cntx.AddRange(previewListUC.selectedItem.cueLLMTextAsList(true));
                        cntx.Add(previewListUC.selectedItem.modifsText);
                        return cntx;

                    default: return null;
                }
            }
        }
        public string selectedStringPrompt // selected prompt as string for generation
        {
            get => selectedListPrompt == null ? "" : String.Join(" ", selectedListPrompt.ToArray());
        }
        private bool _scanningFlag = false;
        public bool scanningFlag
        {
            get
            {
                return _scanningFlag;
            }
            set
            {
                if (value)
                {
                    btnScanChecked.Content = "Cancel Scan";
                    VisualHelper.SetButtonEnabled(btnQuerySelected, false); VisualHelper.SetButtonEnabled(btnClose, false);
                    btnScanChecked.Background = Utils.ToSolidColorBrush("#FFFED17F");
                }
                else
                {
                    VisualHelper.SetButtonEnabled(btnScanChecked, true); btnScanChecked.Content = "Scan All Checked";
                    VisualHelper.SetButtonEnabled(btnQuerySelected, true); VisualHelper.SetButtonEnabled(btnClose, true);
                    btnScanChecked.Background = Brushes.MintCream;
                }
                _scanningFlag = value; scanPreviewUC.scanningFlag = value; previewListUC.scanningFlag = value;
            }
        }
        public void Clear()
        {
            active.Clear(); 
        }
        private void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem; if (mi == null) return;
            string header = Convert.ToString(mi.Header);
            if (header.Equals("Read Only")) active.IsReadOnly = mi.IsChecked;
            active.MenuCommand(header); UpdateCounter();
        }
        bool inverting = false;
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inverting = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    Utils.DelayExec(300, () => { btnMenu.ContextMenu.IsOpen = !inverting; });
                }
                if (e.ClickCount == 2)
                {
                    mi_Click(miInvertChecking, null);
                }
            }
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (active.GetPrompts(true).Count == 0) { opts.Log("Error[748]: no checked prompts in the list"); return; }
            string ss = "";
            string sbegin = fashionUC.fashionMode == FashioningUC.FashionModeTypes.context ? string.Join(Environment.NewLine, fashionUC.getContext())+ Environment.NewLine : "";
            string send = Environment.NewLine + Environment.NewLine;
            foreach (var s in active.GetPrompts(true)) ss += sbegin + s.Item1 + s.Item2 + send; 
            Clipboard.SetText(ss);
            _ = new PopupText(btnCopy, "List copied");
        }
        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (active.GetPrompts(true).Count == 0) { opts.Log("Error[749]: no checked prompts in the list"); return; }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Prompts (.txt)|*.txt"; // Filter files by extension
            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result != true) return;
            List<string> ls = new List<string>();
            string sbegin = fashionUC.fashionMode == FashioningUC.FashionModeTypes.context ? string.Join(Environment.NewLine, fashionUC.getContext()) + Environment.NewLine : "";
            foreach (Tuple<string, string> prm in active.GetPrompts(true)) { ls.Add(sbegin + prm.Item1 + prm.Item2); ls.Add(""); }
            Utils.writeList(dlg.FileName, ls);
        }
        public (int , int) UpdateCounter()
        {
            int i = active.GetPrompts(true).Count; int j = active.GetPrompts(false).Count;
            lbCheckCount.Content = i + " out of " + j; lbCheckCount.Foreground = Brushes.Navy;
            (int p, int c) = UpdateLLMcounter();
            if (fashionUC.fashionMode == FashioningUC.FashionModeTypes.ask_llm) VisualHelper.SetButtonEnabled(btnScanChecked, i > 0 && p > 0);
            else VisualHelper.SetButtonEnabled(btnScanChecked, i > 0);
            return (i, j);
        }
        protected void lbCheckCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            UpdateCounter();
        }
        #region LLM components
        private async void btnRunLLMServer_Click(object sender, RoutedEventArgs e)
        {
            _ = new PopupText(btnRunLLMServer, "Launching...", 1);
            bool bb = await previewListUC.LMstudio.FullLaunchAsync(true);
            if (bb)
            {
                previewListUC.LMstudio.LmStudioCloseMonitor(ref LmProcess);
                if (LmProcess != null) LmProcess.Exited += new EventHandler(LmProcess_Exited);
            }
            IsLLMEnabled = bb;
            if (bb && opts.llm.AutoAsk && previewListUC.pvItems.Count > 0) await LMMScan();          
        }
        Process LmProcess;
        private void LmProcess_Exited(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    IsLLMEnabled = false;
                    previewListUC.LMstudio.LmProcess_Exited();
                }));
        }
        protected bool _IsLLMEnabled = false;
        public bool IsLLMEnabled { get => _IsLLMEnabled && previewListUC.LMstudio != null && previewListUC.LMstudio.Connected; 
            set
            {
                btnRunLLMServer.Foreground = value ? Brushes.Gray : Brushes.Maroon; btnRunLLMServer.BorderBrush = value ? Brushes.Gray : Brushes.Maroon;
                btnAskLLM.Background = value ? Utils.ToSolidColorBrush("#FFF2FCDD") : Utils.ToSolidColorBrush("#FFFCE6CB");
                _IsLLMEnabled = value;
            } 
        }
        private async void chkAutoAsk_Checked(object sender, RoutedEventArgs e)
        {
            opts.llm.AutoAsk = chkAutoAsk.IsChecked.Value;           
            btnAskLLM.Foreground = opts.llm.AutoAsk ? Brushes.Gray : Brushes.Black;
            VisualHelper.SetButtonEnabled(btnAskLLM, !opts.llm.AutoAsk);
            if (chkAutoAsk.IsChecked.Value && chkAutoAsk.IsEnabled && previewListUC.pvItems.Count > 0)
                if (!await LMMScan()) opts.Log("Warning: user interuption!");
        }
        private void chkShowBoth_Checked(object sender, RoutedEventArgs e)
        {
            opts.llm.ShowBoth = chkShowBoth.IsChecked.Value;
            previewListUC.showBoth = opts.llm.ShowBoth;
        }
        private void dblTemperature_OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (!dblTemperature.IsEnabled) return;
            opts.llm.LMStemperature = dblTemperature.Value;
            if (opts.llm.LMStemperature > 0.79) dblTemperature.Background = Brushes.Tomato;
            else dblTemperature.Background = null;
            opts.llm.LMSmax_tokens = intMaxTokens.Value;
        }
        #endregion

        #region Ask LLM 
        private bool _askingFlag = false;
        public bool askingFlag
        {
            get => _askingFlag;           
            set
            { 
                _askingFlag = value;
                VisualHelper.SetButtonEnabled(btnScanChecked, !value);
            }
        }
        public async Task<string> AskLLM(string prompt, double _temperature = 0.0, int _max_tokens = 30)
        {
            if (!IsLLMEnabled) { opts.Log("Error: LM Studio is not currently active."); return ""; }
            return await previewListUC.LMstudio.LMSclient.SimpleCompletionAsync(prompt, opts.llm.LMScontext.Replace('\r',' '), _temperature, _max_tokens);
        }
        private readonly string promptStr = "$prompt$";
        public async Task<string> ProcessPVI(PreviewItemUC pvi)
        {
            string prt = fashionUC.joinStrList(fashionUC.getAskLLM());
            if (pvi.cueText.Contains(promptStr)) prt = prt.Replace(promptStr, pvi.cueText);
            else prt += "\n" + pvi.cueText;             
            string reply = await AskLLM(prt, opts.llm.LMStemperature, opts.llm.LMSmax_tokens);
            pvi.cueLLMText = reply;
            return reply;
        }
        private async void btnAskLLM_Click(object sender, RoutedEventArgs e)
        {            
            PreviewItemUC pvi = previewListUC.selectedItem;
            if (pvi == null) { opts.Log("Error: no selected prompt is detected"); return; }
            await ProcessPVI(pvi);
            SelectionChanged(pvi, null);
            UpdateCounter();
        }
        public (int, int) UpdateLLMcounter()
        {
            if (fashionUC.fashionMode != FashioningUC.FashionModeTypes.ask_llm) return (0, 0);
            int c = 0; int p = 0;
            foreach (PreviewItemUC pvi in previewListUC.pvItems)
            {
                if (pvi.IsBoxChecked) c++;
                if (!pvi.IsCueLLMEmpty) p++;
            }
            tbLLMcounter.Text = "LLM processed: " + p + " out of " + c;
            return (p, c);
        }
        public int ValidLLMcount()
        {
            if (fashionUC.fashionMode != FashioningUC.FashionModeTypes.ask_llm) return 0;
            int c = 0; 
            foreach (PreviewItemUC pvi in previewListUC.pvItems) if (pvi.IsBoxChecked && !pvi.IsCueLLMEmpty) c++;
            return c;
        }

        private async Task<bool> LMMScan()
        {
            if (scanningFlag) { opts.Log("Error: no asking LLM while generating images."); return false; }
            if (!IsLLMEnabled) { opts.Log("Warning: LM Studio is not currently active."); return false; }
            try
            {
                askingFlag = true;
                foreach (PreviewItemUC pvi in previewListUC.pvItems)
                {
                    if (!pvi.IsBoxChecked) continue;
                    if (!pvi.cueLLMText.Equals("")) continue;
                    if (!chkAutoAsk.IsChecked.Value) return false;
                    await ProcessPVI(pvi);
                    UpdateLLMcounter();
                }
            }
            finally { askingFlag = false; }
            return true;
        }
        #endregion Ask LLM
    }
}
