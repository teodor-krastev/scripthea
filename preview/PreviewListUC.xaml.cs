using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using scripthea.master;
using scripthea.options;
using Path = System.IO.Path;
using UtilsNS;

namespace scripthea.preview
{    
    /// <summary>
    /// Interaction logic for CueItemsUC.xaml
    /// </summary>
    public partial class PreviewListUC : UserControl, iPreview
    {
        public PreviewListUC()
        {
            InitializeComponent();
            pvItems = new ObservableCollection<PreviewItemUC>();
        }
        protected Options opts;
        public ObservableCollection<PreviewItemUC> pvItems { get; }
        public bool showBoth
        {
            get => opts.llm.ShowBoth;
            set
            {
                foreach (PreviewItemUC pvi in pvItems)
                    pvi.UpdateShowing(value);
                if (opts == null) return;
                opts.llm.ShowBoth = value;
            }
        }
        public bool isBusy
        {
            get { return Mouse.OverrideCursor == Cursors.Wait; }
            set { if (value) Mouse.OverrideCursor = Cursors.Wait; else Mouse.OverrideCursor = null; }
        }
        public bool scanningFlag { get; set; }
        public LMstudioUC LMstudio;
        private List<string> extraFile;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            try
            {
                isBusy = true;
                string extraFilename = Path.Combine(Utils.configPath, "omit-llm.txt");
                if (File.Exists(extraFilename)) extraFile = new List<string>(File.ReadAllLines(extraFilename));
                else extraFile = new List<string>();
            }
            catch (Exception ex) { Utils.TimedMessageBox(ex.Message); }
            finally { isBusy = false; }
        }
        public void Finish()
        {

        }
        #region Loading
        protected void ConditionEvents(int afterIdx) // after loading
        {
            for (int i = 0; i < pvItems.Count; i++)
            {
                if (i < afterIdx) continue;
                pvItems[i].OnSelectionChanged += new RoutedEventHandler(SelectionChanged);
                pvItems[i].OnItemChanged += new RoutedEventHandler(ItemChanged);
            }
        }
        public int LoadPrompts(List<Tuple<string, string>> prompts)
        {
            Clear();
            if (AppendPrompts(prompts) > 0) pvItems[0].selected = true;
            return prompts.Count;
        }
        public int AppendPrompts(List<Tuple<string, string>> prompts) // return total count
        {
            int k = pvItems.Count;
            foreach (var prompt in prompts)
                AddPvCue(prompt.Item1, prompt.Item2);
            ConditionEvents(k); if (k == 0 && pvItems.Count > 0) pvItems[0].selected = true;
            for (int i = 0; i < pvItems.Count; i++)
            {
                UpdateLLMCue(pvItems[i].cueText, i); pvItems[i].UpdateShowing(showBoth);
            }
            return prompts.Count; // total count
        }
        public int LoadPrompts(List<Tuple<List<string>, string>> prompts) 
        {
            Clear();
            foreach (var prompt in prompts)
                AddPvCue(prompt.Item1, prompt.Item2);
            ConditionEvents(-1); pvItems[0].selected = true;
            return prompts.Count;
        }
        private void AddPvCue(string cueText, string modifs)
        {
            pvItems.Add(new PreviewItemUC(cueText, modifs) { selected = false, IsBoxChecked = true, index = pvItems.Count });
            if (extraFile.Count > 0) pvItems.Last().extras.AddRange(extraFile);
            spPvList.Children.Add(pvItems.Last());
        }
        private void AddPvCue(List<string> cueText, string modifs)
        {
            pvItems.Add(new PreviewItemUC(cueText, modifs) { selected = false, IsBoxChecked = true, index = pvItems.Count });
            if (extraFile.Count > 0) pvItems.Last().extras.AddRange(extraFile);
            spPvList.Children.Add(pvItems.Last());
        }       
        public string UpdateLLMCue(string cueText, int idx = -1) // if idx==-1 -> selected item; idx - zero based
        {
            int k = -1;
            if (idx == -1) 
            { 
                PreviewItemUC si = selectedItem;
                if (si == null) { opts.Log("Error: no selected prompt is detected"); return ""; }
                else k = si.index;
            }
            else
            {
                if (!Utils.InRange(idx, 0, pvItems.Count-1)) { opts.Log("Error: index out of range"); return ""; }
                k = idx;
            }            
            //pvItems[k].cueLLMText = AskLLM(cueText);
            return pvItems[k].cueLLMText;
        }
        public string UpdateLLMCue(List<string> cueText, int idx = -1)
        {
            return UpdateLLMCue(string.Join("\n", cueText.ToArray(), idx));
        }
        #endregion Loading

        public event RoutedEventHandler OnItemChanged;
        public void ItemChanged(object sender, RoutedEventArgs e)
        {
            OnItemChanged?.Invoke(sender, e);
        }
        public void Clear()
        {
            for (int i = pvItems.Count - 1; i >= 0; i--) RemoveItemAt(i);
        }
        #region Selection
        public Tuple<string, string> selectedPrompt 
        { 
            get 
            {
                if (selectedItem == null) return new Tuple<string, string>("","");
                return new Tuple<string, string>(selectedItem.cueLLMText, selectedItem.modifsText);  
            } 
        }
        public event RoutedEventHandler OnSelectionChanged;
        public void SelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedItem = sender as PreviewItemUC;
            foreach (PreviewItemUC pi in pvItems) pi.selected = false;
            OnSelectionChanged?.Invoke(sender, e);
        }
        public List<Tuple<string, string>> GetFixedPrompts(bool onlyChecked, bool LLM) // only origin cue or LLM
        {
            List<Tuple<string, string>> pst = new List<Tuple<string, string>>();
            for (int i = 0; i < pvItems.Count; i++)
            {
                if (!pvItems[i].IsBoxChecked && onlyChecked) continue;
                pst.Add(new Tuple<string, string>(LLM ? pvItems[i].cueLLMText : pvItems[i].cueText, pvItems[i].modifsText)); // condition text -> LLM if any, origin cue otherwise
            }
            return pst;
        }
        public List<Tuple<string, string>> GetPrompts(bool onlyChecked) // condition text -> LLM if any, origin cue otherwise
        {
            List<Tuple<string, string>> pst = new List<Tuple<string, string>>();
            for (int i = 0; i < pvItems.Count; i++)
            {
                if (!pvItems[i].IsBoxChecked && onlyChecked) continue; 
                pst.Add(new Tuple<string, string>(pvItems[i].cueConditionText, pvItems[i].modifsText)); 
            }
            return pst;
        }
        private bool _IsReadOnly;
        public bool IsReadOnly
        {
            get => _IsReadOnly;
            set 
            { 
                _IsReadOnly = value;
                foreach (PreviewItemUC pi in pvItems) pi.IsReadOnly = value;
            }
        }
        protected PreviewItemUC getByIndex(int idx) // index at time of creation
        {
            for (int i = 0; i < pvItems.Count; i++)
            {
                if (pvItems[i].index.Equals(idx)) return pvItems[i];
            }
            return null;
        }
        public int selectByIndex(int idx) //  0-based / idx -1 -> next checked to selected one
        {
            int k = idx; if (!Utils.InRange(k,0, pvItems.Count-1)) return -1; // out of range
            for (int i = 0; i < pvItems.Count; i++)
            {
                if (k == -1 && pvItems[i].selected)
                {
                    for (int j = i; j < pvItems.Count; i++)
                        if (pvItems[i].IsBoxChecked && !pvItems[i].IsCueLLMEmpty) k = pvItems[i].index;
                }
                if (pvItems[i].selected) pvItems[i].selected = false;
            }
            if (k == -1) return -1; // fail to select    
            selectedItem = getByIndex(k); selectedItem.selected = true;
            return k;            
        }
        public bool IsValid(int idx) // if checked and with LLM cue
        {
            PreviewItemUC pi = getByIndex(idx);
            if (pi == null) return false;
            return pi.IsBoxChecked && !pi.IsCueLLMEmpty;
        }
        public List<int> GetValidList() // 0 based indexes
        {
            var ls = new List<int>();
            foreach (PreviewItemUC pi in pvItems) 
                if (pi.checkBox.IsChecked.Value && !pi.IsCueLLMEmpty) ls.Add(pi.index);
            return ls;
        }
        public int selectedIdx { get => _selectedItem == null ? -1 : _selectedItem.index; }
        private PreviewItemUC _selectedItem = null; 
        public PreviewItemUC selectedItem
        {
            get { return _selectedItem; }
            private set { _selectedItem = value; }
        }
        #endregion Selection
        public int checkedCount
        {
            get
            {
                int k = 0;
                foreach (PreviewItemUC pi in pvItems) if (pi.checkBox.IsChecked.Value) k++;
                return k;
            }
        } 
        private readonly string[] miTitles = { "Check All", "Uncheck All", "Invert Checking", "Invert Pool Checking" };
        string bufMask = "";
        private void RemoveItemAt(int idx) 
        {
            if (!Utils.InRange(idx, 0, pvItems.Count - 1)) return;
            Application.Current.Dispatcher.Invoke(() =>
            {
                spPvList.Children.Remove(pvItems[idx]);
            });
            pvItems.RemoveAt(idx);
        }
        public void MenuCommand(string cmd)
        {
            if (cmd.Equals("Check with Mask or Range"))
            {
                bufMask = new InputBox("Check with Mask or Range [#..#] e.g.[3..8]", bufMask, "").ShowDialog(); string msk = bufMask.Trim();
                if (bufMask.Equals("")) return;
                int i0; int i1; (i0, i1) = SctUtils.rangeMask(msk, pvItems.Count); i0--; i1--; // shift internal<->external index
                if (!(i0 == -1 && i1 == -1)) // range
                {
                    foreach (PreviewItemUC pi in pvItems)
                    {
                        pi.checkBox.IsChecked = Utils.InRange(pi.index, i0, i1, true);
                    }                   
                    return;
                }
            }
            if (cmd.Equals("Remove Checked"))
            {
                if (checkedCount == 0) { opts.Log("Error: no checked items"); return; }
                for (int i = pvItems.Count - 1; i >= 0; i--)
                {
                    if (pvItems[i].IsBoxChecked) RemoveItemAt(i);
                }
                ItemChanged(null, null);
            }
            else
            {
                foreach (PreviewItemUC pi in pvItems)
                {
                    bool? turn2 = null;
                    switch (cmd)
                    {
                        case "Check All":
                            turn2 = true;
                            break;
                        case "Uncheck All":
                            turn2 = false;
                            break;
                        case "Check with Mask":
                            turn2 = Utils.IsWildCardMatch(pi.cueConditionText, bufMask);
                            break;
                        case "Invert Checking":
                            turn2 = !pi.checkBox.IsChecked.Value;
                            break;
                    }
                    if (turn2 != null) pi.checkBox.IsChecked = (bool)turn2;
                }
            }
        }
    }
}
