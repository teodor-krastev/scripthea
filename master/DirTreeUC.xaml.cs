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
using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using Brushes = System.Windows.Media.Brushes;
using UtilsNS;
using System.Windows.Media.Animation;
using Image = System.Windows.Controls.Image;
using Color = System.Drawing.Color;
using scripthea.viewer;

namespace scripthea.master
{
    public static class ImgUtils
    {
        public const string descriptionFile = "DESCRIPTION.idf";
        public static string defaultImageDepot
        { get { return System.IO.Path.Combine(Utils.basePath, "images"); } }
        public static ImageInfo.ImageGenerator DefaultImageGenerator = ImageInfo.ImageGenerator.StableDiffusion;

        public static BitmapImage file_not_found { get { return UnhookedImageLoad(Utils.basePath + "\\Properties\\file_not_found.jpg", ImageFormat.Jpeg); } }

        public static int checkImageDepot(string imageDepot, bool checkDesc = true)
        {
            string idepot = imageDepot.EndsWith("\\") ? imageDepot : imageDepot + "\\";

            DirectoryInfo di = new DirectoryInfo(idepot);
            if ((di.Attributes & FileAttributes.System) == FileAttributes.System) return -1; // system dir
            if (!Directory.Exists(idepot)) return -1;
            if (checkDesc)
            {
                if (!File.Exists(Path.Combine(idepot, descriptionFile)) && checkDesc) return -1; // no desc file
                List<string> ls = new List<string>(File.ReadAllLines(Path.Combine(idepot, descriptionFile)));
                return ls.Count - 1;
            }
            else
            {
                string[] imgPng = Directory.GetFiles(imageDepot, "*.png"); string[] imgJpg = Directory.GetFiles(imageDepot, "*.jpg");
                return imgPng.Length + imgJpg.Length;
            }
        }

        public static ImageFormat GetImageFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg": return ImageFormat.Jpeg;
                case ".png": return ImageFormat.Png;
                case ".gif": return ImageFormat.Gif;
                case ".bmp": return ImageFormat.Bmp;
                default: return null;
            }
        }
        public static string GetImageExt(ImageFormat iFormat)
        {
            if (iFormat == ImageFormat.Jpeg) return ".jpg";
            if (iFormat == ImageFormat.Png) return ".png";
            if (iFormat == ImageFormat.Gif) return ".gif";
            if (iFormat == ImageFormat.Bmp) return ".bmp";
            return "";
        }

        public static Bitmap ChangeColor(Bitmap scrBitmap, Color newColor) //Color.Red;
        {
            //You can change your new color here. Red,Green,LawnGreen any..
            Color actualColor;
            //make an empty bitmap the same size as scrBitmap
            Bitmap newBitmap = new Bitmap(scrBitmap.Width, scrBitmap.Height);
            for (int i = 0; i < scrBitmap.Width; i++)
            {
                for (int j = 0; j < scrBitmap.Height; j++)
                {
                    //get the pixel from the scrBitmap image
                    actualColor = scrBitmap.GetPixel(i, j);
                    // > 150 because.. Images edges can be of low pixel colr. if we set all pixel color to new then there will be no smoothness left.
                    if (actualColor.A > 150)
                        newBitmap.SetPixel(i, j, newColor);
                    else
                        newBitmap.SetPixel(i, j, actualColor);
                }
            }
            return newBitmap;
        }

        public static Color ColorFromHue(double hue)
        {
            if (!Utils.InRange(hue, 0, 360)) return new Color();
            // Convert hue to RGB
            double H = hue / 60;
            int i = (int)Math.Floor(H);
            double f = H - i;
            double p = 1 - f;
            double q = f;
            double t = f;
            double r, g, b;
            switch (i)
            {
                case 0: r = 1; g = t; b = 0;
                    break;
                case 1: r = q; g = 1; b = 0;
                    break;
                case 2: r = 0; g = 1; b = t;
                    break;
                case 3: r = 0; g = q; b = 1;
                    break;
                case 4: r = t; g = 0; b = 1;
                    break;
                case 5: r = 1; g = 0; b = q;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("hue", hue, "Hue must be between 0 and 360.");
            }
            // Create a Color object from the RGB values
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }    
        public static BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap, ImageFormat imageFormat)
        {
            var bitmapImage = new BitmapImage();
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, imageFormat);
                memory.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage.Clone();
        }

        public static BitmapImage UnhookedImageLoad(string filename, ImageFormat imageFormat = null)
        {
            ImageFormat iFormat = imageFormat == null ? GetImageFormat(filename) : imageFormat;
            if (iFormat == null) return null; // unrecogn. format; need to specify one
            System.Drawing.Image selectedImage = System.Drawing.Image.FromFile(filename);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(selectedImage);
            BitmapImage bitmapImage = BitmapToBitmapImage(bitmap, iFormat);
            selectedImage.Dispose(); bitmap.Dispose(); GC.Collect();
            return bitmapImage;
        }

        public static bool CopyToImageFormat(string sourceImage, string targetImage, ImageFormat targetImageFormat = null) // if null use source imageFormat 
        {
            ImageFormat iFormat = targetImageFormat == null ? GetImageFormat(sourceImage) : targetImageFormat;
            if (iFormat == null) return false; // unrecogn. format; need to specify one
            System.Drawing.Image selectedImage = System.Drawing.Image.FromFile(sourceImage);
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(selectedImage);
            string tfn = Path.ChangeExtension(targetImage, GetImageExt(iFormat));
            bitmap.Save(Utils.AvoidOverwrite(tfn), iFormat);
            bitmap.Dispose();
            return true;
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
        public static bool GetMetaDataItems(string imageFilePath, out Dictionary<string, string> itemMap, bool original = false)
        {
            itemMap = new Dictionary<string, string>();
            if (!Path.GetExtension(imageFilePath).ToLower().Equals(".png")) return false;
            var query = "/tEXt/{str=parameters}";
            try
            {
                using (Stream fileStream = File.Open(imageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    BitmapMetadata bitmapMetadata = decoder.Frames[0].Metadata as BitmapMetadata;
                    if (bitmapMetadata == null) return false;
                    var metadata = bitmapMetadata.GetQuery(query);
                    string md = metadata?.ToString(); if (md == null) { return false; }
                    var mda = md.Split((char)10); string[] mdb; string prompt = "";
                    if (mda.Length < 2) return false;
                    if (mda.Length > 2)
                    {
                        List<string> ls = new List<string>(mda);
                        mdb = ls[ls.Count - 1].Split(',');
                        ls.RemoveAt(ls.Count - 1);
                        prompt = String.Join("_", ls.ToArray());
                    }
                    else
                    {
                        prompt = mda[0];
                        mdb = mda[1].Split(',');
                    }
                    itemMap.Add("prompt", prompt);
                    if (mdb.Length.Equals(0)) return false;
                    foreach (var item in mdb)
                    {
                        var mdc = item.Split(':');
                        if (mdc.Length != 2) return false;
                        string nm = mdc[0].Trim();
                        if (!original)
                        {
                            if (nm.Equals("CFG scale")) nm = "scale";
                            if (nm.Equals("Model hash")) nm = "ModelHash";
                            else nm = nm.ToLower();
                        }
                        itemMap.Add(nm, mdc[1].Trim());
                    }
                    fileStream.Close();
                }
            }
            catch (Exception e) { Utils.TimedMessageBox("Error (I/O): " + e.Message, "Error message", 3000); return false; }
            return itemMap.Count > 0;
        }
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
        string AppDataStr = "<AppData>";
        public string AppData
        {
            get
            {
                if (Directory.Exists(Utils.basePath)) return Utils.basePath.EndsWith("\\") ? Utils.basePath : Utils.basePath + "\\";
                else Utils.TimedMessageBox("No directory: " + Utils.basePath);
                return "";
            }
        }
        List<string> history; string historyFile;
        public void Init()
        {
            List<string> ld = new List<string>(Directory.GetLogicalDrives());
            historyFile = Path.Combine(Utils.configPath, "history.lst");
            if (File.Exists(historyFile))
            {
                List<string> ls = Utils.readList(historyFile); history = new List<string>();
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
            if (!Utils.isInVisualStudio)
            {
                tbSelected.Visibility = Visibility.Collapsed; tvFolders.Margin = new Thickness(0);
            }
        }
        public void Finish()
        {
            Utils.writeList(historyFile, history);
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
                        subitem.FontWeight = FontWeights.Normal; subitem.FontSize = tvFolders.FontSize;
                        if (ImgUtils.checkImageDepot(s, false) > 0)
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Brushes.Blue; //Coral; OrangeRed;
                        }
                        if (ImgUtils.checkImageDepot(s, true) > 0)
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Utils.ToSolidColorBrush("#FF02CB02"); // Brushes.LimeGreen;  MediumSeaGreen  SeaGreen              
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
            if (OnLog != null) OnLog(txt, clr);
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
            if (tvFolders.SelectedItem != null)
            {
                string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
                Active(pth); tbSelected.Text = pth;
            }
        }
        private void tvFolders_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            cmFolders.Items.Clear();
            MenuItem mi = new MenuItem(); mi.Header = "New folder (subfolder to the selected)"; mi.Click += mi_Click; cmFolders.Items.Add(mi);
            MenuItem mj = new MenuItem(); mj.Header = "Rename"; mj.Click += mi_Click; cmFolders.Items.Add(mj);
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
                case "Rename":
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