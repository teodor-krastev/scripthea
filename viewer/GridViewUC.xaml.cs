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
using scripthea.master;
using scripthea.options;
using UtilsNS;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for GridViewUC.xaml
    /// </summary>
    public partial class GridViewUC : UserControl, iPicList
    {
        protected List<PicItemUC> picItems; 
        public bool OutOfResources = false;
        public GridViewUC() 
        {
            InitializeComponent();
            picItems = new List<PicItemUC>();
        }
        Options opts;
        public bool checkable { get; set; }
        public void Init(ref Options _opts, bool _checkable)
        {
            undoRec = new UndoRec(); undoRec.Clear();
            opts = _opts; checkable = _checkable;
            lbZoom.Content = opts.viewer.ThumbZoom.ToString() + "%";
            chkShowCue.IsChecked = opts.viewer.ThumbCue; chkShowFilename.IsChecked = opts.viewer.ThumbFilename;            
        }
        public bool CancelRequest = false;
        private bool ShuttingDown = false;
        public void Finish()
        {
            ShuttingDown = true; 
        }
        public ImageDepot iDepot { get; set; }
        public bool IsAvailable 
        { 
            get 
            {
                if (Utils.isNull(iDepot)) return false;
                else return iDepot.isEnabled;
            } 
        }
        private bool _HasTheFocus;
        public bool HasTheFocus 
        { 
            get { return _HasTheFocus; } 
            set
            { 
                _HasTheFocus = value;
                foreach (PicItemUC piUC in picItems)
                    piUC.focused = value;
            }
        }
        public string loadedDepot { get; set; }       
        protected PicItemUC SelectedPicItem() // from inside PicItem
        {
            if (!IsAvailable) return null;
            foreach (PicItemUC ps in picItems)
                if (ps.selected) return ps;
            return null;
        }
        protected ImageInfo SelectedItem(int idx) // idx - 0 based
        {
            ImageInfo ii = null;
            if (iDepot != null)
                if (iDepot.isEnabled && Utils.InRange(idx, 0, iDepot.items.Count - 1))
                    ii = iDepot.items[idx];
            if (ii == null && SelectedPicItem() != null) ii = iDepot.items[SelectedPicItem().idx];
            return ii;
        }
        private bool _Loading;
        public bool Loading
        {
            get { return _Loading; }
            private set
            {
                _Loading = value;
                if (Loading) { Mouse.OverrideCursor = Cursors.Wait; }
                else { Mouse.OverrideCursor = null; }
                Utils.DoEvents();
            }
        }
        public void UpdateVis()
        {
            try
            {
                Loading = true; CancelRequest = false;
                if (!IsAvailable) Clear(); 
                picItemsClear(); int cnt = iDepot.items.Count; 
                foreach (var itm in iDepot.Export2Viewer())
                {
                    if (ShuttingDown) return;
                    if (CancelRequest) { CancelRequest = false; return; }
                    PicItemUC piUC = new PicItemUC(ref opts, checkable); piUC.IsChecked = true;
                    picItems.Add(piUC); labelNum.Content = (int)(100.0 * picItems.Count / cnt) + "%";
                    int k = Utils.InRange(thumbsPerRow, 2,20) ? thumbsPerRow : 4;
                    if ((picItems.Count % k) == 1) 
                    {
                        if (scroller.VerticalOffset > 0) scroller.ScrollToHome(); 
                        Utils.DoEvents(); 
                    }
                    piUC.ContentUpdate(itm.Item1, imageFolder, SelectedItem(itm.Item1));                      
                    piUC.VisualUpdate();
                    piUC.OnSelect -= new RoutedEventHandler(SelectTumb); piUC.OnSelect += new RoutedEventHandler(SelectTumb); 
                    wrapPics.Children.Add(piUC);
                    if (checkable)
                    {
                        piUC.chkChecked.Checked -= new RoutedEventHandler(ChangeContent);   piUC.chkChecked.Checked += new RoutedEventHandler(ChangeContent); 
                        piUC.chkChecked.Unchecked -= new RoutedEventHandler(ChangeContent); piUC.chkChecked.Unchecked += new RoutedEventHandler(ChangeContent);
                    }
                    if (itm.Item1 == 0)
                    {
                        piUC.selected = true;
                        OnSelect(itm.Item1,  iDepot);
                    }
                }
                if (picItems.Count > 0) picItems[0].selected = true;
                ChangeContent(this, null); 
            }
            finally 
            { Loading = false; labelNum.Content = ""; selectedIndex = 0; scrollToIdx(); }
        }
        public void UpdateVisRecord(int idx, ImageInfo ii) // update visual record from ii
        {
            if (iDepot == null || ii == null) return;
            if (!Utils.InRange(idx, 0, iDepot.items.Count - 1) || !Utils.InRange(idx, 0, picItems.Count - 1) ) return;
            picItems[idx].ContentUpdate(idx, iDepot.path, ii); selectedIndex = idx;
        }
        public void SynchroChecked(List<Tuple<int, string, int, string>> chks)
        {
            if (!checkable) return;
            SetChecked(false);
            foreach (Tuple<int, string, int, string> chk in chks)
            {
                int idx = chk.Item1;
                if (Utils.InRange(idx, 0,picItems.Count-1)) picItems[idx].IsChecked = true;
            }                
        }
        public void SetChecked(bool? check)
        {
            if (!checkable) return;
            foreach (PicItemUC piUC in picItems)
            {
                 if (check == null) piUC.IsChecked = !piUC.IsChecked;
                else piUC.IsChecked = Convert.ToBoolean(check);
            }
        }

        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public struct UndoRec // undo buffer
        { 
            public int idx0; public PicItemUC piUC; public ImageInfo ii; public bool inclFile; 
            public void Clear() { idx0 = -1; piUC = null; ii = null; inclFile = false; } // clear buffer
            public bool full { get { return idx0 > -1 && piUC != null && ii != null;  } } // valid buffer
            public bool RealRemove(string iDepotPath)
            {
                if (!inclFile || !full) return true;
                string fn = Path.Combine(iDepotPath, ii.filename);
                if (File.Exists(fn)) { File.Delete(fn); return true; }
                else return false;
            }
        } 
        UndoRec undoRec;

        public void RemoveAt(bool inclFile, int idx = -1) // default selected
        {
            if (undoRec.full)
            {
                undoRec.inclFile = inclFile;
                if (!undoRec.inclFile)
                {
                    if (!undoRec.RealRemove(iDepot.path)) Log("Wrn: Image file is missing");
                }
                undoRec.Clear();
            }
            int si = selectedIndex;
            int j = idx.Equals(-1) ? selectedIndex : idx;
            if (!Utils.InRange(j, 0, picItems.Count - 1)) return;
            // buffer - saving and overwriting
            undoRec.idx0 = j; undoRec.piUC = picItems[j]; undoRec.ii = iDepot.items[j]; undoRec.inclFile = inclFile;
            // remove index -> j
            wrapPics.Children.Remove(picItems[j]);                
            picItems.RemoveAt(j); GC.Collect(); wrapPics.UpdateLayout();
            // renumber picItems
            for (int i = 0; i < picItems.Count; i++)           
                picItems[i].idx = i;                       
            selectedIndex = Utils.EnsureRange(si, 0, picItems.Count-1);               
        }
        public bool Recover()
        {
            bool bb = undoRec.idx0 > -1 && undoRec.piUC != null && undoRec.ii != null; // valid buffer
            if (bb)
            {
                ImgUtils.SaveBitmapImageToDisk(undoRec.piUC.bitmapImage, Path.Combine(iDepot.path, undoRec.ii.filename));
                wrapPics.Children.Insert(undoRec.idx0, undoRec.piUC);
                picItems.Insert(undoRec.idx0, undoRec.piUC);
                iDepot.items.Insert(undoRec.idx0, undoRec.ii); iDepot.Save();
                // renumber picItems
                for (int i = 0; i < picItems.Count; i++)
                    picItems[i].idx = i;
                selectedIndex = Utils.EnsureRange(undoRec.idx0, 0, picItems.Count-1);
                undoRec.idx0 = -1; undoRec.piUC = null;
            }
            return bb; 
        }
        private void picItemsClear()
        {
            for(int i = 0; i < picItems.Count; i++)
            {
                picItems[i].Clear();
            }            
            picItems.Clear(); GC.Collect(); wrapPics.Children.Clear(); wrapPics.UpdateLayout();
        }
        public string imageFolder { get { return iDepot == null ? "":iDepot.path; } }
        public void CheckRange(int first, int last)
        {
            foreach (PicItemUC piUC in picItems)
                if (checkable) piUC.IsChecked = Utils.InRange(piUC.idx, first, last);                
        }
        private string _markMask = "";
        public string markMask { get { return _markMask; } }
        public void MarkWithMask(string mask)
        {            
            _markMask = mask;
            foreach (PicItemUC piUC in picItems)
            {
                bool bb = mask.Equals("") ? false : Utils.IsWildCardMatch(piUC.imgInfo.prompt, mask);
                if (checkable) piUC.IsChecked = bb;
                else piUC.marked = bb;
            }
        }
        public void Clear(bool inclDepotItems = false)
        {            
            picItemsClear(); 
            if (inclDepotItems) iDepot?.items.Clear();                       
        }
        public bool FeedList(string imageDepot) 
        {
            Clear();
            if (!Directory.Exists(imageDepot)) { Log("Error[986]: no such folder -> " + imageDepot); return false; }
            //if (ImgUtils.checkImageDepot(imageDepot) == 0) { Log("Error[]: not image depot folder -> " + imageDepot); return false; }
            ImageDepot _iDepot = new ImageDepot(imageDepot, ImageInfo.ImageGenerator.FromDescFile);
            return FeedList(ref _iDepot);
        }
        public bool FeedList(ref ImageDepot _iDepot) // external iDepot; regular use
        {
            if (_iDepot == null) return false;
            if (!Directory.Exists(_iDepot.path)) { Log("Error[785]: no such folder -> " + _iDepot.path); return false; }
            iDepot = _iDepot; loadedDepot = iDepot.path;
            UpdateVis();
            return true;
        }
        public int selectedIndex // 0 based index
        {
            get 
            {
                foreach (PicItemUC piUC in picItems)                
                    if (piUC.selected) return piUC.idx;                    
                return -1; 
            }
            set
            {
                if (!Utils.InRange(value, 0, Count-1) || Count == 0) return;
                int idxS = value;
                PicItemUC piUC2 = null;
                foreach (PicItemUC piUC in picItems) // reset all
                {                    
                    piUC.selected = piUC.idx.Equals(idxS);                    
                    if (piUC.selected) { piUC.focused = true; piUC2 = piUC; }
                } 
                if (piUC2 == null) 
                    { Log("Error[352]: internal selected index"); return; }
                scrollToIdx(value);
                if (piUC2 != null)
                    OnSelect(piUC2.idx, iDepot);
            }
        }
        public int Count { get { return picItems.Count; } }
        public List<Tuple<int, string, int, string>> GetItems(bool check, bool uncheck)
        {
            if (!checkable && iDepot.isEnabled) { return iDepot.Export2Viewer(); }
            List<Tuple<int, string, int, string>> itms = new List<Tuple<int, string, int, string>>();
            foreach (PicItemUC piUC in picItems)
            {
                if (piUC.imgInfo == null) continue;
                if (piUC.IsChecked == null) continue;
                if ((bool)piUC.IsChecked)
                {
                    if (check) itms.Add(new Tuple<int, string, int, string>(piUC.idx, piUC.imgInfo.prompt, piUC.imgInfo.rate, piUC.imgInfo.filename));
                }
                else
                {
                    if (uncheck) itms.Add(new Tuple<int, string, int, string>(piUC.idx, piUC.imgInfo.prompt, piUC.imgInfo.rate, piUC.imgInfo.filename));
                }
            }
            return itms;            
        }
        
        public delegate void PicViewerHandler(int idx, ImageDepot _iDepot); // 0 based
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, ImageDepot _iDepot)
        {
            if (SelectEvent != null) SelectEvent(idx, _iDepot);
        }
        public event RoutedEventHandler OnChangeContent;
        protected void ChangeContent(object sender, RoutedEventArgs e)
        {
            if (OnChangeContent != null) OnChangeContent(sender, e);
        }
        protected void SelectTumb(object sender, RoutedEventArgs e)
        {
            if (Count == 0) return;
            foreach (PicItemUC piUC in picItems) // reset all            
                if (piUC.selected) piUC.selected = false;
            string fn = (sender as PicItemUC).imgInfo.filename; int k = 1;
            foreach (PicItemUC piUC in picItems)
            {
                if (piUC.Equals(sender))
                {
                    piUC.selected = true;
                    OnSelect(piUC.idx, iDepot);
                }
                k++;
            }
        }
        private void UpdatePicItems()
        {
            if (Count == 0) return;
            foreach (PicItemUC piUC in picItems) piUC.VisualUpdate();
        } 
        private void chkShowCue_Checked(object sender, RoutedEventArgs e)
        {
            opts.viewer.ThumbCue = chkShowCue.IsChecked.Value; opts.viewer.ThumbFilename = chkShowFilename.IsChecked.Value;
            UpdatePicItems();
        }
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            int newZoom = opts.viewer.ThumbZoom + (sender.Equals(btnZoomIn) ? 5 : -5);
            opts.viewer.ThumbZoom = Utils.EnsureRange(newZoom, 40, 300);
            lbZoom.Content = opts.viewer.ThumbZoom.ToString() + "%";
            UpdatePicItems();            
        }
        private void btnItemUp_MouseDown(object sender, MouseButtonEventArgs e) // Left/Right
        {
            if (Count == 0) return;
            int k = sender.Equals(btnItemUp) ? -1 : 1;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 0, Count-1);
        }
        const int rows4page = 4;
        private void btnHome_MouseDown(object sender, MouseButtonEventArgs e) // PgUp/PgDown  Home/End
        {
            if (Count == 0) return;
            if (sender.Equals(btnHome)) { selectedIndex = 0; scrollToIdx(); return; }
            if (sender.Equals(btnEnd)) { selectedIndex = Count-1; scrollToIdx(); return; }
            int k = sender.Equals(btnPageUp) ? -rows4page : rows4page; k *= thumbsPerRow;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 0, Count-1);
            scrollToIdx();
        }
        private void scrollToIdx(int selectedIdx = -1) // -1 for selectedIndex
        {            
            int si = selectedIdx == -1 ? selectedIndex : selectedIdx; si--;
            if (wrapPics.ActualHeight < rowTumbs.ActualHeight) return;                         
            int targetRow = thumbsPerRow > 0 ? (int)Math.Floor((double)si / thumbsPerRow) : 0; 
            if (targetRow.Equals(0)) { scroller.ScrollToHome(); return; }
            if (targetRow.Equals(rowsCount)) { scroller.ScrollToEnd(); return; }
            if (rowsCount > 0)
                scroller.ScrollToVerticalOffset(wrapPics.ActualHeight * (targetRow - 2) / rowsCount); 
        }
        private int thumbsPerRow { get { if (Count.Equals(0)) return -1; return (int)Math.Floor(wrapPics.ActualWidth / picItems[0].ActualWidth); } }
        private int rowsCount { get { return thumbsPerRow > 0 ? (int)Math.Ceiling((double)Count / thumbsPerRow) : 5; } }
        private void scroller_KeyDown(object sender, KeyEventArgs e)
        {           
            switch (e.Key)
            {
                case Key.Left:
                    btnItemUp_MouseDown(btnItemUp, null);
                    break;
                case Key.Space:
                    if (selectedIndex == -1) return;
                    if (picItems[selectedIndex].checkable) picItems[selectedIndex].IsChecked = !picItems[selectedIndex].IsChecked;
                    else btnItemUp_MouseDown(btnItemDown, null);                    
                    break;
                case Key.Right:
                    btnItemUp_MouseDown(btnItemDown, null);
                    break;
                case Key.Up:
                    selectedIndex = Utils.EnsureRange(selectedIndex - thumbsPerRow, 0, Count-1);
                    break;
                case Key.Down:
                    selectedIndex = Utils.EnsureRange(selectedIndex + thumbsPerRow, 0, Count-1);
                    break;
                case Key.PageUp:
                    btnHome_MouseDown(btnPageUp, null); e.Handled = true;
                    break;
                case Key.PageDown:
                    btnHome_MouseDown(btnPageDown, null); e.Handled = true;
                    break;
                case Key.Home: 
                    btnHome_MouseDown(btnHome, null);
                    break;
                case Key.End:  
                    btnHome_MouseDown(btnEnd, null);
                    break;
            }
        }
        private void gridViewUC_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!IsAvailable || picItems.Count.Equals(0) || selectedIndex.Equals(-1)) return;
            HasTheFocus = true;
        }
        private void gridViewUC_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsAvailable || picItems.Count.Equals(0) || selectedIndex.Equals(-1)) return;
            HasTheFocus = false;
        }

        private void btnTumbOpt_Click(object sender, RoutedEventArgs e)
        {
            if (rowTumbOpt.Height.Value.Equals(1)) rowTumbOpt.Height = new GridLength(30);
            else rowTumbOpt.Height = new GridLength(1);
        }
    }
}

/*
 * using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class MainWindow : Window
{
    private StackPanel stackPanel;
    private List<string> imageUrls = new List<string>()
    {
        "https://example.com/image1.jpg",
        "https://example.com/image2.jpg",
        "https://example.com/image3.jpg",
        "https://example.com/image4.jpg",
        "https://example.com/image5.jpg",
    };

    public MainWindow()
    {
        stackPanel = new StackPanel();
        this.Content = stackPanel;

        // start loading images on multiple threads
        LoadImagesAsync();
    }

    private async void LoadImagesAsync()
    {
        // create tasks to load images
        var tasks = new List<Task<ImageSource>>();
        foreach (var url in imageUrls)
        {
            tasks.Add(Task.Run(() => LoadImage(url)));
        }

        // wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // add loaded images to stack panel
        foreach (var image in results)
        {
            stackPanel.Children.Add(new Image { Source = image });
        }
    }

    private ImageSource LoadImage(string url)
    {
        // load image from URL
        var bitmap = new BitmapImage(new Uri(url));

        // set decode pixel width to improve performance
        bitmap.DecodePixelWidth = 200;

        return bitmap;
    }
}
*/
