using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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
using System.Windows.Controls.Primitives;
using scripthea.master;
using scripthea.options;
using UtilsNS;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for ScanPreviewUC.xaml
    /// </summary>
    public partial class ScanPreviewUC : UserControl
    {
        public ScanPreviewUC()
        {
            InitializeComponent();
            allPrompts = new List<string>();
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);          
        }
        DataTable dTable; List<CheckBox> checks;
        public void LoadPrompts(List<string> prompts)
        {
            if (prompts.Count == 0)
            {
                Log("Error[742]: no prompts in the list"); return;
            }
            allPrompts = new List<string>(prompts);
            dTable = new DataTable(); checks = new List<CheckBox>(); 
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            dTable.Columns.Add(new DataColumn("On", typeof(bool)));
            dTable.Columns.Add(new DataColumn("Prompt", typeof(string)));
            for (int i = 0; i < prompts.Count; i++)
                dTable.Rows.Add(i + 1, true, prompts[i]);
            dGrid.ItemsSource = dTable.DefaultView; 
            if (!this.IsVisible) return;
            Utils.DoEvents();

            //chkTable_Checked(null, null); return;
            
            for (int i = 0; i < prompts.Count; i++)
            {
                DataGridCell dgc = DataGridHelper.GetCellByIndices(dGrid, i, 1);
                if (Utils.isNull(dgc)) { Log("Error[449]: missing cell #" + i.ToString()); continue; }
                CheckBox chk = dgc.FindVisualChild<CheckBox>();
                if (Utils.isNull(chk)) { Log("Error[448]: missing check #" + i.ToString()); continue; }                
                chk.Checked += new RoutedEventHandler(chkTable_Checked); chk.Unchecked += new RoutedEventHandler(chkTable_Checked);
                checks.Add(chk);
            }                            
            if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
            Utils.DelayExec(1000, () => { chkTable_Checked(null, null); }); 
        }
        public void BindData()
        {
            Binding binding = new Binding("."); //ItemsSource="{Binding Path=., Mode=TwoWay}"  SourceUpdated="OnTargetUpdated"
            binding.BindsDirectlyToSource = true;
            binding.Mode = BindingMode.TwoWay;
            binding.Source = dTable;
            dGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
        }

        string bufMask = "";
        private void mi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            if (header.Equals("Check with Mask or Range"))
            { 
                bufMask = new InputBox("Check with Mask or Range [#..#] e.g.[3..8]", bufMask, "").ShowDialog(); string msk = bufMask.Trim();
                if (bufMask.Equals("")) return;
                int i0; int i1; (i0, i1) = SctUtils.rangeMask(msk, dTable.Rows.Count);
                if (!(i0 == -1 && i1 == -1))
                {
                    foreach (DataRow row in dTable.Rows)
                    {
                        int j = (int)row["#"];
                        row["On"] = Utils.InRange(j, i0,i1,true);
                    }
                    chkTable_Checked(null, null);
                    return;
                }
            }
            if (header.Equals("Remove Checked"))
            {
                for (int i = dTable.Rows.Count - 1; i >=0; i--)
                {
                    DataRow row = dTable.Rows[i];
                    if ((bool)row["On"]) dTable.Rows.RemoveAt(i);                    
                }                
            }
            else
            {
                foreach (DataRow row in dTable.Rows)
                {
                    bool? turn2 = null;
                    switch (header)
                    {
                        case "Check All":
                            turn2 = true;
                            break;
                        case "Uncheck All":
                            turn2 = false;
                            break;
                        case "Check with Mask":
                            string ss = Convert.ToString(row["Prompt"]);
                            turn2 = Utils.IsWildCardMatch(ss, bufMask);
                            break;
                        case "Invert Checking":
                            turn2 = !Convert.ToBoolean(row["On"]);
                            break;
                        case "Read Only":
                            col.IsReadOnly = miReadOnly.IsChecked;// to be dealt with later...
                            break;  
                    }
                    if (turn2 != null) row["On"] = (bool)turn2;       
                }
            }
            chkTable_Checked(null, null);
        }
        bool inverting = false;
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inverting = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    Utils.DelayExec(300, () => { btnMenu.ContextMenu.IsOpen = !inverting; });
                }
                if (e.ClickCount == 2)
                {
                    mi_Click(miInvertChecking, null);
                }
            }
        }
        private bool _scanning = false;
        public bool scanning
        {
            get
            {
                return _scanning;
            }
            set
            {
                if (value)
                {
                    btnScanChecked.Content = "Cancel Scan";
                    btnQuerySelected.IsEnabled = false; btnClose.IsEnabled = false;
                    btnScanChecked.Background = Brushes.Coral;
                }
                else
                {
                    btnScanChecked.IsEnabled = true; btnScanChecked.Content = "Scan All Checked";
                    btnQuerySelected.IsEnabled = true; btnClose.IsEnabled = true;
                    btnScanChecked.Background = Brushes.MintCream;
                }
                _scanning = value;
            }
        }
        private void chkTable_Checked(object sender, RoutedEventArgs e)
        {
            lbCheckCount.Content = checkedPrompts().Count.ToString() + " out of " + dTable.Rows.Count.ToString(); lbCheckCount.Foreground = Brushes.Navy;
        }
        private DataGridTextColumn col; 
        private void dGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.Column.Header.ToString())
            {
                case ("#"):
                case ("On"):
                    var cl = e.Column as DataGridTextColumn; if (Utils.isNull(cl)) return;
                    cl.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    break;
                case ("Prompt"):
                    col = e.Column as DataGridTextColumn; if (Utils.isNull(col)) return;
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    col.ElementStyle = style; col.IsReadOnly = miReadOnly.IsChecked;
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;
            }
        }
        private string _selectedPrompt = "";
        public string selectedPrompt
        {
            get { return _selectedPrompt; }
            set { _selectedPrompt = value; btnQuerySelected.IsEnabled = !value.Equals(""); }
        }
        private void dGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (converting) return;
            DataRowView dataRow = null; ;
            try
            {
               dataRow = (DataRowView)dGrid.SelectedItem;
            }
            catch(System.InvalidCastException ex) { selectedPrompt = ""; }
            if (Utils.isNull(dataRow)) return;
            selectedPrompt = Convert.ToString(dataRow.Row.ItemArray[2]);
            if (!Utils.isNull(e)) e.Handled = true;
            lastSelectedRow = dGrid.SelectedIndex;
        }
        public List<string> allPrompts { get; private set; }
        int lastSelectedRow = -1;
        public List<string> checkedPrompts()
        {
            int sr = lastSelectedRow;
            if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                if (chk != null) dTable.Rows[sr]["on"] = chk.IsChecked.Value;
            }
            List<string> ls = new List<string>();                
            foreach (DataRow row in dTable.Rows)
            {
                if (Convert.ToBoolean(row["On"]))
                    ls.Add(Convert.ToString(row["Prompt"]));
            }
            return ls;
        }
        public void selectByPropmt(string pr)
        {
            for (int i = 0; i < dTable.Rows.Count; i++)
            {
                TextBlock tb = DataGridHelper.GetCellByIndices(dGrid, i, 2).FindVisualChild<TextBlock>();
                if (Utils.isNull(tb)) continue; 
                if (tb.Text.Equals(pr))
                {
                    dGrid.SelectedIndex = i; dGrid.Focus();
                    btnQuerySelected.IsEnabled = !scanning; return;
                }
            }
        }
        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            List<string> ls = checkedPrompts(); string ss = "";
            foreach (var s in ls) ss += s + Environment.NewLine;
            Clipboard.SetText(ss);
        }
        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {                    
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Prompts (.txt)|*.txt"; // Filter files by extension
            //dlg.InitialDirectory = Controller.scriptListPath;
            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result != true) return;
            Utils.writeList(dlg.FileName, checkedPrompts());
        }
        private void lbCheckCount_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            chkTable_Checked(null, null);
        }
    }
}
