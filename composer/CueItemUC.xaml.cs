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
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
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
            List<string> lt = new List<string>();
            foreach(string ss in ls)
            {
                if (!ss.Trim().Equals(string.Empty)) lt.Add(ss.Trim());
            }
            return lt;
        }
    }
}

