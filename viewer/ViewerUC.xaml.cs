using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using scripthea.master;
using UtilsNS;
using Path = System.IO.Path;
using Newtonsoft.Json;

namespace scripthea.viewer 
{
    public class ImageInfo
    {
        public enum ImageGenerator { StableDiffusion, Crayion, FromDescFile}       
        public ImageInfo()
        {

        }
        public ImageInfo(string fullfilename, ImageGenerator imageGenerator, bool keepName) 
        {
            filename = "";
            if (!File.Exists(fullfilename)) return;
            string suggestedName = ""; bool bb;
            switch (imageGenerator) 
            {
                case ImageGenerator.StableDiffusion:
                    bb = FromSDFile(fullfilename, out suggestedName); 
                    if (!bb) 
                    {
                        FromSDFile(fullfilename, out suggestedName); return; // when it is not SD file
                    }                       
                    break;
                case ImageGenerator.Crayion:
                    if (!FromCraiyonFile(fullfilename, out suggestedName)) return;
                    break;
            }
            if (keepName) { filename = Path.GetFileName(fullfilename); return; } 
            try
            {
                if (!fullfilename.Equals(suggestedName, StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Move(fullfilename, suggestedName); // Rename the oldFileName into newFileName                    
                }             
                filename = Path.GetFileName(suggestedName);       
            }
            catch (System.IO.IOException IOe) { filename = ""; Utils.TimedMessageBox("Error: (" + System.IO.Path.GetFileName(fullfilename) + ") " + IOe.Message);  }
        }
        private List<string> SplitFilename(string fn, char sep) // pattern <txt><sep><txt><sep><cue><ext>
        {
            var ls = new List<string>();
            string[] spl = fn.Split(sep);
            if (spl.Length < 3) return ls;
            ls.Add(spl[0]);
            ls.Add(spl[1] + Utils.randomString(9 - spl[1].Length, true));
            string ext = System.IO.Path.GetExtension(fn);
            string fnn = System.IO.Path.ChangeExtension(fn, null); // no ext            
            ls.Add(fnn.Substring(spl[0].Length + spl[1].Length + 2)); // cue
            ls.Add(ext);
            return ls;
        }
        public ImageInfo(string json)
        {
            FromString(json);
        }
        public ImageInfo(Dictionary<string, object> dict)
        {
            FromDictionary(dict);
        }
        /*{"prompt", "String" }, {"negative_prompt", "String" }, { "seed", "Int64" }, { "width", "Int64" }, { "height", "Int64" },
            { "sampler_name", "String" }, { "cfg_scale", "Double" }, { "steps", "Int64" }, { "batch_size", "Int64" }, { "restore_faces", "Boolean" },
            { "sd_model_hash", "String" }, { "denoising_strength", "Int64" }, { "job_timestamp", "String" }*/
        public string prompt { get; set; }
        public string negative_prompt { get; set; }
        public long steps { get; set; }
        public string sampler_name { get; set; }
        public double cfg_scale { get; set; }
        public double denoising_strength { get; set; }
        public long seed { get; set; }
        public long width { get; set; }
        public long height { get; set; }
        public long batch_size { get; set; }
        public bool restore_faces { get; set; }
        public string sd_model_hash { get; set; }
        public string filename { get; set; } // without folder
        public string job_timestamp { get; set; }
        // its own
        public object tags { get; set; } // open stucture for future use: set of labels to be selected and/or define position in a image structure within a Scripthea project
        public string history { get; set; } // stages of variations, '|' separated -> for future use
        public string MD5Checksum { get; set; } 
        // internal
        public bool IsEnabled() { return !filename.Equals(""); } // file hasn't been assinged
        public bool IsModified(string folder) // check if recorded MD5 is egual to MD5 of the image file
        {
            string ffn = Path.Combine(folder, filename);
            if (!File.Exists(ffn)) return false;
            return MD5Checksum.Equals(ffn);
        }
        public string Size()
        {
            return Convert.ToString(width) + "x" + Convert.ToString(height);
        }
        
        public void historyAdd(string txt)
        {
            string tx = txt.Replace('|', '_'); // not to confuse
            if (history.Equals("")) history = tx;
            else history += '|' + tx;
        }
        public List<string> historyLog()
        {
            return new List<string>(history.Split('|'));
        }
        public bool FromSDFile(string fullfilename, out string suggestedName) // true if it's there and it's sd image 
        {
            suggestedName = ""; // with folder
            if (!File.Exists(fullfilename)) return false;
            filename = Path.GetFileName(fullfilename);
            MD5Checksum = Utils.GetMD5Checksum(fullfilename);
            history = "";

            Dictionary<string, string> meta;
            bool sd = ImgUtils.GetMetaDataItems(fullfilename, out meta);
            if (sd) FromMetaDictionary(meta);
            if (!sd) { filename = ""; return false; }

            // suggest a name
            List<string> ls = SplitFilename(filename, '-'); 
            if (ls.Count >= 4) 
            {
                if (Utils.isNumeric(ls[0]) && Utils.isNumeric(ls[1])) // check pattern
                    suggestedName = System.IO.Path.ChangeExtension("SD-" + ls[1], ls[3]);                 
            }
            bool sf = false;  
            if (suggestedName == "")
            {
                sf = filename.StartsWith("SD-"); // file is there (previous import)
                suggestedName = sf ? filename : "SD-" + filename; 
            }
            suggestedName = System.IO.Path.Combine(Path.GetDirectoryName(fullfilename), suggestedName); // complete with folder
            if (sd && !sf) // pattern or not, meta data is there
                suggestedName = Utils.AvoidOverwrite(suggestedName);            
            return sd;
        }
        public bool FromCraiyonFile(string fullfilename, out string suggestedName) // true if it's there and it's sd image 
        {
            suggestedName = "";
            if (File.Exists(fullfilename)) return false;
            return true;
        }
        public void FromString(string json)
        {
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            FromDictionary(dict);
        }
        public void FromMetaString(string json)
        {
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            FromMetaDictionary(dict);
        }
        public bool SameAs(ImageInfo imageInfo)
        {
            return To_String().Equals(imageInfo?.To_String());
        }
        public string To_String()
        {
            return JsonConvert.SerializeObject(this);
        }
        public void FromDictionary(Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("prompt")) prompt = Convert.ToString(dict["prompt"]);
            negative_prompt = dict.ContainsKey("negative_prompt") ? Convert.ToString(dict["negative_prompt"]) : "";
            if (dict.ContainsKey("steps")) steps = Convert.ToInt64(dict["steps"]);
            if (dict.ContainsKey("sampler_name")) sampler_name = Convert.ToString(dict["sampler_name"]);
            if (dict.ContainsKey("cfg_scale")) cfg_scale = Convert.ToDouble(dict["cfg_scale"]);
            seed = dict.ContainsKey("seed") ? Convert.ToInt64(dict["seed"]) : -1;
            if (dict.ContainsKey("width")) width = Convert.ToInt64(dict["width"]);
            if (dict.ContainsKey("height")) height = Convert.ToInt64(dict["height"]);
            denoising_strength = dict.ContainsKey("denoising_strength") ? Convert.ToDouble(dict["denoising_strength"]) : 0;
            batch_size = dict.ContainsKey("batch_size") ?Convert.ToInt64(dict["batch_size"]) : 1;
            restore_faces = dict.ContainsKey("restore_faces") ? Convert.ToBoolean(dict["restore_faces"]) : false;            
            if (dict.ContainsKey("sd_model_hash")) sd_model_hash = Convert.ToString(dict["sd_model_hash"]);
            if (dict.ContainsKey("filename")) filename = Convert.ToString(dict["filename"]);
            if (dict.ContainsKey("job_timestamp")) job_timestamp = Convert.ToString(dict["job_timestamp"]);            
            if (dict.ContainsKey("MD5Checksum")) MD5Checksum = Convert.ToString(dict["MD5Checksum"]);
            history = dict.ContainsKey("history") ? Convert.ToString(dict["history"]) : "";
            tags = dict.ContainsKey("tags") ? dict["history"] : null;
        }
        public void FromMetaDictionary(Dictionary<string,string> dict) 
        {
            if (dict.ContainsKey("prompt")) prompt = dict["prompt"]; negative_prompt = ""; denoising_strength = 0; batch_size = 1; restore_faces = false;
            if (dict.ContainsKey("steps")) steps = Convert.ToInt32(dict["steps"]);
            if (dict.ContainsKey("sampler")) sampler_name = dict["sampler"];
            if (dict.ContainsKey("scale")) cfg_scale = Convert.ToInt32(dict["scale"]);
            seed = dict.ContainsKey("seed") ? Convert.ToInt64(dict["seed"]) : -1;
            if (dict.ContainsKey("size"))
            {
                string size = dict["size"]; string[] sa = size.Split('x');
                width =  Convert.ToInt64(sa[0]); height = Convert.ToInt64(sa[1]);
            }
            if (dict.ContainsKey("ModelHash")) sd_model_hash = dict["ModelHash"];
            if (dict.ContainsKey("filename")) filename = dict["filename"];
            if (dict.ContainsKey("MD5Checksum")) MD5Checksum = dict["MD5Checksum"];
            history = dict.ContainsKey("history") ? Convert.ToString(dict["history"]) : "";
        }
        public Dictionary<string, object> ToDictionary(bool SDonly = false)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("prompt", prompt); 
            dict.Add("negative_prompt", negative_prompt);
            dict.Add("steps", steps); 
            dict.Add("sampler_name", sampler_name); 
            dict.Add("cfg_scale", cfg_scale.ToString());
            dict.Add("denoising_strength", denoising_strength); 
            dict.Add("batch_size", batch_size);
            dict.Add("restore_faces", restore_faces);
            dict.Add("seed", seed); 
            dict.Add("sd_model_hash", sd_model_hash); 
            dict.Add("filename", filename); 
            dict.Add("job_timestamp", job_timestamp);
            dict.Add("widht", width); 
            dict.Add("height", height);
            if (SDonly)
            {
                dict.Add("MD5Checksum", MD5Checksum); 
                dict.Add("history", history);
                dict.Add("tags", tags); 
            }
            return dict;
        }
        // clone 1. target.FromString(source.To_String())
        //       2. target.FromDictionary(Source.ToDictionary())
    }
    public static class ImageDepotConvertor
    {
        public static bool Old2New(string idf)
        {
            ImageDepot imageDepot = new ImageDepot(idf, ImageInfo.ImageGenerator.FromDescFile, true);
            imageDepot.Save(true);
            return true; 
        }
        public static bool AutoConvert = true;
        public static bool? CheckFileVersion(string folder) // true - new one (OK); false - old one
        {
            bool bb = false;
            string desc = Path.Combine(folder, ImgUtils.descriptionFile);
            if (!File.Exists(desc)) return null;
            List<string> body = Utils.readList(desc, false);
            if (body.Count == 0) return null;
            if (body[0].StartsWith("#"))
            {
                Dictionary<string, string> header = JsonConvert.DeserializeObject<Dictionary<string, string>>(body[0].Substring(1));
                if (header.ContainsKey("application"))
                {
                    string[] sa = header["application"].Split(' ');
                    if (sa[0] != "Scripthea" || sa.Length != 2) return null; 
                    string[] sb = sa[1].Split('.'); if (sb.Length != 4) return null;
                    bb = Convert.ToInt32(sb[3]) > 69;
                }
            }
            return bb;
        }
    }
    public class ImageDepot
    {
        public ImageDepot(string _folder, ImageInfo.ImageGenerator _imageGenerator = ImageInfo.ImageGenerator.FromDescFile, bool _IsReadOnly = false) 
        {
            if (!Directory.Exists(_folder)) return; isReadOnly = _IsReadOnly; string desc = Path.Combine(_folder, ImgUtils.descriptionFile);
            header = new Dictionary<string, string>(); items = new List<ImageInfo>();
            
            if (_imageGenerator == ImageInfo.ImageGenerator.FromDescFile && File.Exists(desc)) // read type from file
            {
                if (!isReadOnly)
                {
                    bool? bb = ImageDepotConvertor.CheckFileVersion(_folder);
                    if (bb == null) { Utils.TimedMessageBox("Error: Corrupted Scripthea image depot!", "Error", 4000); return; }
                    if (!(bool)bb) // old version
                    {
                        if (ImageDepotConvertor.AutoConvert) ImageDepotConvertor.Old2New(_folder); // convert 
                        else isReadOnly = true;                                                    // OR lock
                    }    
                }                
                List<string> body = Utils.readList(desc,false);
                if (body.Count == 0) return;
                if (body[0].StartsWith("#"))
                {
                    header = JsonConvert.DeserializeObject<Dictionary<string, string>>(body[0].Substring(1));
                    body.RemoveAt(0);
                    if (header.ContainsKey("ImageGenerator"))
                        foreach (ImageInfo.ImageGenerator ig in Enum.GetValues(typeof(ImageInfo.ImageGenerator)))
                        {
                            if (ig.Equals(header["ImageGenerator"])) imageGenerator = ig;
                        }
                    if (header.ContainsKey("application"))
                    {
                        string[] sa = header["application"].Split(' ');
                        if (sa[0] != "Scripthea" || sa.Length != 2) { Utils.TimedMessageBox("Error: NOT Scripthea image depot file!", "Error", 5000); return; }
                        string[] sb = sa[1].Split('.'); if (sb.Length != 4) return;
                        appBuilt = Convert.ToInt32(sb[3]);
                    } 
                    foreach (string ss in body)
                    {
                        ImageInfo ii = new ImageInfo();
                        if (appBuilt < 70) ii.FromMetaString(ss);
                        else ii.FromString(ss);
                        items.Add(ii);
                    }                       
                }
            }
            else
            {
                imageGenerator = _imageGenerator; header.Add("ImageGenerator", imageGenerator.ToString());
                header.Add("webui", "AUTOMATIC1111"); header.Add("application", "Scripthea " + Utils.getAppFileVersion);
            }
            path = _folder;
        }
        public bool isEnabled
        {
            get { return Utils.isNull(header) ? false : header.Count > 0 && !Utils.isNull(items); }
        }
        public int appBuilt { get; private set; }
        public bool isReadOnly { get; private set; }
        public Dictionary<string, string> header;
        public List<ImageInfo> items;
        public bool RemoveAt(int idx, bool inclFile)
        {
            if (!isEnabled || isReadOnly) return false;
            if (!Utils.InRange(idx, 0, items.Count - 1)) return false; 
            if (inclFile)
            {
                string fn = Path.Combine(path, items[idx].filename);
                if (!File.Exists(fn)) return false;
                File.Delete(fn);
            }
            items.RemoveAt(idx);
            return true;
        }
        public ImageDepot VirtualClone(string targetPath, List<Tuple<int,string,string>> filter = null) // int -> index; string -> filename (may differ); string -> prompt (for consistency)
        {
            if (!Directory.Exists(targetPath)) return null;
            ImageDepot dp = new ImageDepot(targetPath, imageGenerator, isReadOnly);
            if (Utils.isNull(filter)) dp.items.AddRange(items);
            else
            {
                foreach (var itm in filter)
                {
                    ImageInfo ii = items[itm.Item1 - 1];
                    if (ii.prompt != itm.Item3) { Utils.TimedMessageBox("Error: broken filter"); break; }
                    ii.filename = itm.Item2; 
                    dp.items.Add(ii);
                }
            } 
            return dp;
        }
        public bool SameAs(ImageDepot imageDepot) // compare data, location-blind 
        {
            bool bb = JsonConvert.SerializeObject(header).Equals(JsonConvert.SerializeObject(imageDepot?.header));
            if ((items.Count != imageDepot.items.Count) || !bb) return false;
            for (int i = 0; i < items.Count; i++)
            {
                bb &= items[i].SameAs(imageDepot.items[i]);
            }
            return bb;
        }
        public List<string> allImages() // in this folder 
        {
            List<string> pngs = new List<string>(Directory.GetFiles(path, "*.png"));
            List<string> jpgs = new List<string>(Directory.GetFiles(path, "*.jpg"));
            pngs.AddRange(jpgs);
            for (int i = 0; i < pngs.Count; i++)
                pngs[i] = Path.GetFileName(pngs[i]);
           return new List<string>(pngs);
        }
        public bool fileMatch(List<string> imgs, out int idxFile, out int idxII) // found match within imgs and items[*].filename
        {
            bool found = false; idxFile = -1; idxII = -1;
            for (int i = 0; i < imgs.Count; i++)
            {            
                for (int j = 0; j <  items.Count; j++)
                {
                    found = items[j].filename.Equals(imgs[i], StringComparison.InvariantCultureIgnoreCase);
                    if (found) { idxFile = i; idxII = j; break; }
                }
                if (found) break;
            }
            return found;
        }
        public List<string> Extras()
        {
            List<string> imgs = allImages();
            int idxFile; int idxII; bool fnd;
            do
            { 
                fnd = fileMatch(imgs, out idxFile, out idxII); 
                if (fnd) imgs.RemoveAt(idxFile);
            } while (fnd);
            return new List<string>(imgs);
        } 
        public string path { get; private set; }
        public ImageInfo.ImageGenerator imageGenerator { get; private set; }
        public bool Validate(bool? correctEntry) // argument: if correctEntry is null ask user
               // return true: if no mismatched entries (OK depot) OR if correctEntry and file is missing then entries are deleted
               // return false: there were missing files with not-corrected entries (unfinished business)
        {
            List<string> allImgs = allImages(); // from the folder
            bool ok = true;
            /*int idxFile; int idxII; bool fnd;
            do { fnd = fileMatch(allImgs, out idxFile, out idxII); } while (fnd);*/
            int i = 0; int itemsCount = items.Count; 
            while (i < items.Count)
            {
                bool found = false; 
                foreach (string img in allImgs)
                {
                    found = items[i].filename.Equals(img, StringComparison.InvariantCultureIgnoreCase);
                    if (found) break;  
                }
                if (!found)
                {
                    if (correctEntry == null)
                    {
                        //Configure the message box
                        var messageBoxText =
                            "Image depot entry <"+ items[i].filename+ "> has no corresponding image file.\rClick \"Yes\" to correct entry list, \"No\" to skip this correction, or \"Cancel\" to exit validation.";
                        // Display message box
                        var messageBoxResult = MessageBox.Show(messageBoxText, "Image Depot Validation", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                        // Process message box results
                        switch (messageBoxResult)
                        {
                            case MessageBoxResult.Yes: // correct entry list
                                items.RemoveAt(i); found = true; continue;                                
                            case MessageBoxResult.No: // skip this correction
                                break;
                            case MessageBoxResult.Cancel: // exit validation
                                return false;                                
                        }
                    }
                    else
                    {
                        if (Convert.ToBoolean(correctEntry)) { items.RemoveAt(i); continue; }
                    }                    
                }
                i++; ok &= found;
            }
            if (itemsCount != items.Count)
            {
                Save(true); Utils.TimedMessageBox((itemsCount - items.Count).ToString()+" image depot entries have been removed", "Warning");
            }
            return ok;
        }
        public bool Append(ImageInfo ii)
        {
            if (!isEnabled || isReadOnly) return false ;
            using (StreamWriter w = File.AppendText(Path.Combine(path, ImgUtils.descriptionFile)))
            {
                w.WriteLine(ii.To_String());
            }
            items.Add(ii); return true;
        }
        public void Save(bool forced = false)
        {
            if (!forced)
                if (isReadOnly) return;
            List<string> ls = new List<string>();
            header["application"]= "Scripthea " + Utils.getAppFileVersion;
            ls.Add("#"+ JsonConvert.SerializeObject(header));
            foreach (ImageInfo ii in items)
                ls.Add(ii.To_String());
            //Utils.DelayExec(20, new Action(() => {  }));       
            Utils.writeList(Path.Combine(path, ImgUtils.descriptionFile), ls);
            Utils.Sleep(200);
        }
        public List<Tuple<int, string, string>> Export2Viewer()
        {
            List<Tuple<int, string, string>> lst = new List<Tuple<int, string, string>>();
            for (int i = 0; i < items.Count; i++)
                lst.Add(new Tuple<int, string, string>(i+1, items[i].filename, items[i].prompt));
            return lst;
        }
    }
    interface iPicList
    {
        void Init(ref Options _opts, bool _checkable);
        void Finish();
        bool IsAvailable { get; }
        bool HasTheFocus { get; set; }
        ImageDepot iDepot { get; set; }
        string loadedDepot { get; set; }
        bool FeedList(string imageFolder); // the way to load the list
        bool FeedList(ref ImageDepot _iDepot); // external iDepot; regular use
        void UpdateVis(); // update visual from iDepot
        void SynchroChecked(List<Tuple<int, string, string>> chks);
        void SetChecked(bool? check); // if null invert; returns checked
        string markMask { get; }
        void Mark(string mask); // mark some items; if "" unmark all 
        void Clear(bool inclDepotItems = false);
        int selectedIndex { get; set; } // one based index in no-checkable mode
        int Count { get; }
        List<Tuple<int, string, string>> GetItems(bool check, bool uncheck); // idx, filename, prompt
    }
    /// <summary>
    /// Interaction logic for ViewerUC.xaml
    /// </summary>
    public partial class ViewerUC : UserControl, iFocusControl
    {       
        public ViewerUC()
        {
            InitializeComponent();
            views = new List<iPicList>(); 
            views.Add(tableViewUC); tableViewUC.SelectEvent += new TableViewUC.PicViewerHandler(picViewerUC.loadPic); 
            views.Add(gridViewUC);  gridViewUC.SelectEvent += new GridViewUC.PicViewerHandler(picViewerUC.loadPic);
            gridViewUC.OnLog += new Utils.LogHandler(Log); picViewerUC.OnLog += new Utils.LogHandler(Log);
        }
        private ImageDepot iDepot;
        iPicList activeView { get { return views[tabCtrlViews.SelectedIndex]; } }
        private DispatcherTimer timer;
        private Options opts;
        public void Init(ref Options _opts) // ■▬►
        {
            opts = _opts;
            chkAutoRefresh.IsChecked = opts.viewer.Autorefresh; imageFolder = opts.composer.ImageDepotFolder; 
            colListWidth.Width = new GridLength(opts.composer.ViewColWidth);
            ImageDepotConvertor.AutoConvert = opts.viewer.ConvertImageDepot;
            foreach (iPicList ipl in views)
                ipl.Init(ref opts, false);            
        }
        public void Finish()
        {
            opts.composer.ViewColWidth = Convert.ToInt32(colListWidth.Width.Value);
            opts.viewer.Autorefresh = chkAutoRefresh.IsChecked.Value;
            foreach (iPicList ipl in views)
                ipl.Finish();
        }
        public UserControl parrent { get { return this; } }
        public GroupBox groupFolder { get { return gbFolder; } }
        public TextBox textFolder { get { return tbImageDepot; } }

        private string _imageFolder;
        public string imageFolder
        {
            get
            {
                if (Directory.Exists(tbImageDepot.Text)) _imageFolder = tbImageDepot.Text;
                else _imageFolder = ImgUtils.defaultImageDepot;
                return _imageFolder.EndsWith("\\") ? _imageFolder: _imageFolder + "\\";
            }
            set
            {
                _imageFolder = value;  tbImageDepot.Text = value;
            }
        }       
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        /// <summary>
        /// order - new is buffering
        /// next - if another new delete the file if inclFile
        ///     - "+" to restore
        /// work only with tableView
        /// </summary>
        private class Undo
        {   
            private ImageDepot imageDepot; private int idx; private bool inclFile;
            private ImageInfo ii;
            public Undo(ref ImageDepot _imageDepot, int idx0, bool _inclFile) // buffer the entry when created
            {
                imageDepot = _imageDepot; idx = idx0; inclFile = _inclFile;
                if (Utils.InRange(idx, 0, imageDepot.items.Count - 1))
                    ii = imageDepot.items[idx]; // by ref ?
            }
            public void Clear()
            {
                ii = null; idx = -1;
            }
            private bool checkOut()
            {
                if (imageDepot == null) return false;
                if (!imageDepot.isEnabled) return false;
                if (ii == null) return false;
                if (!Utils.InRange(idx, 0,imageDepot.items.Count - 1)) return false;               
                return true; 
            }
            public bool recover2ImageDepot() // get it back
            {
                if (!checkOut()) return false;
                imageDepot.items.Insert(idx, ii); imageDepot.Save();
                Clear();
                return true;
            }
            public void realRemove() // get it back
            {
                if (inclFile && checkOut())
                {
                    string fn = Path.Combine(imageDepot.path, ii.filename);
                    if (File.Exists(fn)) File.Delete(fn);
                    else Utils.TimedMessageBox("File <" + fn + "> does not exist");
                }
                Clear();
            }
        }
        private Undo undo = null;

        public int RemoveSelected(bool inclFile = false)
        {
            if (!activeView.HasTheFocus) return -1;
            string ss = inclFile ? "and image file" : ""; bool anim = animation; animation = false;
            Log("Deleting image #" + activeView.selectedIndex.ToString()+ " entry "+ ss, Brushes.Tomato);
            if (iDepot == null) { Log("no active image depot found"); return -1; }
            if (!iDepot.isEnabled) { Log("current image depot - not active"); return -1; }
            int idx0 = activeView.selectedIndex - 1;
            if (!Utils.InRange(idx0, 0, iDepot.items.Count - 1)) { Log("index out of limits"); return -1; }

            string markMask = activeView.markMask;
            if (tabCtrlViews.SelectedIndex == 0) // tableView
            {                
                if (undo != null) undo.realRemove();
                undo = new Undo(ref iDepot, idx0, inclFile); // 
                if (iDepot.RemoveAt(idx0, opts.viewer.RemoveImages)) iDepot.Save(); // iDepot correction
                else { Log("Unsuccessful delete operation"); return -1; }
                Refresh(); 
            }
            else // gridView
            {
                gridViewUC.RemoveAt(inclFile);
                if (iDepot.RemoveAt(idx0, opts.viewer.RemoveImages)) iDepot.Save(); // iDepot correction
                else { Log("Unsuccessful delete operation"); return -1; }
            }
            if (!iDepot.isEnabled) { Log("current image depot - not active"); return -1; }
            // restore selection, masked and animation  
            activeView.selectedIndex = Utils.EnsureRange(idx0, 0, iDepot.items.Count - 1) + 1;
            activeView.Mark(markMask);
            if (anim) animation = true;
            if (iDepot.isEnabled) lbDepotInfo.Content = iDepot.items.Count.ToString() + " images";
            return idx0;
        } 
        public void Clear() 
        {
            activeView?.Clear(); picViewerUC?.Clear(); activeView?.Mark(""); undo?.Clear();
        }
        private bool updating = false; private bool showing = false;
        public bool ShowImageDepot(string imageDepot)
        {
            if (updating) return false;
            updating = true;
            if (tbImageDepot.Text != imageDepot) tbImageDepot.Text = imageDepot; 
            else tbImageDepot_TextChanged(null, null);
            updating = false; showing = true;
            return tbImageDepot.Foreground.Equals(Brushes.Black);
        }
        List<iPicList> views;
        
        private void btnFindUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (tabCtrlViews.SelectedIndex.Equals(0)) tableViewUC.SortTableByIndex();
            int k = sender.Equals(btnFindUp) ? -1 : 1;
            if (activeView.selectedIndex.Equals(-1)) activeView.selectedIndex = 1;
            int idx = activeView.selectedIndex + k - 1;
            List<Tuple<int, string, string>> items = activeView.GetItems(true,true);
            while (idx > -1 && idx < items.Count)
            {
                string prompt = Convert.ToString(items[idx].Item3);                
                if (Utils.IsWildCardMatch(prompt,tbFind.Text) || tbFind.Text.Equals("")) //prompt.IndexOf(tbFind.Text) > -1)
                {
                    activeView.selectedIndex = idx+1; break;
                }
                idx += k;
            }
        }
        private int checkImageDepot()
        {
            int cnt = ImgUtils.checkImageDepot(imageFolder);
            if (cnt > 0) { lbDepotInfo.Content = cnt.ToString() + " images"; lbDepotInfo.Foreground = Brushes.Blue; }
            else { lbDepotInfo.Content = "This is not an image depot."; lbDepotInfo.Foreground = Brushes.Tomato; }
            return cnt;
        }
        public void Refresh(string iFolder = "")
        {
            string folder = iFolder;
            if (iDepot != null && iFolder == "")
            {
                if (iDepot.isEnabled && Directory.Exists(iDepot.path))  folder = iDepot.path;
                else folder = imageFolder;
            }
            if (!Directory.Exists(folder)) { Log("Err: Missing directory > " + folder); return; }

            iDepot = new ImageDepot(imageFolder);
            if (!iDepot.isEnabled) { Log("Error: This is not an image depot."); return; }
            List<Tuple<int, string, string>> decompImageDepot = iDepot.Export2Viewer(); 
            if (!Utils.isNull(decompImageDepot))
            {
                showing = false;
                activeView.FeedList(ref iDepot); picViewerUC.iDepot = iDepot;
                showing = true;
            }
            animation = false; btnPlay.IsEnabled = decompImageDepot.Count > 0; 
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e) 
        {
            if (checkImageDepot() == 0)
            {
                if ((sender == btnRefresh) || (sender == tbImageDepot)) Clear();
            }
            else Refresh(imageFolder);
            if (!Utils.isNull(e)) e.Handled = true;
        }
        private void tbImageDepot_TextChanged(object sender, TextChangedEventArgs e)
        {           
            if (checkImageDepot() > 0)
            {
                tbImageDepot.Foreground = Brushes.Black; 
                if (chkAutoRefresh.IsChecked.Value) btnRefresh_Click(sender, e);
            }
            else 
            { 
                tbImageDepot.Foreground = Brushes.Red;                
            }     
        }
        private void chkAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            if (chkAutoRefresh.IsChecked.Value) { colRefresh.Width = new GridLength(0); btnRefresh.Visibility = Visibility.Collapsed; btnRefresh_Click(sender, e); }
            else { colRefresh.Width = new GridLength(40); btnRefresh.Visibility = Visibility.Visible; }
        }
        private int lastIdx = 0;
        private void tabCtrlViews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            animation = false;
            if ((lastIdx == 1) && (tabCtrlViews.SelectedIndex == 0))
            {
                if (gridViewUC.OutOfResources) gridViewUC.Clear();
                else
                {
                    if (gridViewUC.Loading) gridViewUC.CancelRequest = true;
                }
            }
            lastIdx = tabCtrlViews.SelectedIndex;
            if (chkAutoRefresh.IsChecked.Value && showing) btnRefresh_Click(sender, e);             
            if (!Utils.isNull(e)) e.Handled = true;
        }        
        public bool animation
        {
            get { return btnStop.Visibility.Equals(Visibility.Visible); }
            set 
            {
                if (value.Equals(animation)) return;
                bool vl = value;
                if (activeView.iDepot == null) vl = false;
                if (vl) { btnStop.Visibility = Visibility.Visible; btnPlay.Visibility = Visibility.Collapsed; }
                else { btnStop.Visibility = Visibility.Collapsed; btnPlay.Visibility = Visibility.Visible; }
                if (vl)
                {
                    if (timer == null) timer = new DispatcherTimer(TimeSpan.FromSeconds(numDly.Value), DispatcherPriority.Normal, OnTimerTick, Dispatcher);
                    timer?.Start();
                }
                else
                {
                    timer?.Stop();
                }
            }
        }
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (activeView.iDepot == null) return;
            int cnt = activeView.iDepot.items.Count;
            if (activeView.selectedIndex.Equals(cnt)) animation = false;
            if (Utils.InRange(activeView.selectedIndex, 1,cnt-1)) activeView.selectedIndex += 1;
        }
        private void numDly_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (timer == null) return;
            timer.Interval = TimeSpan.FromSeconds(numDly.Value);
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            animation = sender.Equals(btnPlay); 
        }        
        private void ucViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!activeView.HasTheFocus) return;
            if ((e.Key.Equals(Key.Delete) || e.Key.Equals(Key.NumPad0))) //Utils.DelayExec(100, () => {  } );
            {
                Log("@Explore=70");
                RemoveSelected(opts.viewer.RemoveImages);
            }           
            if (e.Key.Equals(Key.Add)) // recover from undo
            {
                int si = activeView.selectedIndex;
                bool bb = false;
                if (tabCtrlViews.SelectedIndex == 0) // table view
                {
                    if (undo == null) return;
                    bb = undo.recover2ImageDepot(); 
                    if (bb) Refresh();
                }
                else // grid view
                {
                    bb = gridViewUC.Recover();
                }
                if (bb) Log("Recover a deleted image", Brushes.Crimson);
                activeView.selectedIndex = Utils.EnsureRange(si, 1, iDepot.items.Count);
                activeView.Mark(tbFind.Text);
                checkImageDepot();
            }                
        }
        private void btnMark_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(btnMark)) activeView.Mark(tbFind.Text);
            else activeView.Mark("");
        }
    }
}
