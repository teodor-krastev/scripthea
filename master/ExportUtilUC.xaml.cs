using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
using Microsoft.WindowsAPICodePack.Dialogs;
using scripthea.viewer;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.master
{
    /// <summary>
    /// Interaction logic for ExportUtilUC.xaml
    /// </summary>
    public partial class ExportUtilUC : UserControl
    {
        public ExportUtilUC()
        {
            InitializeComponent();
        }
        private Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
            iPicker.Init(ref _opts);
            List<string> ls = new List<string>(new string[] { "keep image types", "export all as .PNG", "export all as .JPG" });
            iPicker.Configure(' ',ls, "Rename files to respective prompts", "", "Export", true).Click += new RoutedEventHandler(Export);
            OnChangeDepot(null, null);
            iPicker.OnChangeDepot += new RoutedEventHandler(OnChangeDepot);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt,"Informaion",3000);
        }

        private void OnChangeDepot(object sender, RoutedEventArgs e)
        {
            iPicker.btnCustom.IsEnabled = iPicker.isEnabled; 
            DepotFolder df = sender as DepotFolder;
            df?.Validate(null);
        }

        private void Export(object sender, RoutedEventArgs e)
        {
            if (!iPicker.isEnabled) return;
            List<Tuple<int, string, string>> lot = iPicker.ListOfTuples(true, false); // idx, filename, prompt
            if (lot.Count.Equals(0)) { Log("Error: not checked images."); return; }

            string sourceFolder = iPicker.iDepot.path;  string targetFolder = "";
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = ImgUtils.defaultImageDepot;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) targetFolder = dialog.FileName;
            else return;
            if (targetFolder.Equals(sourceFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                Log("Error: source and target folders must be different."); return;
            }
            string tfn = "";
            foreach (var itm in lot)
            {
                tfn = iPicker.chkCustom1.IsChecked.Value ? itm.Item3.Substring(0, Math.Min(150, itm.Item3.Length)) : itm.Item2;
                tfn = Utils.correctFileName(tfn);
                if (!Utils.validFileName(tfn)) tfn = itm.Item2; // prompt text not suitable for filename
                string sffn = Path.Combine(iPicker.imageDepot, itm.Item2); // src full path
                string tffn = Path.Combine(targetFolder, tfn);
                ImageFormat iFormat = null;
                switch (iPicker.comboCustom.SelectedIndex)
                {
                    case 0: iFormat = ImgUtils.GetImageFormat(sffn);
                        break;
                    case 1: iFormat = ImageFormat.Png;
                        break;
                    case 2: iFormat = ImageFormat.Jpeg;
                        break;
                }
                ImgUtils.CopyToImageFormat(sffn, tffn, iFormat);
            }
            Log(lot.Count.ToString() + " images have been exported to " + targetFolder); 
        }
    }
}

/* <html>
<head>
  <script>
    function displayImages(imageFiles) {
      // Clear the table
      document.getElementById("imageTable").innerHTML = "";

      // Create a table row for each image file
      for (let i = 0; i < imageFiles.length; i++) {
        let tr = document.createElement("tr");
        let td = document.createElement("td");
        let img = document.createElement("img");
        img.src = URL.createObjectURL(imageFiles[i]);
        img.style.width = "100px";
        img.style.height = "100px";
        td.appendChild(img);
        tr.appendChild(td);
        document.getElementById("imageTable").appendChild(tr);
      }
    }
  </script>
</head>
<body>
  <input type="file" multiple onchange="displayImages(this.files)">
  <table id="imageTable"></table>
</body>
</html>

This page contains an input element of type "file" that allows the user to select multiple image files. The onchange event is set to call 
the displayImages function, passing in the selected files.

The displayImages function first clears the table by setting the innerHTML of the table with id "imageTable" to an empty string. 
Then, it loops over each image file and creates a table row with a single cell that contains an image element. The source of the 
image is set to an object URL created from the file. The width and height of the image are set to 100px to ensure that it fits within the cell. 
Finally, the table row and cell are appended to the table.
*/

