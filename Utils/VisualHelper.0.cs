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

namespace UtilsNS
{   
    public static class VisualHelper
    {
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
    }
    public static class DataGridHelper
    {
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

            if (row != null)
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
    }
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
}
