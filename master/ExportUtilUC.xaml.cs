using System;
using System.Collections.Generic;
using System.Drawing;
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
using Brushes = System.Windows.Media.Brushes;
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
            iPicker.Configure(' ',ls, "Rename files to prompts", "Create web-page", "Export", true).Click += new RoutedEventHandler(Export);
            OnChangeDepot(null, null); iPicker.chkCustom2.IsChecked = true;
            iPicker.OnChangeDepot += new RoutedEventHandler(OnChangeDepot);
            iPicker.AddMenuItem("Convert .PNG to .JPG").Click += new RoutedEventHandler(ConvertPNG2JPG); 
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
            ImageDepot df = sender as ImageDepot;
            df?.Validate(null);
        }
        private void ConvertPNG2JPG(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = ImgUtils.defaultImageDepot;
            dialog.Title = "Select a PNG File";
            dialog.Multiselect = true; 
            dialog.DefaultExtension = ".png"; dialog.Filters.Add(new CommonFileDialogFilter("PNG images", "png"));
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;   
            int cnt = 0; string fd = "";        
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;               
                foreach (string filePath in dialog.FileNames)
                {
                    Bitmap image = new Bitmap(filePath);
                    string newFilePath = Utils.AvoidOverwrite(Path.ChangeExtension(filePath, ".jpg"));
                    image.Save(newFilePath, ImageFormat.Jpeg); cnt++;
                    fd = Path.GetDirectoryName(filePath);
                }
            } finally { Mouse.OverrideCursor = null; }
            Utils.TimedMessageBox("PNG to JPG conversion successful!\r\r"+ cnt.ToString()+" JPG images created in "+ fd, "Info", 3500);    
        }
    
        private void Export(object sender, RoutedEventArgs e)
        {
            if (!iPicker.isEnabled) return;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait; iPicker.btnCustom.Background = Brushes.Coral; Utils.DoEvents();

                List<Tuple<int, string, string>> lot = iPicker.ListOfTuples(true, false); // idx (1 based), filename, prompt
                if (lot.Count.Equals(0)) { Log("Error: not checked images."); return; }

                string sourceFolder = iPicker.iDepot.path; string targetFolder = "";
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                //dialog.InitialDirectory = ImgUtils.defaultImageDepot;
                dialog.Title = "Select a folder for the exported image depot";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) targetFolder = dialog.FileName;
                else return;
                if (targetFolder.Equals(sourceFolder, StringComparison.InvariantCultureIgnoreCase))
                {
                    Log("Error: source and target folders must be different."); return;
                }
                string tfn = ""; List<Tuple<int, string, string>> filter = new List<Tuple<int, string, string>>();
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
                        case 0:
                            iFormat = ImgUtils.GetImageFormat(sffn);
                            break;
                        case 1:
                            iFormat = ImageFormat.Png;
                            break;
                        case 2:
                            iFormat = ImageFormat.Jpeg;
                            break;
                    }
                    ImgUtils.CopyToImageFormat(sffn, tffn, iFormat);
                    filter.Add(new Tuple<int, string, string>(itm.Item1, tfn, itm.Item3));
                }
                if (iPicker.chkCustom2.IsChecked.Value)
                {
                    ImageDepot vdf = iPicker.iDepot.VirtualClone(targetFolder, filter);
                    if (Utils.isNull(vdf)) return;
                    List<string> ls = Utils.readList(Path.Combine(Utils.configPath, "export-template.xhml"), false);
                    Dictionary<string, string> opts = Utils.readDict(Path.Combine(Utils.configPath, "export-template.opts"));
                    Dictionary<string, object> rep = new Dictionary<string, object>();
                    foreach (var pair in opts) rep.Add(pair.Key, Convert.ToString(pair.Value));
                    List<string> li = new List<string>();
                    foreach (ImageInfo ii in vdf.items)
                        li.Add(ii.To_String() + ",");
                    rep.Add("IMAGES", li);
                    List<string> lt = Utils.CreateFromTemplate(ls, rep);
                    Utils.writeList(Path.Combine(targetFolder, "Scripthea-images.html"), lt);
                    if (opts.ContainsKey("showWebpage"))
                        if (Convert.ToInt32(opts["showWebpage"]) == 1)
                            Utils.CallTheWeb(Path.Combine(targetFolder, "Scripthea-images.html"));
                }
                Log(lot.Count.ToString() + " images have been exported to " + targetFolder);
            }
            finally { Mouse.OverrideCursor = null; iPicker.btnCustom.Background = Brushes.White; }
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

