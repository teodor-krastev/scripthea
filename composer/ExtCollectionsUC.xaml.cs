using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
using Newtonsoft.Json;
using Path = System.IO.Path;
using UtilsNS;
using scripthea.options;

namespace scripthea.composer
{
    public class ECdesc
    {
        [JsonIgnore]
        public static string descName = "DESCRIPTION.coll";

        public string name; // must. the name of the file or core for multi-file AND core of suggested names for cues files
        public string urlSource; // dataset card at huggingface or elsewhere
        public int rowCount; // must (total if multi-file)
        public bool filtered; // from the original source
        public bool useCategogies;
        public bool useModifiers;
        public string version;
        public string comment; // description of the set
        public string filename; // must; no path; if contains * OR ? multifile
                                 // ext: .STX -> prompt only text file, categogies and separModif - ireleveant;
                                 // .SJL -> jsonl format 
        [JsonIgnore]
        public static string folderPath = "";
        [JsonIgnore]
        public List<string> Filenames // no path
        {
            get 
            {
                List<string> ls = new List<string>();
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
            folderPath = _folderPath;
            return JsonConvert.DeserializeObject<ECdesc>(File.ReadAllText(fn));
        }
        public bool Validate()
        {
            if ((name == null) || (name == "")) return false;
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
        public Dictionary<string,string> ToDictionary()
        {
            Dictionary<string, string> dct = new Dictionary<string, string>();      
            dct["name"] = name;
            dct["urlSource"] = urlSource;
            dct["rowCount"] = rowCount.ToString();
            dct["filtered"] = filtered.ToString();
            dct["useCategogies"] = useCategogies.ToString();
            dct["useModifiers"] = useModifiers.ToString();
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
    {   classical = 1,      // 1. Fine Art, Classical & Historical Art
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
        public Dictionary<Categories, int> categories; // matching percentage; -1 -> NAN
        public List<string> modifs;
          
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

        public string Filter;

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
        private Options opts;
        public ECollection(ref Options _opts)
        {
            opts = _opts;
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
           // regex filter
            if (!ecq.Filter.Trim().Equals(string.Empty))
            {
                if (!Utils.IsWildCardMatch(aprt, ecq.Filter)) return false;
                eCnt["regex"] += 1; 
            }               
            return true;
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
            if (!OpenEColl(_folderPath)) return null; opts.Log("Extraction from " + Path.GetFileName(folderPath) + "; total lines: " + ecd.rowCount);
            List<ECprompt> lst = new List<ECprompt>(); DateTime dt = DateTime.Now;
            ECprompt prt; int lineCount = 0;
            bool? bb = iteration(ecq, out prt);
            while (bb != null) // 
            {
                if ((bool)bb) lst.Add(prt); lineCount++;
                bb = iteration(ecq, out prt);
            }
            // report 
            if (ecq.SegmentFlag)
            {
                opts.Log("Segment from: " + ecq.SegmentFrom + " .. " + ecq.SegmentTo+"; lenght: " + eCnt["seg"]);
            }
            string ss1 = ""; string ss2 = "";
            if (ecq.WordsFlag)
            {
                ss1 = "word size cut: " + eCnt["size"];
            }
            if (ecq.sjlFlag && ecq.CatFlag)
            {
                ss1 += "; categories cut: " + eCnt["cat"];
            }
            if (!ecq.Filter.Trim().Equals(string.Empty))
            {
                ss2 = "key word cut: " + eCnt["regex"];                
            }   
            if (ss1+ss2 != "") opts.Log(ss1 +"  "+ ss2);

            TimeSpan ts = DateTime.Now - dt;
            ss1 = ts.TotalSeconds.ToString("G3"); 

            Random random = new Random();
            if (ecq.RandomSampleFlag && lst.Count > ecq.RandomSampleSize)
            {
                int k = lst.Count;                
                while (lst.Count > ecq.RandomSampleSize) lst.RemoveAt(random.Next(0, lst.Count-1));
                opts.Log("sampling " + k + " lines to extract " + lst.Count + " cues; " + ss1 + " sec.");
            }
            else opts.Log("number of cues extracted: " + lst.Count + "  "+ss1+" sec." );
            if (!opts.general.debug) 
            { 
                if (Utils.InRange(lst.Count, 501, 1000))
                {
                    if (!Utils.ConfirmationMessageBox("The extracted cues number is "+lst.Count+" (>500).\n Would you like continue (Yes) or Cancel (No) the extraction?"))
                    { lst.Clear(); return lst; }
                }
                if (lst.Count > 1000)
                {
                    if (!Utils.ConfirmationMessageBox("The cues number exceeds 1000.\n Would you like to trancate to 1000 (Yes) or Cancel (No) the extraction?"))
                    { lst.Clear(); return lst; }
                    while (lst.Count > 1000) lst.RemoveAt(random.Next(0, lst.Count-1));
                }
            }
            return lst;
        }
    }
    /// <summary>
    /// Interaction logic for ExtCollectionsUC.xaml
    /// </summary>
    public partial class ExtCollectionsUC : UserControl
    {
        public ExtCollectionsUC()
        {
            InitializeComponent();
        }
        private Options opts; public ExtCollMng extCollMng; private List<CheckBox> catChecks = new List<CheckBox>();
        public void Init(ref Options _opts)
        {
            opts = _opts; // extCollMng = new ExtCollMng();
            // invariant limits
            numSegmentFrom.Minimum = 1; numSegmentTo.Minimum = 1; 
            numWordsMin.Minimum = 1; numWordsMax.Minimum = 2;
            numRandomSample.Minimum = 2; numRandomSample.Maximum = 1000;

            catChecks.Clear(); wpCats.Children.Clear();
            foreach (string cat in Enum.GetNames(typeof(Categories)))
            {
                CheckBox chk = new CheckBox() { Content = cat.Replace("_", "-").PadRight(18, ' '), FontSize = 13,  Margin = new Thickness(3) };//Courier New FontFamily = new FontFamily("Lucida Console"),
                catChecks.Add(chk); wpCats.Children.Add(chk);
            }
        }
        public void Finish()
        {
            extCollMng?.Close();
            if (!Directory.Exists(folderPath)) return;
            File.WriteAllText(Path.Combine(folderPath, lastQuery), JsonConvert.SerializeObject(GetQueryFromVisuals()));
        }
        public bool CoverOn 
        { 
            get { return rectCover.Visibility == Visibility.Visible; } 
            set { if (value) rectCover.Visibility = Visibility.Visible; else rectCover.Visibility = Visibility.Collapsed; sjlFlag = !value; } 
        }
        private HashSet<Categories> GetCategories()
        {
            HashSet<Categories> cts = new HashSet<Categories>();
            for (int i = 0; i < catChecks.Count; i++)
            {
                if (catChecks[i].IsChecked.Value) cts.Add((Categories)(i + 1));
            }
            return cts;
        }
        private bool FilterByCategory(ECprompt prt) // true -> pass
        {
            bool bb = false;
            HashSet<Categories> cats = GetCategories();
            foreach (var inCat in prt.categories)
            {
                if (cats.Contains(inCat.Key)) bb |= inCat.Value <= sliderThreshold.Value;
            }
            return bb;
        }
        public ECquery GetQueryFromVisuals()
        {
            ECquery ecq = new ECquery()
            {
                sjlFlag = _sjlFlag,

                SegmentFlag = Convert.ToBoolean(chkSegment.IsChecked.Value),
                SegmentFrom = numSegmentFrom.Value,
                SegmentTo = numSegmentTo.Value,

                CatFlag = false,

                WordsFlag = Convert.ToBoolean(chkWords.IsChecked.Value),
                WordsMin = numWordsMin.Value,
                WordsMax = numWordsMax.Value,

                Filter = tbFilter.Text,

                RandomSampleFlag = Convert.ToBoolean(chkRandomSample.IsChecked.Value),
                RandomSampleSize = numRandomSample.Value
            };
            bool bb = ecdesc != null;
            if (bb) bb = ecdesc.useCategogies;
            if (sjlFlag && bb)
            {
                ecq.CatFlag = chkFilterByCat.IsChecked.Value;
                ecq.CatThreshold = (int)sliderThreshold.Value;
                ecq.categories = GetCategories();
            }
            if (ecq.SegmentFrom > ecq.SegmentTo) opts.Log("Error: segment bondaries.");
            if (ecq.WordsMin > ecq.WordsMax) opts.Log("Error: words size limits.");
            return ecq;
        }
        public void SetQuery2Visuals(ECquery qry)
        {
            chkSegment.IsChecked = qry.SegmentFlag;
            numSegmentFrom.Value = qry.SegmentFrom;
            numSegmentTo.Value = qry.SegmentTo;

            chkFilterByCat.IsChecked = qry.CatFlag;
            sliderThreshold.Value = qry.CatThreshold;

            if (qry.categories != null && qry.sjlFlag)
            {
                foreach (CheckBox chk in catChecks)
                    chk.IsChecked = false;
                foreach (Categories cat in qry.categories)
                    catChecks[(int)cat - 1].IsChecked = true;
            }

            chkWords.IsChecked = qry.WordsFlag;
            numWordsMin.Value = qry.WordsMin;
            numWordsMax.Value = qry.WordsMax;

            tbFilter.Text = qry.Filter;

            chkRandomSample.IsChecked = qry.RandomSampleFlag;
            numRandomSample.Value = qry.RandomSampleSize;
        }
        public string folderPath { get; private set; } = "";
        private readonly string lastQuery = "LastQuery.json";
        public ECdesc ecdesc { get; private set; }
        private bool _sjlFlag;
        public bool sjlFlag 
        { 
            get { return _sjlFlag; } 
            private set
            {
                _sjlFlag = value;
                bool bb = ecdesc != null;
                if (bb) bb = ecdesc.useCategogies;
                if (value && bb) rowDefCats.Height = new GridLength(77); 
                else rowDefCats.Height = new GridLength(0);
            } 
        }
        public bool SetFolder(string _folderPath)
        {
            if (_folderPath == "") { folderPath = _folderPath; return true; }
            if (!Directory.Exists(_folderPath)) return false;
            string dfn = Path.Combine(_folderPath, ECdesc.descName);
            if (!File.Exists(dfn)) return false;
            ecdesc = ECdesc.OpenECdesc(_folderPath); string ss = "";
            bool bb = ecdesc.Validate();
            if (bb)
            {
                folderPath = _folderPath; ss = "(" + ecdesc.rowCount + " lines)";
                numSegmentFrom.Maximum = ecdesc.rowCount - 1; numSegmentTo.Maximum = ecdesc.rowCount; 
            }
            else  folderPath = "";
            lbInfo.Content = "Extract cues from external prompt collection " + ss;
            string qfn = Path.Combine(folderPath, lastQuery); 
            if (File.Exists(qfn))
            {
                SetQuery2Visuals(JsonConvert.DeserializeObject<ECquery>(File.ReadAllText(qfn)));
            }
            sjlFlag = Path.GetExtension(ecdesc.filename).ToUpper() == ".SJL";
            return bb;
        }
        private string NextIdxFilePath() // two digit end
        {
            if (folderPath == "") return "";
            List<string> fns = new List<string>(Directory.GetFiles(folderPath, "*.cues"));
            for (int i = 1; i < 100; i++)
            {
                string fn = Path.Combine(folderPath, ecdesc.name) + "-" + i.ToString("D2") + ".cues";
                if (fns.FindIndex(x => x.Equals(fn, StringComparison.InvariantCultureIgnoreCase)) == -1) return fn;
            }
            return "";
        }
        public event EventHandler NewCuesEvent;       
        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            if (folderPath == "") { opts.Log("Error[911]: no valid folder"); return; }
            tbInfo.Text = "info"; 
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;               
                ECollection ec = new ECollection(ref opts);
                ec.OpenEColl(folderPath); ECquery ecq = GetQueryFromVisuals();

                List<ECprompt> cues = ec.ExtractCueList(folderPath, ecq);
                if (cues.Count == 0) { opts.Log("Warnning: No cues have been extracted."); return; }

                string fn = NextIdxFilePath();
                if (fn == "") return;
                if (File.Exists(fn)) File.Delete(fn);
                using (StreamWriter sw = File.AppendText(fn))
                {
                    sw.WriteLine("## query: " + ecq.ToJson()); sw.WriteLine("##");
                    foreach (ECprompt prt in cues)
                    {
                        sw.WriteLine(prt.prompt);
                        if (prt.categories != null) 
                            if (prt.categories.Count > 0)
                                sw.WriteLine("# "+JsonConvert.SerializeObject(prt.categories));
                        sw.WriteLine("---");
                    }
                }
                NewCuesEvent?.Invoke(this, EventArgs.Empty);
                //tbInfo.Text = ec.wordsCount.Count.ToString() + " processed lines; " + ec.AverWordsCount().ToString() + " average words count; " + cues.Count.ToString() + " prompts produced.";
            } finally { Mouse.OverrideCursor = null; }
        }
        private void btnExtColl_Click(object sender, RoutedEventArgs e)
        {
            extCollMng.ShowWindow();
        }
        private void sliderThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lbThreshold.Content = "Cat.threshold (" + (int)sliderThreshold.Value + "%)";
        }        
    }
}
