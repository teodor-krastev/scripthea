using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;
using UtilsNS;

// https://stackoverflow.com/questions/6415037/populate-treeview-from-list-of-file-paths-in-wpf

// display folders and subfolders in a treeview wpf c#

namespace UtilsNS
{
    /// <summary>
    /// Interaction logic for DirTreeUC.xaml
    /// </summary>
    public partial class DirTreeUC : UserControl
    {
        public DirTreeUC()
        {
            InitializeComponent();
        }
        public void Init()
        {
            foreach (string s in Directory.GetLogicalDrives())
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = s;
                item.Tag = s;
                cbDrives.Items.Add(item);
                cbDrives.SelectedIndex = 0;
            }
        }
        public delegate void SelectHandler(string path);
        public event SelectHandler OnSelect;
        protected void Select(string path)
        {
            if (OnSelect != null) OnSelect(path);
        }
        public delegate void ActiveHandler(string path);
        public event SelectHandler OnActive;
        protected void Active(string path)
        {
            if (OnActive != null) OnActive(path);
        }
        public bool IsImageDepot(string fld) // condition to get marked
        {
            return File.Exists(Path.Combine(fld, "description.txt"));
        }
        TreeViewItem dummyNode = null;         
        void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender; 
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);  // the name of the folder
                        subitem.Tag = s;                                        // the path to the folder
                        if (IsImageDepot(s))
                        {
                            subitem.FontWeight = FontWeights.Bold; subitem.Foreground = Brushes.OrangeRed;
                        }
                        else subitem.FontWeight = FontWeights.Normal;
                        bool bb = true; bool bc = true; ;
                        try
                        {                           
                            bc = Directory.GetDirectories(s).Length > 0;
                        }
                        catch { bb = false; }
                        if (!bb) continue;
                        if (bc) subitem.Items.Add(dummyNode);                        
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception ex) { Log(ex.Message); }
            }
        }
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string s = (cbDrives.SelectedItem as ComboBoxItem).Content.ToString();
            TreeViewItem item = new TreeViewItem();            
            item.Header = s;
            item.Tag = s;
            item.FontWeight = FontWeights.Normal;
            item.Items.Add(dummyNode);
            item.Expanded += new RoutedEventHandler(folder_Expanded);
            tvFolders.Items.Clear();
            tvFolders.Items.Add(item); 
            item.IsExpanded = true;            
        }
        private void tvFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = (TreeViewItem)e.NewValue;
            if (Utils.isNull(item)) return;
            string pth = item.Header.ToString(); 
            while (true)
            {
                if (!(item.Parent is TreeViewItem)) break;
                TreeViewItem prn = (TreeViewItem)item.Parent;
                pth = Path.Combine(prn.Header.ToString(),pth);
                item = prn;
            }                           
            Select(pth);
        }
        public void Log(string msg)
        {
            Utils.TimedMessageBox(msg);
        }
        public bool CatchAFolder(string pth)
        {
            bool bb = false; 
            List<string> fld = new List<string>(pth.Split('\\'));
            ComboBoxItem cbf = null;
            foreach (ComboBoxItem cbi in cbDrives.Items)
                if (cbi.Content.ToString().Equals(fld[0] + "\\")) { cbf = cbi; break; }
            if (cbf.Equals(null)) Log("Error: no drive: " + fld[0] + "\\");
            cbDrives.SelectedItem = cbf;
            TreeViewItem prn = (TreeViewItem)tvFolders.Items[0];
            for (int i = 1; i < fld.Count; i++)
            {
                bb = false;
                foreach (TreeViewItem item in prn.Items)
                {
                    if (Convert.ToString(item.Header).Equals(fld[i]))
                    {
                        item.IsExpanded = true; prn = item; bb = true; break;
                    }                         
                }
                if (!bb) Log("Error: no folder: " + fld[i]);
            }
            if (bb) prn.IsSelected = true;
            return bb;
        }
        private void tvFolders_KeyDown(object sender, KeyEventArgs e)
        {
            if (!tvFolders.SelectedItem.Equals(null) && e.Key.Equals(Key.Enter))
            {
                string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
                Active(pth); tbSelected.Text = pth;
            }                
        }
        private void tvFolders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!tvFolders.SelectedItem.Equals(null))
            {
                string pth = (tvFolders.SelectedItem as TreeViewItem).Tag.ToString();
                Active(pth); tbSelected.Text = pth;
            }
        }
    }
}
