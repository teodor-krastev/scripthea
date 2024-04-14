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
using System.Windows.Shapes;
using scripthea.master;
using UtilsNS;
using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;

namespace scripthea.viewer
{
    /// <summary> 
    /// Interaction logic for PicViewerUC.xaml
    /// </summary>
    public partial class PicViewerUC : UserControl
    {
        public PicViewerUC()
        {
            InitializeComponent(); iDepot = null;
        }
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            rowBottom.Height = new GridLength(Utils.EnsureRange(opts.viewer.PicViewPromptH, 30, 500));
            colMeta.Width = new GridLength(Utils.EnsureRange(opts.viewer.PicViewMetaW, 3,500));
            sldRank_MouseDoubleClick(null, null);
        }
        public void Finish()
        {
            opts.viewer.PicViewPromptH = (int)rowBottom.Height.Value;
            opts.viewer.PicViewMetaW = (int)colMeta.Width.Value;
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public ImageDepot iDepot { get; set; }
        public void Clear()
        {
            lbIndex.Content = ""; tbPath.Text = ""; tbName.Text = "";
            image.Source = null; image.UpdateLayout(); 
            tbCue.Text = ""; lboxMetadata.Items.Clear();
        }
        public void loadPic(int idx, string imageDir, ImageInfo ii)
        {
            string filePath = Path.Combine(imageDir, ii.filename);             
            UpdateMeta(imageDir, ii, null); // clear
            
            lbIndex.Content = "[ " + idx.ToString() + " ]";
            tbPath.Text = System.IO.Path.GetDirectoryName(filePath)+"\\";
            tbName.Text = System.IO.Path.GetFileName(filePath);
            if (!File.Exists(filePath))
            {
                image.Source = SctUtils.file_not_found;
                tbName.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            tbName.Foreground = System.Windows.Media.Brushes.Navy;
            if (File.Exists(filePath))
            {
                image.Source = ImgUtils.UnhookedImageLoad(filePath, ImgUtils.ImageType.Png);
                if (image.Source == null)
                    { Log("Exhausted resources - use table view instead", Brushes.Red); return; }
            }
            //var uri = new Uri(filePath); var bitmap = new BitmapImage(uri);  image.Source = bitmap.Clone(); image.UpdateLayout(); bitmap = null;
            tbCue.Text = ii.prompt; 
            // iDepot compare
            //if (chkExtra.IsChecked.Value) return; ?
            if (iDepot == null) return;
            if (!iDepot.isEnabled) return;
            if (!Utils.InRange(idx, 1, iDepot.items.Count, true)) return;
            if (!ii.prompt.Equals(ii.prompt, StringComparison.InvariantCultureIgnoreCase)) return;
            if (!ii.filename.Equals(tbName.Text, StringComparison.InvariantCultureIgnoreCase)) return;
            bool modified = !String.IsNullOrEmpty(ii.MD5Checksum);
            if (modified) modified = !Utils.GetMD5Checksum(filePath).Equals(ii.MD5Checksum);           
            UpdateMeta(imageDir, ii, modified);
        }
        private int ZoomFactor // [%]
        {
            get 
            {
                if (image == null) return -1;
                if (image.Source == null) return -1;
                ImageSource imageSource = image.Source;
                double xScale = image.ActualWidth / imageSource.Width; double yScale = image.ActualHeight / imageSource.Height; 
                int scale = Convert.ToInt32(100.0 * (xScale + yScale) / 2); // aver
                lbZoomFactor.Content = scale.ToString() + " %";
                return scale; 
            }
        }
        private int attemptCount = 0;
        private void UpdateMeta(string imageDir, ImageInfo ii, bool? modified) // null -> clear
        {
            if (modified == null) // get it here (later)
            {
                lboxMetadata.Items.Clear(); return;
            }            
            if (ii == null) return;
            string filePath = Path.Combine(imageDir, ii.filename);
            if (!File.Exists(filePath)) return;
            Dictionary<string, string> meta;
            if (ImgUtils.GetMetaDataItems(filePath, out meta)) {  attemptCount = 0; }
            else
            {
                if (attemptCount < 2) // retry in case it's still opening
                    Utils.DelayExec(1000, new Action(() => { attemptCount++; UpdateMeta(imageDir, ii, modified); }));
                //meta.Add("No access to Meta data: ", ""); meta.Add(" the info is missing or ", ""); meta.Add("file is opened by a process.", "");
                meta = Utils.dictObject2String(ii.ToDictionary(true));
            } 
            // clean up meta
            meta.Remove("prompt");   
            void removeMetaItem(string key, string val) // remove some defaults
            {
                if (meta.ContainsKey(key))
                    if (meta[key] == val) meta.Remove(key);
            }
            removeMetaItem("history", "");
            removeMetaItem("tags", "");
            removeMetaItem("negative_prompt", "");
            removeMetaItem("batch_size", "1");
            removeMetaItem("denoising_strength", "0");
            removeMetaItem("restore_faces", "False");
            if (meta.ContainsKey("MD5Checksum")) meta.Remove("MD5Checksum");

            Utils.dict2ListBox(meta, lboxMetadata);
           
            if ((bool)modified)
            {
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = "--MODIFIED--"; lbi.Foreground = System.Windows.Media.Brushes.Red;
                lboxMetadata.Items.Add(lbi);
            }                               
            Utils.Sleep(200);
        }
        private void imageMove(double h, double v) // ?
        {
            Thickness m = image.Margin;
            m.Left += h; m.Right -= h;
            m.Top += v; m.Bottom -= v;
            image.Margin = m;           
        }
        public void zoomControl(int zoomWay) // -1 -> zoomOut; 0 -> fit in; 1 -> zoomIn
        {
            switch (zoomWay)
            {
                case -1: imgZoomIn_MouseDown(imgZoomOut, null);
                    break;
                case 0: imgZoomFit_MouseDown(imgZoomFit, null);
                    break;
                case 1: imgZoomIn_MouseDown(imgZoomIn, null);
                    break;
            }
        }
        bool scrollable = false;
        private void imgZoomIn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!scrollable)
            {
                noscrollGrid.Children.Remove(image); noscrollGrid.Visibility = Visibility.Collapsed;
                scrollViewer.Visibility = Visibility.Visible; scrollViewer.Content = image;
                scrollable = true;
            }
            double k = sender.Equals(imgZoomIn) ? 1.2 : 1/1.2;            
            image.Width = k * image.ActualWidth; image.Height = k * image.ActualHeight;             
        }
        private void imgZoomFit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (scrollable)
            {
                scrollViewer.Content = null; scrollViewer.Visibility = Visibility.Collapsed;  
                noscrollGrid.Visibility = Visibility.Visible; noscrollGrid.Children.Add(image);
                scrollable = false;
            }
            image.Width = Double.NaN; image.Height = Double.NaN;
        }
        private void image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string filePath = Path.Combine(tbPath.Text, tbName.Text);
            DataObject data = new DataObject(DataFormats.FileDrop, new string[] { filePath });
            // Start the drag-and-drop operation
            DragDrop.DoDragDrop(image, data, DragDropEffects.Copy);
        }
        private void image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int i = ZoomFactor;
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage((BitmapSource)image.Source);
            Utils.TimedMessageBox("The image has been copied to the clipboard");
        }

        private void sldRank_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (opts == null) return;
            bool bb = Convert.ToBoolean(opts.viewer.BnWrank);
            if (!Utils.isNull(sender)) bb = !bb;
            if (bb) {topGradient.Color = Brushes.White.Color; bottomGradient.Color = Utils.ToSolidColorBrush("#FF636363").Color; }
            else { topGradient.Color = Utils.ToSolidColorBrush("#FFFE9177").Color; bottomGradient.Color = Utils.ToSolidColorBrush("#FF7FB4FF").Color; }
            opts.viewer.BnWrank = bb;
        }
    }
}
