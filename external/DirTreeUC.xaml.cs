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
using UtilsNS;
using System.Drawing;
using System.Drawing.Imaging;
using Brushes = System.Windows.Media.Brushes;

// https://stackoverflow.com/questions/6415037/populate-treeview-from-list-of-file-paths-in-wpf

// display folders and subfolders in a treeview wpf c#

namespace UtilsNS
{
    public static class ImgUtils
    {    
        public static string defaultImageDepot
        { get { return System.IO.Path.Combine(Utils.basePath, "images"); } }

        public static bool checkImageDepot(string imageDepot, bool checkDesc = true)
        {
            string idepot = imageDepot.EndsWith("\\") ? imageDepot : imageDepot + "\\";
            if (!Directory.Exists(idepot)) return false;
            if (!File.Exists(Path.Combine(idepot, "description.txt")) && checkDesc) return false;
            return true;
        }
        public static BitmapImage ToBitmapImage(Bitmap bitmap, ImageFormat imageFormat)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, imageFormat);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
        public static void VisualCompToPng(UIElement element, string filename = "")
        {
            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(element), null, rect);
            }
            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var file = File.OpenWrite(filename))
                {
                    encoder.Save(file);
                }
            }
        }
    }
    /// <summary>
    /// Interaction logic for DirTreeUC.xaml
    /// </summary>
    public partial class DirTreeUC : UserControl
    {
        public DirTreeUC()
        {
            InitializeComponent();
        }
        string AppDataStr = "<AppData>";
        public string AppData
        {
            get { return Utils.basePath; }
        }
        public void Init()
        {
            List<string> ld = new List<string>(Directory.GetLogicalDrives());
            ld.Insert(0, AppDataStr);
            foreach (string s in ld)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = s;
                item.Tag = s;
                cbDrives.Items.Add(item);
                cbDrives.SelectedIndex = 1;
            }
            if (!Utils.isInVisualStudio)
            {
                tbSelected.Visibility = Visibility.Collapsed; tvFolders.Margin = new Thickness(0);
            }
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
        }

        TreeViewItem dummyNode = null;
        void folder_Expanded(object sender, RoutedEventArgs e)
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
                        subitem.FontWeight = FontWeights.Normal;
                        if (ImgUtils.checkImageDepot(s, true))
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Brushes.LimeGreen; //Coral; // OrangeRed;
                        }
                        else
                        {
                            subitem.FontSize = tvFolders.FontSize;
                        }
                        bool bb = true; bool bc = true; ;
                        try
                        {
                            bc = Directory.GetDirectories(s).Length > 0;
                        }
                        catch { bb = false; }
                        if (!bb) continue;
                        if (bc) subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception ex) { Log(ex.Message); }
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
        public void Log(string msg)
        {
            Utils.TimedMessageBox(msg);
        }
        public bool CatchAFolder(string pth)
        {
            bool bb = false;
            if (!Directory.Exists(pth)) return bb;
            List<string> fld = new List<string>(pth.Split('\\'));
            ComboBoxItem cbf = null;
            foreach (ComboBoxItem cbi in cbDrives.Items)
                if (cbi.Content.ToString().Equals(fld[0] + "\\")) { cbf = cbi; break; }
            if (cbf.Equals(null)) Log("Error: no drive: " + fld[0] + "\\");
            if (pth.Substring(0, AppData.Length).Equals(AppData, StringComparison.OrdinalIgnoreCase))
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
                if (!bb) Log("Error: no folder found: " + fld[i]);
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
        private void tvFolders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!tvFolders.SelectedItem.Equals(null))
            {
                string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
                Active(pth); tbSelected.Text = pth;
            }
        }
        private void tvFolders_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            cmFolders.Items.Clear();
            MenuItem mi = new MenuItem(); mi.Header = "New folder"; mi.Click += mi_Click; cmFolders.Items.Add(mi);
            MenuItem mj = new MenuItem(); mj.Header = "Rename"; mj.Click += mi_Click; cmFolders.Items.Add(mj);
        }
        void mi_Click(object sender, RoutedEventArgs e)
        {
            if (tvFolders.SelectedItem.Equals(null)) return;
            string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
            string prn = Directory.GetParent(pth).FullName; int maxLen = 30;
            string input = ""; string dir = ""; string prn1 = "";
            switch (Convert.ToString((sender as MenuItem).Header))
            {
                case "New folder":                   
                    prn1 = prn.Length > maxLen ? "..."+prn.Substring(prn.Length - maxLen) : prn;
                    input = new InputBox("New folder in " + prn1, "", "").ShowDialog();
                    if (input.Equals("")) return;
                    dir = Path.Combine(prn, input);
                    if (Directory.Exists(dir)) Utils.TimedMessageBox("The folder \"" + dir + "\" already exists","Error message",3000);
                    else 
                    {
                        Directory.CreateDirectory(dir); refreshTree(); CatchAFolder(prn);
                    }
                    break;
                case "Rename":
                    string lastDir = Path.GetFileName(pth);
                    prn1 = prn.Length > maxLen ? "..." + prn.Substring(prn.Length - maxLen) : prn;
                    input = new InputBox("Rename <"+ lastDir + "> in " + prn1, lastDir, "").ShowDialog();
                    if (input.Equals("")) return;
                    dir = Path.Combine(prn, input);
                    if (Directory.Exists(dir)) Utils.TimedMessageBox("The folder \"" + dir + "\" already exists", "Error message", 3000);
                    else
                    {
                        Directory.Move(pth,dir); refreshTree(); CatchAFolder(prn);
                    }
                    break;
            }
        }
    }    
}
