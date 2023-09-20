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
        public List<Tuple<string, string, ModifStatus>> mSet
        {
            get { return _mSet; }
            set 
            { 
                if (ReadOnly) { Utils.TimedMessageBox(title + " is read only mSet."); return; }
                _mSet = new List<Tuple<string, string, ModifStatus>>(value);
                string tip = title.Equals("Reset")? "Reset all modifs to unchecked" : ""; 
                foreach(var mi in value) { tip += mi.Item2 + "; "; }
                ToolTip = tip;
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
            _mSet = __mSet;
        }
        private bool _ReadOnly;
        public bool ReadOnly
        {
            get 
            {
                if (title == null) return _ReadOnly;
                else return _ReadOnly || title.Equals("Reset"); 
            }
            set 
            { 
                _ReadOnly = value;
                if (value) Background = Brushes.LightYellow;
                else Background = null;
            }
        }
    }
}
