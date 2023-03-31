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
        public bool checkable { get; private set; }
        public void Init(ref Options _opts, bool _checkable)
        {
            opts = _opts; checkable = _checkable;
            lbZoom.Content = opts.ThumbZoom.ToString() + "%";
            chkShowCue.IsChecked = opts.ThumbCue; chkShowFilename.IsChecked = opts.ThumbFilename;
        }
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
                {
                    piUC.focused = value;
                }              
            }
        }
        public string loadedDepot { get; set; }

        protected ImageInfo DepotItem(int idx) // idx -> iDepot index
        {
            if (!IsAvailable) return null;            
            if (Utils.InRange(idx, 0, iDepot.items.Count - 1))
               return iDepot.items[idx];
            return null;
        }
        protected PicItemUC SelectedPicItem() // idx from inside PicItem
        {
            if (!IsAvailable) return null;
            foreach (PicItemUC ps in picItems)
                if (ps.selected) return ps;
            return null;
        }
        public void UpdateVis()
        {
            try
            {                
                Mouse.OverrideCursor = Cursors.Wait; Utils.DoEvents();
                if (!IsAvailable) Clear(); 
                picItemsClear(); int cnt = iDepot.items.Count; 
                foreach (var itm in iDepot.Export2Viewer())
                {
                    if (ShuttingDown) return;
                    PicItemUC piUC = new PicItemUC(ref opts, checkable); piUC.IsChecked = true;
                    picItems.Add(piUC); labelNum.Content = (int)(100.0 * picItems.Count / cnt) + "%";
                    int k = Utils.InRange(thumbsPerRow, 2,20) ? thumbsPerRow : 4;
                    if ((picItems.Count % k) == 1) 
                    {
                        if (scroller.VerticalOffset > 0) scroller.ScrollToHome(); 
                        Utils.DoEvents(); 
                    }
                    if (!piUC.ContentUpdate(itm.Item1, Path.Combine(imageFolder, itm.Item2), itm.Item3))
                        { Log("Exhausted resources - use table view instead", Brushes.Red); OutOfResources = true; break; }
                    piUC.VisualUpdate();
                    piUC.OnSelect += new RoutedEventHandler(SelectTumb); wrapPics.Children.Add(piUC);
                    if (checkable)
                    {
                        piUC.chkChecked.Checked += new RoutedEventHandler(ChangeContent); piUC.chkChecked.Unchecked += new RoutedEventHandler(ChangeContent);
                    }
                    if (itm.Item1 == 1)
                    {
                        piUC.selected = true;
                        OnSelect(itm.Item1, piUC.imageFolder + piUC.filename, piUC.prompt);
                    }
                }
                if (picItems.Count > 0) picItems[0].selected = true;
                ChangeContent(this, null); 
            }
            finally 
            { Mouse.OverrideCursor = null; labelNum.Content = ""; selectedIndex = 1; scrollToIdx(); }
        }
        public void SynchroChecked(List<Tuple<int, string, string>> chks)
        {
            if (!checkable) return;
            SetChecked(false);
            foreach (Tuple<int, string, string> chk in chks)
                picItems[chk.Item1 - 1].IsChecked = true;
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
        public void RemoveAt(int idx = -1) // default selected
        {            
            int j = idx.Equals(-1) ? selectedIndex - 1 : idx;
            if (Utils.InRange(j, 0, picItems.Count - 1))
            {
                wrapPics.Children.Remove(picItems[j]);
                picItems[j] = null;
                picItems.RemoveAt(j);
            }    
            GC.Collect(); wrapPics.UpdateLayout();
            // renumber picItems
            for (int i = 0; i < picItems.Count; i++)
            {
                picItems[i].idx = i + 1; 
            }           
            selectedIndex = Utils.EnsureRange(j, 0, picItems.Count - 1) + 1;               
        }
        private void picItemsClear()
        {
            for(int i = 0; i < picItems.Count; i++)
            {
                picItems[i].Clear();
            }            
            picItems.Clear(); GC.Collect(); wrapPics.Children.Clear(); wrapPics.UpdateLayout();
        }
        public string imageFolder { get { return iDepot.path; } }
        public void Mark(string mask)
        {
            foreach (PicItemUC piUC in picItems)
                piUC.marked = mask.Equals("") ? false : Utils.IsWildCardMatch(piUC.prompt, mask);
        }
        public void Clear(bool inclDepotItems = false)
        {
            picItemsClear(); 
            if (inclDepotItems) iDepot?.items.Clear();                       
        }
        public bool FeedList(string imageDepot) 
        {
            Clear();
            if (!Directory.Exists(imageDepot)) { Log("Err: no such folder -> " + imageDepot); return false; }
            //if (ImgUtils.checkImageDepot(imageDepot) == 0) { Log("Err: not image depot folder -> " + imageDepot); return false; }
            ImageDepot _iDepot = new ImageDepot(imageDepot, ImageInfo.ImageGenerator.FromDescFile);
            return FeedList(ref _iDepot);
        }
        public bool FeedList(ref ImageDepot _iDepot) // external iDepot; regular use
        {
            if (_iDepot == null) return false;
            if (!Directory.Exists(_iDepot.path)) { Log("Err: no such folder -> " + _iDepot.path); return false; }
            iDepot = _iDepot; loadedDepot = iDepot.path;
            UpdateVis();
            return true;
        }
        public int selectedIndex // 1 based index
        {
            get 
            {
                foreach (PicItemUC piUC in picItems)                
                    if (piUC.selected) return piUC.idx;                    
                return -1; 
            }
            set
            {
                if (!Utils.InRange(value, 1, Count) || Count == 0) return;
                PicItemUC piUC2 = null;
                foreach (PicItemUC piUC in picItems) // reset all
                {
                    piUC.selected = piUC.idx.Equals(value);
                    if (piUC.selected) { piUC.focused = true; piUC2 = picItems[value - 1]; }
                }                                                                          
                scrollToIdx(value);
                if (piUC2 != null)
                    OnSelect(piUC2.idx, piUC2.imageFolder + piUC2.filename, piUC2.prompt);
            }
        }
        public int Count { get { return picItems.Count; } }
        public List<Tuple<int, string, string>> GetItems(bool check, bool uncheck)
        {
            if (!checkable && iDepot.isEnabled) { return iDepot.Export2Viewer(); }
            List<Tuple<int, string, string>> itms = new List<Tuple<int, string, string>>();
            foreach (PicItemUC piUC in picItems)
            {
                if (piUC.IsChecked == null) continue;
                if ((bool)piUC.IsChecked)
                {
                    if (check) itms.Add(new Tuple<int, string, string>(piUC.idx, piUC.filename, piUC.prompt));
                }
                else
                {
                    if (uncheck) itms.Add(new Tuple<int, string, string>(piUC.idx, piUC.filename, piUC.prompt));
                }
            }
            return itms;            
        }
        
        public delegate void PicViewerHandler(int idx, string filePath, string prompt);
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, string filePath, string prompt)
        {
            if (SelectEvent != null) SelectEvent(idx, filePath, prompt);
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
            string fn = (sender as PicItemUC).filename; int k = 1;
            foreach (PicItemUC piUC in picItems)
            {
                if (piUC.Equals(sender))
                {
                    piUC.selected = true;
                    OnSelect(piUC.idx, piUC.imageFolder + piUC.filename, piUC.prompt);
                }
                k++;
            }
        }
        private void ibtnTumbOpt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (rowTumbOpt.Height.Value.Equals(1)) rowTumbOpt.Height = new GridLength(30);
            else rowTumbOpt.Height = new GridLength(1);
        }
        private void UpdatePicItems()
        {
            if (Count == 0) return;
            foreach (PicItemUC piUC in picItems) piUC.VisualUpdate();
        } 
        private void chkShowCue_Checked(object sender, RoutedEventArgs e)
        {
            opts.ThumbCue = chkShowCue.IsChecked.Value; opts.ThumbFilename = chkShowFilename.IsChecked.Value;
            UpdatePicItems();
        }
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            int newZoom = opts.ThumbZoom + (sender.Equals(btnZoomIn) ? 5 : -5);
            opts.ThumbZoom = Utils.EnsureRange(newZoom, 40, 300);
            lbZoom.Content = opts.ThumbZoom.ToString() + "%";
            UpdatePicItems();            
        }
        private void btnItemUp_MouseDown(object sender, MouseButtonEventArgs e) // Left/Right
        {
            if (Count == 0) return;
            int k = sender.Equals(btnItemUp) ? -1 : 1;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 1, Count);
        }
        const int rows4page = 4;
        private void btnHome_MouseDown(object sender, MouseButtonEventArgs e) // PgUp/PgDown  Home/End
        {
            if (Count == 0) return;
            if (sender.Equals(btnHome)) { selectedIndex = 1; scrollToIdx(); return; }
            if (sender.Equals(btnEnd)) { selectedIndex = Count; scrollToIdx(); return; }
            int k = sender.Equals(btnPageUp) ? -rows4page : rows4page; k *= thumbsPerRow;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 1, Count);
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
                    if (selectedIndex > -1)
                    {
                        if (picItems[selectedIndex-1].checkable) picItems[selectedIndex-1].IsChecked = !picItems[selectedIndex-1].IsChecked;
                        else btnItemUp_MouseDown(btnItemDown, null);
                    }
                    break;
                case Key.Right:
                    btnItemUp_MouseDown(btnItemDown, null);
                    break;
                case Key.Up:
                    selectedIndex = Utils.EnsureRange(selectedIndex - thumbsPerRow, 1, Count);
                    break;
                case Key.Down:
                    selectedIndex = Utils.EnsureRange(selectedIndex + thumbsPerRow, 1, Count);
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
