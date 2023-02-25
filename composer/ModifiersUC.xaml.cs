using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
using System.Collections.Specialized;
using Newtonsoft.Json;
using UtilsNS;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for ModifiersUC.xaml
    /// </summary>
    public partial class ModifiersUC : UserControl
    {
        public List<ModifListUC> modifLists;
        public Dictionary<string, bool> ModifMap;
        public List<ModifItemUC> modifItems
        {
            get
            {
                List<ModifItemUC> mdf = new List<ModifItemUC>();
                if (!Utils.isNull(modifLists))
                {
                    foreach (ModifListUC mdl in modifLists)
                        mdf.AddRange(mdl.modifList);
                }
                return mdf;
            }
        }
        public ModifiersUC()
        {
            InitializeComponent();
        }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts; chkAddEmpty.IsChecked = opts.AddEmptyModif; chkConfirmGoogling.IsChecked = opts.ConfirmGoogling; 
            numSample.Value = Utils.EnsureRange(opts.ModifSample, 1,9);
            modifLists = new List<ModifListUC>();
            var files = new List<string>(Directory.GetFiles(Utils.configPath, "*.mdfr"));
            if (File.Exists(mapFile))
            {
                string json = System.IO.File.ReadAllText(mapFile);
                ModifMap = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
            }
            if (Utils.isNull(ModifMap)) ModifMap = new Dictionary<string, bool>();
            foreach (string fn in files)
            {
                ModifListUC cmu = new ModifListUC(fn, ref opts);
                cmu.OnChange += new RoutedEventHandler(Change);
                cmu.OnLog += new Utils.LogHandler(Log);
                modifLists.Add(cmu);
                stackModifiers.Children.Add(cmu);
                if (!Utils.isNull(ModifMap))
                    if (ModifMap.ContainsKey(cmu.ModifListName))
                        cmu.isVisible = ModifMap[cmu.ModifListName];
            }
            ShowMap = true; ShowMap = false; tbModifPrefix.Text = opts.ModifPrefix;
            SetSingleScanMode(true);
        }

        public void Finish()
        {
            opts.ConfirmGoogling = chkConfirmGoogling.IsChecked.Value;
            ShowMap = false; // update ModifMap;
            System.IO.File.WriteAllText(mapFile, JsonConvert.SerializeObject(ModifMap));
        }
        public event RoutedEventHandler OnChange;
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (OnChange != null) OnChange(this, e);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }        
        protected void CatChange(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox chk in listBox.Items)
                ModifMap[chk.Content.ToString()] = chk.IsChecked.Value;
            foreach (ModifListUC cmu in modifLists)
                cmu.isVisible = ModifMap.ContainsKey(cmu.ModifListName) ? ModifMap[cmu.ModifListName] : true;
        }
        public string mapFile { get { return System.IO.Path.Combine(Utils.configPath, "modifiers.map"); } }
        private bool _ShowMap;
        public bool ShowMap
        {
            get { return _ShowMap; }
            set
            {
                if (Utils.isNull(ModifMap)) { _ShowMap = false; return; }
                if (value)
                {
                    colMap.Width = new GridLength(180);
                    listBox.Items.Clear();
                    foreach (var cmu in modifLists)
                    {
                        CheckBox chk = new CheckBox();
                        chk.Height = 23;
                        chk.Checked += new RoutedEventHandler(CatChange); chk.Unchecked += new RoutedEventHandler(CatChange);
                        chk.IsChecked = ModifMap.ContainsKey(cmu.ModifListName) ? ModifMap[cmu.ModifListName] : true;
                        chk.Content = cmu.ModifListName;
                        listBox.Items.Add(chk);
                    }
                    imgOpen.Visibility = Visibility.Collapsed; imgClose.Visibility = Visibility.Visible;
                    btnModifMap.SetValue(Grid.ColumnProperty, 0);
                }
                else
                {
                    colMap.Width = new GridLength(1);
                    CatChange(null, null);
                    bool firstSet = false;
                    foreach (ModifListUC cmu in modifLists)
                    {
                        cmu.isVisible = ModifMap[cmu.ModifListName];
                        if (cmu.isVisible && !firstSet) { cmu.SetHeaderPosition(true); firstSet = true; }
                        else cmu.SetHeaderPosition(false);
                    }
                    imgOpen.Visibility = Visibility.Visible; imgClose.Visibility = Visibility.Collapsed; ; 
                    btnModifMap.SetValue(Grid.ColumnProperty, 1);
                }
                _ShowMap = value;
            }
        }
        public string Composite() // for single mode
        {
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Scannable))
                ss += sc.Equals("") ? "" : opts.ModifPrefix + sc + " ";
            return FixItemsAsString() + ss;
        }
        public List<string> ModifItemsByType(ModifStatus ms)
        {
            List<string> ls = new List<string>();
            if (ms.Equals(ModifStatus.Scannable) && opts.AddEmptyModif) ls.Add("");
            foreach (ModifListUC sm in modifLists)
            {
                if (!sm.isChecked) continue;
                foreach (ModifItemUC mdf in sm.modifList)
                    if (mdf.modifStatus.Equals(ms)) ls.Add(mdf.Text);
            }
            return ls;
        }
        public string FixItemsAsString()
        {
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Fixed))
                ss += opts.ModifPrefix + sc + " ";
            return ss;
        }
        public void SetSingleScanMode(bool singleMode)
        {
            foreach (ModifItemUC mdf in modifItems)
                mdf.singleMode = singleMode;
        }
        private void btnModifMap_Click(object sender, RoutedEventArgs e)
        {
            ShowMap = !ShowMap;
        }
        private void removeDuplicates()
        {
            Log("Removing duplicate modifiers... ");
            for (int i = 0; i < modifLists.Count; i++)
            {
                ModifListUC ml = modifLists[i]; // source               
                ml.chkCategory.FontWeight = FontWeights.Bold; ml.chkCategory.UpdateLayout(); Utils.DoEvents();
                for (int j = 0; j < ml.modifList.Count; j++)
                {
                    ModifItemUC mi = modifItems[j]; // inside the source
                    for (int k = i + 1; k < modifLists.Count; k++)
                    {
                        if (modifLists[k].removeByText(mi.Text)) 
                            modifLists[k].UpdateLayout();
                    }
                }
                ml.chkCategory.FontWeight = FontWeights.Normal; ml.chkCategory.UpdateLayout();
            }
        }
        private void chkRemoveDuplicates_Checked(object sender, RoutedEventArgs e)
        {
            removeDuplicates();
        }
        private void chkRemoveDuplicates_Unchecked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < modifLists.Count; i++)
            {
                modifLists[i].OpenCat();
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            lbCategories.Items.Clear();
            if (tbSearch.Text == "") return;           
            foreach (ModifListUC ml in modifLists)
            {
                int k = ml.markWithWildcard(tbSearch.Text);
                if (k == 0) continue;
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = ml.ModifListName + " ("+k+")";
                lbCategories.Items.Add(lbi);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbSearch.Text = "";
            foreach (ModifListUC ml in modifLists)
                ml.demark();
            lbCategories.Items.Clear();
        }
        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter)) btnSearch_Click(sender, e);
        }
        private void chkAddEmpty_Checked(object sender, RoutedEventArgs e)
        {
            opts.AddEmptyModif = chkAddEmpty.IsChecked.Value;
        }
        private void chkConfirmGoogling_Checked(object sender, RoutedEventArgs e)
        {
            opts.ConfirmGoogling = chkConfirmGoogling.IsChecked.Value;
        }

        private void numSample_ValueChanged(object sender, RoutedEventArgs e)
        {
            opts.ModifSample = numSample.Value; 
        }

        private void tbModifPrefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.ModifPrefix = tbModifPrefix.Text;
        }
    }
}
