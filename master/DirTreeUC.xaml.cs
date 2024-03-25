﻿using System;
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
using ExifLib;

namespace scripthea.master
{
    public static class ImgUtils
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

        public static BitmapImage file_not_found { get { return UnhookedImageLoad(Path.Combine(Utils.configPath,"file_not_found.jpg"), ImageFormat.Jpeg); } }
        public static SolidColorBrush ToSolidColorBrush(string hex_code)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex_code);
        }
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
                    List<string> ls = Utils.readList(fn,true);
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
        /// ModifyExifData
        /// </summary>
        /// <param name="inputImagePath"></param>
        /// <param name="outputImagePath"></param>
        /// https://medium.com/@dannyc/get-image-file-metadata-in-c-using-net-88603e6da63f
        /// 
        /*public enum JpgPropertyTag: int
        {
            // PropertyTagTypeShort
            ImageWidth = 0x0100, ImageHeight = 0x0101, // JPEGQuality = 0x5010, Adobe private property

            // PropertyTagTypeASCII
            Artist = 0x013B, Copyright = 0x8298, 
            DateTime = 0x0132, 
            SoftwareUsed = 0x0131, UserComment = 0x9286 	
        }

        public static void ModifyExifData(string inputImagePath, Dictionary<JpgPropertyTag, string> propTags, string outputImagePath = "")
        {
            // Load the image
            System.Drawing.Image image = System.Drawing.Image.FromFile(inputImagePath);

            // Get the PropertyItems (EXIF data) of the image
            PropertyItem[] properties = image.PropertyItems;

            // Modify the desired EXIF property
            int propertyId = 0x0132; // 0x0132 is the EXIF tag for DateTime
            string newValue = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss");

            // Find the PropertyItem with the desired property ID
            PropertyItem targetProperty = null;
            foreach (var property in properties)
            {
                if (property.Id == propertyId)
                {
                    targetProperty = property;
                    break;
                }
            }

            // If the property exists, modify it. If not, create a new one
            if (targetProperty != null)
            {
                targetProperty.Value = Encoding.ASCII.GetBytes(newValue + '\0');
            }
            else
            {
                targetProperty = image.PropertyItems[0];
                targetProperty.Id = propertyId;
                targetProperty.Type = 2; // 2 is the type for ASCII strings
                targetProperty.Value = Encoding.ASCII.GetBytes(newValue + '\0');
                targetProperty.Len = targetProperty.Value.Length;
            }

            // Set the modified property back to the image
            image.SetPropertyItem(targetProperty);

            // Save the modified image to the output file
            image.Save(outputImagePath == "" ? inputImagePath : outputImagePath , ImageFormat.Jpeg);
        }


        
        public  void Ma1in()
        {

            // Load the JPG image.
            System.Drawing.Image image = System.Drawing.Image.FromFile("my_image.jpg");

            // Get the ExifData object.
            ExifLib.IFD ExifData exifData = image.GetPropertyItem(306).Value as ExifData;

            // Modify the EXIF data.
            exifData.Make = "My Camera";
            exifData.Model = "My Model";
            exifData.DateTime = DateTime.Now;
            exifData.Description = "This is my image description";

            // Set the PropertyItem value of the Image object.
            image.GetPropertyItem(306).Value = exifData;

            // Save the image.
            image.Save("my_image_modified.jpg");
        }*/
    

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
                default: return null; // other
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
        public static double HueFromPercents(double percents) // from 0 to 100 -> 250 to 0 
        {
            if (!Utils.InRange(percents, 0, 100)) return Double.NaN;
            return Math.Abs(percents - 100) * 2.5;
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
            try
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
            catch { return null; }
        }
        public static Bitmap ResizeImage(Bitmap originalImage, int newWidth, int newHeight)
        {
            Bitmap resizedImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }
            return resizedImage;
        }
        /* Example 
        string inputImagePath = "path/to/your/input/image.jpg";
        string outputImagePath = "path/to/your/output/image.jpg";
        using (Bitmap originalImage = new Bitmap(inputImagePath))
        {
            int newWidth = originalImage.Width / 2; // Decrease the width by 50%
            int newHeight = originalImage.Height / 2; // Decrease the height by 50%
            using (Bitmap resizedImage = ResizeImage(originalImage, newWidth, newHeight))
            {
                resizedImage.Save(outputImagePath, ImageFormat.Jpeg);
            }
        }         
         */
        public static BitmapImage UnhookedImageLoad(string filename, ImageFormat imageFormat = null)
        {
            try
            {
                if (!File.Exists(filename)) 
                    { Utils.TimedMessageBox("File <" + filename + "> is missing.", "Error:", 3000); return null; }
                ImageFormat iFormat = imageFormat == null ? GetImageFormat(filename) : imageFormat;
                if (iFormat == null) return null; // unrecogn. format; need to specify one
                System.Drawing.Image selectedImage = System.Drawing.Image.FromFile(filename);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(selectedImage);
                BitmapImage bitmapImage = BitmapToBitmapImage(bitmap, iFormat);
                selectedImage.Dispose(); bitmap.Dispose(); GC.Collect();
                return bitmapImage;
            }
            catch { return null; }
        }
        /*
public System.Drawing.Color GetTriadicColors(System.Drawing.Color rgbColor, double turnColorWheel) // turnColorWheel [0..360]
{
    // Convert the base color to HSL            

    float hue = rgbColor.GetHue();
    float saturation = rgbColor.GetSaturation();
    float lightness = rgbColor.GetBrightness();

    float h1 = (hue + (float)turnColorWheel) % 360;

    System.Drawing.Color.RGBtoHSL(baseColor.R, baseColor.G, baseColor.B, out h, out s, out l);

    // Calculate the three triadic hues
    float h1 = (h + 120) % 360;
    float h2 = (h + 240) % 360;

    // Convert the hues back to RGB colors
    Color triadic1 = ColorExtensions.FromAhsb(baseColor.A, h1, s, l);
    Color triadic2 = ColorExtensions.FromAhsb(baseColor.A, h2, s, l);

    // Return the three triadic colors
    return new Color { baseColor, triadic1, triadic2 };
}

public static System.Windows.Media.Color turnColorWheel(System.Windows.Media.Color baseColor, double turn2degree = 180) // turn2degree 
{

}
double h = 120; // Hue value (0-360)
double s = 0.5; // Saturation value (0-1)
double l = 0.5; // Lightness value (0-1)

System.Windows.Media.Color color = ColorHelper.FromHsl(h, s, l);
byte r = color.R; // Red component value (0-255)
byte g = color.G; // Green component value (0-255)
byte b = color.B; // Blue component value (0-255)

// Use the RGB values to create a new Color object
Color rgbColor = Color.FromRgb(r, g, b);
*/

        public static async Task<BitmapImage> LoadBitmapImageFromFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                BitmapImage bitmapImage = new BitmapImage();
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();
                }
                bitmapImage.Freeze(); // Necessary for cross-thread operations
                return bitmapImage;
            });
        }
        /* how to call it
        private async void LoadAndDisplayBitmapImageFromFileAsync(string filePath, ref Image imageComp)
        {
            BitmapImage bitmapImage = await LoadBitmapImageFromFileAsync(filePath);

            if (bitmapImage != null)
            {
                // Display the image in an Image control, for example
                image1.Source = bitmapImage;
            }
            else
            {
                MessageBox.Show("Failed to load the image.");
            }
        }*/

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
        public static void SaveBitmapImageToDisk(BitmapImage bitmapImage, string filePath) // in PNG format
        {
            // Create a BitmapEncoder object
            BitmapEncoder encoder = null;
            if (GetImageFormat(filePath) == ImageFormat.Jpeg) { encoder = new JpegBitmapEncoder(); }
            if (GetImageFormat(filePath) == ImageFormat.Png) { encoder = new PngBitmapEncoder(); }
            if (encoder == null) { Utils.TimedMessageBox("Unknown image type of file: " + filePath); return; }

            // Create a BitmapFrame object from the BitmapImage
            BitmapFrame frame = BitmapFrame.Create(bitmapImage);

            // Add the frame to the encoder
            encoder.Frames.Add(frame);

            // Create a FileStream object
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                // Save the image to the file
                encoder.Save(fileStream);
            }
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
            if (!Utils.isInVisualStudio)
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
                        
                        int checkImageDepot = ImgUtils.checkImageDepot(s, false); if (checkImageDepot < 0) continue;                       
                        if (checkImageDepot > 0)
                        {
                            subitem.FontSize = tvFolders.FontSize + 0.5;
                            subitem.Foreground = Brushes.Blue; subitem.ToolTip = "#"+ checkImageDepot.ToString();
                        }
                        checkImageDepot = ImgUtils.checkImageDepot(s, true); if (checkImageDepot < 0) continue;
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