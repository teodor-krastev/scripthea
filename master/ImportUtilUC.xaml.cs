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
using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;
using scripthea.viewer;
using scripthea.options;
using scripthea.external;
using UtilsNS;

namespace scripthea.master
{
    public static class ExtraImgUtils
    {
        public static bool GetMetadata(string imagePath, out Dictionary<string, string> meta, ImageInfo ii = null)
        {
            if (ImgUtils.GetMetadata1111(imagePath, out meta))
            {
                if (ii == null) return true;
                ii.filename = Path.GetFileName(imagePath);
                ii.FromMeta1111Dict(meta);
                return true;
            }
            if (cUtils.GetMetadataComfy(imagePath, out meta))
            {
                if (ii == null) return true;
                ii.filename = Path.GetFileName(imagePath);
                ii.FromMetaComfyDict(meta);
                return true;
            }
            return false;
        }
    }
    /// <summary>
    /// Interaction logic for CraiyonImportUC.xaml
    /// </summary>
    public partial class ImportUtilUC : UserControl, iFocusControl
    {
        protected Options opts;
        protected DataTable dTable; 
        public ImportUtilUC()
        {
            InitializeComponent();
        }
        public void Init(ref Options _opts)
        {
            opts = _opts;

            dTable = new DataTable();
            dTable.Columns.Add(new DataColumn("on", typeof(bool)));
            dTable.Columns.Add(new DataColumn("file", typeof(string)));

            tcMain.SelectedIndex = 1;
        }
        public UserControl parrent { get { return this; } }
        public GroupBox groupFolder { get { return gbFolder; } }
        public TextBox textFolder { get { return tbImageDepot; } }

        public void BindData()
        {
            Binding binding = new Binding("."); //ItemsSource="{Binding Path=., Mode=TwoWay}"  SourceUpdated="OnTargetUpdated"
            binding.BindsDirectlyToSource = true;           
            binding.Mode = BindingMode.TwoWay; 
            binding.Source = dTable;
            dGrid.SetBinding(DataGrid.ItemsSourceProperty, binding);
        }

        private string _imageFolder;
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = SctUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder : _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value; //tbImageDepot.Text = value;
            }
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
        public void Clear()
        {
            checkBoxes?.Clear(); dTable?.Rows?.Clear(); lstFiles?.Items?.Clear(); lbAdd2Depot.Content = "";
        }
        private List<CheckBox> checkBoxes = new List<CheckBox>();
        public void LoadImages(string path, bool warning = true)
        {
            Clear(); lbAdd2Depot.Content = "";  
            if (!Directory.Exists(path))
            {
                opts.Log("Error[998]: Directory <" + path + "> does not exist. "); return;
            }            
            if (!Utils.isNull(depotFolder)) // if image-depot is already there
            {
                if (!Utils.comparePaths(depotFolder.path, path)) depotFolder = null; // treat it as new depot
                else lbAdd2Depot.Content = "Add to depot";
            }                               
            else imageFolder = path;
            List<string> orgFiles = new List<string>();          
            if (Utils.isNull(depotFolder))
            {
                if (SctUtils.checkImageDepot(path, true) > -1)
                {
                    depotFolder = new ImageDepot(path, ImageInfo.ImageGenerator.FromDescFile); lbAdd2Depot.Content = "Add to depot";
                }
                else orgFiles = new List<string>(Directory.GetFiles(imageFolder, "*.png"));
            }
            if (!Utils.isNull(depotFolder)) orgFiles = depotFolder.Extras();     
            if (orgFiles.Count.Equals(0) && warning) { opts.Log("No image files to consider in " + path); return; }
            switch (tcMain.SelectedIndex)
            {
                case 0: //tiList - redundant
                    lstFiles?.Items?.Clear(); 
                    foreach (string ss in orgFiles)
                    {
                        CheckBox chk = new CheckBox(); chk.Content = System.IO.Path.GetFileName(ss); chk.IsChecked = true;
                        lstFiles.Items.Add(chk);
                    }
                    btnConvertFolder.IsEnabled = lstFiles.Items.Count > 0;
                    break;
                case 1: //tiGrid
                    dTable?.Rows?.Clear();
                    foreach (string ss in orgFiles)
                        dTable.Rows.Add(true, System.IO.Path.GetFileName(ss));
                    BindData();
                    //dGrid.ItemsSource = dTable.DefaultView;
                    if (dTable.Rows.Count > 0) dGrid.SelectedIndex = 0;
                    btnConvertFolder.IsEnabled = dTable.Rows.Count > 0;
                    break;
                default:
                    opts.Log("Error[23]: intrernal error");
                    break;
            }
            checkBoxes.Clear();
            for (int i = 0; i < dTable.Rows.Count; i++)
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, i, 0).FindVisualChild<CheckBox>();
                if (chk == null) continue;
                chk.Name = "chkList" + i.ToString();
                chk.Tag = i;
                chk.Checked += new RoutedEventHandler(chkCheckedColumn); chk.Unchecked += new RoutedEventHandler(chkCheckedColumn);
                //checkBoxes.Add(chk);
            }
            GetChecked();
        }
        private void chkCheckedColumn(object sender, RoutedEventArgs e)
        {
            GetChecked();
        }
        private List<string> GetChecked(bool print = true) // list of filenames
        {
            List<string> ls = new List<string>();
            if (dTable?.Rows?.Count == 0)
            {
                if (print) lbChecked.Content = "0 out of 0"; return ls;
            }
            int sr = lastSelectedRow; bool cc = false; //dGrid.SelectedIndex
            if (Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                CheckBox chk = DataGridHelper.GetCellByIndices(dGrid, sr, 0).FindVisualChild<CheckBox>();
                if (chk != null)
                {
                    cc = chk.IsChecked.Value;
                    dTable.Rows[sr]["on"] = chk.IsChecked.Value;
                }
            }                       
            for (int i = 0; i < dTable.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dTable.Rows[i]["on"]))
                    ls.Add(Convert.ToString(dTable.Rows[i]["file"]));
            }
            if (print)
                lbChecked.Content = ls.Count.ToString() + " out of " + dTable.Rows.Count.ToString(); //+ (cc ? " X": " O"); //"+ sr.ToString() +"
            return ls;
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
                Utils.TimedMessageBox("Error[666]: no implementation");
            }
            return true;
        }
        public bool converting = false; ImageDepot depotFolder =null;
        private void btnConvertFolder_Click(object sender, RoutedEventArgs e)
        {
            ImageInfo.ImageGenerator imageGenType = (ImageInfo.ImageGenerator)cbSource.SelectedIndex;          

            int rc = dTable.Rows.Count;
            if (rc == 0) return; int k = 0; int nok = 0; converting = true;
            image.Source = null; lastLoadedPic = "";// ImgUtils.file_not_found; 
            image.UpdateLayout();
            
            if (Utils.isNull(depotFolder))
                depotFolder = new ImageDepot(imageFolder, imageGenType);
            try
            {
                GetChecked();
                List<string> unchk = new List<string>();                 
                foreach (DataRow row in dTable.Rows)
                {
                    string efn = Convert.ToString(row["file"]);
                    if (!Convert.ToBoolean(row["on"]))
                    {
                        unchk.Add(efn); continue;
                    }
                    ImageInfo ii = new ImageInfo();
                    ii.ImageImport(Path.Combine(imageFolder, efn), imageGenType, chkKeepNames.IsChecked.Value);
                    if (!ii.IsEnabled()) { nok++; continue; }
                    depotFolder.items.Add(ii);                   
                    k++;
                }
                depotFolder.Save();
                //if (nok > 0) -> unclear
                if (chkDeleteUnchecked.IsChecked.Value)
                {
                    foreach(string fn in unchk)
                    {
                        File.Delete(Path.Combine(imageFolder, fn));
                    }
                }
            }
            finally
            {
                if (rc > k) LoadImages(imageFolder, false);
                else
                {
                    dTable.Rows.Clear(); image.Source = null; lastLoadedPic = ""; GetChecked();
                }
                string sk = (nok > 0) ? "\r\r" + nok.ToString() + " images have malformatted or missing metadata!" : "";
                ClearlbMetadata();
                opts.Log("Done! Image depot of " + k.ToString() + " images was created." + sk , Brushes.DarkGreen);               
                converting = false;
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
                    col.IsReadOnly = true;
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;
            }
        }

        int lastSelectedRow = -1; string lastLoadedPic = "";
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

            string fn = Convert.ToString(dataRow.Row.ItemArray[1]); //System.IO.Path.ChangeExtension(Convert.ToString(dataRow.Row.ItemArray[1]), ".png");
            fn = System.IO.Path.Combine(imageFolder,fn);
            if (File.Exists(fn)) 
            { 
                image.Source = ImgUtils.UnhookedImageLoad(fn); lastLoadedPic = fn;
                tbPrompt.Text = "";
                Dictionary<string, string> meta = new Dictionary<string, string>();
                if (ExtraImgUtils.GetMetadata(fn, out meta))
                {                    
                    tbPrompt.Foreground = Brushes.Navy;                   
                    if (meta.ContainsKey("prompt")) // 1111
                    {
                        tbPrompt.Text = "prompt: " + meta["prompt"]; meta.Remove("prompt");
                    }
                    if (meta.ContainsKey("positive")) // comfy
                    {
                        tbPrompt.Text = "positive: " + meta["positive"]; meta.Remove("positive");
                    }
                }
                else
                {
                    tbPrompt.Foreground = Brushes.Tomato;
                    tbPrompt.Text = "Error: unrecognizable or missing metadata of an image.";
                }
                Utils.dict2ListBox(meta, lbMetadata);
            }
            else { opts.Log("Error[158]: file not found-> " + fn); lastLoadedPic = ""; }
            if (!Utils.isNull(e)) e.Handled = true;
            // int sr = dGrid.SelectedIndex; 
            // TextBlock textBlock = DataGridHelper.GetCellByIndices(dGrid, sr, 1).FindVisualChild<TextBlock>();
            GetChecked();
            lastSelectedRow = dGrid.SelectedIndex;
        }
        public void ClearlbMetadata()
        {
            tbPrompt.Text = ""; Utils.dict2ListBox(new Dictionary<string, string>(), lbMetadata);
        }
        private void textBlock_KeyDown(object sender, KeyEventArgs e)
        {
            int sr = dGrid.SelectedIndex; 
            if (e.Key.Equals(Key.Space) && Utils.InRange(sr, 0, dTable.Rows.Count - 1))
            {
                var chk = DataGridHelper.GetCellByIndices(dGrid, sr, 0).FindVisualChild<CheckBox>();
                if (Utils.isNull(chk)) return;
                chk.IsChecked = !chk.IsChecked.Value;
                GetChecked();
            }                
        }  

        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SctUtils.checkImageDepot(tbImageDepot.Text,false) > 0) tbImageDepot.Foreground = Brushes.Black;
            else tbImageDepot.Foreground = Brushes.Red;
            LoadImages(tbImageDepot.Text);
        }

        private void chkDeleteUnchecked_Checked(object sender, RoutedEventArgs e)
        {
            chkDeleteUnchecked.Foreground = Brushes.Red;
        }
        private void chkDeleteUnchecked_Unchecked(object sender, RoutedEventArgs e)
        {
            chkDeleteUnchecked.Foreground = Brushes.Black;
        }
        private void MCheckUncheck(object sender, MouseButtonEventArgs e)
        {
            GetChecked();
        }       
        private void mi_Click(object sender, RoutedEventArgs e)
        {
            if (dTable.Rows.Count == 0) { opts.Log("Error[222]: No loaded images found."); return; }
            MenuItem mi = sender as MenuItem; string header = Convert.ToString(mi.Header);
            foreach (DataRow dr in dTable.Rows)
            {
                switch (header)
                {
                    case "Check All":
                        dr["on"] = true;
                        break;
                    case "Uncheck All":
                        dr["on"] = false;
                        break;
                    case "Invert Checking":
                        dr["on"] = !Convert.ToBoolean(dr["on"]);
                        break;
                }
            }
            GetChecked();
        }
        bool inverting = false;
        private void imgMenu_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (dTable.Rows.Count == 0) { opts.Log("Error[223]: No loaded images found."); return; }
            inverting = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 1)
                {
                    Utils.DelayExec(300, () => { btnMenu.ContextMenu.IsOpen = !inverting; }); 
                }
                if (e.ClickCount == 2)
                {
                    inverting = true;
                    foreach (DataRow dr in dTable.Rows)
                        dr["on"] = !Convert.ToBoolean(dr["on"]);
                }
            }
            GetChecked();
        }
        private void image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (lastLoadedPic.Equals("")) return;
            DataObject data = new DataObject(DataFormats.FileDrop, new string[] { lastLoadedPic });
            // Start the drag-and-drop operation
            DragDrop.DoDragDrop(image, data, DragDropEffects.Copy);
        }
        private void lbMetadata_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            tbPrompt.Width = lbMetadata.ActualWidth-10;
        }
    }
}
