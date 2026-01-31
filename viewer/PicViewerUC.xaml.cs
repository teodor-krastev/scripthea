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
using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;
using scripthea.composer;
using scripthea.master;
using scripthea.options;
using UtilsNS;

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
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            rowBottom.Height = new GridLength(Utils.EnsureRange(opts.viewer.PicViewPromptH, 30, 500));
            colMeta.Width = new GridLength(Utils.EnsureRange(opts.viewer.PicViewMetaW, 3,500));
            miNeutral_Click(null, null); 
            actIdx = -1;
        }
        public void Finish()
        {
            opts.viewer.PicViewPromptH = (int)rowBottom.Height.Value;
            opts.viewer.PicViewMetaW = (int)colMeta.Width.Value;
        }
        
        public int actIdx { get; private set; } // when refresh
        public ImageDepot iDepot { get; private set; } 
        public void SetiDepot(ImageDepot _iDepot)
        {
            iDepot = _iDepot;
        }
        protected ImageInfo SelectedItem(int idx, ImageDepot _iDepot)  // idx 0 based
        {
            ImageInfo ii = null; 
            if (_iDepot != null)
                if (_iDepot.isEnabled && Utils.InRange(idx, 0, _iDepot.items.Count - 1))
                    ii = _iDepot.items[idx];
            return ii;
        }
        public void Clear()
        {
            lbIndex.Content = ""; tbPath.Text = ""; tbName.Text = "";
            image.Source = null; image.UpdateLayout(); 
            tbCue.Text = ""; lboxMetadata.Items.Clear();
        }
        public void loadPic(int idx, ImageDepot _iDepot) // 0 based
        {
            ImageInfo ii = SelectedItem(idx, _iDepot); actIdx = -1; if (ii is null) return;
            string filePath = Path.Combine(_iDepot.path, ii.filename);             
            UpdateMeta(_iDepot.path, ii, null); // clear
            actIdx = idx;
            lbIndex.Content = "[ " + (idx+1).ToString() + " ]";
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
                if (image.Source is null)
                    { opts.Log("Exhausted resources - use table view instead", Brushes.Red); return; }
            }
            //var uri = new Uri(filePath); var bitmap = new BitmapImage(uri);  image.Source = bitmap.Clone(); image.UpdateLayout(); bitmap = null;
            tbCue.Text = ii.prompt; 
            // iDepot compare
            //if (chkExtra.IsChecked.Value) return; ?
            if (_iDepot is null) return;
            if (!_iDepot.isEnabled) return;
            if (!Utils.InRange(idx, 0, _iDepot.items.Count-1, true)) return;
            if (!ii.prompt.Equals(ii.prompt, StringComparison.InvariantCultureIgnoreCase)) return;
            if (!ii.filename.Equals(tbName.Text, StringComparison.InvariantCultureIgnoreCase)) return;

            UpdateMeta(_iDepot.path, ii, IsModified(_iDepot.path, ii));
        }
        protected bool IsModified(string imageDir, ImageInfo ii)
        {
            bool modified = !String.IsNullOrEmpty(ii.MD5Checksum); 
            if (modified) modified = !Utils.GetMD5Checksum(Path.Combine(imageDir,ii.filename)).Equals(ii.MD5Checksum);
            return modified;
        }
        private int ZoomFactor // [%]
        {
            get 
            {
                if (image is null) return -1;
                if (image.Source is null) return -1; 
                ImageSource imageSource = image.Source; Point wh = ImgUtils.GetActualSizeInPixels(image);
                double xScale = wh.X / imageSource.Width; double yScale = wh.Y / imageSource.Height; 
                int scale = Convert.ToInt32(100.0 * (xScale + yScale) / 2); // aver
                lbZoomFactor.Content = scale.ToString() + " % ";
                return scale; 
            }
        }        
        private void UpdateMeta(int idx, ImageDepot _iDepot, bool? modified, bool tryFileMeta = false)
        {
            ImageInfo ii = SelectedItem(idx, iDepot); 
            if (ii is null) return;
            UpdateMeta(_iDepot.path, ii, modified);
        }        
        private bool UpdateMeta(string imageDir, ImageInfo ii, bool? modified, bool tryFileMeta = false) // update to visuals, null -> clear
        {
            if (modified is null) // get it here (later)
            {
                lboxMetadata.Items.Clear(); return true;
            }            
            if (ii is null) return false;
            string filePath = Path.Combine(imageDir, ii.filename);
            if (!File.Exists(filePath)) return false;
            Dictionary<string, string> meta;
            if (tryFileMeta) // from image file, if from SD-A1111
            {
                if (!ImgUtils.GetMetadata1111(filePath, out meta)) return false;
                /* private int attemptCount = 0;
                 * {  attemptCount = 0; } getting repeat try out later
                else
                {
                    if (attemptCount < 2) // retry in case it's still opening
                        Utils.DelayExec(1000, new Action(() => { attemptCount++; UpdateMeta(imageDir, ii, IsModified(imageDir, ii), true); }));
                }*/
            }              
            else
            {
                //meta.Add("No access to Meta data: ", ""); meta.Add(" the info is missing or ", ""); meta.Add("file is opened by a process.", "");
                meta = Utils.dictObject2String(ii.ToDictionary(), "G4");
            } 
            // clean up meta
            meta.Remove("prompt");   
            void removeMetaItem(string key, string val) // remove some defaults/empty, "*" mask for remove it uncond.
            {
                if (meta.ContainsKey(key))
                { 
                    if (val == "*" ||  meta[key].Trim() == val) meta.Remove(key);
                }
            }
            rowNegative.Height = new GridLength(0);
            if (meta.ContainsKey("negative_prompt"))
            {
                string neg = meta["negative_prompt"];
                if (neg.Trim() != "") 
                {
                    rowNegative.Height = new GridLength(28); tbNegative.Text = neg; stpNegative.ToolTip = neg;
                }
            }
            removeMetaItem("history", "");
            removeMetaItem("tags", "");
            removeMetaItem("negative_prompt", "*");
            removeMetaItem("batch_size", "1");
            removeMetaItem("restore_faces", "False");
            removeMetaItem("sd_model_hash", "*");
            if (meta.ContainsKey("MD5Checksum")) meta.Remove("MD5Checksum");            
            int r = meta.ContainsKey("rate") ? Convert.ToInt32(meta["rate"]) : 0;            
            removeMetaItem("rate", "*");
            if (meta.ContainsKey("job_timestamp"))
                if (meta["job_timestamp"] == "") removeMetaItem("job_timestamp", "");

            if (meta.ContainsKey("sampler_name"))
                if (meta["sampler_name"] == "") meta["sampler_name"] = "<default>";
            if (meta.ContainsKey("model"))
                if (meta["model"] == "") meta["model"] = "<default>";

            Utils.dict2ListBox(meta, lboxMetadata);
            
            ListBoxItem lbi = new ListBoxItem() 
                { Foreground = System.Windows.Media.Brushes.Blue, Background = System.Windows.Media.Brushes.AliceBlue, Content = "rate: " + ((r == 0)? "0 <unrated>" : r.ToString()) }; 
            lbi.FontFamily = new FontFamily("Segoe UI Semibold");
            lboxMetadata.Items.Insert(0,lbi);
            SetSliderRate(r);
            
            if ((bool)modified)
            {
                lbi = new ListBoxItem();
                lbi.Content = "--MODIFIED--"; lbi.Foreground = System.Windows.Media.Brushes.Red;
                lboxMetadata.Items.Add(lbi);
            }
            foreach (object obj in lboxMetadata.Items)
            {
                ListBoxItem lbit = obj as ListBoxItem; if (lbit is null) continue;
                lbit.Selected += new RoutedEventHandler(OnItemSelected);
            }
            return true;
        }
        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
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
            _ = new PopupText(btnCopy, "Image copied");
        }
        public event UpdateVisRecEventHandler OnUpdateVisRecord;        
        protected void SetSliderRate(int value)
        {
            if (!Utils.InRange(value, 0, 10)) return;
            lockRate = true; sldRate.Value = value;
            lockRate = false;
        }
        private bool lockRate = false; // when it has been changed from code 
        private void sldRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lockRate || iDepot is null || opts is null) return;
            if (opts.composer.QueryStatus == Status.Scanning) { Utils.TimedMessageBox("Error[886]: the IDF is updating."); return; }
            ImageInfo ii = SelectedItem(actIdx, iDepot);
            ii.rate = Convert.ToInt16(sldRate.Value);
            UpdateMeta(iDepot.path, ii, IsModified(iDepot.path, ii), false); // local
            if (!iDepot.isReadOnly) 
            { 
                if (!ii.IsChanged) opts.composer.TotalRatingCount++;
                ii.IsChanged = true;  
            }
            iDepot.items[actIdx] = ii; OnUpdateVisRecord?.Invoke(actIdx, ii);                      
        }
        private void miNeutral_Click(object sender, RoutedEventArgs e)
        {
            if (opts is null) return;
            if (sender != null) opts.viewer.BnWrate = sender.Equals(miNeutral);
            miNeutral.IsChecked = opts.viewer.BnWrate; miBlueRed.IsChecked = !miNeutral.IsChecked;
            if (opts.viewer.BnWrate) { topGradient.Color = Brushes.White.Color; bottomGradient.Color = Brushes.Black.Color; }
            else { topGradient.Color = Brushes.OrangeRed.Color; bottomGradient.Color = Brushes.DodgerBlue.Color; } //Utils.ToSolidColorBrush("#FF7FB4FF").Color Utils.ToSolidColorBrush("#FFFA6262").Color;
        }
        private void sldRate_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) sldRate.Value += 1;
            if (e.Delta < 0) sldRate.Value -= 1; 
        }
        private void image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) zoomControl(1);
            if (e.Delta < 0) zoomControl(-1);
        }
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) zoomControl(0);
        }
    }
}