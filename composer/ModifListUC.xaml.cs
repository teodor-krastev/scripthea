using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private Options opts;
        public ModifListUC(string _filename, ref Options _opts)
        {
            InitializeComponent();
            opts = _opts;
            OpenCat(_filename);
        }
        public bool isChecked
        {
            get { return chkCategory.IsChecked.Value && isVisible; }
            set { chkCategory.IsChecked = value; }
        }
        public bool isVisible
        {
            get { return Visibility.Equals(Visibility.Visible); }
            set
            {
                if (value) Visibility = Visibility.Visible;
                else
                {
                    Visibility = Visibility.Collapsed; isChecked = false;
                }
            }
        }
        public void SetHeaderPosition(bool first)
        {
            if (first) chkCategory.Margin = new Thickness(12, 0, 0, 0);
            else chkCategory.Margin = new Thickness(0);
        }
        public bool OpenCat(string _filename = "")
        {
            if (!_filename.Equals("")) filename = _filename; 
            if (!File.Exists(filename)) return false;
            modifListBox.Items.Clear();
            List<string> txtFile = new List<string>(File.ReadAllLines(filename, Encoding.ASCII));
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
                if (ss.StartsWith("["))
                {                    
                    char[] charsToTrim = { '[', ']' };
                    CategoryName = ss.Trim(charsToTrim);
                    isChecked = false;                   
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
                ModifItemUC mi = new ModifItemUC(ref opts) { Height = 20, Text = s0, FontSize = 13 };
                mi.Margin = new Thickness(0, 0, 0, 0); mi.tbContent.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(chkCategory_MouseRightButtonDown);
                if (!s1.Equals("")) mi.tbContent.ToolTip = s1; 
                mi.OnChange += new RoutedEventHandler(Change);
                mi.OnLog += new Utils.LogHandler(Log);
                modifListBox.Items.Add(mi); 
                i++;
            }
            return true;
        }
        public string CategoryName
        {
            get { return Convert.ToString(chkCategory.Content); }
            set { chkCategory.Content = value; }
        }
        public ModifItemUC modifByName(string modifName)
        {
            ModifItemUC rmdf = null;
            foreach (ModifItemUC mdf in modifList)
                if (mdf.Text.Equals(modifName)) { rmdf = mdf; break; }
            return rmdf;
        }
        public void Reset()
        {           
            foreach (ModifItemUC mdf in modifList) mdf.modifStatus = ModifStatus.Off;
            isChecked = false;
        }
        public event RoutedEventHandler OnChange;        
        protected void Change(object sender, RoutedEventArgs e)
        {        
            bool bb = false;     
            if (sender is ModifItemUC)
            {                            
                foreach (var mdf in modifList)
                    bb |= !mdf.modifStatus.Equals(ModifStatus.Off);  
                if (bb) isChecked = true;              
            }
            if (OnChange != null) OnChange(sender,e);             
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
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
            if (!e.Key.Equals(Key.Space) && !e.Key.Equals(Key.Enter) && !e.Key.Equals(Key.Left) && !e.Key.Equals(Key.Right)) return;
            if (Utils.isNull(modifListBox.SelectedItem)) return;
            (modifListBox.SelectedItem as ModifItemUC).SwitchState(e.Key.Equals(Key.Space) || e.Key.Equals(Key.Enter) || e.Key.Equals(Key.Left));
            e.Handled = true;
        }
        public int markWithWildcard(string searchPattern)
        {
            int cnt = 0;
            foreach (ModifItemUC mdf in modifList)
            {
                mdf.marked = Utils.IsWildCardMatch(mdf.Text, searchPattern);
                if (mdf.marked) cnt++;
            } 
            return cnt;
        }
        // https://stackoverflow.com/questions/30299671/matching-strings-with-wildcard
        public void demark()
        {
            foreach (ModifItemUC mdf in modifList)
                if (mdf.marked) mdf.marked = false;
        }
        public void removeByIdx(int idx)
        {
            if (!Utils.InRange(idx, 0, modifList.Count - 1)) return;
            modifListBox.Items.RemoveAt(idx);
        }

        public bool removeByText(string txt)
        {
            List<int> ls = new List<int>();
            for (int i = 0; i < modifList.Count; i++)
                if (txt.Equals(modifList[i].Text, StringComparison.InvariantCultureIgnoreCase)) ls.Add(i);
            for (int i = ls.Count-1; i > -1; i--)
            {
                modifList[ls[i]].Text = "";
                removeByIdx(ls[i]);
            }               
            return ls.Count > 0;
        }
    }
}
