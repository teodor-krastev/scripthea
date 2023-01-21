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
            iPickerA.Init(ref _opts); iPickerA.Configure('A', new List<string>(), "Validate on open", "", "", false);
            iPickerA.OnChangeDepot += new RoutedEventHandler(ChangeDepot);  
            iPickerB.Init(ref _opts); iPickerB.Configure('B', new List<string>(), "Validate on open", "", "",  false);
            iPickerB.OnChangeDepot += new RoutedEventHandler(ChangeDepot); ;
            ChangeDepot(null,null);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt);
        }
        protected void ChangeDepot(object sender, RoutedEventArgs e) // allow button access by iPicker states
        {
            bool bb = !iPickerA.imageDepot.Equals("") && !iPickerB.imageDepot.Equals("") && 
                !iPickerA.imageDepot.Equals(iPickerB.imageDepot, StringComparison.InvariantCultureIgnoreCase);
            btnCopyA2B.IsEnabled = bb; btnCopyB2A.IsEnabled = bb;
            btnMoveA2B.IsEnabled = bb; btnMoveB2A.IsEnabled = bb;
            btnDeleteInA.IsEnabled = iPickerA.isEnabled; btnDeleteInB.IsEnabled = iPickerB.isEnabled;
        }
        private void btnCopyA2B_Click(object sender, RoutedEventArgs e)
        {
            bool bb = false;
            if (sender.Equals(btnCopyA2B)) bb = Copy1to2(iPickerA, iPickerB);
            if (sender.Equals(btnCopyB2A)) bb = Copy1to2(iPickerB, iPickerA);
            if (!bb) Log("Err: Issue during copying");
        }
        private void btnMoveA2B_Click(object sender, RoutedEventArgs e)
        {
            bool bb = false;
            if (sender.Equals(btnMoveA2B)) bb = Move1to2(iPickerA, iPickerB);
            if (sender.Equals(btnMoveB2A)) bb = Move1to2(iPickerB, iPickerA);
            if (!bb) Log("Err: Issue during moving");
        }
        private void btnDeleteInA_Click(object sender, RoutedEventArgs e)
        {
            bool bb = false; 
            if (sender.Equals(btnDeleteInA)) bb = DeleteIn1(iPickerA);
            if (sender.Equals(btnDeleteInB)) bb = DeleteIn1(iPickerB);
            if (!bb) Log("Err: Issue during deleting");
        }

        public bool Copy1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2)
        {
            if (ImgUtils.checkImageDepot(iPicker2.imageDepot, true) == -1)
            {
                if (!iPicker1.isEnabled) return false;
                List<string> ls = new List<string>(File.ReadAllLines(Path.Combine(iPicker1.imageDepot, ImgUtils.descriptionFile)));
                File.WriteAllText(Path.Combine(iPicker2.tbImageDepot.Text, ImgUtils.descriptionFile), ls[0]);
                iPicker2.ReloadDepot();
            }
            if (!iPicker1.isEnabled || !iPicker2.isEnabled)
            {
                Log("Err: Depot " + iPicker1.letter + " has nothing to offer"); return false;
            }
            iPicker2.isChanging = true;
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
                            return false;
                    }
                }
                if (!File.Exists(source_path))
                {
                    Log("File <" + source_path + "> not found."); continue;
                }
                File.Copy(source_path, target_path);
                iPicker2.iDepot.Append(ii);
            }
            iPicker2.isChanging = false;
            return true;
        }
        
        public bool Move1to2(ImagePickerUC iPicker1, ImagePickerUC iPicker2)
        {
            bool bb = Copy1to2(iPicker1, iPicker2);
            if (bb) bb &= DeleteIn1(iPicker1);
            return bb;
        }
        public bool DeleteIn1(ImagePickerUC iPicker1)
        {
            iPicker1.isChanging = true;
            List<Tuple<int, string, string>> lot = iPicker1.ListOfTuples(true, false);
            int k = lot.Count - 1;
            while (k > -1)
            {
                iPicker1.RemoveAt(lot[k].Item1 - 1);
                k--;
            }
            iPicker1.isChanging = false;
            return true;
        }
    }
}
