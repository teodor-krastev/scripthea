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
            dTable = new DataTable();
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            dTable.Columns.Add(new DataColumn("Cue", typeof(string)));
            dTable.Columns.Add(new DataColumn("File", typeof(string)));

            InitializeComponent();
        }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        public void Finish()
        {

        }
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        private string _imageFolder;
        public string imageFolder { get { return _imageFolder; } }
        public void FeedList(List<Tuple<int, string, string>> theList, string imageDepot)
        {
            _imageFolder = imageDepot;
            dTable.Rows.Clear();
            foreach (var itm in theList)
            {
                dTable.Rows.Add(itm.Item1, itm.Item3, itm.Item2);
            }
            dGrid.ItemsSource = dTable.DefaultView;
            if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0; 
        }
        public int selectedIndex 
        {
            get { return dGrid.SelectedIndex+1; }
            set { if (Utils.InRange(value-1, 0,dTable.Rows.Count-1)) dGrid.SelectedIndex = value-1; } 
        }
        public int Count { get { return dTable.Rows.Count; } }
        public List<Tuple<int, string, string>> items 
        { 
            get 
            {
                List<Tuple<int, string, string>> itm = new List<Tuple<int, string, string>>();
                foreach (DataRow row in dTable.Rows)
                    itm.Add(new Tuple<int, string, string>(Convert.ToInt32(row.ItemArray[0]), Convert.ToString(row.ItemArray[2]), Convert.ToString(row.ItemArray[1])));
                return itm;
            } 
        }
        public delegate void PicViewerHandler(int idx, string filePath, string cue);
        public event PicViewerHandler SelectEvent;
        protected void OnSelect(int idx, string filePath, string cue)
        {
            if (SelectEvent != null) SelectEvent(idx, filePath, cue);
        }
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {   
            var col = e.Column as DataGridTextColumn;
            switch (e.Column.Header.ToString())
            {
                case ("#"): case ("File"):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    break;
                case ("Cue"):
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    col.ElementStyle = style;
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;                
            }
       }
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
            OnSelect(iRow + 1, imageFolder + Convert.ToString(dTable.Rows[iRow].ItemArray[2]), Convert.ToString(dTable.Rows[iRow].ItemArray[1]) );
        }
    }
}
