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
using UtilsNS;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for PicItemUC.xaml
    /// </summary>
    public partial class PicItemUC : UserControl
    {
        public PicItemUC(ref Options _opts, bool __checkable)
        {
            InitializeComponent();
            opts = _opts; checkable = __checkable;
        }
        Options opts;
        private int _idx;
        public int idx { get { return _idx; } }
        private bool _selected;
        public bool selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (value)
                {
                    Background = Brushes.RoyalBlue;
                    tbCue.Foreground = Brushes.White; lbFile.Foreground = Brushes.White;
                }
                else
                {
                    Background = Brushes.White;
                    tbCue.Foreground = Brushes.Black; lbFile.Foreground = Brushes.Black;
                }
            }
        }
        private bool _checkable;
        public bool checkable
        {
            get { return _checkable; }
            set
            { 
                _checkable = value; VisualUpdate();
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
        public void ContentUpdate(int index, string filePath, string _prompt)
        {
            _idx = index;
            filename = System.IO.Path.GetFileName(filePath);
            if (File.Exists(filePath))
            {
                imageFolder = System.IO.Path.GetDirectoryName(filePath) +"\\";               
                imgPic.Source = ImgUtils.UnhookedImageLoad(filePath);
            }
            else lbFile.Foreground = Brushes.Tomato;
            lbFile.Text = filename;
            prompt = _prompt; tbCue.Text = prompt;            
            selected = false;
        }
        private void imgPic_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Select(this, null);  
        }
        const int baseWidth = 100; const int baseHeight = 100;
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
