using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
using scripthea.master;
using scripthea.options;
using UtilsNS;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for PicItemUC.xaml
    /// </summary>
    public partial class PicItemUC : UserControl
    {
        public enum statusStates { Selected, Focused, Marked }
        public HashSet<statusStates> status;
        public PicItemUC(ref Options _opts, bool __checkable)
        {
            if (_opts == null) return;
            if (_opts.general.AppTerminating) return;
            InitializeComponent();
            opts = _opts; checkable = __checkable;
            status = new HashSet<statusStates>();
        }
        Options opts;      
        public int idx { get; set; } // 0 based 

        public bool selected
        {
            get { return status.Contains(statusStates.Selected); }
            set
            {               
                if (value)
                {
                    status.Add(statusStates.Selected);
                    focused = focused;                    
                }
                else
                {
                    status.Remove(statusStates.Selected);
                    marked = marked;                     
                }
            }
        }
        public bool focused
        {
            get { return status.Contains(statusStates.Focused);  }
            set
            {
                if (value) status.Add(statusStates.Focused);
                else status.Remove(statusStates.Focused);
                if (!selected) return; 
                if (focused) Background = Brushes.Gray;  
                else Background = Brushes.Silver;
                tbCue.Foreground = Brushes.White; 
                lbNumber.Foreground = Brushes.White; tbFile.Foreground = Brushes.White; lbRate.Foreground = Brushes.White;
            }
        }
        public bool marked
        {
            get { return status.Contains(statusStates.Marked); }
            set
            {
                if (value) status.Add(statusStates.Marked);
                else status.Remove(statusStates.Marked);
                if (selected) return;      
                if (value) Background = ImgUtils.ToSolidColorBrush("#FFE6FFF3"); //Brushes.MintCream;
                else Background = Brushes.White;                        
                tbCue.Foreground = Brushes.Black; 
                lbNumber.Foreground = Brushes.DarkBlue; tbFile.Foreground = Brushes.Black; lbRate.Foreground = Brushes.Maroon;
            }
        }
        private bool _checkable;
        public bool checkable
        {
            get { return _checkable; }
            set
            { 
                _checkable = value;
                if (_checkable) { chkChecked.Visibility = Visibility.Visible; tbCue.Margin = new Thickness(15, 0, 0, 0); }
                else { chkChecked.Visibility = Visibility.Collapsed; tbCue.Margin = new Thickness(0); }
            }
        }
        public bool? IsChecked
        {
            get 
            { 
                if (!checkable) return null;
                return chkChecked.IsChecked.Value; 
            }
            set { chkChecked.IsChecked = value; }
        }
        public event RoutedEventHandler OnSelect;
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected void Select(object sender, RoutedEventArgs e)
        {
            if (OnSelect != null) OnSelect(this, e);
        }
        public string imageFolder;
        
        public void Clear()
        {
            imgPic.Source = null;   
        }
        public ImageInfo imgInfo { get; private set; }
        public BitmapImage bitmapImage { get; private set; }
        public bool file_not_found { get; private set; }
        public bool ContentUpdate(int index, string imageDir, ImageInfo ii) // index 0 based 
        {
            if (ii == null || !Directory.Exists(imageDir)) return false;
            imageFolder = imageDir; imgInfo = ii.Clone();
            string filePath = Path.Combine(imageDir, ii.filename);
            file_not_found = !File.Exists(filePath);
            if (!file_not_found)
            {
                //imgPic.Dispatcher.InvokeAsync(() => // slower for some reason !
                //{
                bitmapImage = ImgUtils.UnhookedImageLoad(filePath);
                imgPic.Source = bitmapImage;
                //});
                if (imgPic.Source == null) return false;
            }
            else
            {
                imgPic.Source = SctUtils.file_not_found; tbFile.Foreground = Brushes.Tomato;
            }
            lbNumber.Content = "[" + (index + 1).ToString() + "] ";  
            tbFile.Text = ii.filename; tbFile.ToolTip = filePath;
            lbRate.Content = (ii.rate == 0) ? "" : " {" + ii.rate.ToString() + "}";
            idx = index; 
            tbCue.Text = ii.prompt; tbCue.ToolTip = ii.prompt;
            selected = false; return !file_not_found;
        }
        private void imgPic_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Select(this, null);  
        }
        const int baseWidth = 100;
       
        const int baseHeight = 100;
        public void VisualUpdate() // by opts
        {
            opts.viewer.ThumbZoom = Utils.EnsureRange(opts.viewer.ThumbZoom, 30, 300);
            this.Width = baseWidth * opts.viewer.ThumbZoom / 100; this.Height = baseHeight * opts.viewer.ThumbZoom / 100;
            // top
            if (checkable || opts.viewer.ThumbCue)
            {
                if (opts.viewer.ThumbCue) CueRow.Height = new GridLength(40);
                else CueRow.Height = new GridLength(15);
            }
            else CueRow.Height = new GridLength(0); 
            if (checkable) { chkChecked.Visibility = Visibility.Visible; tbCue.Margin = new Thickness(15, 0, 0, 0); }
            else { chkChecked.Visibility = Visibility.Collapsed; tbCue.Margin = new Thickness(0); }
            if (opts.viewer.ThumbCue) tbCue.Visibility = Visibility.Visible;
            else tbCue.Visibility = Visibility.Collapsed;
            // bottom
            if (opts.viewer.ThumbFilename) { FileRow.Height = new GridLength(25); tbFile.Visibility = Visibility.Visible; }
            else { tbFile.Visibility = Visibility.Collapsed; FileRow.Height = new GridLength(0); }
            // frame
            if (!opts.viewer.ThumbCue && !opts.viewer.ThumbFilename) grid.Margin = new Thickness(6);
            else grid.Margin = new Thickness(3);
        }
    }
}
