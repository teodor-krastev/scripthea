using System;
using System.Collections.Generic;
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
using UtilsNS;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for OneSeedUC.xaml
    /// </summary>
    public partial class CueItemUC : UserControl
    {
        public CueItemUC()
        {
            InitializeComponent();
        }
        public bool ignoreTitles = true;
        public CueItemUC(string text, bool _radioChecked = false)
        {
            InitializeComponent(); radioChecked = _radioChecked;
            tbCue.Text = text.Trim().Trim('\"').Trim();
        }
        public CueItemUC(List<string> text, bool _radioChecked = false)
        {
            InitializeComponent(); radioChecked = _radioChecked;
            tbCue.Text = ""; 
            foreach (string line in text) tbCue.Text += line + Environment.NewLine;
            tbCue.Text = tbCue.Text.Trim().Trim('\"');
        }
        private int _index = 0; // zero based
        public int index 
        { 
            get { return _index; } 
            set { _index = value; gridFullFrame.Background = (index % 2).Equals(0) ? Utils.ToSolidColorBrush("#FFFFFEF5") /*yellowish*/ : Utils.ToSolidColorBrush("#FFF5F7EA"); } // greenish
        }        
        public bool radioMode
        {
            get { return rbChecked.Visibility.Equals(Visibility.Visible); }
            set
            {
                if (value)
                {
                    rbChecked.Visibility = Visibility.Visible;
                    checkBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    rbChecked.Visibility = Visibility.Collapsed;
                    checkBox.Visibility = Visibility.Visible;
                }
            }
        }
        public bool radioChecked
        {
            get { return rbChecked.IsChecked.Value; }
            set { rbChecked.IsChecked = value; }
        }
        public bool boxChecked
        {
            get { return checkBox.IsChecked.Value; }
            set { checkBox.IsChecked = value; }
        }
        public string cueText
        {
            get { return tbCue.Text.Trim().Trim('\"').Trim(); }
            set { tbCue.Text = value.Trim().Trim('\"').Trim(); }
        }
        public string titleText
        {
            get { return tbTitle.Text.Trim().Trim('\"').Trim(); }
            set 
            {
                if (ignoreTitles) return;
                string txt = value.Trim().Trim('\"').Trim();
                if (txt == "") tbTitle.Visibility = Visibility.Collapsed;
                else tbTitle.Visibility = Visibility.Visible;
                tbTitle.Text = txt; 
            }
        }
        public string subtitleText
        {
            get { return tbSubtitle.Text.Trim().Trim('\"').Trim(); }
            set 
            {
                if (ignoreTitles) return;
                string txt = value.Trim().Trim('\"').Trim();
                if (txt == "") tbSubtitle.Visibility = Visibility.Collapsed;
                else tbSubtitle.Visibility = Visibility.Visible;
                tbSubtitle.Text = txt; 
            }
        }
        public bool empty
        {
            get { return cueText.Trim().Equals(""); }
        }
        public string cueTextAsString(bool noComment)
        {
            return Utils.stringFlatTextBox(tbCue, noComment).Trim().Trim('\"').Trim();
        }
        public List<string> cueTextAsList(bool noComment)
        {
            cueText = cueText.Trim(); tbCue.UpdateLayout();
            List<string> ls = Utils.listFlatTextBox(tbCue, noComment);            
            return ls;
        }
    }
}

