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
using Path = System.IO.Path;
using Newtonsoft.Json;
using scripthea.master;
using scripthea.options;
using UtilsNS;

namespace scripthea.viewer
{
    public class ImageInfo
    {
        public enum ImageGenerator { StableDiffusion, ExtGen, Craiyon, AddonGen, FromDescFile }
        
        // StableDiffusion coverts SD and Addon Gen if the latter is created from within Scrithea, in most cases identical
        // 
        // ExtGen is for unknown source, not meta data, later to be fill in from Image Depot Editor
        public ImageInfo()
        {
            
        }        
        public void ImageImport(string fullfilename, ImageGenerator _imageGenerator, bool keepName) // only for import from file
        {
            filename = ""; imageGenerator = _imageGenerator;
            if (!File.Exists(fullfilename)) return;
            string suggestedName = ""; bool bb; bool be = false;
            switch (imageGenerator)
            {
                case ImageGenerator.StableDiffusion:               
                    bb = FromSDFile(fullfilename, out suggestedName);
                    if (!bb) return;                    
                    break;
                case ImageGenerator.Craiyon:
                    if (!FromCraiyonFile(fullfilename, out suggestedName)) return;
                    break;
                case ImageGenerator.ExtGen:
                    be = FromSDFile(fullfilename, out suggestedName); // try SD format first
                    if (!be) prompt = "[prompt for img. " + Path.GetFileNameWithoutExtension(fullfilename) + "]"; 
                    break;
            }
            if (keepName || (imageGenerator.Equals(ImageGenerator.ExtGen) && !be)) { filename = Path.GetFileName(fullfilename); return; }
            try
            {
                if (!fullfilename.Equals(suggestedName, StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Move(fullfilename, suggestedName); // Rename the oldFileName into newFileName                    
                }
                filename = Path.GetFileName(suggestedName);
            }
            catch (System.IO.IOException IOe) { filename = ""; Utils.TimedMessageBox("Error["+IOe.HResult+"]: (" + System.IO.Path.GetFileName(fullfilename) + ") " + IOe.Message); }
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
            if (!dict.ContainsKey("job_timestamp")) job_timestamp = DateTime.Now.ToString("G");
        }
        /*{"prompt", "String" }, {"negative_prompt", "String" }, { "seed", "Int64" }, { "width", "Int64" }, { "height", "Int64" },
            { "sampler_name", "String" }, { "cfg_scale", "Double" }, { "steps", "Int64" }, { "batch_size", "Int64" }, { "restore_faces", "Boolean" },
            { "model", "String" }, { "sd_model_hash", "String" }, { "denoising_strength", "Int64" }, { "job_timestamp", "String" }*/
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
        public string model { get; set; }
        public string sd_model_hash { get; set; }
        public string filename { get; set; } // without folder
        public string job_timestamp { get; set; }
        // its own
        public int rate { get; set; }
        public object tags { get; set; } // open stucture for future use: set of labels to be selected and/or define position in a image structure within a Scripthea project
        public string history { get; set; } // stages of variations, '|' separated -> for future use
        public string MD5Checksum { get; set; }
        [JsonIgnore]
        public ImageGenerator imageGenerator { get; set; } // for future use; 
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
            bool sd = ExtraImgUtils.GetMetadata(fullfilename, out meta, this);            
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
        public bool FromCraiyonFile(string fullfilename, out string suggestedName) // true if it's there  
        {
            suggestedName = "";
            if (!File.Exists(fullfilename)) return false; string ext = Path.GetExtension(fullfilename);
            string fn = Path.GetFileNameWithoutExtension(fullfilename); 
            string[] fna = fn.Split('_'); if (fna.Length < 3) { Utils.TimedMessageBox("Error[924]: unsuitable image name"); return false; }
            fna[0] = ""; suggestedName = Path.ChangeExtension("C-"+fna[1], ext); fna[1] = ""; 
            prompt = String.Join(" ", fna).Trim();
            suggestedName = System.IO.Path.Combine(Path.GetDirectoryName(fullfilename), suggestedName); // complete with folder
            suggestedName = Utils.AvoidOverwrite(suggestedName);
            return true;
        }
        public void FromString(string json)
        {
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            FromDictionary(dict);
        }
        public bool SameAs(ImageInfo imageInfo)
        {
            return To_String().Equals(imageInfo?.To_String(),StringComparison.InvariantCultureIgnoreCase);
        }
        public string To_String()
        {
            return JsonConvert.SerializeObject(this);
        }
        public void FromDictionary(Dictionary<string, object> dict)
        {
            if (dict == null) return;
            if (dict.ContainsKey("prompt")) prompt = Convert.ToString(dict["prompt"]);
            negative_prompt = dict.ContainsKey("negative_prompt") ? Convert.ToString(dict["negative_prompt"]) : "";
            rate = dict.ContainsKey("rate") ? Convert.ToInt32(dict["rate"]) : 0;
            if (dict.ContainsKey("steps")) steps = Convert.ToInt64(dict["steps"]);
            if (dict.ContainsKey("sampler_name")) sampler_name = Convert.ToString(dict["sampler_name"]);
            if (dict.ContainsKey("cfg_scale")) cfg_scale = Convert.ToDouble(dict["cfg_scale"]);
            seed = dict.ContainsKey("seed") ? Convert.ToInt64(dict["seed"]) : 0;
            if (dict.ContainsKey("width")) width = Convert.ToInt64(dict["width"]);
            if (dict.ContainsKey("height")) height = Convert.ToInt64(dict["height"]);
            denoising_strength = dict.ContainsKey("denoising_strength") ? Convert.ToDouble(dict["denoising_strength"]) : 0;
            batch_size = dict.ContainsKey("batch_size") ? Convert.ToInt64(dict["batch_size"]) : 1;
            restore_faces = dict.ContainsKey("restore_faces") ? Convert.ToBoolean(dict["restore_faces"]) : false;
            if (dict.ContainsKey("model")) model = Convert.ToString(dict["model"]);
            if (dict.ContainsKey("sd_model_hash")) sd_model_hash = Convert.ToString(dict["sd_model_hash"]);
            if (dict.ContainsKey("filename")) filename = Convert.ToString(dict["filename"]);
            if (dict.ContainsKey("job_timestamp")) job_timestamp = Convert.ToString(dict["job_timestamp"]);
            if (dict.ContainsKey("MD5Checksum")) MD5Checksum = Convert.ToString(dict["MD5Checksum"]);
            history = dict.ContainsKey("history") ? Convert.ToString(dict["history"]) : "";
            tags = dict.ContainsKey("tags") ? dict["history"] : null;
        }
        public void FromMeta1111Dict(Dictionary<string, string> dict)
        {
            if (dict == null) return;
            if (dict.ContainsKey("prompt")) prompt = dict["prompt"]; negative_prompt = ""; denoising_strength = 0; batch_size = 1; restore_faces = false;
            rate = dict.ContainsKey("rate") ? Convert.ToInt32(dict["rate"]) : 0;
            if (dict.ContainsKey("steps")) steps = Convert.ToInt32(dict["steps"]);
            if (dict.ContainsKey("sampler")) sampler_name = dict["sampler"];
            if (dict.ContainsKey("scale")) cfg_scale = Convert.ToInt32(dict["scale"]);
            seed = dict.ContainsKey("seed") ? Convert.ToInt64(dict["seed"]) : -1;
            if (dict.ContainsKey("size"))
            {
                string size = dict["size"]; string[] sa = size.Split('x');
                width = Convert.ToInt64(sa[0]); height = Convert.ToInt64(sa[1]);
            }
            if (dict.ContainsKey("model")) model = dict["model"];
            if (dict.ContainsKey("sd_model_hash")) sd_model_hash = dict["sd_model_hash"];
            if (dict.ContainsKey("filename")) filename = dict["filename"];
            if (dict.ContainsKey("MD5Checksum")) MD5Checksum = dict["MD5Checksum"];
            history = dict.ContainsKey("history") ? Convert.ToString(dict["history"]) : "";
        }
        // public enum prmKind { positive, negative, width, height, seed, steps, cfg, sampler_name, scheduler, model, denoise }
        public void FromMetaComfyDict(Dictionary<string, string> dict)
        {
            if (dict == null) return;
            if (dict.ContainsKey("positive")) prompt = dict["positive"];
            if (dict.ContainsKey("negative")) negative_prompt = dict["negative"];
            if (dict.ContainsKey("width")) width = Convert.ToInt64(dict["width"]);
            if (dict.ContainsKey("height")) height = Convert.ToInt64(dict["height"]);
            seed = dict.ContainsKey("seed") ? Convert.ToInt64(dict["seed"]) : 0;
            if (dict.ContainsKey("steps")) steps = Convert.ToInt32(dict["steps"]);
            if (dict.ContainsKey("sampler_name")) sampler_name = dict["sampler_name"];
            if (dict.ContainsKey("scale")) cfg_scale = Convert.ToInt32(dict["scale"]);
            if (dict.ContainsKey("scheduler")) model = dict["scheduler"];
            if (dict.ContainsKey("model")) model = dict["model"];
            if (dict.ContainsKey("denoise")) denoising_strength = Convert.ToDouble(dict["denoise"]);
            batch_size = 1; restore_faces = false;
            // not from comfy metadata
            rate = dict.ContainsKey("rate") ? Convert.ToInt32(dict["rate"]) : 0;
            if (dict.ContainsKey("sd_model_hash")) sd_model_hash = dict["sd_model_hash"];
            if (dict.ContainsKey("filename")) filename = dict["filename"];
            if (dict.ContainsKey("MD5Checksum")) MD5Checksum = dict["MD5Checksum"];
            history = dict.ContainsKey("history") ? Convert.ToString(dict["history"]) : "";
        }
        public Dictionary<string, object> ToDictionary(bool SDonly = false)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("prompt", prompt);
            dict.Add("width", width);
            dict.Add("height", height);
            dict.Add("negative_prompt", negative_prompt);
            dict.Add("sampler_name", sampler_name);
            dict.Add("steps", steps);
            dict.Add("cfg_scale", cfg_scale.ToString());
            dict.Add("seed", seed);
            dict.Add("model", model);
            dict.Add("sd_model_hash", sd_model_hash);
            dict.Add("job_timestamp", job_timestamp);
            dict.Add("denoising_strength", denoising_strength);
            dict.Add("batch_size", batch_size);
            dict.Add("restore_faces", restore_faces);
            dict.Add("filename", filename);
            if (!SDonly)
            {
                dict.Add("MD5Checksum", MD5Checksum);
                dict.Add("history", history);
                dict.Add("tags", tags);
                dict.Add("rate", rate);
            }
            return dict;
        }
        public ImageInfo Clone()
        {
            return new ImageInfo(ToDictionary());
        }
        // copy 1. target.FromString(source.To_String())
        //      2. target.FromDictionary(Source.ToDictionary())
    }
    public static class ImageDepotConvertor
    {
        public static bool AutoConvert = true;
        public static bool ClearEntriesImageDepot = true;
        public static bool Old2New(string idf)
        {
            ImageDepot imageDepot = new ImageDepot(idf, ImageInfo.ImageGenerator.FromDescFile, ImageDepot.SD_WebUI.NA, true);
            imageDepot.Save(true);
            return true;
        }
        public static bool FullSameAs(string idFolderA, ImageInfo imageInfoA, string idFolderB, ImageInfo imageInfoB)
        {
            if (!Directory.Exists(idFolderA) || !Directory.Exists(idFolderB)) return false;
            string imageA = Path.Combine(idFolderA, imageInfoA.filename); string imageB = Path.Combine(idFolderB, imageInfoB.filename);
            if (!File.Exists(imageA) || !File.Exists(imageB)) return false;
            return imageInfoA.SameAs(imageInfoB) && Utils.GetMD5Checksum(imageA).Equals(Utils.GetMD5Checksum(imageB));
        }
        public static bool? CheckFileVersion(string folder) // true - new one (desc.ver>69); false - old one
        {
            bool bb = false;
            string desc = Path.Combine(folder, SctUtils.descriptionFile);
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
        public enum SD_WebUI { NA, A1111, ComfyUI }
        public SD_WebUI sd_WebUI;
        public ImageDepot(string _folder, ImageInfo.ImageGenerator _imageGenerator = ImageInfo.ImageGenerator.FromDescFile, SD_WebUI _sd_WebUI = SD_WebUI.NA, bool _IsReadOnly = false)
        {
            if (!Directory.Exists(_folder)) return; isReadOnly = _IsReadOnly; string desc = Path.Combine(_folder, SctUtils.descriptionFile);
            header = new Dictionary<string, string>(); items = new List<ImageInfo>();
            path = _folder;
            if (_imageGenerator == ImageInfo.ImageGenerator.FromDescFile && File.Exists(desc)) // read type from file
            {
                if (!isReadOnly)
                {
                    bool? bb = ImageDepotConvertor.CheckFileVersion(_folder);
                    if (bb == null) { Utils.TimedMessageBox("Error[482]: Corrupted Scripthea image depot!", "Error", 4000); return; }
                    if (!(bool)bb) // old version
                    {
                        if (ImageDepotConvertor.AutoConvert) ImageDepotConvertor.Old2New(_folder); // convert 
                        else isReadOnly = true;                                                    // OR lock
                    }                   
                }
                // reading header
                List<string> body = Utils.readList(desc, false); descText = File.ReadAllText(desc);
                if (body.Count == 0) return; bool oldVersion = false;
                if (body[0].StartsWith("#"))
                {
                    header = JsonConvert.DeserializeObject<Dictionary<string, string>>(body[0].Substring(1));
                    body.RemoveAt(0);
                    if (header.ContainsKey("ImageGenerator"))
                        foreach (ImageInfo.ImageGenerator ig in Enum.GetValues(typeof(ImageInfo.ImageGenerator)))
                        {
                            if (ig.Equals(header["ImageGenerator"])) imageGenerator = ig;
                        }
                    sd_WebUI = SD_WebUI.NA;
                    if (imageGenerator.Equals(ImageInfo.ImageGenerator.StableDiffusion) &&  header.ContainsKey("webui"))
                    {
                        string webui = header["webui"];                       
                        if (webui.Equals("AUTOMATIC1111") || webui.Equals("A1111")) sd_WebUI = SD_WebUI.A1111;
                        if (webui.Equals("ConmfyUI")) sd_WebUI = SD_WebUI.ComfyUI;
                    }
                    if (header.ContainsKey("application"))
                    {
                        string[] sa = header["application"].Split(' ');
                        if (sa[0] != "Scripthea" || sa.Length != 2) { Utils.TimedMessageBox("Error[125]: NOT Scripthea image depot file!", "Error", 5000); return; }
                        string[] sb = sa[1].Split('.'); if (sb.Length != 4) return;
                        appBuilt = Convert.ToInt32(sb[3]); oldVersion = appBuilt < 70;
                    }
                }
                foreach (string ss in body)
                {
                    ImageInfo ii = new ImageInfo();
                    ii.FromString(ss);
                    items.Add(ii);
                }
            }
            else
            {
                imageGenerator = _imageGenerator; header.Add("ImageGenerator", imageGenerator.ToString());
                header.Add("webui", _sd_WebUI.ToString()); header.Add("application", "Scripthea " + Utils.getAppFileVersion);
            }
            if (ImageDepotConvertor.ClearEntriesImageDepot) Validate(true); // remove entries without files and save it
        }
        public bool isEnabled
        {
            get { return Utils.isNull(header) ? false : header.Count > 0 && !Utils.isNull(items); }
        }
        public int appBuilt { get; private set; }
        public bool isReadOnly { get; private set; }
        public bool IsChanged { get; set; } = false;
        public void OnClose() { if (IsChanged) Save(true); }
        public string descText { get; private set; }
        public Dictionary<string, string> header;
        public List<ImageInfo> items { get; set; }
        public int idxFromFilename(string fn) // only filename
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].filename.Equals(fn, StringComparison.InvariantCultureIgnoreCase)) return i;
            }
            return -1;
        }
        public bool RemoveAt(int idx, bool inclFile)
        {
            if (!isEnabled || isReadOnly) return false;
            if (!Utils.InRange(idx, 0, items.Count - 1)) return false;
            if (inclFile)
            {
                string fn = Path.Combine(path, items[idx].filename);
                if (File.Exists(fn)) File.Delete(fn);
            }
            items.RemoveAt(idx);
            return true;
        }
        public ImageDepot VirtualClone(string targetPath, List<Tuple<int, string, int, string>> filter = null) // int -> index; string -> filename (may differ); string -> prompt (for consistency)
        {
            if (!Directory.Exists(targetPath)) return null;
            ImageDepot dp = new ImageDepot(targetPath, imageGenerator, sd_WebUI, isReadOnly);
            if (Utils.isNull(filter)) dp.items.AddRange(items);
            else
            {
                foreach (var itm in filter)
                {
                    ImageInfo ii = items[itm.Item1];
                    if (ii.prompt != itm.Item2) { Utils.TimedMessageBox("Error[664]: broken filter"); break; }
                    ii.rate = itm.Item3;
                    ii.filename = itm.Item4;
                    dp.items.Add(ii);
                }
            }
            return dp;
        }
        public bool SameAs(ImageDepot imageDepot) // compare data, location-agnostic
        {
            bool bb = JsonConvert.SerializeObject(header).Equals(JsonConvert.SerializeObject(imageDepot?.header));
            if ((items.Count != imageDepot.items.Count) || !bb) return false;
            for (int i = 0; i < items.Count; i++)
            {
                bb &= items[i].SameAs(imageDepot.items[i]);
            }
            return bb;
        }
        public bool SameAs(string imageFolder) // compare DESCRIPTION.idf and location
        {
            bool bb = Directory.Exists(imageFolder) && Utils.comparePaths(imageFolder, path);
            if (!bb) return false;
            bb = descText == File.ReadAllText(Path.Combine(imageFolder, SctUtils.descriptionFile));
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
                for (int j = 0; j < items.Count; j++)
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
        public bool Validate(bool? correctEntry) // argument: remove or not unlinked entries; if correctEntry is null ask user
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
                            "Image depot entry <" + items[i].filename + "> has no corresponding image file.\rClick \"Yes\" to correct entry list, \"No\" to skip this correction, or \"Cancel\" to exit validation.";
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
                Save(true); Utils.TimedMessageBox((itemsCount - items.Count).ToString() + " image depot entries have been removed", "Warning");
            }
            return ok;
        }
        public bool Append(ImageInfo ii)
        {
            if (!isEnabled || isReadOnly) return false;
            using (StreamWriter w = File.AppendText(Path.Combine(path, SctUtils.descriptionFile)))
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
            if (!header.ContainsKey("application")) header["application"] = "Scripthea " + Utils.getAppFileVersion;
            ls.Add("#" + JsonConvert.SerializeObject(header));
            foreach (ImageInfo ii in items)
                ls.Add(ii.To_String());
            //Utils.DelayExec(20, new Action(() => {  }));       
            Utils.writeList(Path.Combine(path, SctUtils.descriptionFile), ls);
            Utils.Sleep(200);
        }
        public List<Tuple<int, string, int, string>> Export2Viewer() // idx 0 based; prompt, rate, filename
        {
            List<Tuple<int, string, int, string>> lst = new List<Tuple<int, string, int, string>>();
            for (int i = 0; i < items.Count; i++)
                lst.Add(new Tuple<int, string, int, string>(i, items[i].prompt, items[i].rate, items[i].filename ));
            return lst;
        }
    }
}
