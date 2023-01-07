using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for CueItemsUC.xaml
    /// </summary>
    public partial class CueListUC : UserControl
    {
        public CueListUC()
        {
            InitializeComponent();
        }
        private List<string> cueFiles; 
        public List<CueItemUC> allCues; // all cues
        public List<List<CueItemUC>> localCues; // cues by tab
        private List<ScrollViewer> localScrolls;
        public bool isBusy = false;
        public string cuesFolder { get { return Path.Combine(Utils.basePath, "cues"); } } 
        public void Init(List<string> _cueFiles = null)
        {
            isBusy = true;
            try
            {
                if (Utils.isNull(_cueFiles)) cueFiles = new List<string>(Directory.GetFiles(cuesFolder, "*.cues"));
                else cueFiles = new List<string>(_cueFiles);

                tcLists.Items.Clear(); localScrolls = new List<ScrollViewer>(); 
                allCues = new List<CueItemUC>(); localCues = new List<List<CueItemUC>>(); 
                foreach (string sf in _cueFiles)
                {
                    if (!File.Exists(sf)) { Log("Error: File <" + sf + "> is missing."); continue; }
                    string se = System.IO.Path.GetFileNameWithoutExtension(sf);
                    if (!Utils.IsValidVarName(se)) { Log("Error: <" + se + "> is not valid cue list name. (no special char.)"); continue; }
                    TabItem newTabItem = new TabItem()
                    {
                        Header = se,
                        Name = "ti" + se,
                        FontSize = 14, FontStyle = FontStyles.Italic,
                        Background = Utils.ToSolidColorBrush("#FFFFFFF8")
                    };
                    tcLists.Items.Add(newTabItem); List<CueItemUC> ocl = new List<CueItemUC>(); localCues.Add(ocl);
                    ScrollViewer sv = new ScrollViewer(); sv.CanContentScroll = true; newTabItem.Content = sv; localScrolls.Add(sv);
                    StackPanel sp = new StackPanel(); sv.Content = sp;                
                    AddCues(sp, ref ocl, sf);
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
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (radioMode)
            {
                if ((OnChange != null)) OnChange(this, e); 
            }
            else lbSelCount.Content = "#"+selectedCues().Count.ToString();
            for (int i = 0; i < tcLists.Items.Count; i++)
            {
                if (selectedCues(i).Count > 0 && !radioMode) ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFF3DE");
                else ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFFFF8");
            }
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        protected void AddCues(StackPanel sp, ref List<CueItemUC> ocl, string fn)
        {
            if (!File.Exists(fn)) { Log("Err: no <" + fn + "> file found"); return; }
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
                    CueItemUC cue = new CueItemUC(ls);
                    cue.OnLog += new Utils.LogHandler(Log);
                    cue.rbChecked.Checked += new RoutedEventHandler(Change); //cue.rbChecked.Unchecked += new RoutedEventHandler(Change);
                    cue.checkBox.Checked += new RoutedEventHandler(Change); cue.checkBox.Unchecked += new RoutedEventHandler(Change);
                    allCues.Add(cue); ocl.Add(cue);
                    sp.Children.Add(cue); ls.Clear();
                }
                else
                {
                    ls.Add(ss);
                }
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
                if (Utils.InRange(tabIdx, 0, tcLists.Items.Count - 1)) ssf = localCues[tabIdx];
                else ssf = allCues;
                foreach (CueItemUC os in ssf)
                    if (os.boxChecked) ssd.Add(os);                
            }
            return ssd;            
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
                    imgRandom.Visibility = Visibility.Visible; imgMenu.Visibility = Visibility.Collapsed; lbSelCount.Visibility = Visibility.Collapsed;
                    tabControl_SelectionChanged(null, null);
                }
                else
                {
                    imgRandom.Visibility = Visibility.Collapsed; imgMenu.Visibility = Visibility.Visible; lbSelCount.Visibility = Visibility.Visible;                    
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
        private readonly string[] miTitles = { "Check All", "Uncheck All", "Invert Checking" };
        private void mi_Click(object sender, RoutedEventArgs e)
        {            
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
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
                    case "Invert Checking":
                        os.boxChecked = !os.boxChecked;
                        break;
                }
            }            
        }
        bool inverting = false;
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inverting = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    Utils.DelayExec(300, () => { imgMenu.ContextMenu.IsOpen = !inverting; });
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
