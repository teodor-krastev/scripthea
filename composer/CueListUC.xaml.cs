using System;
using System.Collections.Generic;
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
using scripthea.options;
using Path = System.IO.Path;
using UtilsNS;

namespace scripthea.composer
{
    public class CueListHeader
    {
        public CueListHeader()
        {
            Tags = new List<string>(); 
        }
        public string Source { get; set; }
        public string application { get; set; } // and version (optionally)
        public List<string> Tags { get; set; }
        public string Comment { get; set; }
    }
    /// <summary>
    /// Interaction logic for CueItemsUC.xaml
    /// </summary>
    public partial class CueListUC : UserControl
    {
        public CueListUC()
        {
            InitializeComponent();
        }
        protected Options opts;
        private List<string> cueFiles; 
        public List<CueItemUC> allCues; // all cues
        public List<List<CueItemUC>> localCues; // cues by tab
        private List<ScrollViewer> localScrolls;
        public bool isBusy = false;
        public string cuesFolder { get { return Path.Combine(Utils.basePath, "cues"); } } 
        public void Init(ref Options _opts, List<string> _cueFiles = null)
        {
            isBusy = true; opts = _opts;
            try
            {
                if (Utils.isNull(_cueFiles)) cueFiles = new List<string>(Directory.GetFiles(cuesFolder, "*.cues"));
                else cueFiles = new List<string>(_cueFiles);

                tcLists.Items.Clear(); localScrolls = new List<ScrollViewer>(); 
                allCues = new List<CueItemUC>(); localCues = new List<List<CueItemUC>>(); 
                foreach (string sf in _cueFiles)
                {
                    if (!File.Exists(sf)) { opts.Log("Error[74]: File <" + sf + "> is missing."); continue; }
                    string se = System.IO.Path.GetFileNameWithoutExtension(sf);
                    if (!Utils.IsValidVarName(se.Replace('-','_'))|| (se.IndexOf('_') > -1)) 
                        { opts.Log("Error[741]: <" + se + "> is not valid cue list name. (only special char allowed is dash.)"); continue; }
                    TabItem newTabItem = new TabItem()
                    {
                        Header = se,
                        Name = "ti" + se.Replace('-', '_'),
                        FontSize = 14, FontStyle = FontStyles.Italic, Height = 27,
                        Background = Utils.ToSolidColorBrush("#FFFFFFF8")
                    };
                    tcLists.Items.Add(newTabItem); List<CueItemUC> ocl = new List<CueItemUC>(); localCues.Add(ocl);
                    ScrollViewer sv = new ScrollViewer(); sv.CanContentScroll = true; newTabItem.Content = sv; localScrolls.Add(sv);
                    StackPanel sp = new StackPanel(); sv.Content = sp;                
                    AddCues(sp, ref ocl, sf); newTabItem.ToolTip = ocl.Count+" cues";
                }          
                if (tcLists.Items.Count > 0) tcLists.SelectedIndex = 0;                
                tabControl_SelectionChanged(null, null);
                if (allCues.Count > 0) { allCues[0].radioChecked = true; allCues[0].rbChecked.UpdateLayout(); }
            }
            catch (Exception ex) { Utils.TimedMessageBox(ex.Message); }
            finally { isBusy = false; }
        }
        public void Finish()
        {

        }
        public event RoutedEventHandler OnChange;
        public void Change(object sender, RoutedEventArgs e)
        {
            if (radioMode)
            {
                if ((OnChange != null)) OnChange(this, e); 
            }
            else lbSelCount.Content = "#"+selectedCues().Count.ToString();
            for (int i = 0; i < tcLists.Items.Count; i++)
            {
                if (selectedCues(i).Count > 0 && !radioMode) ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFF3DE");
                else ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFFFF0");
            }
        }
        
        protected void AddCues(StackPanel sp, ref List<CueItemUC> ocl, string fn)
        {
            if (!File.Exists(fn)) { opts.Log("Error[22]: no <" + fn + "> file found"); return; }
            List<string> cueText = new List<string>(File.ReadAllLines(fn));
            List<string> cueList = new List<string>();
            foreach (string ss in cueText)
            {
                if (ss.Length > 1)
                    if (ss.Substring(0, 2).Equals("##")) continue;
                if (ss.Trim().Equals("")) continue;
                cueList.Add(ss.Trim());
            }             
            List<string> ls = new List<string>();
            foreach (string ss in cueList)
            {               
                if (ss.Equals("---"))
                {
                    string cs = String.Join("|",ls.ToArray()); // fight duplicates
                    bool found = false;
                    foreach (CueItemUC ci in ocl)
                    {
                        string hs = String.Join("|", ci.cueTextAsList(true).ToArray());
                        found = hs.Equals(cs, StringComparison.InvariantCultureIgnoreCase);
                        if (found) break;
                    }
                    if (found) continue;

                    CueItemUC cue = new CueItemUC(ls);
                    cue.rbChecked.Checked += new RoutedEventHandler(Change); //cue.rbChecked.Unchecked += new RoutedEventHandler(Change);
                    cue.checkBox.Checked += new RoutedEventHandler(Change); cue.checkBox.Unchecked += new RoutedEventHandler(Change);
                    
                    allCues.Add(cue); cue.index = ocl.Count; ocl.Add(cue); 
                    sp.Children.Add(cue); ls.Clear();
                }
                else
                {
                    ls.Add(ss); 
                }
            }
        }
        protected void PoolChecking(bool? bl)
        {
            if (allCues == null) return;
            if (radioMode) return;
            foreach (CueItemUC ci in allCues)
            {
                if (bl == null) ci.boxChecked = !ci.boxChecked;
                else ci.boxChecked = (bool)bl;
            }
        }
        public List<CueItemUC> selectedCues(int tabIdx = -1) // -1 -> allCues in non-radio mode
        {
            List<CueItemUC> ssd = new List<CueItemUC>();
            if (radioMode)
            {
                if (tcLists.SelectedIndex < 0)
                {
                    if (tcLists.Items.Count > 0) tcLists.SelectedIndex = 0;
                    else return ssd; 
                }
                foreach (CueItemUC os in localCues[tcLists.SelectedIndex])
                    if (os.radioChecked)
                    {
                        ssd.Add(os); return ssd;
                    }
            }
            else
            {
                List<CueItemUC> ssf;
                if (Utils.InRange(tabIdx, 0, tcLists.Items.Count - 1, true)) ssf = localCues[tabIdx];
                else ssf = allCues;
                foreach (CueItemUC os in ssf)
                    if (os.boxChecked) ssd.Add(os);                
            }
            return ssd;            
        }
        public int ListsCount { get { return tcLists.Items.Count; } }
        public int selListIdx { get { return tcLists.SelectedIndex; } set { tcLists.SelectedIndex = Utils.EnsureRange(value, 0, ListsCount - 1); } }
        public List<CueItemUC> selList { get { return localCues[selListIdx]; } }
        public List<string> SelRandomSelect(int perc)
        {
            List<string> ls = new List<string>();
            if (radioMode) return ls;
            if (perc == 0) // clear
            {
                foreach (CueItemUC ci in selList) ci.boxChecked = false;
                return ls;
            } 
            List<int> items = new List<int>(); 
            for (int i = 0; i < selList.Count; i++) items.Add(i);
            int k = (int)(selList.Count*perc/100); // How many random items you want
            k = Utils.EnsureRange(k, 0, selList.Count);

            Random random = new Random(); // Create the Random object (important to reuse!)

            var shuffledItems = items.OrderBy(item => random.Next()).ToList();
            var uniqueRandomItems = shuffledItems.Take(k);
            SelRandomSelect(0);
            foreach (int ci in uniqueRandomItems)
            {
                selList[ci].boxChecked = true; ls.Add(selList[ci].cueText);
            }                
            return ls;
        }
        public List<string> RandomSelect(int perc, int idx)
        {
            List<string> ls = new List<string>();
            if (radioMode) return ls;
            if (idx == 0) return SelRandomSelect(perc); // selected one
            if (idx < 0) // all of them
            {
                bool bb = true; int hi = selListIdx;
                for (int i = 0; i < localCues.Count; i++)
                {
                    selListIdx = i; ls.AddRange(SelRandomSelect(perc));
                }
                selListIdx = hi;
                return ls;
            }
            selListIdx = idx - 1;
            return SelRandomSelect(perc);
        }
        public int allCueIdx
        {
            get
            {
                if (radioMode)
                {
                    for (int i = 0; i < allCues.Count; i++)
                    {
                        if (allCues[i].radioChecked) return i;
                    }
                    return -1;
                }
                else return -1;
            }
        }
        public int localCueIdx // local (active) list index
        {
            get
            {
                if (tcLists.SelectedIndex < 0) return -1;
                if (radioMode)
                {
                    for (int i = 0; i < localCues[tcLists.SelectedIndex].Count; i++)
                        if (localCues[tcLists.SelectedIndex][i].radioChecked)
                            return i;
                    return -1;
                }
                else return -1;
            }
            set
            {
                if (!radioMode || tcLists.SelectedIndex < 0) return;
                List<CueItemUC> ssd = localCues[tcLists.SelectedIndex];
                if (!Utils.InRange(value, 0,ssd.Count)) return;
                ssd[value].radioChecked = true;
                Change(ssd[value].rbChecked, null);
                if (value == 0) localScrolls[tcLists.SelectedIndex].ScrollToVerticalOffset(0);
            }
        }
        private bool _radioMode = true;
        public bool radioMode 
        {
            get { return _radioMode; }
            set
            {
                _radioMode = value;
                if (Utils.isNull(allCues)) return;
                foreach (CueItemUC os in allCues)
                    os.radioMode = value;
                if (value)
                {
                    imgRandom.Visibility = Visibility.Visible; btnMenu.Visibility = Visibility.Collapsed; lbSelCount.Visibility = Visibility.Collapsed;
                    tabControl_SelectionChanged(null, null);
                }
                else
                {
                    imgRandom.Visibility = Visibility.Collapsed; btnMenu.Visibility = Visibility.Visible; lbSelCount.Visibility = Visibility.Visible;                    
                }
                Change(null, null); 
            }
        }
        Random rand = new Random();
        public void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tcLists.SelectedIndex < 0) return;
            List<CueItemUC> ssd = localCues[tcLists.SelectedIndex];
            if (ssd.Count == 0) return;
            localCueIdx = 0;
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void imgRandom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!radioMode) return;
            List<CueItemUC> ssd = localCues[tcLists.SelectedIndex];
            int si = rand.Next(ssd.Count);
            if (si.Equals(localCueIdx)) si = rand.Next(ssd.Count);
            localCueIdx = si;
        }
        private readonly string[] miTitles = { "Check All", "Uncheck All", "Invert Checking", "Invert Pool Checking" };
        string bufMask = "";
        private void mi_Click(object sender, RoutedEventArgs e)
        {            
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            if (header.Equals("Check with Mask"))
                { bufMask = new InputBox("Check with Mask", bufMask, "").ShowDialog(); if (bufMask.Equals("")) return; }
            foreach (CueItemUC os in localCues[tcLists.SelectedIndex])
            { 
                switch (header)
                {
                    case "Check All":
                        os.boxChecked = true;
                        break; 
                    case "Uncheck All":
                        os.boxChecked = false;
                        break;
                    case "Check with Mask":                        
                        os.boxChecked = Utils.IsWildCardMatch(os.cueText, bufMask); 
                        break;
                    case "Invert Checking":
                        os.boxChecked = !os.boxChecked;
                        break;
                }
            }
            if (header.Equals("Invert Pool Checking")) PoolChecking(null);
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
                    inverting = true;
                    foreach (CueItemUC os in localCues[tcLists.SelectedIndex])
                    os.boxChecked = !os.boxChecked;      
                }
            }
        }
    }
}
