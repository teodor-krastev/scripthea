using System;
using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
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
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;
using scripthea.viewer;
using scripthea.options;
using UtilsNS;

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
        private Options opts; Dictionary<string, string> wopts; private string exportOptions = Path.Combine(Utils.configPath, "export-template.opts");
        public void Init(ref Options _opts)
        {
            opts = _opts;
            iPicker.Init(ref _opts); iPicker.chkCustom2.Foreground = Brushes.DarkGreen; 
            List<string> ls = new List<string>(new string[] { "keep image types", "export all as .PNG", "export all as .JPG" });
            Button btnExport = iPicker.Configure(' ', ls, "Rename files to prompts", "Webpage options", "Export", true);
            btnExport.Click += new RoutedEventHandler(Export); btnExport.ToolTip = "Export image depot to another folder with optional web-page viewer";
            OnChangeDepot(null, null);             
            iPicker.chkCustom2.IsChecked = false; iPicker.chkCustom2.Checked += chkCustom2Checked_Checked; iPicker.chkCustom2.Unchecked += chkCustom2Checked_Checked;
            iPicker.OnChangeDepot += new RoutedEventHandler(OnChangeDepot);
            iPicker.AddMenuItem("Convert .PNG to .JPG").Click += new RoutedEventHandler(ConvertPNG2JPG);
            iPicker.comboCustom.SelectionChanged += FileType_SelectionChanged;
            iPicker.tiStats.Visibility = Visibility.Collapsed;
            wopts = Utils.readDict(exportOptions);
            Wopts2Visuals();
        }          
        public void Finish()
        {
            Visuals2Wopts(false);
            Utils.writeDict(exportOptions, wopts);
        }
        private void OnChangeDepot(object sender, RoutedEventArgs e)
        {
            VisualHelper.SetButtonEnabled(iPicker.btnCustom, iPicker.isEnabled); 
            ImageDepot df = sender as ImageDepot;
            df?.Validate(null); 
        }
        private void FileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (iPicker.comboCustom.SelectedIndex == 2) chkCreateJson.Visibility = Visibility.Visible;
            else chkCreateJson.Visibility = Visibility.Collapsed;
        }
        public bool SaveJpgWfAware(string imagePng, string imageJpg, ImageInfo ii) 
        {
            if (!File.Exists(imagePng)) { Utils.TimedMessageBox("Error: file <" + imagePng + "> not found"); return false; }
            if (ImgUtils.GetImageType(imagePng) != ImgUtils.ImageType.Png) { Utils.TimedMessageBox("Error: file <" + imagePng + "> is not of png type"); return false; }
            if (ImgUtils.GetImageType(imageJpg) != ImgUtils.ImageType.Jpg) { Utils.TimedMessageBox("Error: file <" + imageJpg + "> is not of jpeg type"); return false; }
            string meta = ""; ImgUtils.GetMetadataStringComfy(imagePng, out meta); if (meta == null) meta = "";  meta = meta.Trim(); string ext = ".json";
            if (meta == "" && ii != null) { meta = ii.To_String(); ext = ".dict"; }
            if (meta != "") File.WriteAllText(Path.ChangeExtension(imageJpg, ext), meta);

            ImgUtils.CopyToImageToFormat(imagePng, imageJpg, ImgUtils.ImageType.Jpg);

            return true; // SetJpgMetadata(imageJpg, meta);
        }

        private void ConvertPNG2JPG(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = iPicker.imageDepot == "" ? SctUtils.defaultImageDepot : iPicker.imageDepot;
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
                    BitmapImage image = ImgUtils.LoadBitmapImageFromFile(filePath);
                    string newFilePath = Utils.AvoidOverwrite(Path.ChangeExtension(filePath, ".jpg"));
                    ImgUtils.SaveBitmapImageToDisk(image,newFilePath); cnt++;
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

                List<Tuple<int, string, int, string>> lot = iPicker.ListOfTuples(true, false); // idx (0 based), filename, prompt
                if (lot.Count.Equals(0)) { opts.Log("Error[887]: not checked images."); return; }

                string sourceFolder = iPicker.iDepot.path; string targetFolder = "";
                CommonOpenFileDialog dialog = new CommonOpenFileDialog(); //dialog.InitialDirectory = ImgUtils.defaultImageDepot;
                dialog.Title = "Select an empty folder for the exported image depot";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) targetFolder = dialog.FileName;
                else return;
                if (targetFolder.Equals(sourceFolder, StringComparison.InvariantCultureIgnoreCase))
                {
                    opts.Log("Error[761]: source and target folders must be different."); return;
                }
                string[] files = Directory.GetFiles(targetFolder);                     
                if (files.Length > 0)
                {
                    if (!Utils.ConfirmationMessageBox("Target folder <"+targetFolder+"> is not empty.\n Do you like me to clear the content?")) return;
                    foreach (string file in files)
                        File.Delete(file);
                }
                string tfn = ""; List<Tuple<int, string, int, string>> filter = new List<Tuple<int, string, int, string>>();
                foreach (var itm in lot)
                {
                    tfn = iPicker.chkCustom1.IsChecked.Value ? itm.Item2.Substring(0, Math.Min(150, itm.Item2.Length)) : itm.Item4;
                    tfn = Utils.correctFileName(tfn);
                    if (!Utils.validFileName(tfn)) tfn = itm.Item4; // prompt text not suitable for filename
                    string sffn = Path.Combine(iPicker.imageDepot, itm.Item4); // src full path
                    string tffn = Path.Combine(targetFolder, tfn);
                    ImgUtils.ImageType iFormat = ImgUtils.ImageType.Unknown; // intended (target) format
                    switch (iPicker.comboCustom.SelectedIndex)
                    {
                        case 0:
                            iFormat = ImgUtils.GetImageType(sffn);
                            break;
                        case 1:
                            iFormat = ImgUtils.ImageType.Png;
                            break;
                        case 2:
                            iFormat = ImgUtils.ImageType.Jpg;
                            break;
                    }
                    if (iFormat == ImgUtils.ImageType.Jpg)
                    {
                        ImgUtils.ImageType sFormat = ImgUtils.GetImageType(sffn);
                        tffn = Path.ChangeExtension(tffn, ".jpg"); ImageInfo ii = Utils.InRange(itm.Item1,0, iPicker.iDepot.items.Count-1) ? iPicker.iDepot.items[itm.Item1] : null;
                        switch (sFormat) // source format
                        {
                            case ImgUtils.ImageType.Png: SaveJpgWfAware(sffn, tffn, ii);
                                break;
                            case ImgUtils.ImageType.Jpg: File.Copy(sffn, tffn);
                                break;
                            default: opts.Log("Error: image types conflict");
                                break;
                        }
                    }
                    else tffn = ImgUtils.CopyToImageToFormat(sffn, tffn, iFormat);
                    if (tffn != "") filter.Add(new Tuple<int, string, int, string>(itm.Item1, itm.Item2, itm.Item3, Path.GetFileName(tffn)));
                }
                if (!Visuals2Wopts(true)) return;
                bool bcw = wopts != null;
                if (bcw) bcw &= wopts.ContainsKey("createWebpage");
                if (bcw) bcw &= wopts["createWebpage"] == "1";
                if (bcw)
                {                    
                    ImageDepot vdf = iPicker.iDepot.VirtualClone(targetFolder, filter);
                    if (Utils.isNull(vdf)) return;
                    List<string> ls = Utils.readList(Path.Combine(Utils.configPath, "export-template.xhml"), false);
                    Dictionary<string, object> rep = new Dictionary<string, object>();
                    foreach (var pair in wopts) rep.Add(pair.Key, Convert.ToString(pair.Value));

                    string defaultTitle = "Scripthea images generated by Stable Diffusion";
                    if (tbWebpageTitle.Text.Trim() == "")
                    { rep.Add("pageTitle", defaultTitle); rep.Add("pageSubtitle", ""); }
                    else { rep.Add("pageTitle", tbWebpageTitle.Text.Trim()); rep.Add("pageSubtitle", defaultTitle); }

                    List<string> li = new List<string>();
                    foreach (ImageInfo ii in vdf.items)
                        li.Add(ii.To_String() + ",");
                    rep.Add("IMAGES", li); 
                    vdf.Save(true);
                    List<string> lt = Utils.CreateFromTemplate(ls, rep);
                    Utils.writeList(Path.Combine(targetFolder, "Scripthea-images.html"), lt);
                    Utils.CallTheWeb(Path.Combine(targetFolder, "Scripthea-images.html"));
                }
                opts.Log(lot.Count.ToString() + " images have been exported to " + targetFolder);
            }
            finally { Mouse.OverrideCursor = null; iPicker.btnCustom.Background = Brushes.White; }
        }
        private void Wopts2Visuals() 
        {
            if (wopts == null) return;
            if (wopts.ContainsKey("showPrompt")) chkShowPrompt.IsChecked = wopts["showPrompt"] == "1";
            if (wopts.ContainsKey("showFilename")) chkShowFilename.IsChecked = wopts["showFilename"] == "1";
            if (wopts.ContainsKey("createWebpage")) chkCreateWebpage.IsChecked = wopts["createWebpage"] == "1";
            if (wopts.ContainsKey("exportType")) iPicker.comboCustom.SelectedIndex = Convert.ToInt32(wopts["exportType"]);
            if (wopts.ContainsKey("exportJson")) chkCreateJson.IsChecked = wopts["exportJson"] == "1";

            if (wopts.ContainsKey("imgWidth")) tbImgWidth.Text = wopts["imgWidth"].Trim('%');
            if (wopts.ContainsKey("imgPerRow")) tbImgPerRow.Text = wopts["imgPerRow"];

            FileType_SelectionChanged(null, null);
        }
        private void chkCustom2Checked_Checked(object sender, RoutedEventArgs e)
        {
            if (iPicker.chkCustom2.IsChecked.Value) rowWebOptions.Height = new GridLength(70);
            else rowWebOptions.Height = new GridLength(1);
        }
        private bool Visuals2Wopts(bool errorMsg)
        {
            void CondMsg(string msg)
            {
                if (errorMsg) Utils.TimedMessageBox(msg, "Error", 3000);
            }
            if (wopts == null) wopts = new Dictionary<string, string>();
            wopts["showPrompt"] = chkShowPrompt.IsChecked.Value ? "1" : "0";
            wopts["showFilename"] = chkShowFilename.IsChecked.Value ? "1" : "0";
            wopts["createWebpage"] = chkCreateWebpage.IsChecked.Value ? "1" : "0";
            wopts["exportType"] = iPicker.comboCustom.SelectedIndex.ToString();
            wopts["exportJson"] = chkCreateJson.IsChecked.Value ? "1" : "0"; 

            int iw = 100;
            if (!int.TryParse(tbImgWidth.Text, out iw))
            {
                CondMsg("Syntax error: " + tbImgWidth.Text); return false;
            }
            if (!Utils.InRange(iw, 10,100))
            {
                CondMsg("Out of range error: [10..100] " + tbImgWidth.Text);
                iw = Utils.EnsureRange(iw, 10, 100); tbImgWidth.Text = iw.ToString();
            }
            wopts["imgWidth"] = iw.ToString()+'%';

            int ir = 5;
            if (!int.TryParse(tbImgPerRow.Text, out ir))
            {
                CondMsg("Syntax error: " + tbImgPerRow.Text); return false;
            }
            if (!Utils.InRange(ir, 2, 20))
            {
                CondMsg("Out of range error: [2..20] " + tbImgPerRow.Text);
                iw = Utils.EnsureRange(ir, 2, 20); tbImgPerRow.Text = ir.ToString();
            }
            wopts["imgPerRow"] = ir.ToString();
            return true;
        }

        private void iPicker_Loaded()
        {

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

