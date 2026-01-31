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

namespace scripthea.preview
{
    /// <summary>
    /// Interaction logic for ScanPreviewUC.xaml
    /// </summary>
    public partial class ScanPreviewUC : UserControl, iPreview
    {
        public ScanPreviewUC()
        {
            InitializeComponent();
            allPrompts = new List<Tuple<string, string>>();
        }
        protected Options opts;
        public bool scanningFlag { get; set; }
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
        }
        public void Finish()
        {

        }
        protected DataTable dTable; protected List<CheckBox> checks;
        public int LoadPrompts(List<Tuple<string, string>> prompts)
        {
            if (prompts.Count == 0)
            {
                opts.Log("Error[742]: no prompts in the list"); return -1;
            }
            allPrompts = new List<Tuple<string, string>>(prompts);
            dTable = new DataTable(); checks = new List<CheckBox>(); 
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            dTable.Columns.Add(new DataColumn("On", typeof(bool)));
            dTable.Columns.Add(new DataColumn("Prompt", typeof(string)));
            dTable.BeginLoadData();
            for (int i = 0; i < prompts.Count; i++)
                dTable.Rows.Add(i + 1, true, prompts[i].Item1+ " " + prompts[i].Item2);
            dTable.EndLoadData();
            dGrid.ItemsSource = dTable.DefaultView;                      
            
            if (!this.IsVisible) return -1; //Utils.DoEvents();

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
        public int AppendPrompts(List<Tuple<string, string>> prompts)
        {
            List<Tuple<string, string>> lst = new List<Tuple<string, string>>(allPrompts);
            lst.AddRange(prompts);
            Clear();
            return LoadPrompts(lst);
        }
        public void Clear()
        {
            allPrompts.Clear();
            dTable = new DataTable(); checks = new List<CheckBox>();
        }
        public Tuple<string, string> decompPrompt(string prompt)
        {
            int k = prompt.IndexOf(opts.composer.ModifPrefix); string prm = ""; string mdf = "";
            if (k < 2) prm = prompt;
            else { prm = prompt.Substring(0, k - 1); mdf = prompt.Substring(k); }
            return new Tuple<string, string>(prm, mdf);
        }
        public List<Tuple<string, string>> GetPrompts(bool onlyChecked)
        {
            if (onlyChecked) return checkedPrompts();
            List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
            if (dTable is null) return ls;
            if (dTable.Rows is null) return ls;
            foreach (DataRow row in dTable.Rows)
                ls.Add(decompPrompt(Convert.ToString(row["Prompt"])));
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
        public void MenuCommand(string cmd)
        {
            if (cmd.Equals("Check with Mask or Range"))
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
            if (cmd.Equals("Remove Checked"))
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
                    switch (cmd)
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
                    }
                    if (turn2 != null) row["On"] = (bool)turn2;       
                }
            }
            chkTable_Checked(null, null);
        }
        public bool IsReadOnly 
        {
            get => promptCol.IsReadOnly;
            set => promptCol.IsReadOnly = value; 
        }
        private void chkTable_Checked(object sender, RoutedEventArgs e)
        {
            OnItemChanged?.Invoke(sender, e);
        }
        private DataGridTextColumn promptCol; 
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
                    promptCol = e.Column as DataGridTextColumn; if (Utils.isNull(promptCol)) return;
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    promptCol.ElementStyle = style; 
                    promptCol.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;
            }
        }
        private string _selectedPromptAsStr = "";
        public string selectedPromptAsStr { get => _selectedPromptAsStr; set => _selectedPromptAsStr = value; }
        public Tuple<string, string> selectedPrompt
        {
            get { return decompPrompt(selectedPromptAsStr); }
            //set { _selectedPrompt = value.Item1 + " " + value.Item2; }
        }
        public event RoutedEventHandler OnSelectionChanged;
        
        public event RoutedEventHandler OnItemChanged;
        
        private void dGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (converting) return;
            DataRowView dataRow = null; ;
            try
            {
               dataRow = (DataRowView)dGrid.SelectedItem;
            }
            catch(System.InvalidCastException ex) { selectedPromptAsStr = ""; }
            if (Utils.isNull(dataRow)) return;
            selectedPromptAsStr = Convert.ToString(dataRow.Row.ItemArray[2]);
            if (!Utils.isNull(e)) e.Handled = true;
            lastSelectedRow = dGrid.SelectedIndex;
            OnSelectionChanged?.Invoke(sender, e);
        }
        public List<Tuple<string, string>> allPrompts { get; private set; }
        int lastSelectedRow = -1;
        public List<Tuple<string, string>> checkedPrompts()
        {            
            List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
            if (dTable is null) return ls;
            if (dTable.Rows.Count == 0) return ls;
            int sr = lastSelectedRow;
            if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                if (chk != null) dTable.Rows[sr]["on"] = chk.IsChecked.Value;
            }                         
            foreach (DataRow row in dTable.Rows)
            {
                if (Convert.ToBoolean(row["On"]))
                    ls.Add(decompPrompt(Convert.ToString(row["Prompt"])));
            }
            return ls;
        }

        public bool IsValid(int idx)
        {
            if (dTable is null) return false;
            if (dTable.Rows is null) return false;
            if (!Utils.InRange(idx, 0, dTable.Rows.Count-1)) return false;
            DataRow row = dTable.Rows[idx];
            return Convert.ToBoolean(row["On"]);
        }
        public List<int> GetValidList() // 0 based
        {
            var ls = new List<int>();
            foreach (DataRow row in dTable.Rows)
            {
                if (Convert.ToBoolean(row["On"]))
                    ls.Add(Convert.ToInt32(row["#"])-1);
            }
            return ls;
        }
        public int selectedIdx { get => dGrid.SelectedIndex; }
        public int selectByIndex(int idx) // 0-based / idx -1 -> next checked to selected one
        {
            int k = idx;
            if (idx == -1)
            {
                k = dGrid.SelectedIndex + 1;
                if (!IsValid(k)) selectByIndex(-1);
            }
            if (DataGridHelper.SetFocusOnRow(dGrid, Utils.EnsureRange(k, 0, dTable.Rows.Count - 1)) is null) return -1;
            if (!dGrid.IsFocused) dGrid.Focus();
            if (dGrid.SelectedItem != null) dGrid.ScrollIntoView(dGrid.SelectedItem); 
            return k;            
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
