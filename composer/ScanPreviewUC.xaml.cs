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
        protected Options opts;
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
        }

        protected DataTable dTable; protected List<CheckBox> checks;
        public int LoadPrompts(List<string> prompts)
        {
            if (prompts.Count == 0)
            {
                opts.Log("Error[742]: no prompts in the list"); return -1;
            }
            allPrompts = new List<string>(prompts);
            dTable = new DataTable(); checks = new List<CheckBox>(); 
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            dTable.Columns.Add(new DataColumn("On", typeof(bool)));
            dTable.Columns.Add(new DataColumn("Prompt", typeof(string)));
            for (int i = 0; i < prompts.Count; i++)
                dTable.Rows.Add(i + 1, true, prompts[i]);
            dGrid.ItemsSource = dTable.DefaultView; 
            if (!this.IsVisible) return -1;
            Utils.DoEvents();

            //chkTable_Checked(null, null); return;
            
            for (int i = 0; i < prompts.Count; i++)
            {
                DataGridCell dgc = DataGridHelper.GetCellByIndices(dGrid, i, 1);
                if (Utils.isNull(dgc)) { /*opts.Log("Error[449]: missing cell #" + i.ToString());*/ continue; }
                CheckBox chk = dgc.FindVisualChild<CheckBox>();
                if (Utils.isNull(chk)) { /*opts.Log("Error[448]: missing check #" + i.ToString());*/ continue; }                
                chk.Checked += new RoutedEventHandler(chkTable_Checked); chk.Unchecked += new RoutedEventHandler(chkTable_Checked);
                checks.Add(chk);
            }                            
            if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
            Utils.DelayExec(1000, () => { chkTable_Checked(null, null); });
            return dTable.Rows.Count;
        }
        public List<string> GetPrompts(bool onlyChecked)
        {
            if (onlyChecked) return checkedPrompts();
            List<string> ls = new List<string>();
            if (dTable == null) return ls;
            if (dTable.Rows == null) return ls;
            foreach (DataRow row in dTable.Rows)
                ls.Add(Convert.ToString(row["Prompt"]));
            return ls;
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
                    btnScanChecked.Background = Utils.ToSolidColorBrush("#FFFED17F"); 
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
        public void selectByIndex(int idx) // 0-based
        {
            if (DataGridHelper.SetFocusOnRow(dGrid, Utils.EnsureRange(idx, 0, dTable.Rows.Count - 1)) == null) return;
            if (!dGrid.IsFocused) dGrid.Focus();
            if (dGrid.SelectedItem != null) dGrid.ScrollIntoView(dGrid.SelectedItem);
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
/*
 Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Upon the bus ride that new day, the clouds blossomed pink as if in visual empathy with the poppy red paint below.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Pink lemonade clouds wrap the sky as if Earth were a child's party gift.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Nights of cloud could tuck me in as sure as a babe into softest quilt, for in the cooler weather were days of hearth and harkened heart.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The cloudy night is a million promise-notes of heaven's rain, each of them in graphite grey and etched upon the sky.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The cloudy night comes quietly with the gentle spirit of mother.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The cloudy night softens the vibrant colours of day to soft pastel hues, the kind that awaken the soul and let it stay in serene comfort.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
As we are tucked into the heavenly black to dream each night anew, the cloudy grey makes nighttime all the cosier still.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
A cloudy night is but the promise of much needed rain, and in this we are blessed.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Today's sky is a blue-grey brindle with the softest accents of white.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Tucked beneath the woollen grey sky, there is a sweet warmth to the horse. He is at home here upon the heathered moor.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The sky invites the eyes to play as ever arcing birds upon wing.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Come silver-greys or floral-blues, every sky speaks to the artistic inner eye.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Sky expanded above as an ever-growing dream.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Sky arcs heavenward as the greatest basilica cupola.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The breath of sweet nature plays in the blue, up here in the sky that hugs valleys and mountains just the same.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The sky is dappled by the cloud, a beauty over our motley crew. So we rest on our backs and let our eyes gaze upward, enjoying the nothing that is everything.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
A grey sky swirl of stoic gravitas becomes the backing of those in hero stride.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
In the dawn light the charcoal sky became a wispy silver, as if it was determined to become its own treasure.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Even upon this wintry day, when the sky is a woollen grey shawl upon mountain peaks, I feel our happy memories become a glowing sense of warmth.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The blue sky is my mother's eyes; it is the light that dances in the inbetween, the precious time when night is suspended.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The depth of the sky's blue is as our love, that only over the years to we notice the strength of the hue. Up close it is as clear as pure water, yet when we see the miles it is the blue of fairytale dreams.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
This blue sky is the echo of my porcelain soul: tough, humble and pretty. It takes on the subtle changes as the day matures, an ever evolving artistic palate.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
Sunlight after the rain gave the skylight edge a watercolour halo.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The skylight was a mosaic of colour, as if it had blossomed from seeds of light.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Jaroslaw Jasnikowski
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Vladimir Kush
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Vladimir Kush
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Wes Anderson
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Mark Chagall; Wes Anderson
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Jaroslaw Jasnikowski; Wes Anderson
The skylight brought swirls of colour to the rooftop, as if all the room were nature's cradle.; Jacek Yerka ; minimalism ; simplicity ; Vladimir Kush; Wes Anderson
 
 
 */
