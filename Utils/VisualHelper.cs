using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Windows.Input;

namespace UtilsNS
{   
    public static class VisualHelper
    {
        public static bool IsUserVisible(this UIElement element)
        {
            if (!element.IsVisible)
                return false;
            var container = VisualTreeHelper.GetParent(element) as FrameworkElement;
            if (container == null) throw new ArgumentNullException("container");

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.RenderSize.Width, element.RenderSize.Height));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }
        public static T FindVisualChild<T>(this Visual parent) where T : Visual
        {
            List<T> childs = new List<T>();

            return GetVisualChild(parent, true, ref childs);
        }
        public static IEnumerable<T> FindVisualChilds<T>(this Visual parent) where T : Visual
        {
            List<T> childs = new List<T>();
            GetVisualChild(parent, false, ref childs);
            return childs;
        }
        public static T FindVisualChildByName<T>(this Visual parent, string name) where T : Visual
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<T> childs = new List<T>();

            return GetVisualChild(parent, true, ref childs, true, name);
        }
        private static T GetVisualChild<T>(this Visual parent, bool getOnlyOnechild, ref List<T> list, bool findByName = false, string childName = "") where T : Visual
        {
            T child = default(T);
            if (parent == null) return child;
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                Visual visualChild = (Visual)VisualTreeHelper.GetChild(parent, index);
                child = visualChild as T;

                if (child == null)
                    child = GetVisualChild<T>(visualChild, getOnlyOnechild, ref list);//Find Recursively

                if (child != null)
                {
                    if (getOnlyOnechild)
                    {
                        if (findByName)
                        {
                            var element = child as FrameworkElement;
                            if (element != null && element.Name == childName)
                                break;
                        }
                        else
                            break;
                    }
                    else
                        list.Add(child);
                }
            }
            return child;
        }
        /*public static DataGridCell GetCell(this DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                return cell;
            }
            return null;
        }*/
        public static T GetChildObjectOfTypeInVisualTree<T>(DependencyObject dpob) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(dpob);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(dpob, i);
                T childAsT = child as T;
                if (childAsT != null)
                {
                    return childAsT;
                }
                childAsT = GetChildObjectOfTypeInVisualTree<T>(child);
                if (childAsT != null)
                {
                    return childAsT;
                }
            }
            return null;
        }
        public static bool SelectItemInCombo(ComboBox cb, string txt)
        {
            int idx = -1; string ss = "";
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is ComboBoxItem) ss = (cb.Items[i] as ComboBoxItem).Content as string;
                else ss = cb.Items[i] as string;
                if (txt.Equals(ss, StringComparison.InvariantCultureIgnoreCase)) { idx = i; break; }
            }               
            cb.SelectedIndex = idx;
            return idx != -1;
        }
    }
    //================================================================================================
    public static class DataGridHelper
    {
        public static DataGridRow SetFocusOnRow(this DataGrid dataGrid, int rowIndex)
        {
            // Check if the rowIndex is within the range of the DataGrid items
            if (rowIndex >= 0 && rowIndex < dataGrid.Items.Count)
            {
                // Select the row
                dataGrid.SelectedIndex = rowIndex;

                // Get the DataGridRow object
                DataGridRow row = GetRowByIndex(dataGrid, rowIndex);

                // If the row is not yet generated, scroll into view and wait for it to be generated
                if (row == null)
                {
                    dataGrid.ScrollIntoView(dataGrid.Items[rowIndex]);
                    dataGrid.UpdateLayout();
                    row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
                }
                // Set focus on the row
                if (row != null)
                {
                    row.Focus();
                }
                return row;
            }
            return null;
        }
        public static DataGridColumn GetColumnByIndices(this DataGrid dataGrid, int rowIndex, int columnIndex)
        {
            if (dataGrid == null)
                return null;

            //Validate Indices
            ValidateColumnIndex(dataGrid, columnIndex);
            ValidateRowIndex(dataGrid, rowIndex);

            var row = dataGrid.GetRowByIndex(rowIndex);

            if (row != null)//Get Column for the DataGridRow by index using GetRowColumnByIndex Extension methods
                return row.GetRowColumnByIndex(dataGrid, columnIndex);

            return null;
        }
        public static DataGridCell GetCellByIndices(this DataGrid dataGrid, int rowIndex, int columnIndex)
        {
            if (dataGrid == null)
                return null;

            //Validate Indices
            ValidateColumnIndex(dataGrid, columnIndex);
            ValidateRowIndex(dataGrid, rowIndex);

            var row = dataGrid.GetRowByIndex(rowIndex);

            if (row == null)
            {
                object item = dataGrid.Items[rowIndex]; //dataGrid.UpdateLayout();
                dataGrid.ScrollIntoView(item); //dataGrid.UpdateLayout(); 
                row = dataGrid.GetRowByIndex(rowIndex);
                if (row != null) return row.GetCellByColumnIndex(dataGrid, columnIndex);
            }
            else            
                return row.GetCellByColumnIndex(dataGrid, columnIndex);

            return null;
        }
        public static DataGridRow GetRowByIndex(this DataGrid dataGrid, int rowIndex)
        {
            if (dataGrid == null)
                return null;

            ValidateRowIndex(dataGrid, rowIndex);

            return (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
        }
        public static DataGridColumn GetRowColumnByIndex(this DataGridRow row, DataGrid dataGrid, int columnIndex)
        {

            if (row == null || dataGrid == null)
                return null;

            ValidateColumnIndex(dataGrid, columnIndex);

            var cell = GetCellByColumnIndex(row, dataGrid, columnIndex);

            if (cell != null)
                return cell.Column;

            return null;
        }
        public static DataGridCell GetCellByColumnIndex(this DataGridRow row, DataGrid dataGrid, int columnIndex)
        {
            if (row == null || dataGrid == null)
                return null;

            ValidateColumnIndex(dataGrid, columnIndex);

            DataGridCellsPresenter cellPresenter = row.FindVisualChild<DataGridCellsPresenter>();

            if (cellPresenter != null)
                return ((DataGridCell)cellPresenter.ItemContainerGenerator.ContainerFromIndex(columnIndex));

            return null;
        }
        public static T GetUIElementOfCell<T>(this DataGrid dataGrid, int rowIndex, int columnIndex) where T : Visual
        {
            var cell = GetCellByIndices(dataGrid, rowIndex, columnIndex);

            if (cell != null)
                return cell.FindVisualChild<T>();

            return null;
        }
        public static IEnumerable<T> GetUIElementsOfCell<T>(this DataGrid dataGrid, int rowIndex, int columnIndex) where T : Visual
        {
            var cell = GetCellByIndices(dataGrid, rowIndex, columnIndex);

            if (cell != null)
                return cell.FindVisualChilds<T>();

            return null;
        }
        public static T GetUIElementOfCell<T>(this DataGridCell dataGridCell) where T : Visual
        {
            if (dataGridCell == null)
                return null;

            return dataGridCell.FindVisualChild<T>();
        }
        private static void ValidateColumnIndex(DataGrid dataGrid, int columnIndex)
        {
            if (columnIndex >= dataGrid.Columns.Count)
                throw new IndexOutOfRangeException("columnIndex out of range");
        }
        private static void ValidateRowIndex(DataGrid dataGrid, int rowIndex)
        {
            if (rowIndex >= dataGrid.Items.Count)
                throw new IndexOutOfRangeException("rowIndex out of range");
        }
        public static void SortDataGrid(DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            var column = dataGrid.Columns[columnIndex];

            // Clear current sort descriptions
            dataGrid.Items.SortDescriptions.Clear();

            // Add the new sort description
            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            // Apply sort
            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }
            column.SortDirection = sortDirection;

            // Refresh items to display sort
            dataGrid.Items.Refresh();
        }
    }
    //------------------------------------------------------------------------------------------
    public class ExtendedDataGrid : DataGrid
    {
        public DataGridRow this[int rowIndex]
        {
            get { return DataGridHelper.GetRowByIndex(this as DataGrid, rowIndex); }
        }
        public DataGridColumn this[int rowIndex, int columnIndex]
        {
            get { return DataGridHelper.GetColumnByIndices(this as DataGrid, rowIndex, columnIndex); }
        }
        public DataGridCell GetCellOfColumn(int rowIndex, int columnIndex)
        {
            return this.GetCellByIndices(rowIndex, columnIndex);
        }
        public T GetVisualElementOfCell<T>(int rowIndex, int columnIndex) where T : Visual
        {
            return this.GetCellByIndices(rowIndex, columnIndex).FindVisualChild<T>();
        }
        public IEnumerable<T> GetVisualElementsOfCell<T>(int rowIndex, int columnIndex) where T : Visual
        {
            return this.GetCellByIndices(rowIndex, columnIndex).FindVisualChilds<T>();
        }
    }
    //#######################################################################################
    public static class callPython
    {   
        public static string run_cmd(string cmd, string args)
        {
            // https://stackoverflow.com/questions/67320619/how-can-i-call-python-script-in-c
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "PATH_TO_PYTHON_EXE";
            start.Arguments = string.Format("\"{0}\" \"{1}\"", cmd, args);
            start.UseShellExecute = false;// Do not use OS shell
            start.CreateNoWindow = true; // We don't need new window
            start.RedirectStandardOutput = true;// Any output, generated by application will be redirected back
            start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
                    string result = reader.ReadToEnd(); // Here is the result of StdOut(for example: print "test")
                    return result;
                }
            }
        }
    }
    //IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII
    // example use filename = new InputBox("Cues filename in the active cues pool", fn, "").ShowDialog();
    public class InputBox
    {
        Window Box = new Window();//window for the inputbox
        FontFamily font = new FontFamily("Segoe UI");//font for the whole inputbox
        int FontSize = 12;//fontsize for the input
        Grid grid = new Grid();// items container
        string title = "Message";//title as heading
        string boxcontent;//title, if windows type allows !
        string defaulttext = "";//default textbox content
        string errormessage = "Invalid text";//error messagebox content
        string errortitle = "Error";//error messagebox heading title
        string okbuttontext = "OK";//Ok button content
        string CancelButtonText = "Cancel";
        System.Windows.Media.Brush BoxBackgroundColor = System.Windows.Media.Brushes.WhiteSmoke;// Window Background
        System.Windows.Media.Brush InputBackgroundColor = System.Windows.Media.Brushes.MintCream;// Textbox Background
        bool clickedOk = false;
        TextBox input = new TextBox();
        Button ok = new Button();
        Button cancel = new Button();
        bool inputreset = false;
        public InputBox(string DefaultText)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            title = "Message";
            windowdef();
        }

        public InputBox(string Htitle, string DefaultText, string boxContent)
        {
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                boxcontent = boxContent;
            }
            catch { boxcontent = "Error!"; }
            windowdef();
        }

        public InputBox(string Htitle, string DefaultText, string Font, int Fontsize)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                font = new FontFamily(Font);
            }
            catch { font = new FontFamily("Tahoma"); }
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            if (Fontsize >= 1)
                FontSize = Fontsize;
            windowdef();
        }
        private void windowdef()// window building - check only for window size
        {
            Box.Height = 120;// Box Height
            Box.Width = 450;// Box Width
            Box.Background = BoxBackgroundColor;
            Box.Title = title;
            Box.Content = grid;
            Box.Closing += Box_Closing;
            Box.WindowStyle = WindowStyle.None;
            Box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            TextBlock header = new TextBlock();
            header.TextWrapping = TextWrapping.Wrap;
            header.Background = null;
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.VerticalAlignment = VerticalAlignment.Top;
            header.FontFamily = font;
            header.FontSize = FontSize;
            header.Margin = new Thickness(10, 10, 10, 10);
            header.Text = title;
            grid.Children.Add(header);

            input.Background = InputBackgroundColor;
            input.FontFamily = font;
            input.FontSize = FontSize;
            input.Height = 25;
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            input.VerticalAlignment = VerticalAlignment.Top;
            input.Margin = new Thickness(10, 33, 10, 10);
            input.MinWidth = 200;
            input.MouseEnter += input_MouseDown;
            input.KeyDown += input_KeyDown;
            input.Text = defaulttext;
            grid.Children.Add(input);

            ok.Width = 65;
            ok.Height = 25;
            ok.HorizontalAlignment = HorizontalAlignment.Right;
            ok.VerticalAlignment = VerticalAlignment.Bottom;
            ok.Margin = new Thickness(0, 0, 10, 10);
            ok.Click += ok_Click;
            ok.Content = okbuttontext;

            cancel.Width = 65;
            cancel.Height = 25;
            cancel.HorizontalAlignment = HorizontalAlignment.Right;
            cancel.VerticalAlignment = VerticalAlignment.Bottom;
            cancel.Margin = new Thickness(0, 0, 85, 10);
            cancel.Click += cancel_Click;
            cancel.Content = CancelButtonText;

            grid.Children.Add(ok);
            grid.Children.Add(cancel);

            input.Focus();
        }
        void Box_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //validation
        }
        private void input_MouseDown(object sender, MouseEventArgs e)
        {
            if ((sender as TextBox).Text == defaulttext && inputreset)
            {
                (sender as TextBox).Text = null;
                inputreset = true;
            }
        }
        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && clickedOk == false)
            {
                e.Handled = true;
                ok_Click(input, null);
            }
            if (e.Key == Key.Escape)
            {
                cancel_Click(input, null);
            }
        }
        void ok_Click(object sender, RoutedEventArgs e)
        {
            clickedOk = true;
            if (input.Text == "")
                System.Windows.MessageBox.Show(errormessage, errortitle, MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                Box.Close();
            }
            clickedOk = false;
        }
        void cancel_Click(object sender, RoutedEventArgs e)
        {
            input.Text = "";
            Box.Close();
        }
        public string ShowDialog()
        {
            Box.ShowDialog();
            return input.Text;
        }
    }
    //MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM
    // TO BE FINISHED
    public class MiniTimedMessage
    {
        Window Box = new Window();//window for the inputbox
        FontFamily font = new FontFamily("Segoe UI");//font for the whole inputbox
        int FontSize = 12;//fontsize for the input
        Grid grid = new Grid();// items container
        string title = "Message";//title as heading
        string boxcontent;//title, if windows type allows !
        string defaulttext = "";//default textbox content
        string errormessage = "Invalid text";//error messagebox content
        string errortitle = "Error";//error messagebox heading title
        string okbuttontext = "OK";//Ok button content
        string CancelButtonText = "Cancel";
        System.Windows.Media.Brush BoxBackgroundColor = System.Windows.Media.Brushes.WhiteSmoke;// Window Background
        System.Windows.Media.Brush InputBackgroundColor = System.Windows.Media.Brushes.MintCream;// Textbox Background
        bool clickedOk = false;
        TextBox input = new TextBox();
        Button ok = new Button();
        Button cancel = new Button();
        bool inputreset = false;
        public MiniTimedMessage(string DefaultText)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            title = "Message";
            windowdef();
        }
        public MiniTimedMessage(string Htitle, string DefaultText, string boxContent)
        {
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                boxcontent = boxContent;
            }
            catch { boxcontent = "Error!"; }
            windowdef();
        }
        public MiniTimedMessage(string Htitle, string DefaultText, string Font, int Fontsize)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                font = new FontFamily(Font);
            }
            catch { font = new FontFamily("Tahoma"); }
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            if (Fontsize >= 1)
                FontSize = Fontsize;
            windowdef();
        }
        private void windowdef()// window building - check only for window size
        {
            Box.Height = 120;// Box Height
            Box.Width = 450;// Box Width
            Box.Background = BoxBackgroundColor;
            Box.Title = title;
            Box.Content = grid;
            Box.Closing += Box_Closing;
            Box.WindowStyle = WindowStyle.None;
            Box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            TextBlock header = new TextBlock();
            header.TextWrapping = TextWrapping.Wrap;
            header.Background = null;
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.VerticalAlignment = VerticalAlignment.Top;
            header.FontFamily = font;
            header.FontSize = FontSize;
            header.Margin = new Thickness(10, 10, 10, 10);
            header.Text = title;
            grid.Children.Add(header);

            input.Background = InputBackgroundColor;
            input.FontFamily = font;
            input.FontSize = FontSize;
            input.Height = 25;
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            input.VerticalAlignment = VerticalAlignment.Top;
            input.Margin = new Thickness(10, 33, 10, 10);
            input.MinWidth = 200;
            input.MouseEnter += input_MouseDown;
            input.KeyDown += input_KeyDown;
            input.Text = defaulttext;
            grid.Children.Add(input);

            ok.Width = 65;
            ok.Height = 25;
            ok.HorizontalAlignment = HorizontalAlignment.Right;
            ok.VerticalAlignment = VerticalAlignment.Bottom;
            ok.Margin = new Thickness(0, 0, 10, 10);
            ok.Click += ok_Click;
            ok.Content = okbuttontext;

            cancel.Width = 65;
            cancel.Height = 25;
            cancel.HorizontalAlignment = HorizontalAlignment.Right;
            cancel.VerticalAlignment = VerticalAlignment.Bottom;
            cancel.Margin = new Thickness(0, 0, 85, 10);
            cancel.Click += cancel_Click;
            cancel.Content = CancelButtonText;

            grid.Children.Add(ok);
            grid.Children.Add(cancel);

            input.Focus();
        }
        void Box_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //validation
        }
        private void input_MouseDown(object sender, MouseEventArgs e)
        {
            if ((sender as TextBox).Text == defaulttext && inputreset)
            {
                (sender as TextBox).Text = null;
                inputreset = true;
            }
        }
        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && clickedOk == false)
            {
                e.Handled = true;
                ok_Click(input, null);
            }
            if (e.Key == Key.Escape)
            {
                cancel_Click(input, null);
            }
        }
        void ok_Click(object sender, RoutedEventArgs e)
        {
            clickedOk = true;
            if (input.Text == "")
                System.Windows.MessageBox.Show(errormessage, errortitle, MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                Box.Close();
            }
            clickedOk = false;
        }
        void cancel_Click(object sender, RoutedEventArgs e)
        {
            input.Text = "";
            Box.Close();
        }
        public string ShowDialog()
        {
            Box.ShowDialog();
            return input.Text;
        }
    }
}
