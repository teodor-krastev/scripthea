using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using scripthea.options;
using scripthea.viewer;
using scripthea.preview;
using UtilsNS;
using System.Diagnostics;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace scripthea.style
{
    /// <summary>
    /// Interaction logic for ReferenceSetUC.xaml
    /// </summary>
    public partial class ReferenceSetUC : UserControl
    {
        public ReferenceSetUC()
        {
            InitializeComponent();
        }
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        public List<PicItemUC> picItems { get; private set; } = new List<PicItemUC>();
        protected PicItemUC SelectedPicItem() // from inside PicItem
        {
            foreach (PicItemUC ps in picItems)
                if (ps.selected) return ps;
            return null;
        }
        private void picItemsClear()
        {
            for (int i = 0; i < picItems.Count; i++)
            {
                picItems[i].Clear();
            }
            picItems.Clear(); GC.Collect(); spPicsPile.Children.Clear(); spPicsPile.UpdateLayout();
        }
        public int Count { get { return picItems.Count; } }
        public bool AddPicItem(PicItemUC piUC)
        {
            if (piUC is null) return false;
            if (Count > 9) { opts.Log("Error: max count of 10 is reached."); return false; }
            foreach (PicItemUC pUC in picItems)
            {
                if (ImgUtils.PixelEquals(piUC.bitmapImage, pUC.bitmapImage))
                {
                    opts.Log("Error: duplicated images"); return false;
                }
            }           
            picItems.Add(piUC);
            spPicsPile.Children.Add(piUC);
            piUC.VisualUpdate((int)spPicsPile.ActualWidth);
            piUC.OnSelect -= new RoutedEventHandler(SelectTumb); piUC.OnSelect += new RoutedEventHandler(SelectTumb); SelectTumb(piUC, null);
            tbCounter.Text = "# " + Count;
            return true;
        }
        public PicItemUC AddImageInfo(string iFolder, ImageInfo ii)
        {
            PicItemUC piUC = new PicItemUC(ref opts, false); 
            piUC.ContentUpdate(Count, iFolder, ii);                      
            return piUC;
        }
        public PicItemUC AddImage(string imageFile)
        {
            if (!File.Exists(imageFile)) return null;
            return AddImage(ImgUtils.UnhookedImageLoad(imageFile));
        }
        public PicItemUC AddImage(BitmapImage bitmapImage) 
        {
            if (bitmapImage is null) return null;
            PicItemUC piUC = new PicItemUC(ref opts, false);
            if (piUC.ImageUpdate(bitmapImage)) return piUC;
            else return null;
        }
        protected PicItemUC selectedPic = null;
        protected void SelectTumb(object sender, RoutedEventArgs e)
        {
            selectedPic = null; if(Count == 0) return;
            foreach (PicItemUC piUC in picItems) // reset all            
                if (piUC.selected) piUC.selected = false;
            foreach (PicItemUC piUC in picItems)
            {
                if (piUC.Equals(sender))
                {
                    piUC.selected = true; //OnSelect(piUC.idx, iDepot);
                    selectedPic = piUC; return;
                }
            }
        }
        private void btnPasteClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                BitmapSource bitmap = Clipboard.GetImage();
                if (bitmap is null) return;
                PicItemUC piUC = AddImage(ImgUtils.ConvertBitmapSourceToBitmapImage(bitmap));
                if (!(piUC is null)) AddPicItem(piUC);
            }
            else
            {
                MessageBox.Show("No image found on the clipboard.");
            }
        }
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPic is null) return;
            picItems.Remove(selectedPic); spPicsPile.Children.Remove(selectedPic);
            if (Count > 0) SelectTumb(picItems[0], null);
            tbCounter.Text = "# " + Count;
        }
        
        #region visuals
        private void btnDropIn_DragOver(object sender, DragEventArgs e)
        {
            // Only allow dropping files
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void btnDropIn_Drop(object sender, DragEventArgs e)
        {
            // 1. File
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Retrieve the list of file paths
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    string extension = Path.GetExtension(filePath).ToLower();

                    // Basic validation for image extensions
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

                    if (allowedExtensions.Contains(extension))
                    {
                        AddPicItem(AddImage(filePath));
                    }
                }
                return;
            }
            // 2
            if (e.Data.GetDataPresent("DeviceIndependentBitmap") && false)
            {
                var stream = e.Data.GetData("DeviceIndependentBitmap") as MemoryStream;
                if (stream != null)
                {
                    stream.Position = 0;
                    // DIBs require a bit of header fix-up to load directly, 
                    // but often 'Bitmap' format handles this automatically in WPF.
                    // If you have a standard PNG/JPG stream:
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    PicItemUC piUC = AddImage(bitmap);
                    if (piUC is null) opts.Log("Error: image format problem");
                    else AddPicItem(piUC);
                }
                return;
            }
            // 3. Check for In-Memory Bitmap
            if (e.Data.GetDataPresent(DataFormats.Bitmap))
            {
                // WPF usually wraps GDI+ Bitmaps in a InteropBitmap
                var bitmapSource = e.Data.GetData(DataFormats.Bitmap) as BitmapSource;
                bitmapSource.Freeze();
                if (bitmapSource != null)
                {
                    PicItemUC piUC = AddImage(ImgUtils.ConvertBitmapSourceToBitmapImage(bitmapSource));
                    if (piUC is null) opts.Log("Error: image format problem");
                    else AddPicItem(piUC);
                    ImgUtils.SaveImage(bitmapSource, "d:\\test-image.png");
                }
                return;
            }
         }
        #endregion

    }

}
