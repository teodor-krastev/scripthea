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

namespace scripthea.cuestuff
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
        public CueItemUC(string text, bool _radioChecked = false)
        {
            InitializeComponent(); radioChecked = _radioChecked;
            tbCue.Text = text.Trim().Trim('\"').Trim();
        }
        public CueItemUC(List<string> text, bool _radioChecked = false)
        {
            InitializeComponent(); radioChecked = _radioChecked;
            tbCue.Text = ""; int k = 0;
            foreach (string line in text)
            {
                if (line.StartsWith("#") && k == 0) { headerText = line.Trim('#').Trim(); k++; continue; }
                if (line.StartsWith("#") && k == text.Count - 1) { footerText = line.Trim('#').Trim(); break; }
                tbCue.Text += line + Environment.NewLine; k++;
            }               
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
        private bool _showMeta = true; 
        public bool showMeta 
        { 
            get { return _showMeta; }
            set { _showMeta = value; headerText = tbHeader.Text;  footerText = tbFooter.Text;  }
        }
        public string headerText
        {
            get { return tbHeader.Text.Trim(new char[] { '{', '}', '#' }).Trim(); }
            set 
            {
                string txt = value.Trim(new char[] { '{', '}', '#' }).Trim();
                if (txt != string.Empty && showMeta) tbHeader.Visibility = Visibility.Visible;
                else tbHeader.Visibility = Visibility.Collapsed;
                tbHeader.Text = "# " + txt; 
            }
        }
        public string footerText
        {
            get { return tbFooter.Text.Trim(new char[] { '{', '}', '#' }).Trim(); }
            set 
            {
                string txt = value.Trim(new char[] {'{','}','#'}).Trim();
                if (txt != string.Empty && showMeta) tbFooter.Visibility = Visibility.Visible;
                else tbFooter.Visibility = Visibility.Collapsed;
                tbFooter.Text = "# " + txt; 
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

