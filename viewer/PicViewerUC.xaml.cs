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
using UtilsNS;
using Path = System.IO.Path;
using System.Drawing.Imaging;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for PicViewerUC.xaml
    /// </summary>
    public partial class PicViewerUC : UserControl
    {
        public PicViewerUC()
        {
            InitializeComponent();
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }

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
            if (chkExtra.IsChecked.Value) UpdateMeta();
            
            lbIndex.Content = "[" + idx.ToString() + "]";
            tbPath.Text = System.IO.Path.GetDirectoryName(filePath)+"\\";
            tbName.Text = System.IO.Path.GetFileName(filePath);
            if (!File.Exists(filePath))
            {
                image.Source = ImgUtils.UnhookedImageLoad(Utils.basePath + "\\Properties\\file_not_found.jpg", ImageFormat.Jpeg);
                tbName.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }
            tbName.Foreground = System.Windows.Media.Brushes.Navy;
            if (File.Exists(filePath)) image.Source = ImgUtils.UnhookedImageLoad(filePath, ImageFormat.Png);
            //var uri = new Uri(filePath); var bitmap = new BitmapImage(uri);  image.Source = bitmap.Clone(); image.UpdateLayout(); bitmap = null;
            tbCue.Text = prompt;
        }

/*        private const int NumberOfRetries = 3;
        private const int DelayOnRetry = 1000;

for (int i=1; i <= NumberOfRetries; ++i) {
    try {
        // Do stuff with file
        break; // When done we can break loop
    }
    catch (IOException e) when(i <= NumberOfRetries)
        {
            // You may check error code to filter some exceptions, not every error
            // can be recovered.
            Thread.Sleep(DelayOnRetry);
        }
    }*/

        private int attemptCount = 0;
        private void UpdateMeta()
        {
            if (!File.Exists(imagePath)) return;
            Dictionary<string, string> meta;
            if (ImgUtils.GetMetaDataItems(imagePath, out meta)) { meta.Remove("prompt"); attemptCount = 0; }
            else
            {
                if (attemptCount < 2)
                    Utils.DelayExec(1000, new Action(() => { attemptCount++; UpdateMeta(); }));
                meta.Add("No access to Meta data: ", ""); meta.Add(" the info is missing or ", ""); meta.Add("file is opened by a process.", "");
            }
            Utils.dict2ListBox(meta, lboxMetadata); Utils.Sleep(200);
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
            rowBottom.Height = new GridLength(120);
            UpdateMeta();
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
