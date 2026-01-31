using scripthea.master;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using scripthea.options;
using UtilsNS;
using System.Globalization;

namespace scripthea.viewer
{
    public static class MarkMask { public static string Value { get; set; } }
    public class MarkConditionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string prompt = value as string;
            if (string.IsNullOrEmpty(MarkMask.Value)) return new SolidColorBrush(Colors.White);
            if (!string.IsNullOrEmpty(prompt) && Utils.IsWildCardMatch(prompt, MarkMask.Value))
                return new SolidColorBrush(Colors.MintCream);
            return new SolidColorBrush(Colors.White);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Interaction logic for TableViewUC.xaml
    /// </summary>
    public partial class TableViewUC : UserControl, iPicList
    {
        private DataTable dTable; private const string FilenameHeader = "_     Image  Filename     _";
        public TableViewUC()
        {
            InitializeComponent(); MarkMask.Value = "";
        }
        Options opts;
        public bool checkable { get; private set; }
        public void Init(ref Options _opts, bool _checkable)
        {
            opts = _opts; checkable = _checkable; dGrid.IsReadOnly = !checkable;

            dTable = new DataTable();
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            if (checkable)
                dTable.Columns.Add(new DataColumn("on", typeof(bool)));
            dTable.Columns.Add(new DataColumn("Prompt", typeof(string)));
            dTable.Columns.Add(new DataColumn("Rt", typeof(int)));
            dTable.Columns.Add(new DataColumn(FilenameHeader, typeof(string)));
        }
        public void Finish()
        {

        }
        public bool IsAvailable
        {
            get
            {
                if (Utils.isNull(iDepot)) return false;
                else return iDepot.isEnabled;
            }
        }       
        public bool HasTheFocus { get; set; }
        public void BindData()
        {
            Binding binding = new Binding("."); //ItemsSource="{Binding Path=., Mode=TwoWay}"  SourceUpdated="OnTargetUpdated"
            binding.BindsDirectlyToSource = true;
            binding.Mode = BindingMode.TwoWay;
            binding.Source = dTable;
            dGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
        }
        public ImageDepot iDepot { get; set; }
        private class Undo // now - only for table view; later - maybe unify with grid view
        {
            private ImageDepot imageDepot; private int idx; private bool inclFile; private BitmapImage bitmapImage;
            private ImageInfo ii;
            public Undo(ImageDepot _imageDepot, int idx0, bool _inclFile) // buffer the entry when created
            {
                imageDepot = _imageDepot; idx = idx0; inclFile = _inclFile;
                if (Utils.InRange(idx, 0, imageDepot.items.Count - 1))
                    ii = imageDepot.items[idx]; // by ref ?                
            }
            public event Utils.LogHandler OnLog;
            protected void Log(string txt, SolidColorBrush clr = null)
            {
                if (OnLog != null) OnLog(txt, clr);
            }
            public void ClearState()
            {
                ii = null; idx = -1; bitmapImage = null;
            }
            private bool checkOut()
            {
                if (imageDepot is null) return false;
                if (!imageDepot.isEnabled) return false;
                if (ii is null) return false;
                if (!Utils.InRange(idx, 0, imageDepot.items.Count - 1)) return false;
                return true;
            }
            public bool recover2ImageDepot() // get it back
            {
                if (!checkOut()) return false;
                imageDepot.items.Insert(idx, ii); imageDepot.Save();
                if (inclFile && bitmapImage != null) ImgUtils.SaveBitmapImageToDisk(bitmapImage, Path.Combine(imageDepot.path, ii.filename));
                ClearState();
                return true;
            }
            public void realRemove() // get it back
            {
                if (inclFile && checkOut())
                {
                    string fn = Path.Combine(imageDepot.path, ii.filename);
                    if (File.Exists(fn)) { bitmapImage = inclFile ? ImgUtils.UnhookedImageLoad(Path.Combine(fn)) : null; File.Delete(fn); }
                    else Log("Error: File <" + fn + "> does not exist");
                }
            }
        }
        private Undo undo = null;

        public void RemoveAt(bool inclFile, int idx = -1)
        {
            undo = new Undo(iDepot, idx, inclFile); //undo.OnLog += new Utils.LogHandler(Log);
            undo.realRemove(); // 
        }
        public bool Recover()
        {
            if (undo is null) return false;
            return undo.recover2ImageDepot();
        }
        public string loadedDepot { get; set; }
        private bool updating = false;
        public void UpdateVis()
        {
            if (dTable is null) return;
            try
            {
                updating = true;
                dTable.Rows.Clear(); 
                if (!IsAvailable) return;
                if (iDepot.items.Count > 0)
                {
                    foreach (var itm in iDepot.Export2Viewer())
                    {
                        if (checkable) dTable.Rows.Add(itm.Item1 + 1, true, itm.Item2, itm.Item3, itm.Item4);
                        else dTable.Rows.Add(itm.Item1 + 1, itm.Item2, itm.Item3, itm.Item4);
                    }
                    BindData();
                    selectedIndex = 0;
                }
                else dGrid.ItemsSource = null;  
            }
            finally { updating = false; }
        }        
        public void UpdateVisRecord(int idx, ImageInfo ii) // update visual record from ii
        {
            if (iDepot is null) return;
            if (!Utils.InRange(idx, 0, iDepot.items.Count - 1) || ii is null) return;
            iDepot.items[idx] = ii; dTable.Rows[idx]["Rt"] = ii.rate;
        }
        private bool synchronizing = false;
        public void SynchroChecked(List<Tuple<int, string, int, string>> chks)
        {
            if (!checkable) return;
            try
            {
                synchronizing = true; SetChecked(false);
                foreach (Tuple<int, string, int, string> chk in chks)
                {               
                    int idx = chk.Item1;
                    if (Utils.InRange(idx, 0, dTable.Rows.Count - 1)) dTable.Rows[idx]["on"] = true;
                }               
            }
            finally { synchronizing = false; chkCheckedColumn(null, null); }
        }
        public void SetChecked(bool? check) // null -> invert
        {
            if (!checkable) return;
            foreach (DataRow row in dTable.Rows)
            {                
                if (check is null) row["on"] = !Convert.ToBoolean(row["on"]);
                else row["on"] = Convert.ToBoolean(check);
            }
            //BindData(); dGrid.UpdateLayout();
        }
                
        public string imageFolder { get { return iDepot?.path; } }

        public string markMask { get { return MarkMask.Value; } }
        public void CheckRange(int first, int last)
        {
            foreach (DataRow row in dTable.Rows)
                if (checkable) row["on"] = Utils.InRange(Convert.ToInt32(row["#"]), first,last);
        }
        public void CheckRate(double rate)
        {
            foreach (DataRow row in dTable.Rows)
                if (checkable) row["on"] = Convert.ToInt32(row["Rt"]) > rate;
        }
        private void ForcedViewUpdate() // artificial but effective
        {
            foreach (DataRow row in dTable.Rows)
            {
                string prompt = Convert.ToString(row["Prompt"]);
                if (prompt.EndsWith(" ")) row["Prompt"] = prompt.TrimEnd();
                else row["Prompt"] = prompt + " ";
            }
        }
        public int MarkWithMask(string mask)  
        {
            MarkMask.Value = ""; int k = 0; // reset
            if (checkable)
            {
                foreach (DataRow row in dTable.Rows)
                {
                    string prompt = Convert.ToString(row["Prompt"]);
                    bool bb = mask.Equals("") ? false : Utils.IsWildCardMatch(prompt, mask);
                    row["on"] = Convert.ToBoolean(bb); if (bb) k++;
                }
            }
            else 
            {
                foreach (DataRow row in dTable.Rows)
                {
                    string prompt = Convert.ToString(row["Prompt"]);
                    bool bb = mask.Equals("") ? false : Utils.IsWildCardMatch(prompt, mask);
                    if (bb) k++;
                }
                if (mask != "") MarkMask.Value = mask; ForcedViewUpdate(); 
            }
            dGrid.UpdateLayout();
            return k;
        }
        public void Clear(bool inclDepotItems = false)
        {
            dTable?.Rows?.Clear();  
            if (inclDepotItems) iDepot?.items.Clear();
            undo?.ClearState();
        }
        public bool FeedList(string imageDepot, bool force)
        {
            if (dTable is null) return false;
            Clear();
            if (!Directory.Exists(imageDepot)) { opts.Log("Error[651]: no such folder -> " + imageDepot); return false; }
            //if (ImgUtils.checkImageDepot(imageDepot) == 0) { Log("Error[]: not image depot folder -> " + imageDepot); return false; }
            ImageDepot _iDepot = new ImageDepot(imageDepot, ImageInfo.ImageGenerator.FromDescFile);
            return FeedList(ref _iDepot, force);        
        }
        public bool FeedList(ref ImageDepot _iDepot, bool force) // update from existitng iDepot
        {
            if ((dTable is null) || (_iDepot is null)) return false;
            if (!Directory.Exists(_iDepot.path)) { opts.Log("Error[486]: no such folder -> " + _iDepot.path); return false; }
            iDepot = _iDepot; loadedDepot = iDepot.path; 
            UpdateVis(); 
            if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
            if (checkable)
                for (int i = 0; i < dTable.Rows.Count; i++)
                {
                    CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, i, 1).FindVisualChild<CheckBox>();
                    if (Utils.isNull(chk)) continue;
                    chk.Name = "chkList" + i.ToString();
                    chk.Tag = i;
                    chk.Checked += new RoutedEventHandler(chkCheckedColumn); chk.Unchecked += new RoutedEventHandler(chkCheckedColumn);                
                }
            return true;
        }
        private void chkCheckedColumn(object sender, RoutedEventArgs e)
        {
            if (!synchronizing) ChangeContent(this, null);
        }
        public int selectedIndex // 0 based
        {
            get 
            {
                if ((dTable.Rows.Count == 0) || (dGrid.SelectedIndex == -1)) return - 1;
                return Convert.ToInt32(dTable.Rows[dGrid.SelectedIndex]["#"]) - 1;
            }
            set 
            {
                if (dTable is null) return;
                if (!Utils.InRange(value, 0, dTable.Rows.Count - 1)) return;
                dGrid.Focus(); DataGridHelper.SetFocusOnRow(dGrid, value);
                //dGrid.SelectedIndex = value - 1; DataRowView drv = (DataRowView)dGrid.SelectedItem; 
            }
        }
        public int Count { get { if (dTable is null) return 0; return dTable.Rows.Count; } }
        public int CountChecked
        {
            get
            {
                int cnt = 0;
                if (!checkable) return 0;
                int sr = lastSelectedRow;
                if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
                {
                    CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                    if (chk != null) dTable.Rows[sr]["on"] = chk.IsChecked.Value;
                }
                foreach (DataRow row in dTable.Rows)
                    if (Convert.ToBoolean(row.ItemArray[1])) cnt++;
                return cnt;
            }
        }
        public List<Tuple<int, string, int, string>> GetItems(bool check, bool uncheck) 
        {
            List<Tuple<int, string, int, string>> itms = new List<Tuple<int, string, int, string>>();
            if (iDepot is null || dTable.Rows.Count == 0) return itms;
            if (dTable.Rows.Count == 0) return itms;
            int sr = lastSelectedRow; 
            if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                if (chk!= null) dTable.Rows[sr]["on"] = chk.IsChecked.Value;
            }           
            foreach (DataRow row in dTable.Rows)
            {
                if (checkable)
                {
                    bool bb = false;
                    if (Convert.ToBoolean(row.ItemArray[1]))
                    {
                        bb = check; 
                    }
                    else
                    {
                        bb = uncheck;
                    }
                    if (bb) itms.Add(new Tuple<int, string, int, string>(Convert.ToInt32(row.ItemArray[0])-1, Convert.ToString(row.ItemArray[2]), Convert.ToInt32(row.ItemArray[3]), Convert.ToString(row.ItemArray[4])));
                }
                else itms.Add(new Tuple<int, string, int, string>(Convert.ToInt32(row.ItemArray[0]) - 1, Convert.ToString(row.ItemArray[1]), Convert.ToInt32(row.ItemArray[2]), Convert.ToString(row.ItemArray[3])));
            }
            return itms;
        }
        public delegate void PicViewerHandler(int idx, ImageDepot _iDepot); // idx - 0 based
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, ImageDepot _iDepot)
        {
            if (SelectEvent != null) SelectEvent(idx, _iDepot);
        }
        public event RoutedEventHandler OnChangeContent;
        protected void ChangeContent(object sender, RoutedEventArgs e)
        {
            if (OnChangeContent != null) OnChangeContent(sender, e);
        }
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {   
            var col = e.Column as DataGridTextColumn; if (col is null) return;
            col.IsReadOnly = !e.Column.Header.ToString().Equals("on");
            switch (e.Column.Header.ToString())
            {                
                case ("#"):                                            
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells); //SizeToCells, SizeToHeader
                    break;
                case ("on"):
                case ("Rt"):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                    break;
                case ("Prompt"):
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    col.ElementStyle = style;
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;   
                case (FilenameHeader):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                    break;
            }
        }
        protected ImageInfo SelectedItem(int idx) // idx - 0 based
        {
            ImageInfo ii = null;
            if (iDepot != null)
                if (iDepot.isEnabled && Utils.InRange(idx, 0, iDepot.items.Count-1))
                    ii = iDepot.items[idx];
            return ii;
        }
        int lastSelectedRow = -1;
        public void dGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {  
            try
            {
                if (Utils.isNull(dGrid.SelectedItem) || updating) return;
                DataRowView dataRow = (DataRowView)dGrid.SelectedItem; 
            
                dGrid.Dispatcher.InvokeAsync(() =>
                {
                    dGrid.UpdateLayout();
                    dGrid.ScrollIntoView(dGrid.SelectedItem, null);
                });
                int iRow = Convert.ToInt32(dataRow.Row.ItemArray[0]) - 1; // shift 0 based
                ImageInfo ii = SelectedItem(iRow);
                if (ii is null) { Utils.TimedMessageBox("Unknown image"); return; }
                if (checkable) OnSelect(iRow, iDepot);
                else OnSelect(iRow, iDepot);
                if (!Utils.isNull(e)) e.Handled = true;
                lastSelectedRow = dGrid.SelectedIndex;
            }
            finally { if (!Utils.isNull(e)) e.Handled = true; }
        }
        private void dGrid_KeyDown(object sender, KeyEventArgs e)
        {
            int sr = dGrid.SelectedIndex;
            if (e.Key.Equals(Key.Space) && Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                if (checkable)
                {
                    var chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                    if (chk != null) chk.IsChecked = !chk.IsChecked.Value;
                    ChangeContent(this, null);  //OnSelect(sr, imageFolder, SelectedItem(sr-1));                    
                }
                else dGrid.SelectedIndex = Utils.EnsureRange(sr + 1, 0, dTable.Rows.Count - 1); 
                e.Handled = true;
            }           
        }
        private void dGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)  // TO BE FINISHED !!!
        {
            //ChangeContent(this, null);
            //bool bb = Convert.ToBoolean(e.Row.ItemArray[1]);
        }
        private void dGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Home)) { dGrid.SelectedIndex = 0; e.Handled = true; }
            if (e.Key.Equals(Key.End)) { dGrid.SelectedIndex = Count-1; e.Handled = true; }
            if (e.Key.Equals(Key.Delete)) { e.Handled = true; } 
        }
        public void SortTableByIndex()
        {
            if (dGrid.Columns.Count == 0) return;
            if (dGrid.Columns[0].SortDirection is null) DataGridHelper.SortDataGrid(dGrid); 
        }
        public void focusFirstRow() // for some reason it is more complicated than it should be
        {
            if (dTable is null) return;
            if (dTable.Rows.Count == 0) return;
            dGrid.UpdateLayout(); // Ensure the DataGrid layout is updated            
            dGrid.SelectedIndex = 0; // Select the first row
            dGrid_SelectionChanged(dGrid, null);
            dGrid.ScrollIntoView(dGrid.Items[0]);
            DataGridRow firstRow = (DataGridRow)dGrid.ItemContainerGenerator.ContainerFromIndex(0);
            if (firstRow != null)
            {
                firstRow.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
            // Assuming your DataGrid is named 'myDataGrid'
            if (PresentationSource.FromVisual(dGrid) is null) return;
            KeyEventArgs args = new KeyEventArgs(Keyboard.PrimaryDevice,
                                                PresentationSource.FromVisual(dGrid),
                                                0,
                                                Key.Home); // Or any other key you want
            args.RoutedEvent = Keyboard.KeyDownEvent;
            dGrid.RaiseEvent(args);

        }
        private void tableViewUC_GotFocus(object sender, RoutedEventArgs e)
        {
            HasTheFocus = true;
        }
        private void tableViewUC_LostFocus(object sender, RoutedEventArgs e)
        {
            HasTheFocus = false;
        }
    }
}
