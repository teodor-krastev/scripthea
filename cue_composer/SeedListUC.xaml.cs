using System;
using System.Collections.Generic;
using System.IO;
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
using UtilsNS;

namespace scripthea
{
    /// <summary>
    /// Interaction logic for SeedsUC.xaml
    /// </summary>
    public partial class SeedListUC : UserControl
    {
        public SeedListUC()
        {
            InitializeComponent();
        }
        private List<string> seedFiles; 
        public List<SeedItemUC> allSeeds; // all seeds
        public List<List<SeedItemUC>> localSeeds; // seeds by tab
        public void Init()
        {
            seedFiles = new List<string>(Directory.GetFiles(Utils.configPath, "*.cues"));
            allSeeds = new List<SeedItemUC>(); localSeeds = new List<List<SeedItemUC>>();
            foreach (string sf in seedFiles)
            {
                string se = System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(sf),"");
                char[] charsToTrim = { '.', ' ' }; 
                se = se.Trim(charsToTrim); if (!Utils.IsValidVarName(se)) throw new Exception(se + "is not valid name");
                TabItem newTabItem = new TabItem
                {
                    Header = se,
                    Name = "ti"+se
                };
                tabControl.Items.Add(newTabItem); List<SeedItemUC> ocl = new List<SeedItemUC>(); localSeeds.Add(ocl);
                ScrollViewer sv = new ScrollViewer(); sv.CanContentScroll = true; newTabItem.Content = sv;
                StackPanel sp = new StackPanel(); sv.Content = sp;                
                AddSeeds(sp, ref ocl, sf);
            }
            allSeeds[0].radioChecked = true;
        }
        public event RoutedEventHandler OnChange;
        protected void Change(object sender, RoutedEventArgs e)
        {
            if ((OnChange != null)) OnChange(this, e);
        }

        protected void AddSeeds(StackPanel sp, ref List<SeedItemUC> ocl, string fn)
        {
            if (!File.Exists(fn)) throw new Exception("Err: no <" + fn + "> file");
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
                    SeedItemUC seed = new SeedItemUC(ls);
                    seed.rbChecked.Checked += new RoutedEventHandler(Change); //seed.rbChecked.Unchecked += new RoutedEventHandler(Change);
                    allSeeds.Add(seed); ocl.Add(seed);
                    sp.Children.Add(seed); ls.Clear();
                }
                else
                {
                    ls.Add(ss);
                }
            }
        }
        public List<SeedItemUC> selectedSeeds 
        {
            get 
            {
                List<SeedItemUC> ssd = new List<SeedItemUC>();
                if (radioMode)
                {
                    foreach (SeedItemUC os in localSeeds[tabControl.SelectedIndex])
                        if (os.radioChecked)
                        {
                            ssd.Add(os); return ssd;
                        }
                }
                else
                {
                    foreach (SeedItemUC os in allSeeds)
                        if (os.boxChecked) ssd.Add(os);
                    return ssd;
                }
                return null;
            }
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
                if (tabControl.SelectedIndex < 0) return -1;
                if (radioMode)
                {
                    for (int i = 0; i < localSeeds[tabControl.SelectedIndex].Count; i++)
                        if (localSeeds[tabControl.SelectedIndex][i].radioChecked)
                            return i;
                    return -1;
                }
                else return -1;
            }
            set
            {
                if (!radioMode || tabControl.SelectedIndex < 0) return;
                List<SeedItemUC> ssd = localSeeds[tabControl.SelectedIndex];
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
                foreach (SeedItemUC os in allSeeds)
                    os.radioMode = value;
                if (value) imgRandom.Visibility = Visibility.Visible;
                else imgRandom.Visibility = Visibility.Collapsed;
            }
        }
        Random rand = new Random();
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabControl.SelectedIndex < 0) return;
            List<SeedItemUC> ssd = localSeeds[tabControl.SelectedIndex];
            if (ssd.Count == 0) return;
            localSeedIdx = 0;
        }
        private void imgRandom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!radioMode) return;
            List<SeedItemUC> ssd = localSeeds[tabControl.SelectedIndex];
            int si = rand.Next(ssd.Count);
            if (si.Equals(localSeedIdx)) si = rand.Next(ssd.Count);
            localSeedIdx = si;
        }
    }
}
