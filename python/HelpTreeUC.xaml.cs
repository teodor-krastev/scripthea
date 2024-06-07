using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Windows.Media;
using UtilsNS;

namespace scripthea.python
{
    /// <summary>
    /// Interaction logic for TreeViewUC.xaml  // TreeView with TextBox content wpf c# example 
    /// </summary>
    public partial class HelpTreeUC : UserControl
    {
        private List<TextBlock> tbList;
        public HelpTreeUC()
        {
            InitializeComponent(); tbList = new List<TextBlock>(); 
        } 
        public void SetModuleHelp(string moduleName, List<Tuple<string, string>> help)
        {
            if (moduleName.Equals("")) return;
            TreeViewItem moduleNode = new TreeViewItem() { Header = moduleName , Foreground = Brushes.Maroon };
            if (help == null) return;
            moduleNode.IsExpanded = true;
            foreach (Tuple<string, string> tp in help)
            {
                TreeViewItem tviM = new TreeViewItem();
                tviM.Header = tp.Item1;
                moduleNode.Items.Add(tviM);
                TreeViewItem tviD = new TreeViewItem();
                tbList.Add(new TextBlock
                {
                    Text = tp.Item2, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Navy,
                });
                tviD.Header = tbList[tbList.Count - 1];
                tviM.Items.Add(tviD); 
            }
            HelpTree.Items.Add(moduleNode);
        }
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            // Prevent the item from being selected
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null)
            {
                item.IsSelected = false;
                e.Handled = true; // Stop the event from propagating further
            }
        }
        private void HelpTree_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double nw = Utils.EnsureRange(HelpTree.ActualWidth - 70, 50, 500);
            foreach (TextBlock tb in tbList)
            {
                tb.Width = nw;
            }
        }
    }
}
