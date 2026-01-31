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
    /// Interaction logic for mSetsUC.xaml
    /// </summary>
    public partial class mSetUC : UserControl
    {
        private string _title;
        private List<Tuple<string, string, ModifStatus>> _mSet;
        public string title
        {
            get { return _title; }
            set
            {
                if (ReadOnly) { Utils.TimedMessageBox(title + " is read only mSet."); return; }
                _title = value; tbTitle.Text = value;
            }
        }
        public bool isReset() { return title.Equals("Reset"); }
        public List<Tuple<string, string, ModifStatus>> mSet
        {
            get { return _mSet; }
            set
            {
                string tip = "";
                if (isReset()) { miReadOnly.IsChecked = true; miReadOnly.IsEnabled = false; tip = "Reset all modifs to unchecked"; }
                else { foreach (var mi in value) { tip += mi.Item2 + "; "; } }
                ToolTip = tip; 
                if (ReadOnly && !isReset()) Utils.TimedMessageBox(title + " is read-only mSet.");
                else _mSet = new List<Tuple<string, string, ModifStatus>>(value);
            }
        }
        public mSetUC()
        {
            InitializeComponent();
        }
        public mSetUC(string __title, List<Tuple<string, string, ModifStatus>> __mSet)
        {
            InitializeComponent();
            _ReadOnly = false; 
            title = __title;
            mSet = __mSet;
        }
        private bool _ReadOnly;
        public bool ReadOnly
        {
            get
            {
                if (title is null) return _ReadOnly;
                else return _ReadOnly || isReset();
            }
            set
            {
                _ReadOnly = value;
                if (value) Background = Brushes.LightYellow;
                else Background = null;
            }
        }
        private void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            switch (header)
            {
                case "Rename":
                    if (ReadOnly) { Utils.TimedMessageBox(title + " is read-only mSet."); return; }
                    string newItem = new InputBox("New mSet name", title, "Text input").ShowDialog();
                    if (!newItem.Equals("")) title = newItem;
                    break;
                case "Read-only":
                    ReadOnly = miReadOnly.IsChecked;
                    break;                
            }

        }
    }
}
