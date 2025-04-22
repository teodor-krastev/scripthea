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
        bool FeedList(string imageFolder); // the way to load the list
        bool FeedList(ref ImageDepot _iDepot); // external iDepot; regular use
        void UpdateVis(); // update visual from iDepot
        void UpdateVisRecord(int idx, ImageInfo ii); // update visual record from ii
        void SynchroChecked(List<Tuple<int, string, int, string>> chks);
        void SetChecked(bool? check); // if null invert; returns checked
        string markMask { get; }
        void CheckRange(int first, int last);
        void MarkWithMask(string mask); // mark some items; if "" unmark all 
        void Clear(bool inclDepotItems = false);
        int selectedIndex { get; set; } // one based index in no-checkable mode
        int Count { get; }
        List<Tuple<int, string, int, string>> GetItems(bool check, bool uncheck); // idx, filename, prompt
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
        private ImageDepot iDepot; // { get; private set; }
        public iPicList activeView { get { return views[tabCtrlViews.SelectedIndex]; } }
        private DispatcherTimer timer;
        private Options opts;
        public void UpdateVisRecord(int idx, ImageInfo ii) // update visual record from ii
        {
            activeView.UpdateVisRecord(idx, ii);
        }
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
            numDly.Minimum = 1; numDly.Maximum = 99; numDly.Value = 3;
            imageFolder = opts.composer.ImageDepotFolder; chkAutoRefresh.IsChecked = opts.viewer.Autorefresh; iDepot = null;
            colListWidth.Width = new GridLength(opts.composer.ViewColWidth);
            ImageDepotConvertor.AutoConvert = true; 
            foreach (iPicList ipl in views)
                ipl.Init(ref opts, false);
            picViewerUC.Init(ref _opts);
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
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = SctUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder: _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value;  tbImageDepot.Text = value;
            }
        }       
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        /// <summary>
        /// order - new is buffering
        /// next - if another new delete the file if inclFile
        ///     - "+" to restore
        /// work only with tableView
        /// </summary>
        private class Undo // now - only for table view; later - maybe unify with grid view
        {   
            private ImageDepot imageDepot; private int idx; private bool inclFile; private BitmapImage bitmapImage;
            private ImageInfo ii;
            public Undo(ref ImageDepot _imageDepot, int idx0, bool _inclFile) // buffer the entry when created
            {
                imageDepot = _imageDepot; idx = idx0; inclFile = _inclFile;               
                if (Utils.InRange(idx, 0, imageDepot.items.Count - 1))
                    ii = imageDepot.items[idx]; // by ref ?                
            }
            public event Utils.LogHandler OnLog;
            protected void Log(string txt, SolidColorBrush clr = null)
            {
                if (OnLog != null) OnLog(txt, clr);
            }
            public void ClearState()
            {
                ii = null; idx = -1; bitmapImage = null;
            }
            private bool checkOut()
            {
                if (imageDepot == null) return false;
                if (!imageDepot.isEnabled) return false;
                if (ii == null) return false;
                if (!Utils.InRange(idx, 0,imageDepot.items.Count - 1)) return false;               
                return true; 
            }
            public bool recover2ImageDepot() // get it back
            {
                if (!checkOut()) return false;
                imageDepot.items.Insert(idx, ii); imageDepot.Save();
                if (inclFile && bitmapImage != null) ImgUtils.SaveBitmapImageToDisk(bitmapImage, Path.Combine(imageDepot.path, ii.filename));
                ClearState();
                return true;
            }
            public void realRemove() // get it back
            {
                if (inclFile && checkOut())
                {
                    string fn = Path.Combine(imageDepot.path, ii.filename);
                    if (File.Exists(fn)) { bitmapImage = inclFile ? ImgUtils.UnhookedImageLoad(Path.Combine(fn)) : null; File.Delete(fn); }
                    else Log("Error: File <" + fn + "> does not exist");
                }
            }
        }
        private Undo undo = null;
        public int RemoveSelected(bool inclFile = false)
        {
            if (!activeView.HasTheFocus) return -1;
            if (opts.composer.QueryStatus == Status.Scanning) { Utils.TimedMessageBox("Error[885]: the IDF is updating."); return -1; }
            string ss = inclFile ? "and image file" : ""; bool anim = animation; animation = false;
            Log("Deleting image #" + (activeView.selectedIndex + 1).ToString()+ " entry "+ ss, Brushes.Tomato);
            if (iDepot == null) { Log("Error: no active image depot found"); return -1; }
            if (!iDepot.isEnabled) { Log("Error: current image depot - not active"); return -1; }
            int idx0 = activeView.selectedIndex;
            if (!Utils.InRange(idx0, 0, iDepot.items.Count - 1)) { Log("index out of limits"); return -1; }

            string markMask = activeView.markMask;
            if (tabCtrlViews.SelectedItem == tiTable) // tableView
            {                                
                undo = new Undo(ref iDepot, idx0, inclFile); undo.OnLog += new Utils.LogHandler(Log); 
                undo.realRemove(); // 
                if (iDepot.RemoveAt(idx0, false)) iDepot.Save(); // iDepot correction
                else { Log("Error: Unsuccessful delete operation"); return -1; }
                Refresh(); 
            }
            else // gridView
            {
                gridViewUC.RemoveAt(inclFile);
                if (iDepot.RemoveAt(idx0, opts.viewer.RemoveImagesInIDF)) iDepot.Save(); // iDepot correction
                else { Log("Error[237]: Unsuccessful delete operation"); return -1; }
            }
            if (!iDepot.isEnabled) { Log("Error[238]: current image depot - not active"); return -1; }
            // restore selection, masked and animation  
            activeView.selectedIndex = Utils.EnsureRange(idx0, 0, iDepot.items.Count - 1);
            activeView.MarkWithMask(markMask);
            if (anim) animation = true;
            if (iDepot.isEnabled) lbDepotInfo.Content = iDepot.items.Count.ToString() + " images";
            return idx0;
        } 
        public void Clear() 
        {
            activeView?.Clear(); picViewerUC?.Clear(); activeView?.MarkWithMask(""); undo?.ClearState();
        }
        private bool updating = false; private bool showing = false;
        public bool ShowImageDepot(string imageDepot)
        {
            if (updating) return false;
            updating = true;
            if (tbImageDepot.Text != imageDepot) tbImageDepot.Text = imageDepot; 
            else tbImageDepot_TextChanged(null, null);
            updating = false; showing = true;
            return tbImageDepot.Foreground.Equals(Brushes.Black);
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
        private int checkImageDepot()
        {
            int cnt = SctUtils.checkImageDepot(imageFolder);
            if (cnt > 0) { lbDepotInfo.Content = cnt.ToString() + " images"; lbDepotInfo.Foreground = Brushes.Blue; }
            else { lbDepotInfo.Content = "This is not an image depot."; lbDepotInfo.Foreground = Brushes.Tomato; }
            return cnt;
        }
        public void Refresh(string iFolder = "", object sender = null) 
        {
            iDepot?.OnClose();
            string folder = iFolder;
            if (iDepot != null && iFolder == "")
            {
                if (iDepot.isEnabled && Directory.Exists(iDepot.path)) folder = iDepot.path;
                else folder = imageFolder;
            }
            if (!Directory.Exists(folder)) { Log("Error[874]: Missing directory > " + folder); return; }
            if (iDepot != null && sender != btnRefresh)
            {
                bool bb = sender == tabCtrlViews;
                bool bc = activeView == gridViewUC;
                if (bc) bc = iDepot.SameAs(activeView.loadedDepot);                
                if (iDepot.SameAs(folder) && bb && bc) return;                                        
            }                
            iDepot = new ImageDepot(imageFolder);
            if (!iDepot.isEnabled) { Log("Error[96]: This is not an image depot."); return; }
            else lbDepotInfo.Content = iDepot.items.Count.ToString() + " images"; 
            imageFolderShown = imageFolder;

            List<Tuple<int, string, int, string>> decompImageDepot = iDepot.Export2Viewer(); 
            if (!Utils.isNull(decompImageDepot))
            {
                showing = false;
                activeView.FeedList(ref iDepot); picViewerUC.SetiDepot(iDepot);
                if (activeView == tableViewUC) tableViewUC.focusFirstRow();
                showing = true;
            }
            animation = false; btnPlay.IsEnabled = decompImageDepot.Count > 0;            
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e) 
        {
            if (checkImageDepot() == 0)
            {
                if ((sender == btnRefresh) || (sender == tbImageDepot)) Clear();
            }
            else Refresh(imageFolder, sender);
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {           
            if (checkImageDepot() > 0)
            {
                tbImageDepot.Foreground = Brushes.Black;
                if (chkAutoRefresh.IsChecked.Value)
                {
                    btnRefresh_Click(sender, e);
                }
            }
            else 
            { 
                tbImageDepot.Foreground = Brushes.Red;                
            }     
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value) { colRefresh.Width = new GridLength(0); btnRefresh.Visibility = Visibility.Collapsed; btnRefresh_Click(sender, e); }
            else { colRefresh.Width = new GridLength(40); btnRefresh.Visibility = Visibility.Visible; }
        }
        private int lastIdx = 0;
        private void tabCtrlViews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            animation = false;
            if ((lastIdx == 1) && (tabCtrlViews.SelectedIndex == 0))
            {
                if (gridViewUC.OutOfResources) gridViewUC.Clear();
                else
                {
                    if (gridViewUC.Loading) gridViewUC.CancelRequest = true;
                }
            }
            lastIdx = tabCtrlViews.SelectedIndex;
            if (chkAutoRefresh.IsChecked.Value && showing) btnRefresh_Click(sender, e);             
            if (!Utils.isNull(e)) e.Handled = true;
        }        
        public bool animation
        {
            get { return btnStop.Visibility.Equals(Visibility.Visible); }
            set 
            {
                if (value.Equals(animation)) return;
                bool vl = value;
                if (activeView.iDepot == null) vl = false;
                if (vl) { btnStop.Visibility = Visibility.Visible; btnPlay.Visibility = Visibility.Collapsed; }
                else { btnStop.Visibility = Visibility.Collapsed; btnPlay.Visibility = Visibility.Visible; }
                if (vl)
                {
                    if (timer == null) timer = new DispatcherTimer(TimeSpan.FromSeconds(numDly.Value), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
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
            if (activeView.iDepot == null) return;
            int cnt = activeView.iDepot.items.Count;
            if (activeView.selectedIndex.Equals(cnt-1)) animation = false;
            if (Utils.InRange(activeView.selectedIndex, 0,cnt-2)) activeView.selectedIndex += 1;
        }
        private void numDly_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (timer == null) return;
            timer.Interval = TimeSpan.FromSeconds(numDly.Value);
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            animation = sender.Equals(btnPlay); 
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
                e.Handled = true; return;
            }           
            if (e.Key.Equals(Key.Add)) // recover from undo
            {
                int si = activeView.selectedIndex;
                bool bb = false;
                if (tabCtrlViews.SelectedIndex == 0) // table view
                {
                    if (undo == null) return;
                    bb = undo.recover2ImageDepot(); 
                    if (bb) Refresh();
                }
                else // grid view
                {
                    bb = gridViewUC.Recover();
                }
                if (bb) Log("Recover a deleted image", Brushes.Crimson);
                activeView.selectedIndex = Utils.EnsureRange(si, 0, iDepot.items.Count-1);
                activeView.MarkWithMask(tbFind.Text);
                checkImageDepot();
                e.Handled = true; return;
            }
            int intKey = (int)e.Key-74;
            if (Utils.InRange(intKey, 0, 74))
            {
                if (iDepot == null || picViewerUC == null || opts == null) return;
                if (!iDepot.isEnabled) return;
                if (opts.composer.QueryStatus == Status.Scanning) { Utils.TimedMessageBox("Error[886]: the IDF is updating."); return; }
                picViewerUC.sldRate.Value = intKey;
                e.Handled = true; return;
            }
        }
        private void btnMark_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnMark)) activeView.MarkWithMask(tbFind.Text);
            else activeView.MarkWithMask("");
        }
    }
}
