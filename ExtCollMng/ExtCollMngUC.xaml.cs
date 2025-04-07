using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Http;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Path = System.IO.Path;
using UtilsNS;
using System.Data;

namespace ExtCollMng
{
    public class ECwebRecord
    {
        public string fldName; // folder name (no path); a must; it serves as identifier
        public int rowCount; // must (total if multi-file)
        public bool useCategories;
        public string usage; // what's that
        public string version;
        public string novelty; // what's new (opt.)
        public string zipName; // typically fName.zip; later name*.zip for multi-file 
        public int zipSize;
        public string zipMD5; // of the zip (maybe later)
        public Dictionary<string, string> ToDictionary(bool toShow = true) // 
        {
            Dictionary<string, string> dct = new Dictionary<string, string>();
            dct["fldName"] = fldName;
            dct["rowCount"] = rowCount.ToString();
            dct["useCategories"] = useCategories.ToString();
            dct["usage"] = usage; // comment from local
            dct["version"] = version;
            dct["novelty"] = novelty; // separate dict. file
            dct["zipName"] = zipName;
            dct["zipSize"] = zipSize.ToString();
            if (!toShow) dct["zipMD5"] = zipMD5;
            return dct;
        }
        public static ECwebRecord OpenECwebRecord(string json)
        {
            return JsonConvert.DeserializeObject<ECwebRecord>(json);
        }
    }

    /// <summary>
    /// Interaction logic for ExtCollMngUC.xaml
    /// </summary>
    public partial class ExtCollMngUC : UserControl
    {
        public ExtCollMngUC()
        {
            InitializeComponent();
        }       
        protected readonly string urlColl = "https://scripthea.com/ext-collections/";
        protected readonly string descColl = "ext-collections.DESC";

        public bool Mute = false;
        
        protected string cuesFolder;
        public string tempFolder { get { return Path.Combine(cuesFolder, "_temp"); } }
        public void Init(Utils.LogHandler _OnLog, string rootCuesFolder) //string activeColl
        {
            try
            {
                OnLog += _OnLog;
                if (Directory.Exists(rootCuesFolder)) cuesFolder = rootCuesFolder;
                else { Utils.TimedMessageBox("Error: The cue folder <" + rootCuesFolder + "> not found"); return; }               
            }
            finally
            {
            }             
        }        
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (!txt.StartsWith("Error") && Mute) return;
            if (OnLog != null) OnLog(txt, clr);
        }
        public void Finish()
        {

        }
        public List<ECwebRecord> ReadRemoteExtColls()
        {
            //Log("Reading remote ext.collections info.");
            Uri url = new Uri(urlColl + descColl);
            string[] jsonl = new string[0]; //File.ReadAllLines(Path.Combine(cuesFolder,"ext-collections.DESC"));  
            using (var client = new WebClient())
            {
                try
                {
                    jsonl = client.DownloadString(url).Split('\n');
                    // Parse with Newtonsoft.Json if you have a matching data class
                    // var myObject = JsonConvert.DeserializeObject<MyDataClass>(jsonString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                    Utils.TimedMessageBox(@"Error[927]: collections description file " + url.AbsoluteUri + " is not found. ", "Error", 7000); return null;
                }
            }
            List<ECwebRecord> webRecords = new List<ECwebRecord>();
            foreach (string rec in jsonl)
                { if (rec.Trim() != string.Empty) webRecords.Add(ECwebRecord.OpenECwebRecord(rec)); }
            return webRecords;
        }
        public Dictionary<string, ECdesc> UpdateLocalColls()
        {
            Dictionary<string, ECdesc> localColls = new Dictionary<string, ECdesc>();
            localColls.Clear(); //Log("Reading local ext.collections folders.");
            string[] subdirectoryEntries = Directory.GetDirectories(cuesFolder, "*", SearchOption.TopDirectoryOnly);
            foreach (string subdirectory in subdirectoryEntries)
            {
                if (subdirectory.Contains("_temp")) continue;
                if (File.Exists(Path.Combine(subdirectory, ECdesc.descName)))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(subdirectory);
                    if (localColls.ContainsKey(directoryInfo.Name)) continue;
                    localColls.Add(directoryInfo.Name, ECdesc.OpenECdesc(subdirectory));
                }
            }
            return localColls;
        } 
        public enum CollLocation { unsupported, local, remote, same, update } // only local; only remote; both the same; update avail
        public class ExtCollItem
        {            
            public ECdesc cdesc;
            public ECwebRecord webRecord;
            public CollLocation collLoc
            {
                get
                {
                    if (cdesc != null && webRecord == null) return CollLocation.local;
                    if (cdesc == null && webRecord != null) return CollLocation.remote;
                    if (cdesc != null && webRecord != null)
                    {
                        double lclVer = Convert.ToDouble(cdesc.version);double wrVer = Convert.ToDouble(webRecord.version); 
                        if (wrVer > lclVer) return CollLocation.update;
                        else return CollLocation.same;
                    }
                    return CollLocation.unsupported;
                }
            }
            public string numPrompts
            {
                get
                {
                    switch (collLoc)
                    {
                        case CollLocation.local: return cdesc.rowCount.ToString();
                        case CollLocation.remote:
                        case CollLocation.same: return webRecord.rowCount.ToString();
                        case CollLocation.update: return cdesc.rowCount.ToString() + "/" + webRecord.rowCount.ToString();
                        default: return "NA";
                    }
                }
            }
        } 
        public Dictionary<string, ExtCollItem> CompileCommonDict()
        {
            totalLocal = 0; totalRemote = 0;
            Dictionary<string, ExtCollItem> dct = new Dictionary<string, ExtCollItem>();
            foreach (KeyValuePair<string, ECdesc> pair in UpdateLocalColls())
            {
                dct.Add(pair.Key, new ExtCollItem() { cdesc = pair.Value } );
                totalLocal += pair.Value.rowCount;
            }
            if (!Utils.IsInternetConnectionAvailable()) { Log("Warning: Internet connection issue!"); return dct; }
            foreach (ECwebRecord wr in ReadRemoteExtColls())
            {
                if (dct.ContainsKey(wr.fldName)) dct[wr.fldName].webRecord = wr; // both places
                else dct.Add(wr.fldName, new ExtCollItem() { webRecord = wr }); 
                totalRemote += wr.rowCount;
            }
            return dct;
        }
        int totalLocal, totalRemote;
        public void UpdateCollInfo(string activeColl = "")
        {
            lbCollections.Items.Clear(); string actCollName = Utils.GetTopDirectory(activeColl); 
            foreach (KeyValuePair<string, ExtCollItem> pair in CompileCommonDict())
            {
                RadioButton rb = new RadioButton() { Content = pair.Key + " (" + pair.Value.numPrompts + ") " + pair.Value.collLoc.ToString(), Height = 18, FontSize = 14, Tag = pair.Value };
                rb.Checked += new RoutedEventHandler(RadioButton_Checked); rb.Unchecked += new RoutedEventHandler(RadioButton_Unchecked);
                if (actCollName == "") rb.IsChecked = lbCollections.Items.Count == 0; 
                else rb.IsChecked = pair.Key.Equals(actCollName, StringComparison.InvariantCultureIgnoreCase); 
                if (lbCollections.Items.Count == 0) rb.Margin = new Thickness(0, 5, 0, 0);
                switch (pair.Value.collLoc)
                {
                    case CollLocation.local: 
                        break;
                    case CollLocation.remote: rb.Foreground = Brushes.Navy; 
                        break;
                    case CollLocation.same:  rb.Foreground = Utils.ToSolidColorBrush("#FF565656"); // Brushes.DarkGray;
                        break;
                    case CollLocation.update: rb.Foreground = Brushes.DarkGreen;
                        break;
                }
                lbCollections.Items.Add(rb);                   
            }
            tbLocalCollections.Text = "Local collections, total of " + totalLocal+" prompts";
            tbRemoteCollections.Text = "Remote collections, total of " + totalRemote + " prompts";
        }
        private string selectedColl; private ExtCollItem selectedItem;
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton; rb.FontFamily = new FontFamily("Segoe UI Semibold");
            string header = rb.Content as string; selectedColl = header.Split('(')[0].Trim();
            selectedItem = rb.Tag as ExtCollItem; if (selectedItem == null) { Log("Error: internal 187"); return; }
            switch (selectedItem.collLoc)
            {
                case CollLocation.local: tbStatus.Text = "That collection exists only locally";
                    break;
                case CollLocation.remote: tbStatus.Text = "New collection is available for download";
                    break;
                case CollLocation.same: tbStatus.Text = "The same collection (local and remote)";
                    break;
                case CollLocation.update: tbStatus.Text = "The updated version is available for download";
                    break;
                case CollLocation.unsupported: tbStatus.Text = "Unsupported situation (maybe obsolete?)";
                    break;
            }
            tbStatus.Foreground = rb.Foreground; tbStatus.FontSize = rb.FontSize;

            // local
            btnVisitSource.IsEnabled = selectedItem.cdesc != null && selectedItem.cdesc.urlSource != "";
            if (selectedItem.cdesc != null)
            {
                dGlocal.ItemsSource = selectedItem.cdesc.ToDictionary(); 
                if (dGlocal.Columns.Count > 0) dGlocal.Columns[0].Header = "Coll.Property";
            }
            else dGlocal.ItemsSource = null;
            // remote
            btnDownload.IsEnabled = selectedItem.webRecord != null; btnDownloadZip.IsEnabled = btnDownload.IsEnabled;
            if (selectedItem.webRecord != null)
            {
                dGremote.ItemsSource = selectedItem.webRecord.ToDictionary();
                if (dGremote.Columns.Count > 0) dGremote.Columns[0].Header = "Coll.Property";
            }
            else dGremote.ItemsSource = null;
        }
        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton; rb.FontFamily = new FontFamily("Segoe UI");
        }
        public event Action OnChangeColls;
        private void btnLoadColl_Click(object sender, RoutedEventArgs e)
        {
            UpdateCollInfo();
            OnChangeColls?.Invoke();
        }        
        private void btnVisitSource_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null) return;
            if (selectedItem.cdesc == null) return;
            if (selectedItem.cdesc.urlSource != "")
            {
                if (!Utils.CallTheWeb(selectedItem.cdesc.urlSource)) Log("Error: i-net communication problem");
            }                
        }
        public async Task DownloadZipFileAsync(string url, string destinationPath, int zipSize)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = zipSize; //response.Content.Headers.ContentLength;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            long bytesRead = 0;
                            int bytesReceived;
                            while ((bytesReceived = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesReceived);
                                bytesRead += bytesReceived;
                                if (totalBytes.HasValue)
                                {
                                    // Calculate and report the progress percentage
                                    int progressPercentage = (int)((bytesRead / (double)totalBytes) * 100);
                                    tbStatus.Text = "Downloading " + progressPercentage.ToString() + "%";
                                    progressDownload.Value = progressPercentage;
                                    //Console.WriteLine($"Download progress: {progressPercentage}%");
                                }
                            }
                        }
                    }
                }
            }
        }
        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem == null) { Log("Error: no selected item"); return; }
            if (selectedItem.webRecord == null) { Log("Error: no web-record"); return; }
            if (progressDownload.Visibility == Visibility.Visible) { Log("Warning: ongoing download."); return; }
            try
            {
                ECwebRecord wr = selectedItem.webRecord;               
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
                // dowloading
                string downFile = Path.Combine(tempFolder, wr.zipName); 
                if (File.Exists(downFile))
                {
                    FileInfo fi = new FileInfo(downFile);
                    if (fi.Length == wr.zipSize)
                    {
                        if (!Utils.ConfirmationMessageBox("File <" + wr.zipName + "> is already dowloaded in <"+ tempFolder+">.\n Overwrite (Yes) or Cancel (No)?"))
                        { Log("User cancelation"); return; }
                    }
                    File.Delete(downFile);
                }
                Log("Downloading " + wr.zipName + "...");
                progressDownload.Visibility = Visibility.Visible;
                await DownloadZipFileAsync(urlColl + wr.zipName, downFile, wr.zipSize); // downloading itself
                if (File.Exists(downFile))
                {
                    FileInfo fi = new FileInfo(downFile); 
                    if (fi.Length != wr.zipSize) { Log("Error: Download unsuccessful! (missmaching size)"); return; }
                    if (!String.IsNullOrEmpty(wr.zipMD5))
                    {
                        if (!Utils.GetMD5Checksum(downFile).Equals(wr.zipMD5)) { Log("Error: Download unsuccessful! (MD5 checksum wrong)"); return; }
                    }
                    Log("Download successful!");
                }
                else { Log("Error: Download unsuccessful! (file not found)"); return; }
                progressDownload.Visibility = Visibility.Hidden;
                UnpackColl(downFile, wr.fldName);
            }
            catch (IOException ex) { Log("Error: Download unsuccessful! " + ex.Message);  Log("If the error persists you may try <Download coll. zip via browser>.", Brushes.Maroon); }
            finally
            {
                progressDownload.Visibility = Visibility.Hidden; 
            }
        }
        private void UnpackColl(string downFile, string fldName, bool clearAfter = true)
        {
            string fldLocal = "";
            try
            {           
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
                string fldRemote = Path.Combine(tempFolder, fldName);
                if ((Directory.Exists(fldRemote))) Directory.Delete(fldRemote, true);

                Utils.UnzipFile(downFile, tempFolder); Log("Unziping to " + fldRemote);
                if (!Directory.Exists(fldRemote)) { Log("Error:  Download unsuccessful! Folder <" + fldRemote + "> not found)."); return; }
                fldLocal = Path.Combine(cuesFolder, fldName);
                bool bb = false;
                if (Directory.Exists(fldLocal)) // if Local there
                {
                    bb = true;
                    string[] fa = Directory.GetFiles(fldRemote);
                    foreach (string fn in fa)
                    {
                        string fr = Path.GetFileName(fn);
                        bb &= Utils.CompareFilesByHash(Path.Combine(fldLocal, fr), fn);
                    }
                }
                if (bb)
                {
                    Log("Downloaded collection and local collection are one and the same. Clear up downloaded!");
                    Directory.Delete(fldRemote, true);                   
                    return;
                }
                if (Directory.Exists(fldLocal)) // if Local there, remove the files to be moved from remote
                {
                    List<string> fl = new List<string>(Directory.GetFiles(fldLocal, "*.STX"));
                    fl.AddRange(new List<string>(Directory.GetFiles(fldLocal, "*.SJL")));
                    foreach (string fn in fl)
                    {
                        File.Delete(fn); Log("clear: " + fn);
                    }

                    fl = new List<string>(Directory.GetFiles(fldRemote)); // temp
                    foreach (string fn in fl)
                    {
                        string fr = Path.Combine(fldLocal, Path.GetFileName(fn));
                        if (File.Exists(fr)) { File.Delete(fr); Log("clear: " + fr); }
                        Directory.Move(fn, fr);
                    }
                    Directory.Delete(fldRemote, true);
                }
                else Directory.Move(fldRemote, fldLocal);
            }
            finally
            {
                if (File.Exists(downFile) && clearAfter) { File.Delete(downFile); Log("clear up: " + downFile); }
                if (Directory.Exists(fldLocal)) { Log("Succesful  ext. collection unpacking!"); btnLoadColl_Click(null, null); }           
                tbStatus.Text = "Status";
            }               
        }
        private void btnDownloadZip_Click(object sender, RoutedEventArgs e)
        {        
            ECwebRecord wr = selectedItem.webRecord; 
            if (wr == null) { Log("Error: no remote collection is selected"); return; }
            if (Utils.CallTheWeb(urlColl + wr.zipName)) Log("Downloading using your browser: " + wr.zipName);
            else Log("Error: downloading using your browser: " + wr.zipName);
        }
        private void btnUnpackZip_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".zip"; // Default file extension
            dlg.Filter = "zip files (.zip)|*.zip"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();
            if (!(bool)result) return;
            string fld = Path.ChangeExtension(Path.GetFileName(dlg.FileName), "").Trim('.'); 
            UnpackColl(dlg.FileName, fld, false); // the filename must be the same as collection folder name
        }
        private void lbCollections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RadioButton rb = lbCollections.SelectedItem as RadioButton;
            if (rb == null) return;
            rb.IsChecked = true;
        }
    }
}
