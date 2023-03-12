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
using System.Drawing;
using System.Drawing.Imaging;
using scripthea.master;
using UtilsNS;
using Path = System.IO.Path;

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
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public DepotFolder iDepot { get; set; }
        private string _imagePath;
        public string imagePath { get { return _imagePath; }  private set { _imagePath = value; } }
        public void Clear()
        {
            lbIndex.Content = ""; tbPath.Text = ""; tbName.Text = "";
            image.Source = null; image.UpdateLayout(); 
            tbCue.Text = ""; lboxMetadata.Items.Clear();
        }
        public void loadPic(int idx, string filePath, string prompt)
        {
            if (File.Exists(filePath)) imagePath = filePath;
            bool modified = false;
            if (chkExtra.IsChecked.Value) UpdateMeta(modified);
            
            lbIndex.Content = "[" + idx.ToString() + "]";
            tbPath.Text = System.IO.Path.GetDirectoryName(filePath)+"\\";
            tbName.Text = System.IO.Path.GetFileName(filePath);
            if (!File.Exists(filePath))
            {
                image.Source = ImgUtils.file_not_found;
                tbName.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            tbName.Foreground = System.Windows.Media.Brushes.Navy;
            if (File.Exists(filePath)) image.Source = ImgUtils.UnhookedImageLoad(filePath, ImageFormat.Png);
            //var uri = new Uri(filePath); var bitmap = new BitmapImage(uri);  image.Source = bitmap.Clone(); image.UpdateLayout(); bitmap = null;
            tbCue.Text = prompt;
            // iDepot compare
            if (iDepot == null) return;
            if (!iDepot.isEnabled) return;
            if (idx > iDepot.items.Count) return;
            ImageInfo ii = iDepot.items[idx - 1];
            if (!ii.prompt.Equals(prompt, StringComparison.InvariantCultureIgnoreCase)) return;
            if (!ii.filename.Equals(tbName.Text, StringComparison.InvariantCultureIgnoreCase)) return;
            modified = !Utils.GetMD5Checksum(filePath).Equals(ii.MD5Checksum);
            if (chkExtra.IsChecked.Value && modified) UpdateMeta(modified);
        }

        private int attemptCount = 0;
        private void UpdateMeta(bool? modified)
        {
            if (!File.Exists(imagePath)) return;
            Dictionary<string, string> meta;
            if (ImgUtils.GetMetaDataItems(imagePath, out meta)) { meta.Remove("prompt"); attemptCount = 0; }
            else
            {
                if (attemptCount < 2)
                    Utils.DelayExec(1000, new Action(() => { attemptCount++; UpdateMeta(modified); }));
                meta.Add("No access to Meta data: ", ""); meta.Add(" the info is missing or ", ""); meta.Add("file is opened by a process.", "");
            }           
            Utils.dict2ListBox(meta, lboxMetadata);
            bool modif = false;
            if (modified == null) // get it here (later)
            {

            }
            else modif = (bool)modified;
            if (modif)
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
        private void imgCopy_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetImage((BitmapSource)image.Source);
            Utils.TimedMessageBox("The image is in the clipboard");
        }
        private void chkExtra_Checked(object sender, RoutedEventArgs e)
        {
            lboxMetadata.Visibility = Visibility.Visible;
            columnMeta.Width = new GridLength(150);
            rowBottom.Height = new GridLength(140);
            UpdateMeta(false);
        }

        private void chkExtra_Unchecked(object sender, RoutedEventArgs e)
        {
            lboxMetadata.Visibility = Visibility.Collapsed;
            columnMeta.Width = new GridLength(1);
            rowBottom.Height = new GridLength(42);
        }
        private void image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string filePath = Path.Combine(tbPath.Text, tbName.Text);
            DataObject data = new DataObject(DataFormats.FileDrop, new string[] { filePath });
            // Start the drag-and-drop operation
            DragDrop.DoDragDrop(image, data, DragDropEffects.Copy);
        }
    }
}
