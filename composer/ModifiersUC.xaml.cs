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

        public string mapFile { get { return System.IO.Path.Combine(Utils.configPath + "modifiers.map"); } }
        public void Init()
        {
            separator = "; ";
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
                ModifListUC cmu = new ModifListUC(fn); 
                cmu.OnChange += new RoutedEventHandler(Change);
                cmu.OnLog += new Utils.LogHandler(Log);
                modifLists.Add(cmu);
                stackModifiers.Children.Add(cmu);
                if (!Utils.isNull(ModifMap))
                    if (ModifMap.ContainsKey(cmu.ModifListName)) 
                        cmu.isVisible = ModifMap[cmu.ModifListName];                   
            }
            ShowMap = true; ShowMap = false;            
            SetSingleScanMode(true);
        }

        public void Finish()
        {
            ShowMap = false; // update ModifMap;
            System.IO.File.WriteAllText(mapFile, JsonConvert.SerializeObject(ModifMap));
        }
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
                        chk.IsChecked = ModifMap.ContainsKey(cmu.ModifListName) ? ModifMap[cmu.ModifListName] : true;
                        chk.Content = cmu.ModifListName;
                        listBox.Items.Add(chk);
                    }    
                    btnModifMap.Content = "<<"; btnModifMap.SetValue(Grid.ColumnProperty, 0);
                }
                else
                { 
                    colMap.Width = new GridLength(1);
                    foreach (CheckBox chk in listBox.Items)
                        ModifMap[chk.Content.ToString()] = chk.IsChecked.Value;
                    bool firstSet = false;
                    foreach (ModifListUC cmu in modifLists)
                    {
                        cmu.isVisible = ModifMap[cmu.ModifListName];
                        if (cmu.isVisible && !firstSet) { cmu.SetHeaderPosition(true); firstSet = true; }
                        else cmu.SetHeaderPosition(false);
                    }
                    btnModifMap.Content = ">>"; btnModifMap.SetValue(Grid.ColumnProperty, 1);
                }
                _ShowMap = value;
            }
        } 
        public string separator { get; set; }
        public string Composite() // for single mode
        {
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Scannable))
                ss += separator + sc + " ";
            return FixItemsAsString()+ss;
        }
        public List<string> ModifItemsByType(ModifStatus ms)
        {
            List<string> ls = new List<string>();
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
                ss += separator + sc + " ";
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
    }
}
