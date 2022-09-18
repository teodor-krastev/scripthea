using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using UtilsNS;

namespace scripthea
{
    public class Category
    {
        public string title;
        public string descriptor;
        public bool enabled;
        public List<Tuple<string, string, bool>> content;
    }
    /// <summary>
    /// Interaction logic for OneModifUC.xaml
    /// </summary>
    public partial class CatModifUC : UserControl
    {
        private Category _ctg;
        public Category ctg
        {
            get 
            {
                updateToCtg();
                return _ctg; 
            }
            set 
            {
                _ctg = new Category();
                _ctg.title = value.title;
                _ctg.descriptor = value.descriptor;
                _ctg.enabled = value.enabled;
                _ctg.content = new List<Tuple<string, string, bool>>(value.content);

                updateFromCtg();
            }
        }
        public CatModifUC()
        {
            InitializeComponent();
        }
        public CatModifUC(Category __ctg)
        {
            InitializeComponent();
            ctg = __ctg;
        }
        public event RoutedEventHandler OnChange;        
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected void Change(object sender, RoutedEventArgs e)
        {
            if ((OnChange != null)) OnChange(sender,e);
        }

        private void updateFromCtg()
        {
            chkCategory.Content = _ctg.title;
            chkCategory.Checked += new RoutedEventHandler(Change); chkCategory.Unchecked += new RoutedEventHandler(Change);
            chkCategory.IsChecked = _ctg.enabled;
            modifListBox.Items.Clear();
            foreach (var mdf in _ctg.content)
            {
                CheckBox chk = new CheckBox(); chk.Foreground = Brushes.Navy; 
                chk.Checked += new RoutedEventHandler(Change); chk.Unchecked += new RoutedEventHandler(Change);
                chk.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(chkCategory_MouseRightButtonDown);
                chk.Content = mdf.Item1; chk.IsChecked = mdf.Item3;
                if (!mdf.Item2.Equals("")) chk.ToolTip = mdf.Item2; 
                modifListBox.Items.Add(chk);
            }
        }
        private void updateToCtg()
        {
            _ctg.enabled = chkCategory.IsChecked.Value;
            _ctg.content = new List<Tuple<string, string, bool>>();
            foreach (CheckBox mdf in modifListBox.Items)
            {
                _ctg.content.Add(new Tuple<string, string, bool>(Convert.ToString(mdf.Content), Convert.ToString(mdf.ToolTip), mdf.IsChecked.Value));
            }
        }

        private void chkCategory_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            CheckBox chk = null;
            if (sender is CheckBox)
            {
                chk = sender as CheckBox;                    
            }
            if (Utils.isNull(chk)) return;
            string input = new InputBox("Ask Google about", (string)chk.Content, "").ShowDialog();
            if (input.Equals("")) return;
            Utils.AskTheWeb(input);            
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            string smi = Convert.ToString(mi.Header).Substring(8).TrimEnd('>');
            Utils.AskTheWeb(smi);
        }  

    }
}
