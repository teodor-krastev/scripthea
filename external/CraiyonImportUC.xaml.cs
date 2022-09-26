using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UtilsNS;

namespace scripthea
{
    /// <summary>
    /// Interaction logic for CraiyonImportUC.xaml
    /// </summary>
    public partial class CraiyonImportUC : UserControl
    {
        DataTable dTable; 
        public CraiyonImportUC()
        {
            InitializeComponent();
        }
        public void Init()
        {
            dTable = new DataTable();
            dTable.Columns.Add(new DataColumn("on", typeof(bool)));
            dTable.Columns.Add(new DataColumn("file", typeof(string)));
            tcMain.SelectedIndex = 1;
        }
        private string _imageFolder;
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepo.Text)) _imageFolder = tbImageDepo.Text;
                else _imageFolder = Utils.basePath + "\\images\\";
                return _imageFolder.EndsWith("\\") ? _imageFolder : _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value; tbImageDepo.Text = value;
            }
        }
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);
        public event LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt);
        }
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.isNull(sender))
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = imageFolder;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    tbImageDepo.Text = dialog.FileName; 
                }
            }
            if (!Directory.Exists(imageFolder))
            {
                Log("Err: Directory <" + imageFolder + "> does not exist. "); return;
            }            
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageFolder, "craiyon*.png"));
            lstFiles.Items.Clear(); converting = false;
            switch (tcMain.SelectedIndex)
            {
                case 0: //tiList
                    foreach (string ss in orgFiles)
                    {
                        CheckBox chk = new CheckBox(); chk.Content = System.IO.Path.GetFileName(ss); chk.IsChecked = true;
                        lstFiles.Items.Add(chk);
                    }
                    btnConvertFolder.IsEnabled = lstFiles.Items.Count > 0;
                    break;
                case 1: //tiGrid
                    dTable.Rows.Clear();
                    foreach (string ss in orgFiles)
                        dTable.Rows.Add(true, System.IO.Path.GetFileNameWithoutExtension(ss));
                    dGrid.ItemsSource = dTable.DefaultView;
                    if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
                    btnConvertFolder.IsEnabled = dTable.Rows.Count > 0;
                    break;
                default: Log("Error: intrernal error 23");
                    break;
            }
        }
        private bool converting = false; 
        private void btnConvertFolder_Click(object sender, RoutedEventArgs e)
        {       
            if (dTable.Rows.Count == 0) return; int k = 0; 
            try
            {           
                converting = true; image.Source = null; 
                foreach (DataRow row in dTable.Rows)
                {
                    if (!Convert.ToBoolean(row["on"])) continue;
                    string efn = Convert.ToString(row["file"]); 
                    string ffn = System.IO.Path.Combine(imageFolder, System.IO.Path.ChangeExtension(efn, ".png")); 
                    if (!(efn.Substring(0, 7)).Equals("craiyon")) continue;                    
                    string cue = System.IO.Path.ChangeExtension(efn.Substring(15), null);
                    for (int j = 0; j < 4; j++)
                        if (cue.EndsWith("_br_")) cue = cue.Substring(0, cue.Length - 4);
                    string numFile = System.IO.Path.Combine(imageFolder, System.IO.Path.ChangeExtension("c_" + efn.Substring(8, 6), ".png"));
                    if (File.Exists(numFile)) 
                    { 
                        numFile = System.IO.Path.Combine(imageFolder, System.IO.Path.ChangeExtension("c_" + efn.Substring(8, 6)+Utils.randomString(1), ".png"));
                        if (File.Exists(numFile))
                        {
                            File.Delete(numFile); Log("Warning: deleting -> " + numFile); Utils.Sleep(1000);
                        }
                    }
                    try
                    {
                        File.Move(ffn, numFile); // Rename the oldFileName into newFileName     
                    }
                    catch (System.IO.IOException IOe) { Log("Error: ("+System.IO.Path.GetFileName(ffn)+") " + IOe.Message); continue; }                               
                    using (StreamWriter sw = File.AppendText(imageFolder + "description.txt"))
                    {
                        sw.WriteLine(System.IO.Path.GetFileName(numFile) + "=" + cue);
                    }
                    k++;
                }
            }
            finally
            {
                btnNewFolder_Click(null, null);
                Log("Done! Image depot of "+k.ToString()+" images was created.", Brushes.DarkGreen); converting = false;
            }
            
        }
        private void dGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (converting) return;  
            DataRowView dataRow = (DataRowView)dGrid.SelectedItem;
            if (Utils.isNull(dataRow)) return;
            string fn = System.IO.Path.Combine(imageFolder,System.IO.Path.ChangeExtension( Convert.ToString(dataRow.Row.ItemArray[1]), ".png"));
            if (File.Exists(fn))
            {
                BitmapImage bi = new BitmapImage(new Uri(fn));
                image.Source = bi.Clone(); bi = null;                                                             
            }                    
            else Log("Error: file not found-> " + fn);            
        }
    }
}
