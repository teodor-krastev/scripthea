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
using scripthea.viewer;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea
{
    /// <summary>
    /// Interaction logic for CraiyonImportUC.xaml
    /// </summary>
    public partial class ImportUtilUC : UserControl
    {
        DataTable dTable; 
        public ImportUtilUC()
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
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = ImgUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder : _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value; tbImageDepot.Text = value;
            }
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt);
        }
        public void btnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.isNull(sender))
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = imageFolder;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    tbImageDepot.Text = dialog.FileName; 
                }
            }
            LoadImages(imageFolder);
        }

        public void LoadImages(string path)
        {
            if (!Directory.Exists(path))
            {
                Log("Err: Directory <" + imageFolder + "> does not exist. "); return;
            }
            else imageFolder = path;
            List<string> orgFiles = new List<string>();            
            if (Utils.isNull(depotFolder))
            {               
                if (ImgUtils.checkImageDepot(path, true) > -1) depotFolder = new DepotFolder(path, ImageInfo.ImageGenerator.FromFile);
                else orgFiles = new List<string>(Directory.GetFiles(imageFolder, "*.png"));
            }
            if (!Utils.isNull(depotFolder)) orgFiles = depotFolder.Extras();            
            switch (tcMain.SelectedIndex)
            {
                case 0: //tiList
                    lstFiles.Items.Clear(); 
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
                        dTable.Rows.Add(true, System.IO.Path.GetFileName(ss));
                    dGrid.ItemsSource = dTable.DefaultView;
                    if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
                    btnConvertFolder.IsEnabled = dTable.Rows.Count > 0;
                    break;
                default:
                    Log("Error: intrernal error 23");
                    break;
            }
        }
        private List<string> SplitFilename(string fn, char sep) // pattern <txt><sep><txt><sep><cue><ext>
        {
            var ls = new List<string>();
            string[] spl = fn.Split(sep);
            if (spl.Length < 3) return ls;
            ls.Add(spl[0]); 
            ls.Add(spl[1]+Utils.randomString(9-spl[1].Length,true));
            string ext = System.IO.Path.GetExtension(fn);
            string fnn = System.IO.Path.ChangeExtension(fn, null); // no ext            
            ls.Add(fnn.Substring(spl[0].Length + spl[1].Length + 2)); // cue
            ls.Add(ext);
            return ls;
        }  
        private bool DecodeFilename(string filename, out string newFile, out string cue)
        {
            newFile = ""; cue = ""; List<string> ls; 
            if (filename.StartsWith("craiyon"))
            {
                ls = SplitFilename(filename, '_');
                if ((ls.Count < 4) || !ls[0].Equals("craiyon")) return false;
                if (!Utils.isNumeric(ls[1])) return false;
                newFile = System.IO.Path.ChangeExtension("c-" + ls[1], ls[3]);
                cue = ls[2];
            }
            else // Stable Diffusion
            {
                Utils.TimedMessageBox("Error: no implementation");
            }
            return true;
        }
        private bool converting = false; DepotFolder depotFolder =null;
        private void btnConvertFolder_Click(object sender, RoutedEventArgs e)
        {       
            if (dTable.Rows.Count == 0) return; int k = 0; 
            if (Utils.isNull(depotFolder))
                depotFolder = new DepotFolder(imageFolder, ImageInfo.ImageGenerator.StableDiffusion);
            try
            {           
                converting = true; image.Source = null; 
                foreach (DataRow row in dTable.Rows)
                {
                    if (!Convert.ToBoolean(row["on"])) continue;
                    string efn = Convert.ToString(row["file"]);
                    depotFolder.infos.Add(new ImageInfo(Path.Combine(imageFolder,efn), ImageInfo.ImageGenerator.StableDiffusion, false));                   
                    k++;
                }
                depotFolder.Save();
            }
            finally
            {
                LoadImages(imageFolder);
                Log("Done! Image depot of " + k.ToString() + " images was created.", Brushes.DarkGreen); converting = false;
            }            
        }
        private void dGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var col = e.Column as DataGridTextColumn; if (Utils.isNull(col)) return;
            switch (e.Column.Header.ToString())
            {                
                case ("on"):
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                    break;
                case ("file"):
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
            if (converting) return;
            DataRowView dataRow = (DataRowView)dGrid.SelectedItem;
            if (Utils.isNull(dataRow)) return;
            dGrid.Dispatcher.InvokeAsync(() =>
            {
                dGrid.UpdateLayout();
                dGrid.ScrollIntoView(dGrid.SelectedItem, null);
            });
            string fn = System.IO.Path.Combine(imageFolder,System.IO.Path.ChangeExtension(Convert.ToString(dataRow.Row.ItemArray[1]), ".png"));
            if (File.Exists(fn))
            {
                BitmapImage bi = new BitmapImage(new Uri(fn));
                image.Source = bi.Clone(); image.UpdateLayout(); bi = null;                                                             
            }                    
            else Log("Error: file not found-> " + fn);
            if (!Utils.isNull(e)) e.Handled = true;
        }

        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ImgUtils.checkImageDepot(tbImageDepot.Text,false) > 0) tbImageDepot.Foreground = Brushes.Black;
            else tbImageDepot.Foreground = Brushes.Red;
        }
    }
}
