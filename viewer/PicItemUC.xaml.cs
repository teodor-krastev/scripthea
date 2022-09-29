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
        public PicItemUC(ref Options _opts)
        {
            InitializeComponent();
            opts = _opts;
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
        public void Log(string msg)
        {
            Utils.TimedMessageBox(msg);
        }
        public void ContentUpdate(int index, string filePath, string _prompt)
        {
            _idx = index;
            filename = System.IO.Path.GetFileName(filePath);
            if (File.Exists(filePath))
            {
                imageFolder = System.IO.Path.GetDirectoryName(filePath) +"\\";               
                imgPic.Source = new BitmapImage(new Uri(filePath));
            }
            else lbFile.Foreground = Brushes.Tomato;
            lbFile.Content = filename;
            prompt = _prompt; tbCue.Text = prompt;            
            selected = false;
        }
        private void imgPic_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            Select(this, null);  
        }
        const int baseWidth = 80; const int baseHeight = 96;
        public void VisualUpdate() // by opts
        {
            opts.ThumbZoom = Utils.EnsureRange(opts.ThumbZoom, 30, 300);
            this.Width = baseWidth * opts.ThumbZoom / 100; this.Height = baseHeight * opts.ThumbZoom / 100;
            if (opts.ThumbCue) CueRow.Height = new GridLength(40);
            else CueRow.Height = new GridLength(1);
            if (opts.ThumbFilename) FileRow.Height = new GridLength(25);
            else FileRow.Height = new GridLength(1);
            if (!opts.ThumbCue && !opts.ThumbFilename) grid.Margin = new Thickness(6);
            else grid.Margin = new Thickness(3);
        }
    }
}
