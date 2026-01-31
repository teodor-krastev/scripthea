using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Newtonsoft.Json;
using scripthea.master;
using scripthea.options;
using scripthea.composer;
using UtilsNS;
using System.Globalization;

namespace scripthea.viewer 
{
    public delegate void UpdateVisRecEventHandler(int idx, ImageInfo ii);
    public interface iPicList
    {
        void Init(ref Options _opts, bool _checkable);
        void Finish();
        bool IsAvailable { get; }
        bool HasTheFocus { get; set; }
        ImageDepot iDepot { get; set; }
        string loadedDepot { get; set; }
        void RemoveAt(bool inclFile, int idx = -1);
        bool Recover(); // recover removed
        bool FeedList(string imageFolder, bool force); // the way to load the list
        bool FeedList(ref ImageDepot _iDepot, bool force); // external iDepot; regular use
        void UpdateVis(); // update visual from iDepot
        void UpdateVisRecord(int idx, ImageInfo ii); // update visual record from ii
        void SynchroChecked(List<Tuple<int, string, int, string>> chks);
        void SetChecked(bool? check); // if null invert; returns checked
        string markMask { get; }
        void CheckRange(int first, int last);
        void CheckRate(double rate);
        int MarkWithMask(string mask); // mark some items; if "" unmark all 
        void Clear(bool inclDepotItems = false);
        int selectedIndex { get; set; } // one based index in no-checkable mode
        int Count { get; }
        int CountChecked { get; }
        List<Tuple<int, string, int, string>> GetItems(bool check, bool uncheck); // idx, filename, rate, prompt
    }

    
    /// <summary>
    /// Interaction logic for ViewerUC.xaml
    /// </summary>
    public partial class ViewerUC : UserControl, iFocusControl
    {       
        public ViewerUC()
        {
            InitializeComponent();
            views = new List<iPicList>(); 
            views.Add(tableViewUC); tableViewUC.SelectEvent += new TableViewUC.PicViewerHandler(picViewerUC.loadPic); 
            views.Add(gridViewUC);  gridViewUC.SelectEvent += new GridViewUC.PicViewerHandler(picViewerUC.loadPic);
            picViewerUC.OnUpdateVisRecord += new UpdateVisRecEventHandler(UpdateVisRecord);

        }
        public bool HasTheFocus { get; set; }
        private ImageDepot iDepot; // { get; private set; }
        public iPicList activeView { get { int idx = tabCtrlViews.SelectedIndex == 2 ? 0 : tabCtrlViews.SelectedIndex; return views[idx]; } }
        private DispatcherTimer timer;
        private Options opts;
        public void UpdateVisRecord(int idx, ImageInfo ii) // update visual record from ii
        {
            activeView.UpdateVisRecord(idx, ii); UpdateCounter(true);
        }
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
            numDly.Minimum = 1; numDly.Maximum = 99; numDly.Value = 3;
            _imageFolder = opts.composer.ImageDepotFolder; chkAutoRefresh.IsChecked = opts.viewer.Autorefresh; iDepot = null;
            colListWidth.Width = new GridLength(opts.composer.ViewColWidth);
            ImageDepotConvertor.AutoConvert = true; 
            foreach (iPicList ipl in views)
                ipl.Init(ref opts, false);
            picViewerUC.Init(ref _opts);
            iDepotStats.Init(ref opts);
        }
        public void Finish()
        {
            iDepot?.OnClose();
            opts.composer.ViewColWidth = Convert.ToInt32(colListWidth.Width.Value);
            opts.viewer.Autorefresh = chkAutoRefresh.IsChecked.Value;
            foreach (iPicList ipl in views)
                ipl.Finish();
            picViewerUC.Finish();
        }

        public UserControl parrent { get { return this; } }
        public GroupBox groupFolder { get { return gbFolder; } }
        public TextBox textFolder { get { return tbImageDepot; } }
        public string imageFolderShown { get; private set; }
        private string _imageFolder;
        public string imageFolder // the text box or default
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = SctUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder: _imageFolder + "\\";
            }            
        }       
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (!(opts is null)) opts.Log(txt, clr);
        }
        /// <summary>
        /// order - new is buffering
        /// next - if another new delete the file if inclFile
        ///     - "+" to restore
        /// work only with tableView
        /// </summary>
        public int RemoveSelected(bool inclFile = false)
        {
            if (!activeView.HasTheFocus) return -1;
            if (opts.composer.QueryStatus == Status.Scanning) { Utils.TimedMessageBox("Error[885]: the IDF is updating."); return -1; }
            string ss = inclFile ? "and image file" : ""; bool anim = animation; animation = false;
            Log("Deleting image #" + (activeView.selectedIndex + 1).ToString()+ " entry "+ ss, Brushes.Tomato);
            if (iDepot is null) { Log("Error: no active image depot found"); return -1; }
            if (!iDepot.isEnabled) { Log("Error: current image depot - not active"); return -1; }
            int idx0 = activeView.selectedIndex;
            if (!Utils.InRange(idx0, 0, iDepot.items.Count - 1)) { Log("index out of limits"); return -1; }

            string markMask = activeView.markMask;
            activeView.RemoveAt(inclFile, idx0);
            if (iDepot.RemoveAt(idx0, inclFile)) iDepot.Save(); // iDepot file correction
            else { Log("Error: Unsuccessful delete operation"); return -1; }

            if (tabCtrlViews.SelectedItem == tiTable) Refresh(true); // tableView
            
            if (!iDepot.isEnabled) { Log("Error[238]: current image depot - not active"); return -1; }
            // restore selection, masked and animation  
            activeView.selectedIndex = Utils.EnsureRange(idx0, 0, iDepot.items.Count - 1);
            activeView.MarkWithMask(markMask);
            if (anim) animation = true;
            UpdateCounter(true); 
            return idx0;
        } 
        public void Clear() 
        {
            activeView?.Clear(); picViewerUC?.Clear(); activeView?.MarkWithMask(""); 
        }
        private bool updating = false; private bool showing = false;
        public bool ShowImageDepot(string iFolder)
        {
            if (updating) return false;
            try
            {
                updating = true;
                if (iFolder.Equals(emptyText)) return Refresh(true, emptyText); 

                if (Directory.Exists(iFolder)) tbImageDepot.Text = iFolder; 
                else tbImageDepot_TextChanged(null, null);           
                return tbImageDepot.Foreground.Equals(Brushes.Black);
            }
            finally{ updating = false; showing = true; }
        }
        List<iPicList> views;        
        private void btnFindUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (tabCtrlViews.SelectedIndex.Equals(0)) tableViewUC.SortTableByIndex();
            int k = sender.Equals(btnFindUp) ? -1 : 1;
            if (activeView.selectedIndex.Equals(-1)) activeView.selectedIndex = 0;
            int idx = activeView.selectedIndex + k;
            List<Tuple<int, string, int, string>> items = activeView.GetItems(true,true);
            while (idx > -1 && idx < items.Count)
            {
                string prompt = Convert.ToString(items[idx].Item2);                
                if (Utils.IsWildCardMatch(prompt,tbFind.Text) || tbFind.Text.Equals("")) //prompt.IndexOf(tbFind.Text) > -1)
                {
                    activeView.selectedIndex = idx; break;
                }
                idx += k;
            }
        }
        private int UpdateCounter(bool actView) // update image counter from iDepot (actView=false) or activeView
        {
            lbDepotInfo.Foreground = Brushes.Blue; int cnt = 0;
            if (actView && activeView != null) cnt = activeView.Count;
            else
            {
                if (iDepot != null)
                {
                    if (iDepot.isEnabled)
                        if (iDepot.items.Count > 0)
                        { lbDepotInfo.Content = iDepot.items.Count.ToString() + " images" + (iDepot.IsChanged() ? "*" : ""); return iDepot.items.Count; }
                }
                cnt = SctUtils.checkImageDepot(imageFolder);
            }
            if (cnt > -1) { lbDepotInfo.Content = cnt.ToString() + " images";  }
            else { lbDepotInfo.Content = "This is not an image depot."; lbDepotInfo.Foreground = Brushes.Tomato; }
            btnPlay.IsEnabled = cnt > 0;
            return cnt;
        }
        private TabItem lastTabItem = null;
        public void RefreshDepotFileWithChanges()
        {
            iDepot?.OnClose(); // update the file from memory
        }
        public bool Refresh(bool forced, string iFolder = "") // <empty>
            // if forced -> just unconditionally refresh from iDepot, but recreate iDepot if First: iFolder exists or Second: iDepot is null
            // if forced == false -> compare iDepotFromFile.SameAs(items from visual) and refresh from iDepot if they are NOT the same
        {
            try
            {
                if (views is null) return true; int k = UpdateCounter(!forced); 
                if (forced)
                { 
                    if (iFolder.Equals(emptyText)) 
                    {  
                        if (tabCtrlViews.SelectedIndex == 2) iDepotStats.Clear();
                        else activeView?.Clear(); 
                        picViewerUC.Clear(); UpdateCounter(true); return true;  
                    }
                    if (Directory.Exists(iFolder)) iDepot = new ImageDepot(iFolder);
                    else { if (iFolder != "") { Log("Warning[873]: Non-valid directory > " + iFolder); return false; } }
                    if (iDepot is null && Directory.Exists(imageFolder)) iDepot = new ImageDepot(imageFolder);
                    if (iDepot is null || !iDepot.isEnabled) { Log("Error[874]: Wrong image-depot"); return false; }

                    if (tabCtrlViews.SelectedItem == tiStats)
                    {
                        iDepotStats.Clear();
                        iDepotStats.OnChangeDepot(iDepot.path); 
                        if (iDepotStats.iDepot != null) 
                            if (!iDepotStats.iDepot.IsSameAs(iFolder)) picViewerUC.Clear();
                        UpdateCounter(false); btnPlay.IsEnabled = false;
                        return true;
                    }
                    List<Tuple<int, string, int, string>> decompImageDepot = iDepot.Export2Viewer();
                    animation = false; 
                    if (!Utils.isNull(decompImageDepot))
                    {
                        showing = false;
                        activeView.FeedList(ref iDepot, forced); picViewerUC.SetiDepot(iDepot);
                        if (activeView == tableViewUC) tableViewUC.focusFirstRow();
                        showing = true; UpdateCounter(true);
                    }
                    else Refresh(true, emptyText);
                    btnPlay.IsEnabled = decompImageDepot.Count > 0;
                    return true;
                }
                else
                {
                    if (iDepot is null) return Refresh(true); 
                    if (!Directory.Exists(iFolder)) { Log("Error[873]: Missing directory > " + iFolder); return false; }

                    if (activeView.iDepot is null) return Refresh(true);
                    ImageDepot iDepotFromFile = new ImageDepot(iFolder);
                    List<Tuple<int, string, int, string>> lst = activeView.GetItems(true, true);
                    if (!iDepotFromFile.IsSameAs(lst)) return Refresh(true);
                }
                return true;
            }
            finally { lastTabItem = (TabItem)tabCtrlViews.SelectedItem;  }
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e) 
        {
            string iFolder = tbImageDepot.Text.Trim();
            if (!Directory.Exists(iFolder)) iFolder = emptyText;
            Refresh(true, iFolder);
            if (!Utils.isNull(e)) e.Handled = true;
        }
        public string emptyText { get => opts?.viewer.emptyText; }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {           
            if (Directory.Exists(tbImageDepot.Text))
                tbImageDepot.Foreground = Brushes.Black;
            else 
                tbImageDepot.Foreground = Brushes.Red;  // flag if wrong folder              
            if (chkAutoRefresh.IsChecked.Value)
                btnRefresh_Click(sender, e);           
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value) { colRefresh.Width = new GridLength(0); btnRefresh.Visibility = Visibility.Collapsed; btnRefresh_Click(btnRefresh, e); }
            else { colRefresh.Width = new GridLength(40); btnRefresh.Visibility = Visibility.Visible; }
        }
        private void tabCtrlViews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Directory.Exists(tbImageDepot.Text.Trim())) { Refresh(true, emptyText); return; }

            TabControl tab = (sender is TabControl) ? sender as TabControl : null;
            if (tab is null || !ReferenceEquals(e.OriginalSource, tab)) return;

            int prevIndex = e.RemovedItems.Count > 0 ? tab.Items.IndexOf(e.RemovedItems[0]) : -1;
            int newIndex = e.AddedItems.Count > 0 ? tab.Items.IndexOf(e.AddedItems[0]) : -1;
            if (prevIndex == -1 || newIndex == -1 || iDepot is null) return;
            try
            {           
                animation = false;
                if ((prevIndex == 1) && (newIndex == 0 || newIndex == 2)) // out of grid
                {
                    if (gridViewUC.OutOfResources) gridViewUC.Clear();
                    else gridViewUC.CancelLoading();               
                }                  
                if (tabCtrlViews.SelectedItem.Equals(tiStats)) { btnPlay.IsEnabled = false; iDepotStats.OnChangeDepot(imageFolder); return; }
                bool bb = true;
                if (chkAutoRefresh.IsChecked.Value && showing && !iDepot.IsSameAs(activeView.GetItems(true, true)))
                {
                    bb = Refresh(true); // from iDepot
                    iDepot.Save();
                    UpdateCounter(true);
                } 
                if (showing)
                {
                    if (prevIndex == 0 && newIndex == 1) gridViewUC.selectedIndex = tableViewUC.selectedIndex;
                    if (prevIndex == 1 && newIndex == 0) tableViewUC.selectedIndex = gridViewUC.selectedIndex;
                    btnMark_Click(sender, e);
                }               
            }
            finally { if (!Utils.isNull(e)) e.Handled = true; }
        }        
        public bool animation
        {
            get { return btnStop.Visibility.Equals(Visibility.Visible); }
            set 
            {
                if (value.Equals(animation)) return;
                bool vl = value;
                if (activeView.iDepot is null) vl = false;
                if (vl) { btnStop.Visibility = Visibility.Visible; btnPlay.Visibility = Visibility.Collapsed; }
                else { btnStop.Visibility = Visibility.Collapsed; btnPlay.Visibility = Visibility.Visible; }
                if (vl)
                {
                    if (timer is null) timer = new DispatcherTimer(TimeSpan.FromSeconds(numDly.Value), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
                    timer?.Start();
                }
                else
                {
                    timer?.Stop();
                }
            }
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (activeView.iDepot is null) return;
            int cnt = activeView.iDepot.items.Count;
            if (activeView.selectedIndex.Equals(cnt-1)) animation = false;
            if (Utils.InRange(activeView.selectedIndex, 0,cnt-2)) activeView.selectedIndex += 1;
        }
        private void numDly_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (timer is null) return;
            timer.Interval = TimeSpan.FromSeconds(numDly.Value);
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            animation = sender.Equals(btnPlay) && ((tabCtrlViews.SelectedIndex.Equals(0) || tabCtrlViews.SelectedIndex.Equals(1)) && activeView.Count > 0);
            e.Handled = true;
        }        
        private void ucViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl)
            {
                switch (e.Key)
                {
                    case Key.Add: picViewerUC.zoomControl(1);
                        break;
                    case Key.Multiply: picViewerUC.zoomControl(0);
                        break;
                    case Key.Subtract: picViewerUC.zoomControl(-1);
                        break;
                }
                e.Handled = true; return;
            }
            if (!activeView.HasTheFocus) return;
            if (e.Key.Equals(Key.Delete) || e.Key.Equals(Key.Subtract)) // it goes here first and then to inside comp.
            {
                Log("@Explore=70"); 
                RemoveSelected(opts.viewer.RemoveImagesInIDF);
                UpdateCounter(true); e.Handled = true; return;
            }           
            if (e.Key.Equals(Key.Add)) // recover from undo
            {
                int si = activeView.selectedIndex;
                bool bb = activeView.Recover();
                if (bb && tabCtrlViews.SelectedIndex == 0) Refresh(true);
                if (bb) Log("Recover a deleted image", Brushes.Crimson);
                activeView.selectedIndex = Utils.EnsureRange(si, 0, iDepot.items.Count-1);
                activeView.MarkWithMask(tbFind.Text);
                UpdateCounter(true);
                e.Handled = true; return;
            }
            int intKey = (int)e.Key-74;
            if (Utils.InRange(intKey, 0, 74))
            {
                if (iDepot is null || picViewerUC is null || opts is null) return;
                if (!iDepot.isEnabled) return;
                if (opts.composer.QueryStatus == Status.Scanning) { Utils.TimedMessageBox("Error[886]: the IDF is updating."); return; }
                picViewerUC.sldRate.Value = intKey;
                e.Handled = true; return;
            }
        }
        private void btnMark_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnMark))
            { tbMarkCount.Visibility = Visibility.Visible; tbMarkCount.Text = "#" + activeView.MarkWithMask(tbFind.Text); colMark.Width = new GridLength(170); }
            else { activeView.MarkWithMask(""); tbMarkCount.Visibility = Visibility.Collapsed; colMark.Width = new GridLength(143); }
        }
        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter)) { btnMark_Click(btnMark, e); e.Handled = true; }
        }
    }
}
