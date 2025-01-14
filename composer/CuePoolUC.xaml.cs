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
using scripthea.options;
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
        public string rootCuesFolder = Path.Combine(Utils.basePath, "cues");
        public string cuesFolder { get; private set; }
        public string mapFile { get { return Path.Combine(cuesFolder, "cue_pools.map"); } }
        public int poolCount { get { return tabControl.Items.Count - 3; } }
        private List<Dictionary<string, bool>> poolMap;
        public List<string> GetLists(int idx) // full path
        {
            List<string> ls = new List<string>();
            if (!Utils.InRange(idx, 0, poolCount - 1)) return ls;
            foreach (var pair in poolMap[idx])
                if (pair.Value)
                    ls.Add(System.IO.Path.Combine(cuesFolder, Path.ChangeExtension(pair.Key, ".cues")));
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
                if (!File.Exists(Path.Combine(cuesFolder, Path.ChangeExtension(pair.Key,".cues"))))
                {
                    return pair.Key;
                }
            }            
            return "";
        }
        private Options opts; Button btnSDparams; Courier.CueSelectionHandler ChangeCue;
        public void Init(ref Options _opts, ref Courier.CueSelectionHandler _ChangeCue)
        {
            if (opts == null) { extCollUC.NewCuesEvent += new EventHandler(ExtCuesAdded); }
            opts = _opts; ChangeCue = _ChangeCue;
            if (Directory.Exists(opts.composer.WorkCuesFolder)) cuesFolder = opts.composer.WorkCuesFolder;
            else 
            {   cuesFolder = rootCuesFolder; 
                if (opts.composer.WorkCuesFolder != "") Log("Error: cues folder <" + opts.composer.WorkCuesFolder + "> does not exist.\n\n Set to default."); 
            }
            List<string> cfs = cuesFolders(); cfs.Insert(0, "<root>"); 
            foreach (string cf in cfs)
            {
                cbCuesFolders.Items.Add(new ComboBoxItem() { Content = cf });                
            }
            int spot = workCuesIndex();
            if (spot < 0) { cuesFolder = rootCuesFolder; cbCuesFolders.SelectedIndex = 0; }
            else cbCuesFolders.SelectedIndex = spot;
            extCollUC.Init(ref opts); 

            iPickerX.Init(ref _opts); iPickerX.Configure('X', new List<string>(), "Including modifiers", "", "Browse", true).Click += new RoutedEventHandler(Browse_Click);
            iPickerX.chkCustom1.IsChecked = true;
            iPickerX.chkCustom1.Checked += new RoutedEventHandler(Modifiers_Checked); iPickerX.chkCustom1.Unchecked += new RoutedEventHandler(Modifiers_Checked);
            btnSDparams = new Button() { Content = "set SD params ►>", Background = null, VerticalAlignment = VerticalAlignment.Center, Height = 26, Width = 130, IsEnabled = false,
                    ToolTip = "Copy parameters from selected image to SD parameters tab", Margin = new Thickness(7, 2, 0, 0) };
            btnSDparams.Click += new RoutedEventHandler(btnSDparams_Click); 
            iPickerX.stPnl2.Children.Add(btnSDparams);                        

            updateFromCuesFolder(cuesFolder);
            
            // editor tab
            cueEditor.Init(ref opts);
        }
        private int workCuesIndex()
        {
            int k = 0; int spot = -1;  string workCuesName = cuesFolderNameByPath(cuesFolder);
            foreach (ComboBoxItem cbi in cbCuesFolders.Items)
            {
                if (workCuesName.Equals(Convert.ToString(cbi.Content), StringComparison.InvariantCultureIgnoreCase)) { spot = k; cbi.FontFamily = new FontFamily("Segoe UI Semibold"); }
                else cbi.FontFamily = new FontFamily("Segoe UI");
                k++;
            }
            if (spot < 0) ((ComboBoxItem)cbCuesFolders.Items[0]).FontFamily = new FontFamily("Segoe UI Semibold");
            return spot;
        }
        public void updateFromCuesFolder(string _cuesFolder, bool updateSame = false)
        {
            cuesFolder = Directory.Exists(_cuesFolder) ? _cuesFolder : rootCuesFolder;
            opts.composer.WorkCuesFolder = cuesFolder;
            extCollUC.CoverOn = !cuesFolder.EndsWith("-coll");
            if (extCollUC.CoverOn) imgExtractOpts.Visibility = Visibility.Collapsed;
            else imgExtractOpts.Visibility = Visibility.Visible;
            if (!updateSame)
            {
                if (!extCollUC.CoverOn)
                { 
                    extCollUC.SetFolder(cuesFolder);                    
                }
                else extCollUC.SetFolder("");
                rowExtColl.Height = new GridLength(extCollUC.sjlFlag ? 220 : 143); 
            }            
            if (File.Exists(mapFile))
            {
                string json = System.IO.File.ReadAllText(mapFile);
                poolMap = JsonConvert.DeserializeObject<List<Dictionary<string, bool>>>(json);
                if (!poolMap.Count.Equals(poolCount)) { Log("Error[359]: Broken pool-map file. Fix or delete it and restart."); return; }
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
                files[j] = Path.GetFileNameWithoutExtension(files[j]);  
            
            for (int i = 0; i < poolCount; i++ ) // remove missing
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
                clu.Init(ref opts, GetLists(i));
                couriers.Add(new Courier(ref clu));
                TabItem ti = tabControl.Items[i] as TabItem;
                ti.Content = clu;
            }
            // ImageDepot tab
            couriers.Add(new Courier(ref iPickerX));

            foreach (Courier cr in couriers)
                { cr.OnCueSelection -= ChangeCue; cr.OnCueSelection += ChangeCue;}               
            if (cueLists.Count > 0)
                if (cueLists[0].allCues.Count > 0)
                    { cueLists[0].allCues[0].radioChecked = true; cueLists[0].Change(null, null); }
        }
        protected void Browse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (Directory.Exists(opts.composer.ImageDepotFolder)) dialog.InitialDirectory = opts.composer.ImageDepotFolder;
            dialog.Title = "Select an image depot folder ";
            dialog.IsFolderPicker = true; iPickerX.lbChecked.Content = "loading...";
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) { iPickerX.lbChecked.Content = "---"; return; }
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
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (opts != null) opts.Log(txt, clr);
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
        public void UpdateShowMeta(bool showMeta)
        {
            foreach (CueListUC clu in cueLists)
                clu.UpdateShowMeta(showMeta);
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
                    CheckBox chk = new CheckBox() { Content = pair.Key, IsChecked = pair.Value, Margin = new Thickness(3) };
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
            File.WriteAllText(mapFile, JsonConvert.SerializeObject(poolMap));
            opts.composer.WorkCuesFolder = cuesFolder;
            extCollUC.Finish();
        }
        private TabItem lastTab = null; private int lastPoolIdx = -1;
        public event SelectionChangedEventHandler ExternalSelectionChanged;
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender as TabControl).Name.Equals("tabControl")) return;
            ExternalSelectionChanged?.Invoke(sender, e);
            int idx = tabControl.SelectedIndex;
            if (tiEditor.Equals(lastTab) && Utils.InRange(idx, 0, poolCount+1) && cueEditor.newCues) // out of editor tab
            {
                if (cuesFolder == string.Empty) Init(ref opts, ref ChangeCue);
                else updateFromCuesFolder(cuesFolder);
                cueEditor.newCues = false;
            }
            if (tiPoolMap.Equals(lastTab) && Utils.InRange(idx, 0,poolCount-1)) // out of map tab
            {
                UpdatePoolMapFromVisuals();
                for (int i = 0; i < poolCount; i++)
                {
                    if (!cueLists[i].isBusy)
                    {                        
                        cueLists[i].Init(ref opts, GetLists(i)); cueLists[i].radioMode = radioMode;
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
        // multi-folder for cues
        public string cuesFolderPathByName(string nm)
        {
            if (nm.Equals(string.Empty)) return string.Empty;
            if (nm.Equals("<root>")) return rootCuesFolder;
            string folder = Path.Combine(rootCuesFolder, nm);
            if (Directory.Exists(folder)) return folder;
            else return string.Empty;
        }
        public string cuesFolderNameByPath(string path)
        {
            if (path.Equals(string.Empty)) return string.Empty;
            if (Utils.comparePaths(path, rootCuesFolder)) return "<root>";
            DirectoryInfo directoryInfo = new DirectoryInfo(path);          
            return directoryInfo.Name;
        }
        public List<string> cuesFolders(bool OnlyNames = true) 
        {
            List<string> ls = new List<string>();            
            string[] subdirectoryEntries = Directory.GetDirectories(rootCuesFolder, "*", SearchOption.AllDirectories);
            foreach (string subdirectory in subdirectoryEntries)
            {
                string[] files = Directory.GetFiles(subdirectory, "*.cues");
                if (subdirectory.EndsWith("-coll"))
                {
                    if (!File.Exists(Path.Combine(subdirectory, ECdesc.descName))) continue;
                }
                else if (files.Length == 0) continue;
                if (OnlyNames) ls.Add(cuesFolderNameByPath(subdirectory));
                else ls.Add(subdirectory);
            }
            return ls;
        }       
        private void cbCuesFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbCuesFolders.ToolTip = cuesFolderPathByName(Convert.ToString(((ComboBoxItem)cbCuesFolders.SelectedItem).Content));
            btnLoad.IsEnabled = !Utils.comparePaths(Convert.ToString(cbCuesFolders.ToolTip), cuesFolder);
            btnLoad.Foreground = btnLoad.IsEnabled ? Brushes.Black : Brushes.Silver;
        }
        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            UpdatePoolMapFromVisuals(); File.WriteAllText(mapFile, JsonConvert.SerializeObject(poolMap));
            if (sender != null && e != null) updateFromCuesFolder(cuesFolderPathByName(Convert.ToString(((ComboBoxItem)cbCuesFolders.SelectedItem).Content)));
            cbCuesFolders_SelectionChanged(null, null); workCuesIndex();
        }
        protected void ReopenCuesFolder() // after the the cues folder content has been changed
        {
            UpdatePoolMapFromVisuals(); File.WriteAllText(mapFile, JsonConvert.SerializeObject(poolMap));
            if (Directory.Exists(cuesFolder)) updateFromCuesFolder(cuesFolder, true);
            else Log("Error[187]: <" + cuesFolder + "> folder not found.");
        }
        public void MapUpdateFromVisuals()
        {
            //cbCuesFolders_SelectionChanged(null, null); workCuesIndex();
        }
        private bool ListboxReady(ListBox listBox)
        {
            bool bb = false;
            if (listBox.Equals(lBoxApool)) bb = grpApool.BorderBrush == Brushes.Navy;
            if (listBox.Equals(lBoxBpool)) bb = grpBpool.BorderBrush == Brushes.Navy;
            bb &= !Utils.isNull(listBox.SelectedItem);
            return bb; 
        }
        private void imgRight_MouseDown(object sender, MouseButtonEventArgs e) // from A to B
        {
            if (!ListboxReady(lBoxApool))
                { Log("Error: No item is selected to be moved."); return; }
            CheckBox chk = lBoxApool.SelectedItem as CheckBox;
            if (chk == null)
                { Log("Error: No item is selected to be moved. 2"); return; }
            CheckBox newChk = new CheckBox()
                { Content = chk.Content, IsChecked = chk.IsChecked.Value, Margin = new Thickness(3) };
            lBoxApool.Items.Remove(lBoxApool.SelectedItem);
            lBoxBpool.Items.Add(newChk); lBoxBpool.SelectedItem = newChk;
        }
        private void imgLeft_MouseDown(object sender, MouseButtonEventArgs e)  // from B to A
        {
            if (!ListboxReady(lBoxBpool))
                { Log("Error: No item is selected to be moved."); return; }
            CheckBox chk = lBoxBpool.SelectedItem as CheckBox;
             if (chk == null)
                { Log("Error: No item is selected to be moved. 2"); return; }
            CheckBox newChk = new CheckBox()
                { Content = chk.Content, IsChecked = chk.IsChecked.Value, Margin = new Thickness(3) };
            lBoxBpool.Items.Remove(lBoxBpool.SelectedItem);
            lBoxApool.Items.Add(newChk); lBoxApool.SelectedItem = newChk;
        }
        private void imgUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ListboxReady(lBoxApool) && !ListboxReady(lBoxBpool))
                { Log("Error: No item is selected to be reordered."); return; }
            int j = sender == imgUp ? -1 : 1;
            if (ListboxReady(lBoxApool))
            {
                int k = lBoxApool.SelectedIndex; 
                if (Utils.InRange(k + j, 0, lBoxApool.Items.Count - 1))
                {
                    var item = lBoxApool.Items[k];
                    lBoxApool.Items.RemoveAt(k);
                    lBoxApool.Items.Insert(k + j, item);  lBoxApool.SelectedIndex = k + j;
                }
            }           
            if (ListboxReady(lBoxBpool))
            {
                int k = lBoxBpool.SelectedIndex;
                if (Utils.InRange(k + j, 0, lBoxApool.Items.Count - 1))
                {
                    var item = lBoxBpool.Items[k];
                    lBoxBpool.Items.RemoveAt(k);
                    lBoxBpool.Items.Insert(k + j, item); lBoxBpool.SelectedIndex = k + j;
                }
            }
        }
        private void imgExtractOpts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ListboxReady(lBoxApool) && !ListboxReady(lBoxBpool))
            { Log("Error: No item is selected to be renamed."); return; }
            CheckBox chk = new CheckBox();
            if (ListboxReady(lBoxApool))
                chk = lBoxApool.SelectedItem as CheckBox;
            if (ListboxReady(lBoxBpool))
                chk = lBoxBpool.SelectedItem as CheckBox;
            string item = Path.ChangeExtension(Convert.ToString(chk.Content), ".cues");
            string fullPath = Path.Combine(cuesFolder, item);
            if (!File.Exists(fullPath)) { Log("Error: cues file not found ->" + item); return; }
            List<string> ls = Utils.readList(fullPath, false);
            if (ls.Count <3) { Log("Error: wrong cues file ->" + item); return; }
            string eo = ls[0];
            if (!eo.StartsWith("## query:")) { Log("Error: wrong cues file ->" + item); return; }           
            extCollUC.SetQuery2Visuals(ECquery.ReadECquery(eo.Substring(9).Trim()));
        }
        private void imgEdit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ListboxReady(lBoxApool) && !ListboxReady(lBoxBpool))
                { Log("Error: No item is selected to be renamed."); return; }
            CheckBox chk = new CheckBox();
            if (ListboxReady(lBoxApool))
                chk = lBoxApool.SelectedItem as CheckBox;
            if (ListboxReady(lBoxBpool))
                chk = lBoxBpool.SelectedItem as CheckBox;
            string oldName = Convert.ToString(chk.Content);
            string newName = new InputBox("Rename Cues file to...", oldName, "").ShowDialog();
            if (newName == "") return;
            oldName = Path.ChangeExtension(oldName, ".cues"); string newName1 = Path.ChangeExtension(newName, ".cues");
            if (!File.Exists(Path.Combine(cuesFolder, oldName))) { Log("Error: cues file not found ->" + oldName); return; }            
            if (File.Exists(Path.Combine(cuesFolder, newName1))) 
                { if (!Utils.ConfirmationMessageBox("The file <"+newName1+"> already exists, overwrite?")) return; }
            chk.Content = newName;
            File.Move(Path.Combine(cuesFolder, oldName), Path.Combine(cuesFolder, newName1));
            ReopenCuesFolder(); // updateFromCuesFolder(cuesFolder); 
        }
        private void imgDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ListboxReady(lBoxApool) && !ListboxReady(lBoxBpool))
                { Log("Error: No item is selected to be deleted."); return; }
            CheckBox chk = new CheckBox();
            if (ListboxReady(lBoxApool))
            {
                chk = lBoxApool.SelectedItem as CheckBox; string fn = Convert.ToString(chk.Content) + ".cues";
                if (!Utils.ConfirmationMessageBox("Please confirm. Yes will delete " + fn)) return;
                lBoxApool.Items.Remove(lBoxApool.SelectedItem);
                File.Delete(Path.Combine(cuesFolder, fn));
            }
            if (ListboxReady(lBoxBpool))
            {
                chk = lBoxBpool.SelectedItem as CheckBox; string fn = Convert.ToString(chk.Content) + ".cues";
                if (!Utils.ConfirmationMessageBox("Please confirm. Yes will delete " + fn)) return;
                lBoxBpool.Items.Remove(lBoxBpool.SelectedItem);
                File.Delete(Path.Combine(cuesFolder, fn));
            }
            ReopenCuesFolder();
        }

        protected void ExtCuesAdded(object sender, EventArgs e)
        {
            ReopenCuesFolder();
        }
        private void lBoxApool_GotFocus(object sender, RoutedEventArgs e)
        {
            grpApool.BorderBrush = Brushes.Navy;           
        }       
        private void lBoxBpool_GotFocus(object sender, RoutedEventArgs e)
        {            
            grpBpool.BorderBrush = Brushes.Navy;
        }
        private void lBoxApool_LostFocus(object sender, RoutedEventArgs e)
        {
            grpApool.BorderBrush = Brushes.Silver;
        }
        private void lBoxBpool_LostFocus(object sender, RoutedEventArgs e)
        {
            grpBpool.BorderBrush = Brushes.Silver;
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
                List<CueItemUC> cil = cueList.selectedCues();
                if (cil.Count > 0) cueSel.AddRange(cil[0].cueTextAsList(true));
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
                List<Tuple<int, string, int, string>> lt = iPicker.ListOfTuples(true, false);
                if (lt == null) return lls; 
                foreach (Tuple<int, string, int, string> tpl in lt)
                {
                    string[] pa = { tpl.Item2 }; lls.Add(new List<string>(pa));
                }
            }
            return lls;
        }
    }
}
