using System;
using System.Collections.Generic;
using System.IO;
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

namespace scripthea.composer
{
   
    /// <summary>
    /// Interaction logic for OneModifUC.xaml
    /// </summary>
    public partial class ModifListUC : UserControl
    {
        public List<ModifItemUC> modifList
        {
            get 
            { 
                List<ModifItemUC> ml = new List<ModifItemUC>();
                foreach (ModifItemUC mdf in modifListBox.Items)
                    ml.Add(mdf);
                return ml;
            }
        }           
        public ModifListUC()
        {
            InitializeComponent();
        }
        public string filename;
        public ModifListUC(string _filename)
        {
            InitializeComponent();
            OpenCat(_filename);
        }
        public bool enabled
        {
            get { return chkCategory.IsChecked.Value; }
            set { chkCategory.IsChecked = value; }
        }
        public bool OpenCat(string _filename)
        {
            filename = _filename; if (!File.Exists(_filename)) return false;           
            List<string> txtFile = new List<string>(File.ReadAllLines(_filename));
            List<string> modiFile = new List<string>();
            foreach (string ss in txtFile)
            {
                string st = ss.Trim();
                if (!st.Equals("")) modiFile.Add(st);
            }
            int i = 0;
            while (i < modiFile.Count)
            {
                string ss = modiFile[i].Trim();
                if (ss.Equals("")) continue;
                if (ss[0].Equals('['))
                {                    
                    char[] charsToTrim = { '[', ']' };
                    catName = ss.Trim(charsToTrim);
                    enabled = false;                   
                    i++; continue;
                }
                string[] sa = ss.Split('=');
                string s0 = ""; string s1 = "";
                switch (sa.Length)
                {
                    case 0: continue;
                    case 1:
                        s0 = sa[0].Trim();
                        break;
                    case 2:
                        s0 = sa[0].Trim(); s1 = sa[1].Trim();
                        break;
                }
                ModifItemUC mi = new ModifItemUC() { Height = 24, Text = s0, FontSize = 13 };
                mi.Margin = new Thickness(0, 0, 0, 0); mi.tbContent.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(chkCategory_MouseRightButtonDown);
                if (!s1.Equals("")) mi.tbContent.ToolTip = s1; 
                mi.OnChange += new RoutedEventHandler(Change);
                modifListBox.Items.Add(mi); 
                i++;
            }
            return true;
        }
        public string catName
        {
            get { return Convert.ToString(chkCategory.Content); }
            set { chkCategory.Content = value; }
        }
        public event RoutedEventHandler OnChange;        
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected void Change(object sender, RoutedEventArgs e)
        {
            if ((OnChange != null) && enabled) OnChange(sender,e);
        }

        private void chkCategory_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            string input = new InputBox("Ask Google about", (string)chkCategory.Content, "").ShowDialog();
            if (input.Equals("")) return;
            Utils.AskTheWeb(input, false);            
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            string smi = Convert.ToString(mi.Header).Substring(8).TrimEnd('>');
            Utils.AskTheWeb(smi);
        }

        private void modifListBox_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key.Equals(Key.Space)) { chkCategory.IsChecked = !chkCategory.IsChecked.Value; }
            if (!e.Key.Equals(Key.Enter) && !e.Key.Equals(Key.Left) && !e.Key.Equals(Key.Right)) return;
            if (Utils.isNull(modifListBox.SelectedItem)) return;
            (modifListBox.SelectedItem as ModifItemUC).SwitchState(e.Key.Equals(Key.Enter) || e.Key.Equals(Key.Left));
        }
    }
}
