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

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for SeedsUC.xaml
    /// </summary>
    public partial class CueListUC : UserControl
    {
        public CueListUC()
        {
            InitializeComponent();
        }
        private List<string> seedFiles; 
        public List<CueItemUC> allSeeds; // all seeds
        public List<List<CueItemUC>> localSeeds; // seeds by tab
        public bool isBusy = false;
        public void Init(List<string> _seedFiles = null)
        {
            isBusy = true;
            try
            {
                if (Utils.isNull(_seedFiles)) seedFiles = new List<string>(Directory.GetFiles(Utils.configPath, "*.cues"));
                else seedFiles = new List<string>(_seedFiles);

                allSeeds = new List<CueItemUC>(); localSeeds = new List<List<CueItemUC>>(); tcLists.Items.Clear();
                foreach (string sf in _seedFiles)
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
                    tcLists.Items.Add(newTabItem); List<CueItemUC> ocl = new List<CueItemUC>(); localSeeds.Add(ocl);
                    ScrollViewer sv = new ScrollViewer(); sv.CanContentScroll = true; newTabItem.Content = sv;
                    StackPanel sp = new StackPanel(); sv.Content = sp;                
                    AddSeeds(sp, ref ocl, sf);
                }
                if (allSeeds.Count > 0) allSeeds[0].radioChecked = true;
                //if (tcLists.SelectedIndex.Equals(-1)) tcLists.SelectedIndex = 0; //???
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
            else lbSelCount.Content = "#"+selectedSeeds().Count.ToString();
            for (int i = 0; i < tcLists.Items.Count; i++)
            {
                if (selectedSeeds(i).Count > 0 && !radioMode) ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFF3DE");
                else ((TabItem)tcLists.Items[i]).Background = Utils.ToSolidColorBrush("#FFFFFFF8");
            }
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        protected void AddSeeds(StackPanel sp, ref List<CueItemUC> ocl, string fn)
        {
            if (!File.Exists(fn)) { Log("Err: no <" + fn + "> file found"); return; }
            List<string> seedText = new List<string>(File.ReadAllLines(fn));
            List<string> seedList = new List<string>();
            foreach (string ss in seedText)
            {
                if (ss.Length > 1)
                    if (ss.Substring(0, 2).Equals("##")) continue;
                if (ss.Trim().Equals("")) continue;
                seedList.Add(ss.Trim());
            }             
            List<string> ls = new List<string>();
            foreach (string ss in seedList)
            {               
                if (ss.Equals("---"))
                {
                    CueItemUC seed = new CueItemUC(ls);
                    seed.OnLog += new Utils.LogHandler(Log);
                    seed.rbChecked.Checked += new RoutedEventHandler(Change); //seed.rbChecked.Unchecked += new RoutedEventHandler(Change);
                    seed.checkBox.Checked += new RoutedEventHandler(Change); seed.checkBox.Unchecked += new RoutedEventHandler(Change);
                    allSeeds.Add(seed); ocl.Add(seed);
                    sp.Children.Add(seed); ls.Clear();
                }
                else
                {
                    ls.Add(ss);
                }
            }
        }
        public List<CueItemUC> selectedSeeds(int tabIdx = -1) // -1 -> allSeeds in non-radio
        {
            List<CueItemUC> ssd = new List<CueItemUC>();
            if (radioMode)
            {
                if (tcLists.SelectedIndex < 0) { return ssd; }
                foreach (CueItemUC os in localSeeds[tcLists.SelectedIndex])
                    if (os.radioChecked)
                    {
                        ssd.Add(os); return ssd;
                    }
            }
            else
            {
                List<CueItemUC> ssf;
                if (Utils.InRange(tabIdx, 0, tcLists.Items.Count - 1)) ssf = localSeeds[tabIdx];
                else ssf = allSeeds;
                foreach (CueItemUC os in ssf)
                    if (os.boxChecked) ssd.Add(os);                
            }
            return ssd;            
        }

        public int allSeedIdx
        {
            get
            {
                if (radioMode)
                {
                    for (int i = 0; i < allSeeds.Count; i++)
                    {
                        if (allSeeds[i].radioChecked) return i;
                    }
                    return -1;
                }
                else return -1;
            }
        }
        public int localSeedIdx // local (active) list index
        {
            get
            {
                if (tcLists.SelectedIndex < 0) return -1;
                if (radioMode)
                {
                    for (int i = 0; i < localSeeds[tcLists.SelectedIndex].Count; i++)
                        if (localSeeds[tcLists.SelectedIndex][i].radioChecked)
                            return i;
                    return -1;
                }
                else return -1;
            }
            set
            {
                if (!radioMode || tcLists.SelectedIndex < 0) return;
                List<CueItemUC> ssd = localSeeds[tcLists.SelectedIndex];
                if (!Utils.InRange(value, 0,ssd.Count)) return;
                ssd[value].radioChecked = true;
                Change(ssd[value].rbChecked, null);
            }
        }
        private bool _radioMode = true;
        public bool radioMode 
        {
            get { return _radioMode; }
            set
            {
                _radioMode = value;
                if (Utils.isNull(allSeeds)) return;
                foreach (CueItemUC os in allSeeds)
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
            List<CueItemUC> ssd = localSeeds[tcLists.SelectedIndex];
            if (ssd.Count == 0) return;
            localSeedIdx = 0;
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void imgRandom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!radioMode) return;
            List<CueItemUC> ssd = localSeeds[tcLists.SelectedIndex];
            int si = rand.Next(ssd.Count);
            if (si.Equals(localSeedIdx)) si = rand.Next(ssd.Count);
            localSeedIdx = si;
        }
        private readonly string[] miTitles = { "Check All", "Uncheck All", "Invert Checking" };
        private void imgMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (radioMode) return;
            cmCue.Items.Clear(); 
            for (int i = 0; i < 3; i++)
            {
                MenuItem mi = new MenuItem(); mi.Header = miTitles[i];                
                mi.Click += mi_Click;
                cmCue.Items.Add(mi);
            }
        }
        private void mi_Click(object sender, RoutedEventArgs e)
        {            
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            foreach (CueItemUC os in localSeeds[tcLists.SelectedIndex])
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
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Image image = sender as Image;
                ContextMenu contextMenu = image.ContextMenu;
                contextMenu.PlacementTarget = image;
                contextMenu.IsOpen = true;
                e.Handled = true;
                imgMenu_ContextMenuOpening(sender, null);
            }
        }
    }
}
