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
using scripthea.master;
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
            InitializeComponent();
            opts = _opts; checkable = __checkable;
            status = new HashSet<statusStates>();
        }
        Options opts;      
        public int idx { get; set; }

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
                if (focused) Background = Brushes.RoyalBlue;  
                else Background = Brushes.Gray;
                tbCue.Foreground = Brushes.White; tbFile.Foreground = Brushes.White;
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
                tbCue.Foreground = Brushes.Black; tbFile.Foreground = Brushes.Black;
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
        public string imageFolder, filename, prompt;
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public void Clear()
        {
            imgPic.Source = null;   
        }
        public bool ContentUpdate(int index, string filePath, string _prompt)
        {
            idx = index;
            filename = System.IO.Path.GetFileName(filePath);
            if (File.Exists(filePath))
            {
                //imgPic.Dispatcher.InvokeAsync(() => // slower for some reason !
                //{
                    imageFolder = System.IO.Path.GetDirectoryName(filePath) +"\\";
                    imgPic.Source = ImgUtils.UnhookedImageLoad(filePath);                              
                //});
                if (imgPic.Source == null) return false;  
            }
            else tbFile.Foreground = Brushes.Tomato; 
            tbFile.Text = filename; tbFile.ToolTip = filePath;
            prompt = _prompt; tbCue.Text = prompt; tbCue.ToolTip = prompt;
            selected = false; return true;
        }
        private void imgPic_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Select(this, null);  
        }
        const int baseWidth = 100;
       
        const int baseHeight = 100;
        public void VisualUpdate() // by opts
        {
            opts.ThumbZoom = Utils.EnsureRange(opts.ThumbZoom, 30, 300);
            this.Width = baseWidth * opts.ThumbZoom / 100; this.Height = baseHeight * opts.ThumbZoom / 100;
            // top
            if (checkable || opts.ThumbCue)
            {
                if (opts.ThumbCue) CueRow.Height = new GridLength(40);
                else CueRow.Height = new GridLength(15);
            }
            else CueRow.Height = new GridLength(1);
            if (checkable) { chkChecked.Visibility = Visibility.Visible; tbCue.Margin = new Thickness(15, 0, 0, 0); }
            else { chkChecked.Visibility = Visibility.Collapsed; tbCue.Margin = new Thickness(0); }
            if (opts.ThumbCue) tbCue.Visibility = Visibility.Visible;
            else tbCue.Visibility = Visibility.Collapsed;
            // bottom
            if (opts.ThumbFilename) FileRow.Height = new GridLength(25);
            else FileRow.Height = new GridLength(1);
            // frame
            if (!opts.ThumbCue && !opts.ThumbFilename) grid.Margin = new Thickness(6);
            else grid.Margin = new Thickness(3);
        }
    }
}
