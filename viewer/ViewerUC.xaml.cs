using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UtilsNS;

namespace scripthea.viewer
{   
    interface iPicList
    {
        void Init(ref Options _opts);
        void Finish();
        string imageFolder { get; }
        void Clear();
        void FeedList(List<Tuple<int, string, string>> theList, string imageDepot);  // index, filename, prompt     
        int selectedIndex { get; set; } // one based index
        int Count { get; }
        List<Tuple<int, string, string>> items { get; }
    }
    /// <summary>
    /// Interaction logic for ViewerUC.xaml
    /// </summary>
    public partial class ViewerUC : UserControl
    {       
        public ViewerUC()
        {
            InitializeComponent();
            views = new List<iPicList>();
            views.Add(tableViewUC); tableViewUC.SelectEvent += new TableViewUC.PicViewerHandler(picViewerUC.loadPic); 
            views.Add(gridViewUC);  gridViewUC.SelectEvent += new GridViewUC.PicViewerHandler(picViewerUC.loadPic); 
        }
        iPicList activeView { get { return views[tabCtrlViews.SelectedIndex]; } }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            chkAutoRefresh.IsChecked = opts.Autorefresh; imageFolder = opts.ImageDepotFolder; 
            colListWidth.Width = new GridLength(opts.ViewColWidth);
            foreach (iPicList ipl in views)
                ipl.Init(ref opts);            
        }
        public void Finish()
        {
            opts.ViewColWidth = Convert.ToInt32(colListWidth.Width.Value);
            opts.Autorefresh = chkAutoRefresh.IsChecked.Value;
            foreach (iPicList ipl in views)
                ipl.Finish();
        }
        private string _imageFolder;
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = ImgUtils.defaultImageDepot;
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
        public void Clear() 
        {
            activeView.Clear(); picViewerUC.Clear();
        }
        private bool updating = false; private bool showing = false;
        public void ShowImageDepot(string imageDepot)
        {
            if (updating) return;
            updating = true;
            tbImageDepot.Text = imageDepot; 
            tbImageDepot_TextChanged(null, null);
            updating = false; showing = true;
        }
        List<iPicList> views;
        private List<Tuple<int, string, string>> DecompImageDepot(string imageDepot, bool checkFileAndOut)
        {
            if (ImgUtils.checkImageDepot(imageDepot, true) < 1) return null;
            List<Tuple<int, string, string>> lt = new List<Tuple<int, string, string>>();
            List<string> ls = new List<string>(File.ReadAllLines(imageDepot + "description.txt")); int k = 1;
            foreach (string ss in ls)
            {               
                string[] sa = ss.Split('=');
                if (sa.Length != 2) { Log("Err: wrong line format <" + ss + ">. "); return null; }
                if (checkFileAndOut)
                    if (!File.Exists(imageDepot + sa[0])) continue;
                lt.Add(new Tuple<int, string, string>(k, sa[0], sa[1])); k++;
            }
            return lt;
        }
        private void btnFindUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int k = sender.Equals(btnFindUp) ? -1 : 1;
            if (activeView.selectedIndex.Equals(-1)) activeView.selectedIndex = 1;
            int idx = activeView.selectedIndex + k -1;
            List<Tuple<int, string, string>> items = activeView.items;
            while (idx > -1 && idx < items.Count)
            {
                string prompt = Convert.ToString(items[idx].Item3);
                if ((prompt.IndexOf(tbFind.Text) > -1) || tbFind.Text.Equals(""))
                {
                    activeView.selectedIndex = idx+1; break;
                }
                idx += k;
            }
        }
        private bool checkImageDepot(string folder)
        {
            bool bb = ImgUtils.checkImageDepot(tbImageDepot.Text) > 0;
            if (bb) lbDepotInfo.Content = "";
            else lbDepotInfo.Content = "This is not an image depot.";
            return bb;
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        { 
            if (!checkImageDepot(tbImageDepot.Text))                 
            {                
                if (sender.Equals(btnRefresh)) Clear();
                return; 
            }
            List <Tuple<int, string, string>> decompImageDepot = DecompImageDepot(imageFolder, true);
            if (!Utils.isNull(decompImageDepot))
            {
                activeView.FeedList(decompImageDepot, imageFolder); 
            }
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {           
            if (checkImageDepot(tbImageDepot.Text))
            {
                tbImageDepot.Foreground = Brushes.Black; 
                opts.ImageDepotFolder = tbImageDepot.Text; Log("@WorkDir");
                if (chkAutoRefresh.IsChecked.Value) btnRefresh_Click(sender, e);
            }
            else { tbImageDepot.Foreground = Brushes.Red; }     
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value)
            { colRefresh.Width = new GridLength(0); btnRefresh.Visibility = Visibility.Collapsed; btnRefresh_Click(sender, e); }
            else { colRefresh.Width = new GridLength(70); btnRefresh.Visibility = Visibility.Visible; }
        }
        private void tabCtrlViews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value && showing) btnRefresh_Click(sender, e); 
            if (!Utils.isNull(e)) e.Handled = true;
        }
    }
}
