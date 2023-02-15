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
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.viewer
{
    /// <summary>
    /// Interaction logic for TableViewUC.xaml
    /// </summary>
    public partial class TableViewUC : UserControl, iPicList
    {
        private DataTable dTable;
        public TableViewUC()
        {
            InitializeComponent();
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
            dTable.Columns.Add(new DataColumn("Image Filename", typeof(string)));
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
        public void BindData()
        {
            Binding binding = new Binding("."); //ItemsSource="{Binding Path=., Mode=TwoWay}"  SourceUpdated="OnTargetUpdated"
            binding.BindsDirectlyToSource = true;
            binding.Mode = BindingMode.TwoWay;
            binding.Source = dTable;
            dGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
        }
        public DepotFolder iDepot { get; set; }
        public string loadedDepot { get; set; }
        public void UpdateVis()
        {
            if (dTable == null) return;
            dTable.Rows.Clear(); 
            if (!IsAvailable) return;
            if (iDepot.items.Count > 0)
            {
                foreach (var itm in iDepot.Export2Viewer())
                {
                    if (checkable) dTable.Rows.Add(itm.Item1, true, itm.Item3, itm.Item2);
                    else dTable.Rows.Add(itm.Item1, itm.Item3, itm.Item2);
                }
                BindData();
            }
            else dGrid.ItemsSource = null;            
        }
        public void SynchroChecked(List<Tuple<int, string, string>> chks)
        {
            if (!checkable) return;
            SetChecked(false);
            foreach (Tuple<int, string, string> chk in chks)
                dTable.Rows[chk.Item1-1]["on"] = true;
        }
        public void SetChecked(bool? check)
        {
            if (!checkable) return;
            foreach (DataRow row in dTable.Rows)
            {                
                if (check == null) row["on"] = !Convert.ToBoolean(row["on"]);
                else row["on"] = Convert.ToBoolean(check);
            }            
        }
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        
        public string imageFolder { get { return iDepot?.path; } }
        public void Clear(bool inclDepotItems = false)
        {
            dTable?.Rows?.Clear();  
            if (inclDepotItems) iDepot?.items.Clear();                      
        }
        public bool FeedList(string imageDepot)
        {
            if (dTable == null) return false;
            Clear();
            if (!Directory.Exists(imageDepot)) { Log("Err: no such folder -> " + imageDepot); return false; }
            //if (ImgUtils.checkImageDepot(imageDepot) == 0) { Log("Err: not image depot folder -> " + imageDepot); return false; }
            DepotFolder _iDepot = new DepotFolder(imageDepot, ImageInfo.ImageGenerator.FromDescFile);
            return FeedList(ref _iDepot);        
        }
        public bool FeedList(ref DepotFolder _iDepot) // update from existitng iDepot
        {
            if ((dTable == null) || (_iDepot == null)) return false;
            if (!Directory.Exists(_iDepot.path)) { Log("Err: no such folder -> " + _iDepot.path); return false; }
            iDepot = _iDepot; loadedDepot = iDepot.path; 
            UpdateVis();
            if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
            return true;
        }
        public int selectedIndex 
        {
            get { return dGrid.SelectedIndex + 1; }
            set { if (Utils.InRange(value-1, 0,dTable.Rows.Count-1)) dGrid.SelectedIndex = value - 1; } 
        }
        public int Count { get { return dTable.Rows.Count; } }
        public List<Tuple<int, string, string>> GetItems(bool check, bool uncheck) 
        {
            if (!checkable && iDepot.isEnabled) { return iDepot.Export2Viewer(); }
            List<Tuple<int, string, string>> itms = new List<Tuple<int, string, string>>();
            if (dTable.Rows.Count == 0) return itms;

            int sr = lastSelectedRow; 
            if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                if (chk!= null) dTable.Rows[sr]["on"] = chk.IsChecked.Value;
            }
            foreach (DataRow row in dTable.Rows)
            {
                if (Convert.ToBoolean(row.ItemArray[1]))
                {
                    if (check) itms.Add(new Tuple<int, string, string>(Convert.ToInt32(row.ItemArray[0]), Convert.ToString(row.ItemArray[3]), Convert.ToString(row.ItemArray[2])));
                }
                else
                {
                    if (uncheck) itms.Add(new Tuple<int, string, string>(Convert.ToInt32(row.ItemArray[0]), Convert.ToString(row.ItemArray[3]), Convert.ToString(row.ItemArray[2])));
                }
            }
            return itms;
        }
        public delegate void PicViewerHandler(int idx, string filePath, string prompt);
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, string filePath, string prompt)
        {
            if (SelectEvent != null) SelectEvent(idx, filePath, prompt);
        }
        public event RoutedEventHandler OnChangeContent;
        protected void ChangeContent(object sender, RoutedEventArgs e)
        {
            if (sender is DataRow)
            {

            }
            if (OnChangeContent != null) OnChangeContent(sender, e);
        }
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {   
            var col = e.Column as DataGridTextColumn; if (col == null) return;
            col.IsReadOnly = !e.Column.Header.ToString().Equals("on");
            switch (e.Column.Header.ToString())
            {
                case ("#"):
                case ("on"):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    break;
                case ("Image Filename"):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
                    break;
                case ("Prompt"):
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    col.ElementStyle = style;
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;                
            }
        }
        int lastSelectedRow = -1;
        private void dGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)dGrid.SelectedItem;
            if (Utils.isNull(dataRow)) return;
            dGrid.Dispatcher.InvokeAsync(() =>
            {
                dGrid.UpdateLayout();
                dGrid.ScrollIntoView(dGrid.SelectedItem, null);
            });
            int iRow = Convert.ToInt32(dataRow.Row.ItemArray[0]) - 1;
            if (checkable) OnSelect(iRow + 1, Path.Combine(imageFolder, Convert.ToString(dTable.Rows[iRow].ItemArray[3])), Convert.ToString(dTable.Rows[iRow].ItemArray[2]));
            else OnSelect(iRow + 1, Path.Combine(imageFolder, Convert.ToString(dTable.Rows[iRow].ItemArray[2])), Convert.ToString(dTable.Rows[iRow].ItemArray[1]));
            if (!Utils.isNull(e)) e.Handled = true;
            lastSelectedRow = dGrid.SelectedIndex;
        }

        private void dGrid_KeyDown(object sender, KeyEventArgs e)
        {
            int sr = dGrid.SelectedIndex;
            if (e.Key.Equals(Key.Space) && Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                var chk = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<CheckBox>();
                if (chk != null) chk.IsChecked = !chk.IsChecked.Value;      
                OnSelect(sr, Path.Combine(imageFolder, Convert.ToString(dTable.Rows[sr].ItemArray[3])), ""); 
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
        }
    }
}
