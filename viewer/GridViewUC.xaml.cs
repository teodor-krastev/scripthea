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
        public void Init(ref Options _opts)
        {
            opts = _opts;
            lbZoom.Content = opts.ThumbZoom.ToString() + "%";
            chkShowCue.IsChecked = opts.ThumbCue; chkShowFilename.IsChecked = opts.ThumbFilename;
        }
        public void Finish()
        {

        }

        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }

        private string _imageFolder = "";
        public string imageFolder { get { return _imageFolder; } }
        public void FeedList(List<Tuple<int, string, string>> theList, string imageDepot) 
        {
            if (Utils.isNull(theList)) { Log("Error: not items found."); return; }
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {               
                _imageFolder = imageDepot;
                picItems.Clear();                 
                foreach (var itm in theList)
                {
                    PicItemUC piUC = new PicItemUC(ref opts); picItems.Add(piUC); Utils.DoEvents();
                    piUC.ContentUpdate(itm.Item1, imageFolder + itm.Item2, itm.Item3); piUC.VisualUpdate();
                    piUC.OnSelect += new RoutedEventHandler(SelectTumb); wrapPics.Children.Add(piUC);  
                    if (itm.Item1 == 1)
                    {
                        piUC.selected = true;
                        OnSelect(itm.Item1, piUC.imageFolder + piUC.filename, piUC.prompt);
                    }
                }
                if (picItems.Count > 0) picItems[0].selected = true;
            }
            finally { Mouse.OverrideCursor = null; }
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
        public List<Tuple<int, string, string>> items
        {
            get
            {
                List<Tuple<int, string, string>> itm = new List<Tuple<int, string, string>>();
                foreach (PicItemUC piUC in picItems)               
                    itm.Add(new Tuple<int, string, string>(piUC.idx, piUC.filename, piUC.prompt));
                return itm;
            }
        }
        
        public delegate void PicViewerHandler(int idx, string filePath, string prompt);
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, string filePath, string prompt)
        {
            if (SelectEvent != null) SelectEvent(idx, filePath, prompt);
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
            int k = sender.Equals(btnPageUp) ? -20 : 20;
            selectedIndex = Utils.EnsureRange(selectedIndex + k, 1, Count);
            if (wrapPics.ActualHeight < rowTumbs.ActualHeight) return;
            double selectedPos = (double)selectedIndex / Count; 
            scroller.ScrollToVerticalOffset(selectedPos * wrapPics.ActualHeight - rowTumbs.ActualHeight/3);
        }

        private void scroller_KeyDown(object sender, KeyEventArgs e)
        {           
            switch (e.Key)
            {
                case (Key.Home): btnHome_MouseDown(btnHome, null);
                    break;
                case (Key.End):  btnHome_MouseDown(btnEnd, null);
                    break;
            }
        }
    }
}
