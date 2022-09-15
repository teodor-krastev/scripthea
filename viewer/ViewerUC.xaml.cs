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
        void FeedList(List<Tuple<int, string, string>> theList, string imageDepot);  // index, filename, cue     
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
            imageFolder = opts.ImageDepotFolder;
            colListWidth.Width = new GridLength(opts.ViewColWidth);
            foreach (iPicList ipl in views)
                ipl.Init(ref opts);            
        }
        public void Finish()
        {
            opts.ViewColWidth = Convert.ToInt32(colListWidth.Width.Value);
            foreach (iPicList ipl in views)
                ipl.Finish();
        }
        private string _imageFolder;
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = Utils.basePath + "\\images\\";
                return  _imageFolder.EndsWith("\\") ? _imageFolder: _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value;  tbImageDepot.Text = value;
            }
        }
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        
        private bool checkImageDepot(string imageDepot = "")
        {
            string idepot = imageDepot == "" ? imageFolder : imageDepot;
            idepot = idepot.EndsWith("\\") ? idepot : idepot + "\\";
            if (!Directory.Exists(idepot))
            {
                Log("Err: Directory <" + idepot + "> does not exist. "); return false;
            }
            /* if (!File.Exists(idepot + "description.txt")) // probably no need
            {
                Log("Err: File <" + idepot + "description.txt" + "> does not exist."); return false;
            }*/
            return true;
        }
        List<iPicList> views;
        private List<Tuple<int, string, string>> DecompImageDepot(string imageDepot, bool checkFileAndOut)
        {
            if (!checkImageDepot("")) return null;
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
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {            
            if (sender.Equals(btnNewFolder))
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = imageFolder;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    tbImageDepot.Text = dialog.FileName;
                    if (checkImageDepot()) 
                        activeView.FeedList(DecompImageDepot(imageFolder, true), imageFolder);
                }           
                return;
            }  
            else // btnRefresh
            {
                if (!checkImageDepot()) return;
                List <Tuple<int, string, string>> decompImageDepot = DecompImageDepot(imageFolder, true);
                if (!Utils.isNull(decompImageDepot)) activeView.FeedList(decompImageDepot, imageFolder);
            }               
        }
        private void btnFindUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int k = sender.Equals(btnFindUp) ? -1 : 1;
            if (activeView.selectedIndex.Equals(-1)) activeView.selectedIndex = 1;
            int idx = activeView.selectedIndex + k -1;
            List<Tuple<int, string, string>> items = activeView.items;
            while (idx > -1 && idx < items.Count)
            {
                string cue = Convert.ToString(items[idx].Item3);
                if ((cue.IndexOf(tbFind.Text) > -1) || tbFind.Text.Equals(""))
                {
                    activeView.selectedIndex = idx+1; break;
                }
                idx += k;
            }
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnRefresh.IsEnabled = checkImageDepot(tbImageDepot.Text);
        }
    }
}
