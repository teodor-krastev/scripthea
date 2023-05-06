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
using UtilsNS;
using Path = System.IO.Path;

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
            ChangeDepot(null,null);
        }
        private void SetIPicker(ref ImagePickerUC iPicker, char letter)
        {
            iPicker.Init(ref opts); iPicker.Configure(letter, new List<string>(), "Validate on open", "", "", false);
            iPicker.OnChangeDepot += new RoutedEventHandler(ChangeDepot);
            iPicker.cmImgMenu.Items.Add(new Separator());
            MenuItem mi = new MenuItem() { Header = "Synchronize image depot", Tag = letter };
            mi.Click += new RoutedEventHandler(miSynchronize_Click);
            iPicker.cmImgMenu.Items.Add(mi);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt);
        }
        protected void ChangeDepot(object sender, RoutedEventArgs e) // allow button access by iPicker states
        {
            void SetCursor(Button btn, bool normal)
            {
                if (normal) btn.Cursor = Cursors.Hand;
                else btn.Cursor = Cursors.No;
            }
            bool bb = !iPickerA.imageDepot.Equals("") && !iPickerB.imageDepot.Equals("") && 
                !iPickerA.imageDepot.Equals(iPickerB.imageDepot, StringComparison.InvariantCultureIgnoreCase);
            btnCopyA2B.IsEnabled = bb; SetCursor(btnCopyA2B, bb);
            btnCopyB2A.IsEnabled = bb; SetCursor(btnCopyB2A, bb);
            btnMoveA2B.IsEnabled = bb; SetCursor(btnMoveA2B, bb);
            btnMoveB2A.IsEnabled = bb; SetCursor(btnMoveB2A, bb);
            btnDeleteInA.IsEnabled = iPickerA.isEnabled; SetCursor(btnDeleteInA, iPickerA.isEnabled);
            btnDeleteInB.IsEnabled = iPickerB.isEnabled; SetCursor(btnDeleteInB, iPickerB.isEnabled);
        }
        private void btnCopyA2B_Click(object sender, RoutedEventArgs e)
        {
            int k = -1;
            if (sender.Equals(btnCopyA2B)) k = Copy1to2(iPickerA, iPickerB); 
            if (sender.Equals(btnCopyB2A)) k = Copy1to2(iPickerB, iPickerA);
            if (k == -1) Utils.TimedMessageBox("Err: Issue during copying", "Error", 3000);
            else Utils.TimedMessageBox(k+" files have been copied", "Information", 3000);
        }
        private void btnMoveA2B_Click(object sender, RoutedEventArgs e)
        {
            int k = -1;
            if (sender.Equals(btnMoveA2B)) k = Move1to2(iPickerA, iPickerB);
            if (sender.Equals(btnMoveB2A)) k = Move1to2(iPickerB, iPickerA);
            if (k == -1) Utils.TimedMessageBox("Err: Issue during moving", "Error", 3000);
            else Utils.TimedMessageBox(k + " files have been moved", "Information", 3000);

        }
        private void btnDeleteInA_Click(object sender, RoutedEventArgs e)
        {
            int k = -1; 
            if (sender.Equals(btnDeleteInA)) k = DeleteIn1(iPickerA);
            if (sender.Equals(btnDeleteInB)) k = DeleteIn1(iPickerB);
            if (k == -1) Utils.TimedMessageBox("Err: Issue during deleting", "Error", 3000);
            else Utils.TimedMessageBox(k + " files have been deleted", "Information", 3000);
        }
        public int Copy1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2)
        {
            if (ImgUtils.checkImageDepot(iPicker2.imageDepot, true) == -1)
            {
                if (!iPicker1.isEnabled) return -1;
                List<string> ls = new List<string>(File.ReadAllLines(Path.Combine(iPicker1.imageDepot, ImgUtils.descriptionFile)));
                File.WriteAllText(Path.Combine(iPicker2.tbImageDepot.Text, ImgUtils.descriptionFile), ls[0]);
                iPicker2.ReloadDepot();
            }
            if (!iPicker1.isEnabled || !iPicker2.isEnabled)
            {
                Log("Err: Depot " + iPicker1.letter + " has nothing to offer"); return -1;
            }
            iPicker2.isChanging = true; int k = 0;
            List<ImageInfo> lii = iPicker1.imageInfos(true, false);
            foreach (ImageInfo ii in lii)
            {
                string source_path = Path.Combine(iPicker1.imageDepot, ii.filename);
                string target_path = Path.Combine(iPicker2.imageDepot, ii.filename);
                if (File.Exists(target_path))
                {
                    //Configure the message box
                    var messageBoxText =
                        "Image file <" + target_path + "> already exists - overwrite?\r  Click \"Yes\" to overwrite the file, \"No\" to skip copying the file, or \"Cancel\" to exit operation.";
                    var caption = "Image Depot Copying ";
                    var button = MessageBoxButton.YesNoCancel;
                    var icon = MessageBoxImage.Warning;

                    // Display message box
                    var messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                    // Process message box results
                    switch (messageBoxResult)
                    {
                        case MessageBoxResult.Yes: // correct entry list
                            File.Delete(target_path); Utils.Sleep(200);
                            break;
                        case MessageBoxResult.No: // skip this correction
                            continue;
                        case MessageBoxResult.Cancel: // exit validation
                            return -1;
                    }
                }
                if (!File.Exists(source_path))
                {
                    Log("File <" + source_path + "> not found."); continue;
                }
                File.Copy(source_path, target_path); k++;
                iPicker2.iDepot.Append(ii);
            }
            iPicker2.isChanging = false;
            return k;
        }
        
        public int Move1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2)
        {
            int k = Copy1to2(iPicker1, iPicker2);
            if (k > -1) k = DeleteIn1(iPicker1);
            return k;
        }
        public int DeleteIn1(ImagePickerUC iPicker1)
        {
            iPicker1.isChanging = true;
            List<Tuple<int, string, string>> lot = iPicker1.ListOfTuples(true, false);
            int k = lot.Count - 1; int j = 0;
            while (k > -1)
            {
                iPicker1.RemoveAt(lot[k].Item1 - 1);
                k--; j++;
            }
            iPicker1.isChanging = false;
            return j;
        }
        ImagePickerUC iPickerByName(char letter) 
        { 
            switch (letter) {
                case 'A': return iPickerA;
                case 'B': return iPickerB;
                default: return null;
            }
        }
        private void miSynchronize_Click(object sender, RoutedEventArgs e)
        {
            bool bb = false;
            try
            {            
                if (!(sender is MenuItem)) return;
                ImagePickerUC iPicker = iPickerByName(Convert.ToChar((sender as MenuItem).Tag));
                ImageDepot df = iPicker?.iDepot;
                if (df == null) { Log("Error: Invalid image depot! (11)", Brushes.Red); return; }
                if (!df.isEnabled) { Log("Error: Invalid image depot! (12)", Brushes.Red); return; }
                iPicker.ReloadDepot();
                iPicker.isChanging = true;
                bb = df.Validate(true);
                iPicker.isChanging = true;
                if (df.Extras().Count == 0) return;
                MessageBoxResult result = MessageBox.Show("All the files without image depot entry (" + df.Extras().Count.ToString() + ") will be deleted. Continue?",
                    "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel) return;
                foreach (string fn in df.Extras())
                {
                    string ffn = Path.Combine(df.path, fn);
                    if (!File.Exists(ffn)) { bb = false; Log("A file is missing: "+ffn);  continue; }
                    File.Delete(ffn);
                }
            }    
            finally
            {
                if (bb) Log("The image depot is synchronized");
                else Log("Error: Problem with the image depot synchronization.", Brushes.Red);
            }
        }
    }
}
