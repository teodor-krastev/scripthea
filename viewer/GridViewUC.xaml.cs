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
        List<PicItemUC> picItems;
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
        public void Finish()
        {

        }
        public DepotFolder iDepot { get; set; }
        public string loadedDepot { get; set; }

        public void UpdateVis()
        {
            try
            {                
                Mouse.OverrideCursor = Cursors.Wait;
                picItemsClear();
                foreach (var itm in iDepot.Export2Viewer())
                {
                    PicItemUC piUC = new PicItemUC(ref opts, checkable); piUC.IsChecked = true;
                    picItems.Add(piUC); Utils.DoEvents();
                    piUC.ContentUpdate(itm.Item1, Path.Combine(imageFolder, itm.Item2), itm.Item3); piUC.VisualUpdate();
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
            finally { Mouse.OverrideCursor = null; }

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
        public void picItemsClear()
        {
            for(int i = 0; i < picItems.Count; i++)
            {
                picItems[i] = null;
            }            
            picItems.Clear(); wrapPics.Children.Clear(); wrapPics.UpdateLayout();
        }
        public string imageFolder { get { return iDepot.path; } }
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
            DepotFolder _iDepot = new DepotFolder(imageDepot, ImageInfo.ImageGenerator.FromDescFile);
            return FeedList(ref _iDepot);
        }
        public bool FeedList(ref DepotFolder _iDepot) // external iDepot; regular use
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
                foreach (PicItemUC piUC in picItems) // reset all            
                    if (piUC.selected) piUC.selected = false;
                PicItemUC piUC2 = picItems[value - 1];
                piUC2.selected = true;
                OnSelect(piUC2.idx, piUC2.imageFolder + piUC2.filename, piUC2.prompt);
            }
        }
        public int Count { get { return picItems.Count; } }
        public List<Tuple<int, string, string>> GetItems(bool check, bool uncheck)
        {
            List<Tuple<int, string, string>> itms = new List<Tuple<int, string, string>>();
            if (!checkable) return itms;
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
        private void btnItemUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Count == 0) return;
            int k = sender.Equals(btnItemUp) ? -1 : 1;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 1, Count);
        }
        private void btnHome_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Count == 0) return;
            if (sender.Equals(btnHome)) { selectedIndex = 1; scroller.ScrollToHome(); return; }
            if (sender.Equals(btnEnd)) { selectedIndex = Count; scroller.ScrollToEnd(); return; }
            int k = sender.Equals(btnPageUp) ? -5 : 5; k *= thumbsPerRow;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 1, Count);
            if (wrapPics.ActualHeight < rowTumbs.ActualHeight) return;           
            double selectedPos = (double)selectedIndex / Count; 
            scroller.ScrollToVerticalOffset(selectedPos * wrapPics.ActualHeight - rowTumbs.ActualHeight/3);
        }
        private int thumbsPerRow { get { return (int)Math.Floor(wrapPics.ActualWidth / picItems[0].ActualWidth); } }
        private int RowsCount { get { return (int)Math.Ceiling((double)Count / thumbsPerRow); } }
        private void scroller_KeyDown(object sender, KeyEventArgs e)
        {           
            switch (e.Key)
            {
                case Key.Left:
                    btnItemUp_MouseDown(btnItemUp, null);
                    break;
                case Key.Space:
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

    }
}
