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
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using scripthea.master;
using scripthea.viewer;
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
        private int poolCount { get { return tabControl.Items.Count - 3; } }
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
        private Options opts; Button btnSDparams;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            if (!Directory.Exists(cuesFolder)) { Utils.TimedMessageBox("Fatal error: cues folder <" + cuesFolder + "> does not exist.", "Closng application", 4000); Application.Current.Shutdown(); }
            iPickerX.Init(ref _opts); iPickerX.Configure('X', new List<string>(), "Including modifiers", "", "Browse", true).Click += new RoutedEventHandler(Browse_Click);
            iPickerX.chkCustom1.IsChecked = true;
            iPickerX.chkCustom1.Checked += new RoutedEventHandler(Modifiers_Checked); iPickerX.chkCustom1.Unchecked += new RoutedEventHandler(Modifiers_Checked);
            btnSDparams = new Button(); btnSDparams.Content = " set SD params ►>"; btnSDparams.Margin = new Thickness(7, 2, 0, 0); btnSDparams.Background = null;
            btnSDparams.VerticalAlignment = VerticalAlignment.Center; btnSDparams.Height = 26; btnSDparams.Click += new RoutedEventHandler(btnSDparams_Click); btnSDparams.IsEnabled = false;
            iPickerX.stPnl2.Children.Add(btnSDparams);

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
            cueLists = new List<CueListUC>(); couriers = new List<Courier>();
            for (int i = 0; i < poolCount; i++)
            {
                CueListUC clu = new CueListUC(); cueLists.Add(clu); 
                clu.OnLog += new Utils.LogHandler(Log);
                clu.Init(GetLists(i));
                couriers.Add(new Courier(ref clu));
                (tabControl.Items[i] as TabItem).Content = clu;
            }
            if (cueLists.Count > 0)
                if (cueLists[0].allCues.Count > 0)
                    cueLists[0].allCues[0].radioChecked = true;
            // ImageDepot
            couriers.Add(new Courier(ref iPickerX)); 
            // editor
            cueEditor.Init(ref opts);
        }
        protected void Browse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (Directory.Exists(opts.composer.ImageDepotFolder)) dialog.InitialDirectory = opts.composer.ImageDepotFolder;
            dialog.Title = "Select an image depot folder ";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            iPickerX.tbImageDepot.Text = dialog.FileName; btnSDparams.IsEnabled = Directory.Exists(dialog.FileName);
        }
        public event Utils.LogHandler OnSDparams;
        protected void btnSDparams_Click(object sender, RoutedEventArgs e) 
        {
            if (OnSDparams == null) return;
            ImageInfo ii = iPickerX.selectedImageInfo;
            if (ii == null) return; 
            OnSDparams(ii.To_String(), null);
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
        private TabItem lastTab = null; private int lastPoolIdx = -1;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender as TabControl).Name.Equals("tabControl")) return;
            int idx = tabControl.SelectedIndex;
            if (tiEditor.Equals(lastTab) && Utils.InRange(idx, 0, poolCount+1) && cueEditor.newCues) // out of editor tab
            {
                Init(ref opts); cueEditor.newCues = false;
            }
            if (tiPoolMap.Equals(lastTab) && Utils.InRange(idx, 0,poolCount-1)) // out of map tab
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
            lastTab = (TabItem)tabControl.SelectedItem; if (Utils.InRange(idx, 0, poolCount-1)) lastPoolIdx = idx;
            if (tabControl.SelectedItem.Equals(tiEditor)) cueEditor.selected = 0;
            if (!Utils.isNull(e)) e.Handled = true;
        }

        public List<Courier> couriers;
        public Courier activeCourier
        { 
            get 
            {
                if (couriers == null) return null;
                if (couriers.Count != (poolCount + 1)) return null;
                if (tabControl.SelectedIndex <= poolCount) return couriers[tabControl.SelectedIndex];
                if (tabControl.SelectedIndex == (poolCount + 1)) return couriers[couriers.Count - 1];
                return null;
            } 
        }
        private void Modifiers_Checked(object sender, RoutedEventArgs e)
        {
            iPickerX.ReloadDepot();
        }
    }

    public class Courier // two ways messenger between (the active pool or image depot) and queryUC
    {
        public bool cueSrc { get; private set; }
        private CueListUC cueList = null; ImagePickerUC iPicker = null;
        
        public Courier(ref CueListUC _cueList)
        {
            cueList = _cueList; cueSrc = true;
            cueList.OnChange += new RoutedEventHandler(Change);
        }
        public Courier(ref ImagePickerUC _iPicker)
        {
            iPicker = _iPicker; cueSrc = false;
            iPicker.OnPicSelect += new RoutedEventHandler(Change);
        }
        public List<string> SelectedCue() // only for radioMode
        {
            List<string> cueSel = new List<string>();
            if (cueSrc)
            { 
                if (!cueList.radioMode) return null;
                cueSel.AddRange(cueList.selectedCues()[0].cueTextAsList(true));
            }
            else
            {
                //if (!iPicker.checkable) return;
                if (iPicker.selectedImageInfo != null) cueSel.Add(iPicker.selectedImageInfo.prompt); 
            }
            return cueSel;
        }

        public delegate void CueSelectionHandler(List<string> cueSelection);
        public event CueSelectionHandler OnCueSelection;        
        protected void Change(object sender, RoutedEventArgs e) // only for radioMode
        {           
            if (OnCueSelection != null) OnCueSelection(SelectedCue());
        }
        public List<List<string>> GetCues()
        {
            List<List<string>> lls = new List<List<string>>();
            if (cueSrc)
            {
                if (cueList.radioMode) return lls;
                foreach (CueItemUC ci in cueList.selectedCues())
                    lls.Add(new List<string>(ci.cueTextAsList(true)));
            }
            else
            {
                //if (iPicker.checkable) return lls;
                foreach(Tuple<int, string, string> tpl in iPicker.ListOfTuples(true, false))
                {
                    string[] pa = { tpl.Item3 }; lls.Add(new List<string>(pa));
                }
            }
            return lls;
        }
    }
}
