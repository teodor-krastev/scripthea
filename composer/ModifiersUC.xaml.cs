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
        private string ModifiersFolder;
        public List<ModifListUC> categories;
        public Dictionary<string, ModifListUC> CategoriesAsDict()
        {
            Dictionary<string, ModifListUC> dct = new Dictionary<string, ModifListUC>();
            foreach (ModifListUC itm in categories) dct.Add(itm.CategoryName, itm);
            return dct;
        }
        public Dictionary<string, bool> ModifMap;
        public List<ModifItemUC> modifItems
        {
            get
            {
                List<ModifItemUC> mdf = new List<ModifItemUC>();
                if (!Utils.isNull(categories))
                {
                    foreach (ModifListUC mdl in categories)
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
            ModifiersFolder = System.IO.Path.Combine(Utils.basePath, "modifiers");
            opts = _opts;
            chkMSetsEnabled.IsChecked = opts.composer.mSetsEnabled; chkMSetsEnabled_Checked(null, null);
            chkAddEmpty.IsChecked = opts.composer.AddEmptyModif; 
            numSample.Value = Utils.EnsureRange(opts.composer.ModifSample, 1,9);
            chkConfirmGoogle.IsChecked = opts.composer.ConfirmGoogling; 
            categories = new List<ModifListUC>();
            var files = new List<string>(Directory.GetFiles(ModifiersFolder, "*.mdfr"));
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
                categories.Add(cmu);
                stackModifiers.Children.Add(cmu);
                if (!Utils.isNull(ModifMap))
                    if (ModifMap.ContainsKey(cmu.CategoryName))
                        cmu.isVisible = ModifMap[cmu.CategoryName];
            }
            ShowMap = true; ShowMap = false; tbModifPrefix.Text = opts.composer.ModifPrefix;
            SetSingleScanMode(true);
            mSetStack.Init(ref opts, ref categories, ModifiersFolder); 
        }
        public void Finish()
        {            
            ShowMap = false; // update ModifMap;
            System.IO.File.WriteAllText(mapFile, JsonConvert.SerializeObject(ModifMap));
            mSetStack.Finish();
        }
        public event RoutedEventHandler OnChange;
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (OnChange != null) OnChange(this, e); mSetStack.ModifCount();
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
            foreach (ModifListUC cmu in categories)
                cmu.isVisible = ModifMap.ContainsKey(cmu.CategoryName) ? ModifMap[cmu.CategoryName] : true;
        }
        public string mapFile { get { return System.IO.Path.Combine(ModifiersFolder, "modifiers.map"); } }
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
                    foreach (var cmu in categories)
                    {
                        CheckBox chk = new CheckBox();
                        chk.Height = 23;
                        chk.Checked += new RoutedEventHandler(CatChange); chk.Unchecked += new RoutedEventHandler(CatChange);
                        chk.IsChecked = ModifMap.ContainsKey(cmu.CategoryName) ? ModifMap[cmu.CategoryName] : true;
                        chk.Content = cmu.CategoryName;
                        listBox.Items.Add(chk);
                    }
                    btnOpenOpts.Visibility = Visibility.Collapsed; btnCloseOpts.Visibility = Visibility.Visible;
                    btnModifMap.SetValue(Grid.ColumnProperty, 0);
                }
                else
                {
                    colMap.Width = new GridLength(1);
                    CatChange(null, null);
                    bool firstSet = false;
                    foreach (ModifListUC cmu in categories)
                    {
                        cmu.isVisible = ModifMap[cmu.CategoryName];
                        if (cmu.isVisible && !firstSet) { cmu.SetHeaderPosition(true); firstSet = true; }
                        else cmu.SetHeaderPosition(false);
                    }
                    btnOpenOpts.Visibility = Visibility.Visible; btnCloseOpts.Visibility = Visibility.Collapsed; 
                    btnModifMap.SetValue(Grid.ColumnProperty, 1);
                }
                _ShowMap = value;
            }
        }
        public string Composite() // for single mode
        {
            if (opts == null) return "";
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Scannable))
                ss += sc.Equals("") ? "" : opts.composer.ModifPrefix + sc + " ";
            return FixItemsAsString() + ss;
        }
        public List<string> ModifItemsByType(ModifStatus ms)
        {
            List<string> ls = new List<string>();
            if (opts != null)
                if (ms.Equals(ModifStatus.Scannable) && opts.composer.AddEmptyModif) ls.Add("");
            foreach (ModifListUC sm in categories)
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
                ss += opts.composer.ModifPrefix + sc + " ";
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
            for (int i = 0; i < categories.Count; i++)
            {
                ModifListUC ml = categories[i]; // source               
                ml.chkCategory.FontWeight = FontWeights.Bold; ml.chkCategory.UpdateLayout(); Utils.DoEvents();
                for (int j = 0; j < ml.modifList.Count; j++)
                {
                    ModifItemUC mi = modifItems[j]; // inside the source
                    for (int k = i + 1; k < categories.Count; k++)
                    {
                        if (categories[k].removeByText(mi.Text)) 
                            categories[k].UpdateLayout();
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
            for (int i = 0; i < categories.Count; i++)
            {
                categories[i].OpenCat();
            }
        }
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            lbCategories.Items.Clear();
            if (tbSearch.Text == "") return;           
            foreach (ModifListUC ml in categories)
            {
                int k = ml.markWithWildcard(tbSearch.Text);
                if (k == 0) continue;
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = ml.CategoryName + " ("+k+")";
                lbCategories.Items.Add(lbi);
            }
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbSearch.Text = "";
            foreach (ModifListUC ml in categories)
                ml.demark();
            lbCategories.Items.Clear();
        }
        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter)) btnSearch_Click(sender, e);
        }
        private void chkAddEmpty_Checked(object sender, RoutedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.composer.AddEmptyModif = chkAddEmpty.IsChecked.Value;
        }
        private void numSample_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.composer.ModifSample = numSample.Value; 
        }
        private void tbModifPrefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.composer.ModifPrefix = tbModifPrefix.Text;
        }
        private void chkConfirmGoogle_Checked(object sender, RoutedEventArgs e)
        {
            if (!Utils.isNull(opts)) opts.composer.ConfirmGoogling = chkConfirmGoogle.IsChecked.Value;
        }

        private void chkMSetsEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (opts == null) return;
            if (sender != null) opts.composer.mSetsEnabled = chkMSetsEnabled.IsChecked.Value;
            if (opts.composer.mSetsEnabled) mSetStack.Visibility = Visibility.Visible;
            else mSetStack.Visibility = Visibility.Collapsed;
        }

    }
}
