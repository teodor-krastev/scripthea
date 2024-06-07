using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;
using Path = System.IO.Path;
using Newtonsoft.Json;
using Brushes = System.Windows.Media.Brushes;
using ExifLib;
using System.Windows.Media.Animation;
using Image = System.Windows.Controls.Image;
using scripthea.viewer;
using scripthea.options;
using UtilsNS;

namespace scripthea.master
{
    public static class SctUtils
    {
        public const string descriptionFile = "DESCRIPTION.idf";
        public static string defaultImageDepot
        { get { return System.IO.Path.Combine(Utils.basePath, "images"); } }
        public static ImageInfo.ImageGenerator DefaultImageGenerator = ImageInfo.ImageGenerator.StableDiffusion;

        public static (int, int) rangeMask(string mask, int count) // range 1..count
        {
            int i0 = -1; int i1 = -1;
            if (mask.StartsWith("[") && mask.EndsWith("]"))
            {
                string msk = mask.TrimStart('[').TrimEnd(']');
                int j;
                if (int.TryParse(msk, out j))
                {
                    if (j == 0) return (-1, -1);
                    if (j > 0) return (1, j);
                    else return (count + j, count);
                }
                int ip = msk.IndexOf(".."); if (ip == -1) { Utils.TimedMessageBox("Error[574]: Wrong range syntax, it must be [num..num] ."); return (-1, -1); }
                string ma = string.Empty; string mb = string.Empty;

                if (msk.StartsWith("..")) ma = "1";
                else ma = msk.Substring(0, ip);

                if (msk.EndsWith("..")) mb = count.ToString();
                else mb = msk.Substring(ip + 2);

                if (!(int.TryParse(ma, out i0) && int.TryParse(mb, out i1))) // activeView.CheckRange(i0, i1);
                { Utils.TimedMessageBox("Error[575]: Wrong range syntax, it must be [num..num] ."); return (-1, -1); }
                if (i0 > i1) (i0, i1) = (i1, i0);
            }
            return (i0, i1);
        }

        public static BitmapImage file_not_found { get { return ImgUtils.UnhookedImageLoad(Path.Combine(Utils.configPath, "file_not_found.jpg"), ImgUtils.ImageType.Jpg); } }
        public static int checkImageDepot(string imageDepot, bool checkDesc = true) // return -1 if no access (does not exist)
        {
            string idepot = imageDepot.EndsWith("\\") ? imageDepot : imageDepot + "\\";
            if (!Directory.Exists(idepot)) return -1;
            try
            {
                DirectoryInfo di = new DirectoryInfo(idepot);
                if ((di.Attributes & FileAttributes.System) == FileAttributes.System) return -1; // system dir            
            }
            catch { /*Utils.TimedMessageBox("no access to " + idepot);*/ return -1; }
            try
            {
                if (checkDesc)
                {
                    string fn = Path.Combine(idepot, descriptionFile);
                    if (!File.Exists(fn)) return 0; // no desc file                   
                    List<string> ls = Utils.readList(fn, true);
                    if (ls == null) return -1;
                    return ls.Count;
                }
                else
                {
                    string[] imgPng = Directory.GetFiles(imageDepot, "*.png"); string[] imgJpg = Directory.GetFiles(imageDepot, "*.jpg");
                    return imgPng.Length + imgJpg.Length;
                }
            }
            catch { /* Utils.TimedMessageBox("problem with " + idepot); */ return -1; }
        }
        /// <summary>

    }
    /// <summary>
    /// display folders and subfolders in a treeview wpf c#
    /// </summary>
    public partial class DirTreeUC : UserControl
    {
        public DirTreeUC()
        {
            InitializeComponent();
        }
        private string AppDataStr = "<AppData>";
        public string AppData
        {
            get
            {
                if (Directory.Exists(Utils.basePath)) return Utils.basePath.EndsWith("\\") ? Utils.basePath : Utils.basePath + "\\";
                else Utils.TimedMessageBox("No directory: " + Utils.basePath);
                return "";
            }
        }
        protected Options opts;
        public List<string> history; string historyFile;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            List<string> ld = new List<string>(Directory.GetLogicalDrives());
            historyFile = Path.Combine(Utils.configPath, "history.lst");
            if (File.Exists(historyFile))
            {
                List<string> ls = Utils.readList(historyFile,true); history = new List<string>();
                foreach (string ss in ls)
                    if (Directory.Exists(ss)) history.Add(ss);
            }
            else history = new List<string>();
            ld.Insert(0, AppDataStr);
            foreach (string s in ld)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = s;
                item.Tag = s;
                cbDrives.Items.Add(item);
                cbDrives.SelectedIndex = 1;
            }
            if (!opts.general.debug)
            {
                tbSelected.Visibility = Visibility.Collapsed; tvFolders.Margin = new Thickness(0);
            }
        }
        public void Finish()
        {
            if (history.Count > 0) Utils.writeList(historyFile, history);
        }

        public delegate void SelectHandler(string path);
        public event SelectHandler OnSelect;
        protected void Select(string path)
        {
            if (OnSelect != null) OnSelect(path);
        }
        public event SelectHandler OnActive;
        protected void Active(string path)
        {
            if (OnActive != null) OnActive(path);
            if (history.Count > 0)
            {
                if (Utils.comparePaths(path, history[0])) return;
            }
            history.Insert(0, path);
            while (history.Count > 12) history.RemoveAt(history.Count - 1);
        }
        protected TreeViewItem dummyNode = null;
        protected void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);  // the name of the folder
                        subitem.Tag = s;                                        // the path to the folder
                        subitem.FontWeight = FontWeights.Normal; subitem.FontSize = tvFolders.FontSize;
                        
                        int checkImageDepot = SctUtils.checkImageDepot(s, false); if (checkImageDepot < 0) continue;                       
                        if (checkImageDepot > 0)
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Brushes.Blue; subitem.ToolTip = "#"+ checkImageDepot.ToString();
                        }
                        checkImageDepot = SctUtils.checkImageDepot(s, true); if (checkImageDepot < 0) continue;
                        if (checkImageDepot > 0)
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Utils.ToSolidColorBrush("#FF02CB02"); // greenish
                            subitem.ToolTip = "idf:" + checkImageDepot.ToString();                                                          
                        }
                        bool bc = true; ;
                        try
                        {
                            bc = Directory.GetDirectories(s).Length > 0;
                        }
                        catch (Exception ex) { Log("I: "+ex.Message); continue; }                        
                        if (bc) subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception ey) { Log("II: "+ey.Message); }
            }
        }
        public void refreshTree() { comboBox_SelectionChanged(null, null); }
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string s = (cbDrives.SelectedItem as ComboBoxItem).Content.ToString();

            if (s.Equals(AppDataStr)) s = AppData;
            TreeViewItem item = new TreeViewItem();
            item.Header = s;
            item.Tag = s;
            item.FontWeight = FontWeights.Normal;
            item.Items.Add(dummyNode);
            item.Expanded += new RoutedEventHandler(folder_Expanded);
            tvFolders.Items.Clear();
            tvFolders.Items.Add(item);
            item.IsExpanded = true;
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void tvFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = (TreeViewItem)e.NewValue;
            if (Utils.isNull(item)) return;
            string pth = item.Header.ToString();
            while (true)
            {
                if (!(item.Parent is TreeViewItem)) break;
                TreeViewItem prn = (TreeViewItem)item.Parent;
                pth = Path.Combine(prn.Header.ToString(), pth);
                item = prn;
            }
            Select(pth);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null && opts.general.debug) OnLog(txt, clr);
        }
        public bool CatchAFolder(string pth)
        {
            bool bb = false;
            if (pth == null) return bb;
            if (!Directory.Exists(pth)) return bb;
            List<string> fld = new List<string>(pth.Split('\\'));
            ComboBoxItem cbf = null;
            foreach (ComboBoxItem cbi in cbDrives.Items)
                if (cbi.Content.ToString().Equals(fld[0] + "\\")) { cbf = cbi; break; }
            if (cbf.Equals(null)) Log("Error[397]: no drive: " + fld[0] + "\\");
            if (pth.Length > AppData.Length)
                if (pth.Substring(0, AppData.Length).Equals(AppData, StringComparison.InvariantCultureIgnoreCase))
                    cbf = cbDrives.Items[0] as ComboBoxItem;
            cbDrives.SelectedItem = cbf; // creates root
            TreeViewItem prn = (TreeViewItem)tvFolders.Items[0];
            string itm0 = Convert.ToString(prn.Header);
            fld = new List<string>(pth.Substring(itm0.Length).Split('\\'));
            for (int i = 0; i < fld.Count; i++)
            {
                if (fld[i].Equals("")) continue;
                bb = false;
                foreach (TreeViewItem item in prn.Items)
                {
                    if (Convert.ToString(item.Header).Equals(fld[i]))
                    {
                        item.IsExpanded = true; prn = item; bb = true; break;
                    }
                }
                if (!bb) Log("Error[395]: no folder found: " + fld[i]);
            }
            if (bb) prn.IsSelected = true;
            return bb;
        }
        private void tvFolders_KeyDown(object sender, KeyEventArgs e)
        {
            if (!tvFolders.SelectedItem.Equals(null) && e.Key.Equals(Key.Enter))
            {
                string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
                Active(pth); tbSelected.Text = pth;
            }
        }
        public string selectedPath { 
            get 
            {
                if (tvFolders.SelectedItem == null) return "";
                else return (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
            } 
        }
        private void tvFolders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedPath != "")
            {
                string pth = selectedPath; Active(pth); tbSelected.Text = pth;
            }
        }
        private void tvFolders_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            cmFolders.Items.Clear();
            MenuItem mi = new MenuItem(); mi.Header = "New folder (subfolder to the selected)"; mi.Click += mi_Click; cmFolders.Items.Add(mi);
            MenuItem mj = new MenuItem(); mj.Header = "Rename folder"; mj.Click += mi_Click; cmFolders.Items.Add(mj);
            cmFolders.Items.Add(new Separator());
            MenuItem mk = new MenuItem(); mk.Header = "Copy folder path to clipbrd"; mk.Click += mi_Click; cmFolders.Items.Add(mk);
        }
        void mi_Click(object sender, RoutedEventArgs e)
        {
            if (tvFolders.SelectedItem.Equals(null)) return;
            string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString(); string prn = pth.EndsWith("\\") ? pth : pth + "\\"; // Directory.GetParent(pth).FullName; 
            int maxLen = 30;
            string input = ""; string dir = ""; string prn1 = "";
            switch (Convert.ToString((sender as MenuItem).Header))
            {
                case "New folder (subfolder to the selected)":
                    prn1 = prn.Length > maxLen ? "..." + prn.Substring(prn.Length - maxLen) : prn;
                    input = new InputBox("New folder in " + prn1, "", "").ShowDialog();
                    if (input.Equals("")) return;
                    dir = Path.Combine(prn, input);
                    if (Directory.Exists(dir)) Utils.TimedMessageBox("The folder \"" + dir + "\" already exists", "Error message", 3000);
                    else
                    {
                        Directory.CreateDirectory(dir); refreshTree(); CatchAFolder(prn);
                    }
                    break;
                case "Rename folder":
                    string lastDir = Path.GetFileName(pth); string parentDir = Path.GetDirectoryName(pth);
                    prn1 = parentDir.Length > maxLen ? "..." + parentDir.Substring(prn.Length - maxLen) : parentDir;
                    input = new InputBox("Rename <" + lastDir + "> in " + prn1, lastDir, "").ShowDialog();
                    if (input.Equals("")) return;
                    dir = Path.Combine(parentDir, input);
                    if (Directory.Exists(dir)) { Utils.TimedMessageBox("The folder \"" + dir + "\" already exists", "Error message", 3000); return; }
                    else
                    {
                        Directory.Move(pth, dir); refreshTree(); CatchAFolder(dir);
                    }
                    break;
                case "Copy folder path to clipbrd":
                    System.Windows.Clipboard.SetText(pth + "\\");
                    break;
                default: Utils.TimedMessageBox("internal error #951");
                    break;
            }
        }
        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            cmHistory.Items.Clear();
            for (int i = 0; i < history.Count; i++)
            {
                MenuItem hmi = new MenuItem() { Name = "hmi" + i.ToString(), Header = history[i] };
                hmi.Click += hmi_Click;
                cmHistory.Items.Add(hmi);
            }
            cmHistory.IsOpen = true;
        }
        void hmi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem hmi = sender as MenuItem; string header = Convert.ToString(hmi.Header);
            CatchAFolder(header); Active(header);
        }
    }


    /*I post a solution extending the image control and using the Gif Decoder. The gif decoder has a frames property. I animate the FrameIndex property. 
     * The event ChangingFrameIndex changes the source property to the frame corresponding to the FrameIndex (that is in the decoder). I guess that the gif 
     * has 10 frames per second.*/

    public class GifImage : Image
    {
        private bool _isInitialized;
        private GifBitmapDecoder _gifDecoder;
        private Int32Animation _animation;
        public int FrameIndex
        {
            get { return (int)GetValue(FrameIndexProperty); }
            set { SetValue(FrameIndexProperty, value); }
        }
        private void Initialize()
        {
            _gifDecoder = new GifBitmapDecoder(new Uri("pack://application:,,," + this.GifSource), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            _animation = new Int32Animation(0, _gifDecoder.Frames.Count - 1, new Duration(new TimeSpan(0, 0, 0, _gifDecoder.Frames.Count / 10, (int)((_gifDecoder.Frames.Count / 10.0 - _gifDecoder.Frames.Count / 10) * 1000))));
            _animation.RepeatBehavior = RepeatBehavior.Forever;
            this.Source = _gifDecoder.Frames[0];

            _isInitialized = true;
        }
        public GifImage()
        {
            VisibilityProperty.OverrideMetadata(typeof(GifImage),
                new FrameworkPropertyMetadata(VisibilityPropertyChanged));
        }
        private void VisibilityPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if ((Visibility)e.NewValue == Visibility.Visible)
            {
                ((GifImage)sender).StartAnimation();
            }
            else
            {
                ((GifImage)sender).StopAnimation();
            }
        }

        public readonly DependencyProperty FrameIndexProperty =
            DependencyProperty.Register("FrameIndex", typeof(int), typeof(GifImage), new UIPropertyMetadata(0, new PropertyChangedCallback(ChangingFrameIndex)));

        static void ChangingFrameIndex(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            var gifImage = obj as GifImage;
            gifImage.Source = gifImage._gifDecoder.Frames[(int)ev.NewValue];
        }

        /// <summary>
        /// Defines whether the animation starts on it's own
        /// </summary>
        public bool AutoStart
        {
            get { return (bool)GetValue(AutoStartProperty); }
            set { SetValue(AutoStartProperty, value); }
        }

        public readonly DependencyProperty AutoStartProperty =
            DependencyProperty.Register("AutoStart", typeof(bool), typeof(GifImage), new UIPropertyMetadata(false, AutoStartPropertyChanged));

        private static void AutoStartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                (sender as GifImage).StartAnimation();
        }

        public string GifSource
        {
            get { return (string)GetValue(GifSourceProperty); }
            set { SetValue(GifSourceProperty, value); }
        }

        public readonly DependencyProperty GifSourceProperty =
            DependencyProperty.Register("GifSource", typeof(string), typeof(GifImage), new UIPropertyMetadata(string.Empty, GifSourcePropertyChanged));

        private static void GifSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as GifImage).Initialize();
        }

        /// <summary>
        /// Starts the animation
        /// </summary>
        public void StartAnimation()
        {
            if (!_isInitialized)
                this.Initialize();

            BeginAnimation(FrameIndexProperty, _animation);
        }

        /// <summary>
        /// Stops the animation
        /// </summary>
        public void StopAnimation()
        {
            BeginAnimation(FrameIndexProperty, null);
        }
    }
}
/*Usage example (XAML):

<controls:GifImage x:Name="gifImage" Stretch="None" GifSource="/SomeImage.gif" AutoStart="True" />
*/