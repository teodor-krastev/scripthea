using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;
using UtilsNS;
using System.IO;
using System.Windows.Media;

namespace ExtCollMng
{
    public class ECdesc
    {
        [JsonIgnore]
        public static string descName = "DESCRIPTION.coll";

        public string coreName; // must. the name of the file or core for multi-file AND core of suggested names for cues files
        public string urlSource; // dataset card at huggingface or elsewhere
        public int rowCount; // must (total if multi-file)
        public bool filtered; // from the original source
        public bool useCategories;        
        public string version;
        public string comment; // description of the set
        public string filename; // must; no path; if contains * OR ? multifile
                                // ext: .STX -> prompt only text file, categories and separModif - irrelevant;
                                // .SJL -> jsonl format 
        public bool? sjlFlag()
        {
            if (!Directory.Exists(folderPath)) return null;
            if (!Validate()) return null;
            string ext = Path.GetExtension(filename).ToUpper();
            if (ext.Equals(".STX")) return false;
            else if (ext.Equals(".SJL")) return true;
            else return null;
        } 
        [JsonIgnore]
        public string folderPath = "";
        public string folder()
        {
            if (!Directory.Exists(folderPath)) return "";
            return new DirectoryInfo(folderPath).Name;
        }
        [JsonIgnore]
        public List<string> Filenames // no path
        {
            get
            {
                List<string> ls = new List<string>();
                if (filename == null) return ls;
                if (filename.Contains("*") || filename.Contains("?"))
                {
                    List<string> lt = new List<string>(Directory.GetFiles(folderPath, filename));
                    foreach (string fn in lt) ls.Add(Path.GetFileName(fn));
                }
                else ls.Add(filename);
                return ls;
            }
        }
        public static ECdesc OpenECdesc(string _folderPath)
        {
            string fn = Path.Combine(_folderPath, descName);
            if (!File.Exists(fn)) return null;
            string ss = File.ReadAllText(fn); JObject jobject;
            if (!Utils.TryParse(ss, out jobject)) return null;
            ECdesc cdesc = JsonConvert.DeserializeObject<ECdesc>(ss);
            cdesc.folderPath = _folderPath;
            return cdesc;
        }
        public bool Validate()
        {
            if ((coreName == null) || (coreName == "")) return false;
            if (rowCount < 1) return false;

            if (Filenames.Count == 0) return false;
            foreach (string fn in Filenames)
            {
                string ext = Path.GetExtension(fn).ToUpper();
                if (!(ext.Equals(".STX") || ext.Equals(".SJL"))) return false;
                if (!File.Exists(Path.Combine(folderPath, fn))) return false;
            }
            return true;
        }
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dct = new Dictionary<string, string>();
            dct["folder"] = folder();
            dct["coreName"] = coreName;
            dct["urlSource"] = urlSource;
            dct["rowCount"] = rowCount.ToString();
            dct["filtered"] = filtered.ToString();
            dct["useCategories"] = useCategories.ToString();
            dct["version"] = version;
            dct["comment"] = comment;
            dct["filename"] = filename;
            return dct;
        }
        public void SaveECdesc(string _folderPath = "")
        {
            string fn = Path.Combine(_folderPath == "" ? folderPath : _folderPath, descName);
            File.WriteAllText(fn, JsonConvert.SerializeObject(this));
        }
    }
    public enum Categories
    {
        classical = 1,      // 1. Fine Art, Classical & Historical Art
        impressionism = 2,  // 2. Impressionism & Post-impressionism
        abstract_art = 3,   // 3. Abstract, Post-modern & Psychedelic
        surrealism = 4,     // 4. Surrealism & Dadaism
        fantasy = 5,        // 5. Futuristic, Mythical & Historical Fantasy
        pop_culture = 6,    // 6. Pop Culture, Celebrities & Memes
        cartoon = 7,        // 7. Anime, Cartoon & Stylized Illustration
        realistic = 8       // 8. Realistic & Photographic
    }
    /* 
    public enum TrafficLight { Red, Yellow, Green }
    HashSet<TrafficLight> allowedLights = new HashSet<TrafficLight>() { TrafficLight.Green, TrafficLight.Yellow };
    bool canGo = allowedLights.Contains(TrafficLight.Green); // true
    */
    public class ECprompt
    {
        public string ID;
        public string prompt;
        public Dictionary<Categories, int> categories; // matching percentage
        public Dictionary<string, string> evals;

        public static ECprompt ReadECprompt(string json)
        {
            return JsonConvert.DeserializeObject<ECprompt>(json);
        }
        public string WriteECprompt()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public class ECquery
    {
        public bool sjlFlag;

        public bool SegmentFlag;
        public int SegmentFrom;
        public int SegmentTo;

        public bool WordsFlag;
        public int WordsMin;
        public int WordsMax;

        public bool CatFlag;
        public HashSet<Categories> categories;
        public int CatThreshold;

        public string Pattern;
        public int Way2Match; // 0 - text; 1 - RegEx; 2 - semantic
        public int SemanticBest;

        public bool RandomSampleFlag;
        public int RandomSampleSize;
        public static ECquery ReadECquery(string json)
        {
            return JsonConvert.DeserializeObject<ECquery>(json);
        }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public class ECollection
    {
        private bool localDebug = Utils.TheosComputer();
        public ECollection(Utils.LogHandler _OnLog)
        {
            OnLog += _OnLog;
        }
        public event Utils.LogHandler OnLog;       
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public ECdesc ecd { get; private set; }
        public bool textPrompts { get; private set; } = true; // for now this type only
        public string folderPath { get; private set; }         
        private StreamReader streamReader;
        protected Dictionary<string, int> eCnt = new Dictionary<string, int>(); // etape prompt count 
        public List<int> wordsCount = new List<int>();
        public int AverWordsCount()
        {
            if (wordsCount == null) return -1;
            if (wordsCount.Count == 0) return -1;
            return (int)wordsCount.ToArray().Average();
        }
        public bool? iteration(ECquery ecq, out ECprompt prompt) // true -> take the prompt; false -> not take the prompt; null -> end of collection
        {
            prompt = null;
            if (streamReader.EndOfStream) return null; eCnt["total"] += 1;
            string prt = streamReader.ReadLine().Trim('\"').Trim(); // read next line
            if (ecq.sjlFlag) prompt = ECprompt.ReadECprompt(prt);
            else { prompt = new ECprompt(); prompt.prompt = prt; }
            string aprt = prompt.prompt; //actual prompt

            int wc = WordCount(aprt); wordsCount.Add(wc);
            if (aprt == string.Empty) return false;
            if (!char.IsLetterOrDigit(aprt[0])) return false;
            if (ecq == null) { prompt = null; return true; }
            // actual filter
            if (ecq.SegmentFlag)
            {
                if (eCnt["total"] < ecq.SegmentFrom) return false;
                if (eCnt["total"] > ecq.SegmentTo) return null;
                eCnt["seg"] += 1;
            }
            // words size
            if (ecq.WordsFlag)
            {
                if ((wc < ecq.WordsMin) || (wc > ecq.WordsMax)) return false;
                eCnt["size"] += 1;
            }
            // categories
            if (ecq.sjlFlag && ecq.CatFlag && prompt.categories != null)
            {
                bool bb = false;
                foreach (var inCat in prompt.categories)
                {
                    if (ecq.categories.Contains(inCat.Key)) bb |= inCat.Value >= ecq.CatThreshold;
                }
                if (!bb) return false;
                eCnt["cat"] += 1;
            }
            // simple/regex/semantic filter
            /*if (!ecq.Pattern.Trim().Equals(string.Empty))
            {
                switch (ecq.Way2Match)
                {
                    case 0: if (!Utils.IsWildCardMatch(aprt, ecq.Pattern)) return false;
                        break;
                    case 1: if (!Utils.RegexIsMatch(aprt, ecq.Pattern)) return false;
                        break;
                    case 2: if (!ecq.sjlFlag) return false;
                        
                        break;
                }               
                eCnt["regex"] += 1;
            }*/
            return true;
        }
        private float[] TextEmbeds(string prompt)
        {
            return new float[512];
        }
        Random rand = new Random();
        private double CosineSimilarityNormalized(float[] refText, float[] prt)
        {
            return rand.NextDouble() * 100;
        }
        public List<ECprompt> TextMatching(ECquery ecq, List<ECprompt> lECp) // reduces lECp to the matching cues
        {
            if (ecq.Pattern.Trim().Equals(string.Empty)) return lECp;
            Dictionary<string, double> seman = new Dictionary<string, double>();
            if (ecq.Way2Match == 2) // semantic
            {
                if (!ecq.sjlFlag) return null; // error!!!
                float[] refEmbed = TextEmbeds(ecq.Pattern.Trim());
                Dictionary<string, double> unsorted = new Dictionary<string, double>();
                foreach (ECprompt ecp in lECp)
                {
                    float[] cueEmbed = TextEmbeds(ecp.prompt);
                    double dist = CosineSimilarityNormalized(refEmbed, cueEmbed);
                    unsorted.Add(ecp.ID, dist);
                }
                foreach(var kvp in seman.OrderByDescending(kv => kv.Value))
                {
                    if (seman.Count > ecq.SemanticBest) break; // cut to size
                    seman.Add(kvp.Key, kvp.Value);
                }
                if (seman.Count == 0) return lECp;
            }
            List<ECprompt> rslt = new List<ECprompt>();
            foreach (ECprompt ecp in lECp)
            {
                // simple/regex/semantic filter                                
                switch (ecq.Way2Match)
                {
                    case 0:
                        if (Utils.IsWildCardMatch(ecp.prompt, ecq.Pattern)) rslt.Add(ecp);
                        else continue;
                        break;
                    case 1:
                        if (Utils.RegexIsMatch(ecp.prompt, ecq.Pattern)) rslt.Add(ecp);
                        else continue;
                        break;
                    case 2:
                        if (seman.ContainsKey(ecp.ID)) rslt.Add(ecp);
                        else continue;
                        break;
                }
                eCnt["regex"] += 1;               
            }
            return rslt;
        }
        public int WordCount(string txt)
        {
            string[] the = new string[] { "the", "with" };
            foreach (string ss in the) txt = txt.Replace(ss + " ", " ");
            string[] sa = txt.Split(
                new char[] { ' ', '\t', '\r', '\n', '-', '.', '!', '?', '(', ')', ',', ':', '\'', '`', '\"', '–', '—', '/' },
                StringSplitOptions.RemoveEmptyEntries
            );
            List<string> words = new List<string>();
            foreach (string ss in sa)
            {
                if (ss.Length < 3) continue;
                if (!char.IsLetter(ss[0])) continue;
                if (ss.EndsWith("'s")) words.Add(ss.Substring(0, ss.Length - 2));
                else words.Add(ss);
            }
            return words.Count;
        }
        public bool OpenEColl(string _folderPath)
        {
            ecd = ECdesc.OpenECdesc(_folderPath); folderPath = "";
            if (ecd.Filenames.Count != 1) return false; // only one file for now
            foreach (string fn in ecd.Filenames)
            {
                string ext = Path.GetExtension(Path.Combine(_folderPath, fn)).ToUpper();
                if (!ext.Equals(".STX") && !ext.Equals(".SJL")) return false;
            }
            folderPath = _folderPath;
            streamReader = new System.IO.StreamReader(Path.Combine(_folderPath, ecd.Filenames[0]));
            eCnt.Clear(); eCnt["total"] = 0; eCnt["seg"] = 0; eCnt["size"] = 0; eCnt["cat"] = 0; eCnt["regex"] = 0;
            wordsCount.Clear();
            return true;
        }
        public List<ECprompt> ExtractCueList(string _folderPath, ECquery ecq) // one text file of prompts
        {
            if (!OpenEColl(_folderPath)) return null; Log("Extraction from " + Path.GetFileName(folderPath) + "; total lines: " + ecd.rowCount + "; ");
            List<ECprompt> lst = new List<ECprompt>(); DateTime dt = DateTime.Now;
            ECprompt prt; int lineCount = 0;
            // the extracion
            bool? bb = iteration(ecq, out prt);
            while (bb != null) // 
            {
                if ((bool)bb) lst.Add(prt); lineCount++;
                bb = iteration(ecq, out prt);
            }
            // report 
            if (ecq.SegmentFlag)
            {
                Log("Segment from: " + ecq.SegmentFrom + " .. " + ecq.SegmentTo + "; lenght: " + eCnt["seg"] + "; ");
            }
            string ss1 = ""; string ss2 = "";
            if (ecq.WordsFlag)
            {
                ss1 = "word size cut: " + eCnt["size"] + "; ";
            }
            if (ecq.sjlFlag && ecq.CatFlag)
            {
                ss1 += "; categories cut: " + eCnt["cat"] + "; ";
            }            
            if (!ecq.Pattern.Trim().Equals(string.Empty))
            {
                lst = TextMatching(ecq, lst);
                ss2 = "matching: " + eCnt["regex"] + "; ";
            }            
            if (ss1 + ss2 != "") Log(ss1 + " " + ss2);

            TimeSpan ts = DateTime.Now - dt;
            ss1 = ts.TotalSeconds.ToString("G3");
            
            Random random = new Random();
            if (ecq.RandomSampleFlag && lst.Count > ecq.RandomSampleSize)
            {
                int k = lst.Count;
                while (lst.Count > ecq.RandomSampleSize) lst.RemoveAt(random.Next(0, lst.Count - 1));
                Log("sampling " + k + " lines to extract " + lst.Count + " cues; duration: " + ss1 + " sec.");
            }
            else Log("number of cues extracted: " + lst.Count + "; duration: " + ss1 + " sec.");
            if (!localDebug)
            {
                if (Utils.InRange(lst.Count, 501, 1000))
                {
                    if (!Utils.ConfirmationMessageBox("The extracted cues number is " + lst.Count + " (>500).\n Would you like continue (Yes) or Cancel (No) the extraction?"))
                    { lst.Clear(); return lst; }
                }
                if (lst.Count > 1000)
                {
                    if (!Utils.ConfirmationMessageBox("The cues number exceeds 1000.\n Would you like to trancate to 1000 (Yes) or Cancel (No) the extraction?"))
                    { lst.Clear(); return lst; }
                    while (lst.Count > 1000) lst.RemoveAt(random.Next(0, lst.Count - 1));
                }
            }
            return lst;
        }
    }

}
