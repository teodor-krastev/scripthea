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
using Newtonsoft.Json;
using UtilsNS;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for CuePoolUC.xaml
    /// </summary>
    public partial class CuePoolUC : UserControl
    {
        public CuePoolUC()
        {
            InitializeComponent();            
        }
        public string cuesFolder { get { return Path.Combine(Utils.basePath, "cues"); } }
        public string mapFile { get { return System.IO.Path.Combine(Utils.basePath, "cues", "cue_pools.map"); } }
        private int poolCount { get { return tabControl.Items.Count - 2; } }
        private List<Dictionary<string, bool>> poolMap;
        public List<string> GetLists(int idx) // full path
        {
            List<string> ls = new List<string>();
            if (!Utils.InRange(idx, 0, poolCount - 1)) return ls;
            foreach (var pair in poolMap[idx])
                if (pair.Value)
                    ls.Add(System.IO.Path.Combine(Utils.basePath, "cues", Path.ChangeExtension(pair.Key, ".cues")));
            return ls;
        }
        private List<CueListUC> cueLists;
        public CueListUC ActiveCueList
        {
            get
            {
                if (Utils.isNull(cueLists) || tabControl.SelectedIndex >= poolCount) return null;
                return cueLists[tabControl.SelectedIndex];
            }
        }
        private string missingCues(int poolIdx)
        {
            foreach (var pair in poolMap[poolIdx])
            {
                if (!File.Exists(System.IO.Path.Combine(Utils.basePath, "cues", Path.ChangeExtension(pair.Key,".cues"))))
                {
                    return pair.Key;
                }
            }            
            return "";
        }
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            if (File.Exists(mapFile))
            {
                string json = System.IO.File.ReadAllText(mapFile);
                poolMap = JsonConvert.DeserializeObject<List<Dictionary<string, bool>>>(json);
                if (!poolMap.Count.Equals(poolCount)) throw new Exception("Error: Broken pool-map file. Fix or delete it and restart.");
                for (int i = 0; i < poolCount; i++)
                {
                    string nf = missingCues(i);
                    while (nf != "")
                    {
                        poolMap[i].Remove(nf);
                        nf = missingCues(i);
                    }
                }             
            }           
            else
            {
                poolMap = new List<Dictionary<string, bool>>();
                for (int i = 0; i < poolCount; i++)
                    poolMap.Add(new Dictionary<string, bool>());
            }  
            // check for new cues
            List<string> files = new List<string>(Directory.GetFiles(cuesFolder, "*.cues"));
            for (int j = 0; j < files.Count; j++) // strip all but the name
                files[j] = System.IO.Path.GetFileNameWithoutExtension(files[j]);

            for (int i = 0; i < poolCount; i++ )
            {
                foreach (var pair in poolMap[i])
                {
                    int found = -1;
                    for (int j = 0; j < files.Count; j++)
                    {
                        if (pair.Key.Equals(files[j],StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = j; break;
                        }
                    }
                    if (found > -1) files.RemoveAt(found);                   
                }
            }
            foreach (string fn in files) 
                poolMap[0].Add(fn, true);
            UpdateVisualsFromPoolMap();
            // go for visuals
            cueLists = new List<CueListUC>();
            for (int i = 0; i < poolCount; i++)
            {
                CueListUC clu = new CueListUC(); cueLists.Add(clu); 
                clu.OnLog += new Utils.LogHandler(Log);
                clu.Init(GetLists(i));
                clu.OnChange += new RoutedEventHandler(Change);                   
                (tabControl.Items[i] as TabItem).Content = clu;
            }
            if (cueLists.Count > 0)
                if (cueLists[0].allCues.Count > 0)
                    cueLists[0].allCues[0].radioChecked = true;
            // editor
            cueEditor.Init(ref opts);
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
        private bool _radioMode = true;
        public bool radioMode
        {
            get { return _radioMode; }
            set
            {
                _radioMode = value;
                if (Utils.isNull(cueLists)) return;
                foreach (CueListUC clu in cueLists)
                    clu.radioMode = value;
            }
        }
        private ListBox ListBoxByIndex(int idx)
        {
            if (!Utils.InRange(idx, 0, poolCount - 1)) return null;
            ListBox[] lba = { lBoxApool, lBoxBpool };
            return lba[idx];
        }
        public void UpdateVisualsFromPoolMap()
        {            
            for (int i = 0; i < poolCount; i++)
            {
                ListBoxByIndex(i).Items.Clear();
                foreach (var pair in poolMap[i])
                {
                    CheckBox chk = new CheckBox()
                    { Content = pair.Key, IsChecked = pair.Value, Margin = new Thickness(3) };
                    ListBoxByIndex(i).Items.Add(chk);
                }
            }
        }
        public void UpdatePoolMapFromVisuals()
        {
            for (int i = 0; i < poolCount; i++)
            {
                poolMap[i].Clear();
                foreach (var itm in ListBoxByIndex(i).Items)
                {
                    CheckBox chk = itm as CheckBox;
                    if (Utils.isNull(chk)) return;
                    poolMap[i].Add(chk.Content.ToString(), chk.IsChecked.Value);
                }
            }
        }
        public void Finish()
        {
            foreach (CueListUC clu in cueLists)
                clu.Finish();
            UpdatePoolMapFromVisuals();
            System.IO.File.WriteAllText(mapFile, JsonConvert.SerializeObject(poolMap));
        }
        private void imgDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(lBoxApool.SelectedItem))
            {
                Log("No item is selected for the move."); return;
            }
            CheckBox chk = lBoxApool.SelectedItem as CheckBox;
            CheckBox newChk = new CheckBox()
            { Content = chk.Content, IsChecked = chk.IsChecked.Value, Margin = new Thickness(3) };
            lBoxApool.Items.Remove(lBoxApool.SelectedItem);
            lBoxBpool.Items.Add(newChk); lBoxBpool.SelectedItem = newChk;
        }
        private void imgUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Utils.isNull(lBoxBpool.SelectedItem))
            {
                Log("No item is selected for the move."); return;
            }
            CheckBox chk = lBoxBpool.SelectedItem as CheckBox;
            CheckBox newChk = new CheckBox()
            { Content = chk.Content, IsChecked = chk.IsChecked.Value, Margin = new Thickness(3) };
            lBoxBpool.Items.Remove(lBoxBpool.SelectedItem);
            lBoxApool.Items.Add(newChk); lBoxApool.SelectedItem = newChk;
        }
        private int lastTabIdx = -1; private int lastPoolIdx = -1;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender as TabControl).Name.Equals("tabControl")) return;
            int idx = tabControl.SelectedIndex;
            if (lastTabIdx.Equals(poolCount) && Utils.InRange(idx, 0,poolCount-1)) // out of map tab
            {
                UpdatePoolMapFromVisuals();
                for (int i = 0; i < poolCount; i++)
                {
                    if (!cueLists[i].isBusy)
                    {                        
                        cueLists[i].Init(GetLists(i)); cueLists[i].radioMode = radioMode;
                    }
                }
            }
            if (Utils.InRange(idx, 0, poolCount-1) && !Utils.isNull(cueLists)) 
                cueLists[idx].tabControl_SelectionChanged(null, null);
            lastTabIdx = idx; if (Utils.InRange(idx, 0, poolCount-1)) lastPoolIdx = idx;
            if (tabControl.SelectedItem.Equals(tiEditor)) cueEditor.selected = 0;
            if (!Utils.isNull(e)) e.Handled = true;
        }
    }
}
