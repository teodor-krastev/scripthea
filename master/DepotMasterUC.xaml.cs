using scripthea.viewer;
using System;
using System.Collections.Generic;
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

namespace scripthea.master
{
    /// <summary>
    /// Interaction logic for depotMasterUC.xaml
    /// </summary>
    public partial class DepotMasterUC : UserControl
    {
        public DepotMasterUC()
        {
            InitializeComponent();
        }
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            SetIPicker(ref iPickerA, 'A'); SetIPicker(ref iPickerB, 'B');
            iPickerA.OnSelectEvent += new TableViewUC.PicViewerHandler(PicSelectA);
            ChangeDepot(null,null);
        }
        public ImagePickerUC iPickerByName(char letter)
        {
            switch (letter)
            {
                case 'A': return iPickerA;
                case 'B': return iPickerB;
                default: return null;
            }
        }
        public ImagePickerUC activePicker
        {
            get
            {
                if (iPickerA.HasTheFocus) return iPickerA;
                if (iPickerB.HasTheFocus) return iPickerB;
                return null;
            }
        }
        MenuItem mi1; MenuItem mi2;  MenuItem mi3;
        private void SetIPicker(ref ImagePickerUC iPicker, char letter)
        {
            iPicker.Init(ref opts); iPicker.Configure(letter, new List<string>(), "", "", "", false);
            iPicker.OnChangeDepot += new RoutedEventHandler(ChangeDepot);
            iPicker.cmImgMenu.Items.Add(new Separator());
            mi1 = new MenuItem() { Header = "Remove IDF entries without images", Tag = letter };
            mi1.Click += new RoutedEventHandler(miSynchronize_Click); iPicker.cmImgMenu.Items.Add(mi1);
            mi2 = new MenuItem() { Header = "Remove images without IDF entries", Tag = letter };
            mi2.Click += new RoutedEventHandler(miSynchronize_Click); iPicker.cmImgMenu.Items.Add(mi2);
            mi3 = new MenuItem() { Header = "Synchronize image depot", Tag = letter };
            mi3.Click += new RoutedEventHandler(miSynchronize_Click); iPicker.cmImgMenu.Items.Add(mi3);
        }       
        protected void ChangeDepot(object sender, RoutedEventArgs e) // allow button access by iPicker states
        {
            void SetCursor(Button btn, bool normal)
            {
                if (normal) btn.Cursor = Cursors.Hand;
                else btn.Cursor = Cursors.No;
            }
            bool bb = (iPickerA.isValid || Directory.Exists(iPickerA.iDepot?.path)) && (iPickerB.isValid || Directory.Exists(iPickerB.iDepot?.path));
            if (bb) bb &= !Utils.comparePaths(iPickerA.imageFolder,iPickerB.imageFolder);
            btnCopyA2B.IsEnabled = bb; SetCursor(btnCopyA2B, bb);
            btnCopyB2A.IsEnabled = bb; SetCursor(btnCopyB2A, bb);
            btnMoveA2B.IsEnabled = bb; SetCursor(btnMoveA2B, bb);
            btnMoveB2A.IsEnabled = bb; SetCursor(btnMoveB2A, bb);

            btnDeleteInA.IsEnabled = iPickerA.isValid; SetCursor(btnDeleteInA, iPickerA.isValid);
            btnDeleteInB.IsEnabled = iPickerB.isValid; SetCursor(btnDeleteInB, iPickerB.isValid);
        }
        private void btnCopyA2B_Click(object sender, RoutedEventArgs e)
        {
            int k = -1; List<string> copied;
            if (sender.Equals(btnCopyA2B)) k = Copy1to2(iPickerA, iPickerB, out copied); 
            if (sender.Equals(btnCopyB2A)) k = Copy1to2(iPickerB, iPickerA, out copied);
            switch (k)
            {
                case -1: Utils.TimedMessageBox("Error[323]: Issue during copying", "Error", 3000);
                    break;
                default: Utils.TimedMessageBox(k+" images have been copied", "Information", 3000);
                    break;
            }            
        }
        private void btnMoveA2B_Click(object sender, RoutedEventArgs e)
        {
            int k = -1;
            if (sender.Equals(btnMoveA2B)) k = Move1to2(iPickerA, iPickerB);
            if (sender.Equals(btnMoveB2A)) k = Move1to2(iPickerB, iPickerA);
            switch (k)
            {
                case -1:
                    Utils.TimedMessageBox("Error[98]: Issue during moving", "Error", 3000);
                    break;
                default:
                    Utils.TimedMessageBox(k + " images have been moved", "Information", 3000);
                    break;
            }
        }
        private void btnDeleteInA_Click(object sender, RoutedEventArgs e)
        {
            int k = -1; 
            if (sender.Equals(btnDeleteInA)) k = DeleteIn1(iPickerA);
            if (sender.Equals(btnDeleteInB)) k = DeleteIn1(iPickerB);
            if (k == -1) Utils.TimedMessageBox("Error[112]: Issue during deleting", "Error", 3000);
            else Utils.TimedMessageBox(k +  " images have been deleted", "Information", 3000);
        }
        public int Copy1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2, out List<string> copied) 
        {
            copied = new List<string>(); 
            if (SctUtils.checkImageDepot(iPicker2.imageFolder, true) == -1)
            {
                if (!iPicker1.isEnabled) return -1;
                List<string> ls = new List<string>(File.ReadAllLines(Path.Combine(iPicker1.imageFolder, SctUtils.descriptionFile)));
                File.WriteAllText(Path.Combine(iPicker2.tbImageDepot.Text, SctUtils.descriptionFile), ls[0]);
                iPicker2.ReloadDepot();
            }
            if (!iPicker1.isEnabled || !iPicker2.isEnabled)
            {
                opts.Log("Error[85]: Depot <" + iPicker1.letter + "> has nothing to offer"); return -1;
            }
            iPicker2.isChanging = true; int k = 0;
            List<ImageInfo> lii = iPicker1.imageInfos(true, false);
            foreach (ImageInfo ii in lii)
            {
                string source_path = Path.Combine(iPicker1.imageFolder, ii.filename);
                string target_path = Path.Combine(iPicker2.imageFolder, ii.filename);
                if (File.Exists(target_path))
                {
                    //Configure the message box
                    var messageBoxText =
                        "Image file <" + target_path + "> already exists - overwrite?\r\r  Click \"Yes\" to overwrite the file, or \"No\" to skip copying the file.";
                    var caption = "Image Depot Copying ";
                    var button = MessageBoxButton.YesNo;
                    var icon = MessageBoxImage.Warning;

                    // Display message box
                    var messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                    // Process message box results
                    switch (messageBoxResult)
                    {
                        case MessageBoxResult.Yes: // remove duplicated target
                            int idx = iPicker2.iDepot.idxFromFilename(ii.filename);
                            if (idx < 0) opts.Log("Error[361]: image <" + ii.filename + "> not found.");
                            else iPicker2.iDepot.RemoveAt(idx, true); Utils.Sleep(200);
                            break;
                        case MessageBoxResult.No: // skip this copying
                            continue;                        
                    }
                }
                if (!File.Exists(source_path))
                {
                    opts.Log("File <" + source_path + "> not found."); continue;
                }
                File.Copy(source_path, target_path); k++; copied.Add(Path.GetFileName(source_path));
                if (!iPicker2.iDepot.Append(ii)) { Utils.TimedMessageBox("Error[115]: image depot problem"); break; }
            }
            iPicker2.isChanging = false; iPicker2.iDepot.Save(); iPicker2.ReloadDepot();
            return k;
        }        
        public int Move1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2)
        {
            List<string> copied;
            int k = Copy1to2(iPicker1, iPicker2, out copied); int cnt1 = iPicker1.iDepot.items.Count;
            if (!k.Equals(copied.Count)) Utils.TimedMessageBox("Error[778]: index of image depot problem");
            if (k < 0) return k;
            foreach (string fn in copied)
            {
                int idx = iPicker1.iDepot.idxFromFilename(fn);
                if (idx < 0) opts.Log("Error[998]: image <" + fn + "> not found.");
                else
                {
                    if (!iPicker1.iDepot.RemoveAt(idx, opts.viewer.RemoveImagesInIDF)) Utils.TimedMessageBox("Error[884]: index of image depot problem.");
                }
            }
            if ((cnt1 - iPicker1.iDepot.items.Count) != k) Utils.TimedMessageBox("Error[995]: index of image depot problem.");
            if (k > 0)
            {
                iPicker1.iDepot.Save(); iPicker1.ReloadDepot();
            }
            return k;
        }
        public int DeleteIn1(ImagePickerUC iPicker1)
        {
            iPicker1.isChanging = true;
            List<Tuple<int, string, int, string>> lot = iPicker1.ListOfTuples(true, false);
            int k = lot.Count - 1; int j = 0;
            while (k > -1)
            {
                int idx = lot[k].Item1;
                if (idx < 0) opts.Log("Error[557]: invalid <" + idx + "> index.");
                else
                {
                    if (!iPicker1.iDepot.RemoveAt(idx, opts.viewer.RemoveImagesInIDF)) Utils.TimedMessageBox("Error[885]: index of image depot problem.");
                }
                k--; j++;
            }
            if (j > 0) // number of deleted
            {
                iPicker1.iDepot.Save(); iPicker1.ReloadDepot(); 
            }
            iPicker1.isChanging = false; 
            return j;
        }
        private void miSynchronize_Click(object sender, RoutedEventArgs e)
        {
            bool bb = true; ImagePickerUC iPicker = iPickerByName(Convert.ToChar((sender as MenuItem).Tag));
            try
            {            
                if (!(sender is MenuItem)) return;
                string mis = Convert.ToString((sender as MenuItem).Header);
                ImageDepot df = iPicker?.iDepot;
                if (df is null) { opts.Log("Error[336]: Invalid image depot!", Brushes.Red); return; }
                if (!df.isEnabled) { opts.Log("Error[12]: Invalid image depot!", Brushes.Red); return; }
                iPicker.ReloadDepot();
                iPicker.isChanging = true;
                if (mis.Equals(Convert.ToString(mi1.Header)) || mis.Equals(Convert.ToString(mi3.Header)))
                {
                    bb &= df.Validate(true); iPicker.ReloadDepot();
                }
                iPicker.isChanging = true;
                if (mis.Equals(Convert.ToString(mi2.Header)) || mis.Equals(Convert.ToString(mi3.Header)))
                {
                    List<string> ls = new List<string>(df.Extras());
                    if (ls.Count == 0) return; MessageBoxResult result = MessageBoxResult.OK;
                    if (opts.iDutilities.MasterValidationAsk)
                        result = MessageBox.Show("All the images without image depot entry (" + ls.Count.ToString() + ") will be deleted. Continue?",
                        "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Cancel) return;
                    foreach (string fn in ls)
                    {
                        string ffn = Path.Combine(df.path, fn);
                        if (!File.Exists(ffn)) { bb = false; opts.Log("A file is missing: "+ffn);  continue; }
                        File.Delete(ffn);
                    }
                    opts.Log(ls.Count.ToString() + " images without image depot entry have been deleted.");
                }
            }    
            finally
            {
                if (bb) opts.Log("The image depot is synchronized");
                else opts.Log("Error[364]: Problem with the image depot synchronization.");
                iPicker.isChanging = false;
            }
        }
        protected void PicSelectA(int idx, ImageDepot iDepot)
        {
            //ImageInfo ii = SelectedItem(idx, iDepot);
            if (!chkSynch.IsChecked.Value) return;
            List<ImageInfo> lii = iPickerB.imageInfos(true, true);
            if (lii is null) return;
            if (lii.Count == 0) return;
            iPickerB.SelectItem(Utils.EnsureRange(idx, 0,lii.Count-1)); //iPickerA.Focus();
        }
    }
}
