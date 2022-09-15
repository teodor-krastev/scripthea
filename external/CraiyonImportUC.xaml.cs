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
            dTable.Columns.Add(new DataColumn("#", typeof(int)));
            dTable.Columns.Add(new DataColumn("old file", typeof(string)));
            dTable.Columns.Add(new DataColumn("new file", typeof(string)));
            dTable.Columns.Add(new DataColumn("Cue", typeof(string)));
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
        private void Log(string msg)
        {
            Utils.TimedMessageBox(msg);
        }
        private void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = imageFolder;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbImageDepo.Text = dialog.FileName; 
            }
            if (!Directory.Exists(imageFolder))
            {
                Log("Err: Directory <" + imageFolder + "> does not exist. "); return;
            }
            tcMain.SelectedIndex = 0;
            List<string> orgFiles = new List<string>(Directory.GetFiles(imageFolder, "craiyon*.png"));
            lstFiles.Items.Clear();
            foreach (string ss in orgFiles)
            {
                CheckBox chk = new CheckBox(); chk.Content = System.IO.Path.GetFileName(ss); chk.IsChecked = true;
                lstFiles.Items.Add(chk);
            }
            btnConvertFolder.IsEnabled = lstFiles.Items.Count > 0;
        }

        private void btnConvertFolder_Click(object sender, RoutedEventArgs e)
        {
            bool bb = false;
            foreach (var itm in lstFiles.Items) bb |= (itm as CheckBox).IsChecked.Value;
            if (!bb)
            {
                Log("Err: No files to conver. "); return;
            }
                int i = 0; tcMain.SelectedIndex = 1;
            foreach (var itm in lstFiles.Items)
            {               
                CheckBox chk = itm as CheckBox;
                if (!chk.IsChecked.Value) continue;
                i++; 
                string efn = Convert.ToString(chk.Content); string ffn = imageFolder + efn;              
                if (!(efn.Substring(0,7)).Equals("craiyon")) continue; 
                string numFile = "c_"+efn.Substring(8, 6)+".png";
                string cue = System.IO.Path.ChangeExtension(efn.Substring(15),null);
                for (int j = 0; j < 4; j++)
                    if (cue.EndsWith("_br_")) cue = cue.Substring(0, cue.Length - 4);                

                File.Delete(imageFolder+numFile); 
                File.Move(ffn, imageFolder + numFile); // Rename the oldFileName into newFileName
                
                dTable.Rows.Add(i, efn.Substring(0,22)+"..." , numFile, cue);
                using (StreamWriter sw = File.AppendText(imageFolder + "description.txt"))
                {
                    sw.WriteLine(numFile + "=" + cue);
                }
            }
            dGrid.ItemsSource = dTable.DefaultView;
        }

        private void dGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var col = e.Column as DataGridTextColumn;
            if (e.Column.Header.ToString().Equals("Cue")) col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            else col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
        }
    }
}
