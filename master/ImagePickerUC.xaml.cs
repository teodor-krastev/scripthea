using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;
using scripthea.viewer;
using scripthea.options;
using scripthea.composer;
using UtilsNS;

namespace scripthea.master
{
    /// <summary>
    /// Interaction logic for ImagePickerUC.xaml
    /// </summary>
    public partial class ImagePickerUC : UserControl, iFocusControl
    {
        public ImagePickerUC()
        {
            InitializeComponent();
            views = new List<iPicList>(); views.Add(listView); views.Add(gridView); 
        }
        private Options opts;
        private bool _checkable;
        public bool HasTheFocus { get; set; }
        public bool checkable 
        { 
            get { return _checkable; }
            private set { _checkable = value; listView.SetChecked(value); gridView.SetChecked(value); } 
        }
        private bool IsReadOnly; 
        public char letter { get; private set; }
        public void Init(ref Options _opts, bool __checkable = true)
        {
            opts = _opts; _checkable = __checkable;
            listView.Init(ref _opts, checkable); 
            listView.SelectEvent += new TableViewUC.PicViewerHandler(loadPic); listView.OnChangeContent += new RoutedEventHandler(ChangeContent);
            gridView.Init(ref _opts, checkable); 
            gridView.SelectEvent += new GridViewUC.PicViewerHandler(loadPic); gridView.OnChangeContent += new RoutedEventHandler(ChangeContent);
            iDepotStats.Init(ref opts);
        }
        public Button Configure(char _letter, List<string> cbItems, string checkBox1, string checkBox2, string buttonExecute, bool _IsReadOnly) // configure the extras
        {
            letter = _letter;
            if (letter.Equals(' ')) gbFolder.Header = "Image depot folder ";
            else gbFolder.Header = "Image depot folder [ "+letter+" ] ";
            comboCustom.Items.Clear(); IsReadOnly = _IsReadOnly;
            if (cbItems.Count.Equals(0))
            {
                comboCustom.Visibility = Visibility.Collapsed; //rectSepar.Visibility = Visibility.Visible;
            }
            else
            {
                comboCustom.Visibility = Visibility.Visible; //rectSepar.Visibility = Visibility.Collapsed;
                foreach (string ss in cbItems)
                {
                    ComboBoxItem cbi = new ComboBoxItem() { Content = ss };
                    comboCustom.Items.Add(cbi);
                }
                comboCustom.SelectedIndex = 0;
            }
            chkCustom1.Content = checkBox1;
            if (checkBox1.Equals("")) chkCustom1.Visibility = Visibility.Collapsed;
            else chkCustom1.Visibility = Visibility.Visible;
            chkCustom2.Content = checkBox2;
            if (checkBox2.Equals("")) chkCustom2.Visibility = Visibility.Collapsed;
            else chkCustom2.Visibility = Visibility.Visible;

            btnCustom.Content = buttonExecute;
            if (buttonExecute.Equals(""))
            {
                btnCustom.Visibility = Visibility.Collapsed; colButton.Width = new GridLength(0);
            }
            else
            {
                btnCustom.Visibility = Visibility.Visible; colButton.Width = new GridLength(100);
            }
            return btnCustom;
        }
        public MenuItem AddMenuItem(string header)
        {
            MenuItem mi = new MenuItem() { Header = header };
            cmImgMenu.Items.Add(mi);
            return mi;
        }
        public UserControl parrent { get { return this; } }
        public GroupBox groupFolder { get { return gbFolder; } }
        public TextBox textFolder { get { return tbImageDepot; } }

        public bool RemoveAt(int idx, bool inclFile = true) // idx in iDepot
        {
            if (!Utils.InRange(idx, 0, iDepot.items.Count-1, true)) { opts.Log("Error[485]: index out of range"); return false; }
            if (inclFile)
            {
                string filepath = Path.Combine(imageFolder, iDepot.items[idx].filename);
                if (File.Exists(filepath)) File.Delete(filepath);
                else opts.Log("Error[365]: file <" + filepath + " not found");
            }
            iDepot.items.RemoveAt(idx);
            return true;
        }
        public void ReloadDepot() // from folder in tbImageDepot; after files operation
        {
            //listView.loadedDepot = ""; gridView.loadedDepot = "";
            Refresh(true, imageFolder);
            activeView.SetChecked(true); CountChecked();
            ChangeDepot(iDepot, null);
        }
        public ImageInfo selectedImageInfo
        {
            get 
            {
                if (activeView.selectedIndex == -1 || iDepot is null) return null;
                if (!iDepot.isEnabled) return null;
                return iDepot.items[activeView.selectedIndex];
            }
        }
        public List<ImageInfo> imageInfos(bool check, bool uncheck)
        {
            if (activeView is null || iDepot is null) return null;
            if (!iDepot.isEnabled || !checkable) return null;
            List<ImageInfo> lii = new List<ImageInfo>();
            List<Tuple<int, string, int, string>> lt = ListOfTuples(check, uncheck);
            foreach (var ii in lt)
            {
                int i = ii.Item1;
                if (Utils.InRange(i, 0, iDepot.items.Count-1, true))
                    lii.Add(iDepot.items[i]);
            }
            return lii;
        }
        public List<Tuple<int, string, int, string>> ListOfTuples(bool check, bool uncheck)
        {
            if (iDepot is null) return null;
            if (!iDepot.isEnabled || activeView is null || !checkable) return null;
            return activeView?.GetItems(check, uncheck);
        }
        public void SelectItem(int idx)
        {
            activeView.selectedIndex = idx;
        }
        private List<iPicList> views;
        iPicList activeView { get { int idx = tcMain.SelectedIndex == 2 ? 0 : tcMain.SelectedIndex; return views[idx]; } }
        public string imageFolder // save shortcut to iDepot.depotFolder
        {
            get
            {
                string _imageFolder;
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = SctUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder : _imageFolder + "\\";
            }
        }
        public bool isEnabled // valid iDepot
        { 
            get 
            {   
                if (iDepot is null) return false;
                return iDepot.isEnabled;
            } 
        }
        public bool? isValidFolder { get; private set; } // false - invalid; true - valid and not empty; null - valid and empty !!! future use
        public bool isValid // it looks ready to be used
        {
            get
            {
                if (!isEnabled) return false;
                bool bb = isValidFolder ?? false; bb &= tcMain.SelectedItem != tiStats;
                return bb;
            }
        }
        private bool _isChanging = false;        
        public bool isChanging
        {
            get { return _isChanging; }
            set
            {
                try
                {
                    if (value) Mouse.OverrideCursor = Cursors.Wait;
                    /*if (!value && _isChanging) // end of chaging/editing in iDepot
                    {
                        iDepot.Save(!IsReadOnly); // save the changes on disk
                        activeView.Clear();
                        if (!activeView.FeedList(ref iDepot, false))  // update from iDepot
                            { opts.Log("Error[256]: fail to update image depot"); return; }
                        CountChecked();
                    }*/
                }
                finally { Mouse.OverrideCursor = null; }
                _isChanging = value;
            }
        }
        public void Clear()
        {            
            listView.Clear(); gridView.Clear(); 
            iDepot = null; 
            isValidFolder = false;
            CountChecked();
        }
        public event RoutedEventHandler OnChangeDepot;
        protected void ChangeDepot(object sender, RoutedEventArgs e)
        {
            if (OnChangeDepot != null) OnChangeDepot(sender, e);
            if (tcMain.SelectedItem == tiStats && sender is ImageDepot)
            {
                string path = ((ImageDepot)sender).path;
                if (iDepotStats.iDepot != null)
                    if (!iDepotStats.iDepot.IsSameAs(path)) 
                        image.Source = null;
                iDepotStats.OnChangeDepot(path); 
                return;
            }
            tbImageDepot.Foreground = Brushes.Black;
        }
        protected void ChangeContent(object sender, RoutedEventArgs e)
        {
            CountChecked();
        }       
        public void SetCheckLabel(string txt)
        {
            Utils.DelayExec(300, () => { lbChecked.Content = txt; }); 
        }
        private int Count(bool actView) // active view or desc.idf fro imageFolder
        {
            int cnt = 0;
            if (activeView != null && actView) cnt = activeView.Count;
            else cnt = SctUtils.checkImageDepot(imageFolder, true);
            return cnt;
        }
        private int CountChecked(bool print = true) // returns numb. of checked
        {
            if (print) SetCheckLabel(Count(false).ToString()+" images");
            if (!isEnabled || activeView is null || tcMain.SelectedItem == tiStats) { /*opts.Log("Error[]: No active image depot found.");*/ return -1; }
            int cnt = activeView.CountChecked;
            if (print)
                SetCheckLabel(cnt.ToString() + " out of " + Count(true).ToString());
            if (Count(true) == 0) image.Source = null;
            return cnt; 
        }
        public bool converting = false; public ImageDepot iDepot = null;

        public (ImageDepot, bool?)  LoadImageDepot(string path) // from desc. file 
        {
            ImageDepot wid; bool? ivf; //isValidFolder -> false - invalid; true - valid and not empty; null - valid and empty 
            int iCount = SctUtils.checkImageDepot(path, true); Utils.DoEvents();
            if (iCount > -1)
            {
                wid = new ImageDepot(path, ImageInfo.ImageGenerator.FromDescFile, ImageDepot.SD_WebUI.NA, IsReadOnly);
                if (iCount == 0) ivf = null;
                else ivf = true; 
            }
            else
            {
                if (Directory.Exists(path))
                {
                    ImageDepot.SD_WebUI sdw = ImageDepot.SD_WebUI.NA; // default
                    if (opts.composer.API.StartsWith("SD"))
                    {
                        sdw = (opts.composer.A1111) ? ImageDepot.SD_WebUI.A1111 : ImageDepot.SD_WebUI.ComfyUI;
                    }
                    wid = new ImageDepot(path, SctUtils.DefaultImageGenerator, sdw, IsReadOnly);
                }
                else wid = null;
                ivf = false;
            }
            return (wid, ivf);
        }

        public bool Refresh(bool forced, string iFolder = "")
        // if forced -> just unconditionally refresh from iDepot, but recreate iDepot if iFolder exists or iDepot is null
        // if forced == false -> compare iDepotFromFile.SameAs(items from visual) and refresh from iDepot if they are NOT the same
        {
            try
            {
                if (forced)
                {
                    if (iFolder.Equals(opts?.viewer.emptyText))
                    {
                        if (tcMain.SelectedIndex == 2) iDepotStats.Clear();
                        else activeView?.Clear();
                        image.Source = null; CountChecked(true); return true;
                    }
                    if (Directory.Exists(iFolder)) iDepot = new ImageDepot(iFolder);
                    else { if (iFolder != "") { opts.Log("Error[873]: Missing directory > " + iFolder); return false; } }
                    if (iDepot is null && Directory.Exists(imageFolder)) iDepot = new ImageDepot(imageFolder);
                    if (iDepot is null || !iDepot.isEnabled) { opts.Log("Error[874]: Wrong image-depot"); return false; }

                    if (tcMain.SelectedItem == tiStats)
                    {
                        iDepotStats.Clear();
                        iDepotStats.OnChangeDepot(iDepot.path);
                        CountChecked(); 
                        return true;
                    }
                    List<Tuple<int, string, int, string>> decompImageDepot = iDepot.Export2Viewer();
                    if (!Utils.isNull(decompImageDepot))
                    {
                        SetCheckLabel("loading... "+ iDepot.items.Count);
                        activeView.FeedList(ref iDepot, forced); 
                        if (activeView == listView) listView.focusFirstRow();
                        CountChecked(true);
                    }
                    else Refresh(true, opts?.viewer.emptyText);
                    return true;
                }
                else // it needs Directory.Exists(iFolder) and activeView.iDepot is not null to compare; otherwise it will go forced
                {
                    if (iDepot is null) return Refresh(true);
                    if (!Directory.Exists(iFolder)) { opts.Log("Error[873]: Missing directory > " + iFolder); return false; }

                    if (activeView.iDepot is null) return Refresh(true);
                    ImageDepot iDepotFromFile = new ImageDepot(iFolder);
                    List<Tuple<int, string, int, string>> lst = activeView.GetItems(true, true);
                    if (!iDepotFromFile.IsSameAs(lst)) return Refresh(true);
                }
                return true;
            }
            finally {  }
        }
        public void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {            
            if (tbImageDepot.Text.Trim().Equals("")) { Clear(); ChangeDepot(null, e); return; }            
            if (opts != null)
                if (opts.composer.ImageDepotFolder.Equals(tbImageDepot.Text, StringComparison.InvariantCultureIgnoreCase) && opts.composer.QueryStatus == Status.Scanning)
                { opts.Log("Error[1279]: the working image folder is in process of updating.", Brushes.Red); tbImageDepot.Text = ""; return; }
            if (SctUtils.checkImageDepot(tbImageDepot.Text, true) > 0) tbImageDepot.Foreground = Brushes.Black;
            else { tbImageDepot.Foreground = Brushes.Red; return; }

            (iDepot, isValidFolder) = LoadImageDepot(tbImageDepot.Text);

            if (tcMain.SelectedItem.Equals(tiStats))
            {
                iDepotStats.Clear();
                if (iDepot != null)
                {
                    string path = iDepot.path;
                    if (iDepotStats.iDepot != null)
                        if (!iDepotStats.iDepot.IsSameAs(path))
                            image.Source = null;
                    iDepotStats.OnChangeDepot(path);
                    CountChecked();
                }
                return;
            }
            if (!chkCustom1.IsChecked.Value && Convert.ToString(chkCustom1.Content).Equals("Including modifiers") && iDepot != null) // optional correction for modifiers
            {
                string[] stringSeparators = new string[] { opts.composer.ModifPrefix };
                foreach (ImageInfo ii in iDepot.items)
                {
                    string[] pa = ii.prompt.Split(stringSeparators, System.StringSplitOptions.RemoveEmptyEntries);
                    if (pa.Length < 2) continue;
                    ii.prompt = pa[0];
                }
            }
            Refresh(true); // update from iDepot
            activeView.SetChecked(true);  CountChecked();
            ChangeDepot(iDepot, null);
        }
        private void mi_Click(object sender, RoutedEventArgs e)
        {
            if (!isEnabled) { return; }
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            switch (header)
            {
                case "Check All": activeView.SetChecked(true);
                    break;
                case "Uncheck All": activeView.SetChecked(false);
                    break;
                case "Check by Mask, Range or Rate":
                    string mask = new InputBox("Check by Mask, Range [#..#] or Rate {#}", activeView.markMask, "").ShowDialog().Trim();
                    string msk = mask.Trim();
                    if (msk.Equals("")) return;
                    if ((msk.StartsWith("[") && msk.EndsWith("]")) || (msk.StartsWith("{") && msk.EndsWith("}")))
                    {                    
                        if (msk.StartsWith("[") && msk.EndsWith("]"))
                        {
                            int i0, i1; (i0, i1) = SctUtils.rangeMask(msk, activeView.Count);
                            if (i0 == -1 || i1 == -1) { opts.Log("Error[575]: Wrong range syntax, it must be [num..num] ."); return; }
                            else activeView.CheckRange(i0,i1);
                        }
                        if (msk.StartsWith("{") && msk.EndsWith("}"))
                        {
                            double d = 1;
                            if (!Double.TryParse(msk.TrimStart('{').TrimEnd('}'), out d)) { opts.Log("Error[576]: Wrong rate threshold syntax, it must be {num} ."); return; }                             
                            activeView.CheckRate(d);
                        }
                    }
                    else activeView.MarkWithMask(msk);
                    break;
                case "Invert Checking": activeView.SetChecked(null);
                    break;
                case "Refresh": ReloadDepot();
                    break;
                case "Clear": tbImageDepot.Text = ""; ReloadDepot();
                    break;
            }
            CountChecked();
        }
        private void MCheckUncheck(object sender, MouseButtonEventArgs e)
        {
            CountChecked();
        }
        public event RoutedEventHandler OnPicSelect;
        //public delegate void PicViewerHandler(int idx, string imageDir, ImageInfo ii);
        public event TableViewUC.PicViewerHandler OnSelectEvent; // to the master
        string lastLoadedPic = "";
        protected ImageInfo SelectedItem(int idx, ImageDepot _iDepot)  // idx in iDepot
        {
            ImageInfo ii = null;
            if (_iDepot != null)
                if (_iDepot.isEnabled && Utils.InRange(idx, 0, _iDepot.items.Count - 1))
                    ii = _iDepot.items[idx];
            return ii;
        }
        public void loadPic(int idx, ImageDepot iDepot)
        {
            if (tcMain.SelectedItem == tiStats) return;
            ImageInfo ii = SelectedItem(idx, iDepot);
            string filePath = Path.Combine(iDepot.path, ii.filename);
            if (File.Exists(filePath)) { image.Source = ImgUtils.UnhookedImageLoad(filePath, ImgUtils.ImageType.Png); lastLoadedPic = filePath; }
            else { image.Source = SctUtils.file_not_found; lastLoadedPic = ""; }
            if (OnPicSelect != null) OnPicSelect(ii.prompt, null);
            if (OnSelectEvent != null) OnSelectEvent(idx, iDepot);
        }
        
 /*       private void rbList_Checked(object sender, RoutedEventArgs e)
        {
            if (tcMain is null || iDepot is null) return;
            if (rbList.IsChecked.Value) tcMain.SelectedIndex = 0;            
            if (rbGrid.IsChecked.Value) tcMain.SelectedIndex = 1;
        }*/
        private void tcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tcMain is null) return;
            TabControl tab = (sender is TabControl) ? sender as TabControl : null;
            if (tab is null || !ReferenceEquals(e.OriginalSource, tab)) return;

            int prevIndex = e.RemovedItems.Count > 0 ? tab.Items.IndexOf(e.RemovedItems[0]) : -1;
            int newIndex = e.AddedItems.Count > 0 ? tab.Items.IndexOf(e.AddedItems[0]) : -1;
            if (prevIndex == -1 || newIndex == -1) return;

            try
            {            
                if ((prevIndex == 1) && (newIndex == 0 || newIndex == 2)) // out of grid
                {
                    if (gridView.OutOfResources) gridView.Clear();
                    else gridView.CancelLoading();
                }
                if (tcMain.SelectedItem.Equals(tiStats)) 
                {
                    iDepotStats.Clear(); 
                    if (iDepot != null)
                    {
                        string path = iDepot.path;
                        if (iDepotStats.iDepot != null)
                            if (!iDepotStats.iDepot.IsSameAs(path)) 
                                image.Source = null;
                        iDepotStats.OnChangeDepot(path);
                        iDepot = null;
                    }
                    return;
                }
                ImageDepot wid; bool? ivf;
                (wid, ivf) = LoadImageDepot(tbImageDepot.Text); // wid from disk
                if (wid != null && (bool)ivf)
                {
                    if (!wid.IsSameAs(activeView.iDepot)) activeView.FeedList(ref wid, true);
                }
                if (tcMain.SelectedItem.Equals(tiList))
                {
                    switch (prevIndex) 
                    {
                        case 1: listView.SynchroChecked(gridView.GetItems(true, false)); listView.selectedIndex = gridView.selectedIndex;
                            break;
                        case 2: Refresh(true);/*listView.focusFirstRow(); listView.dGrid_SelectionChanged(null, null);*/ 
                            break;
                    }                    
                }
                if (tcMain.SelectedItem.Equals(tiGrid))
                {
                    switch (prevIndex)
                    {
                        case 0: gridView.SynchroChecked(listView.GetItems(true, false)); gridView.selectedIndex = listView.selectedIndex;
                            break;
                        case 2: Refresh(true);/*listView.focusFirstRow(); listView.dGrid_SelectionChanged(null, null);*/
                            break;
                    }
                }           
            }
            finally { if (views != null) ChangeDepot(iDepot, null); }
        }
        private void image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lastLoadedPic.Equals("")) return;
            DataObject data = new DataObject(DataFormats.FileDrop, new string[] { lastLoadedPic });
            // Start the drag-and-drop operation
            DragDrop.DoDragDrop(image, data, DragDropEffects.Copy);
        }
        bool inverting = false;
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            for (int i = 0; i < cmImgMenu.Items.Count; i++)
            {
                if (cmImgMenu.Items[i] is MenuItem)
                    (cmImgMenu.Items[i] as MenuItem).IsEnabled = isEnabled;
            }
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
                    activeView.SetChecked(null);
                }
            }
            CountChecked();
        }

        private void imagePickerUC_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
        }
    }
}

/* Single Tree in a Sparse Landscape: A lone tree in an otherwise empty field or desert, emphasizing simplicity and isolation.; Henri Matisse
Zen Garden: A carefully raked sand garden with a few strategically placed rocks, symbolizing tranquility and mindfulness.; Henri Matisse
Minimalist Seascape: A calm sea with a clear horizon line, perhaps with a solitary boat or bird in the distance.; Henri Matisse
Silhouette Against a Sunset: A simple outline of a figure or object set against the backdrop of a colorful yet uncluttered sunset.; Henri Matisse
Snow-Covered Branches: Close-up of tree branches lightly covered in snow, showcasing the beauty of nature in a subdued palette.; Henri Matisse
Single Drop of Water: A macro shot of a water droplet, possibly on a leaf or surface, highlighting the elegance in small details.; Henri Matisse
Minimalist Still Life: A composition with one or two objects, such as a bowl and a piece of fruit, arranged in a clean, uncluttered space.; Henri Matisse
Shadow Play: The play of light and shadow on a wall or floor, creating geometric or abstract forms.; Henri Matisse

*/