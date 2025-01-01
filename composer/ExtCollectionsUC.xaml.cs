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
        public int rowNumber; // must (total if multi-file)
        public bool filtered;
        public bool useCategogies;
        public bool separModif;
        public string version;
        public string comment;
        public List<string> filenames; // must; no path;
                                       // ext: .STX -> prompt only text file, categogies and separModif - ireleveant;
                                       // .SJL -> jsonl format 
        [JsonIgnore]
        public static string folderPath = "";
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
            if (rowNumber < 1) return false;             
            if (filenames.Count == 0) return false;
            foreach (string fn in filenames)
            {
                string ext = Path.GetExtension(fn).ToUpper();
                if (!(ext.Equals(".STX") || ext.Equals(".SJL"))) return false;
                if (!File.Exists(Path.Combine(folderPath, fn))) return false;
            }
            return true;
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
        //public string cueCore;  ?
        //public int cueIndex;    ?    
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
        public bool SegmentFlag;
        public int SegmentFrom;
        public int SegmentTo;

        public HashSet<Categories> categories;

        public bool WordsFlag;
        public int WordsMin;
        public int WordsMax;

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
        protected int cnt, // total 
                      cnt0, 
                      cnt1, 
                      cnt2; 
        public List<int> wordsCount = new List<int>();
        public int AverWordsCount()
        {
            if (wordsCount == null) return -1;
            if (wordsCount.Count == 0) return -1;
            return (int)wordsCount.ToArray().Average();
        }
        public bool? iteration(ECquery ecq, out string prompt) // true -> take the prompt; false -> not take the prompt; null -> end of collection
        {
            prompt = string.Empty;
            if (streamReader.EndOfStream) return null; cnt++;
            string prt = streamReader.ReadLine().Trim('\"').Trim(); // read next line
            int wc = WordCount(prt); wordsCount.Add(wc);
            if (prt == string.Empty) return false;
            if (!char.IsLetterOrDigit(prt[0])) return false;
            if (ecq == null) { prompt = prt; return true; }
            // actual filter
            if (ecq.SegmentFlag)
            {
                if (cnt < ecq.SegmentFrom) return false;
                if (cnt > ecq.SegmentTo) return null;    
                cnt0++;            
            }
            // categories -> later
            if (ecq.WordsFlag)
            {
                if ((wc < ecq.WordsMin) || (wc > ecq.WordsMax)) return false;
                cnt1++;
            }
            // regex filter
            if (!ecq.Filter.Trim().Equals(string.Empty))
            {
                if (!Utils.IsWildCardMatch(prt, ecq.Filter)) return false;
                cnt2++;
            }               
            prompt = prt;
            return true;
        }
        public int WordCount(string txt)
        {
            string[] the = new string[] { "the", "a", "an", "of", "with", "in", "on" };
            foreach (string ss in the) txt = txt.Replace(ss + " ", " ");
            string[] words = txt.Split(
                new char[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );
            return words.Length;
        }
        public bool OpenEColl(string _folderPath)
        {
            ecd = ECdesc.OpenECdesc(_folderPath); folderPath = "";
            if (ecd.filenames.Count != 1) return false; // only one file for now
            foreach (string fn in ecd.filenames)
                if (!Path.GetExtension(Path.Combine(_folderPath, fn)).Equals(".STX", StringComparison.InvariantCultureIgnoreCase)) return false; //only txt for now
            folderPath = _folderPath;
            streamReader = new System.IO.StreamReader(Path.Combine(_folderPath, ecd.filenames[0])); cnt0 = 0; cnt0 = 0; cnt1 = 0; cnt2 = 0;
            wordsCount.Clear();
            return true;
        }
        public List<string> ExtractCueList(string _folderPath, ECquery ecq) // one text file of prompts
        {
            if (!OpenEColl(_folderPath)) return null; opts.Log("Extraction from " + Path.GetFileName(folderPath) + "; total lines: " + ecd.rowNumber);
            List<string> lst = new List<string>(); DateTime dt = DateTime.Now;
            string prt; int lineCount = 0;
            bool? bb = iteration(ecq, out prt);
            while (bb != null)
            {
                if ((bool)bb) lst.Add(prt); lineCount++;
                bb = iteration(ecq, out prt);
            }
            // report 
            if (ecq.SegmentFlag)
            {
                opts.Log("Segment from: " + ecq.SegmentFrom + " .. " + ecq.SegmentTo+"; lenght: " + cnt0);
            }
            string ss1 = ""; string ss2 = "";
            if (ecq.WordsFlag)
            {
                ss1 = "word size filtered: " + cnt1;
            }
            if (!ecq.Filter.Trim().Equals(string.Empty))
            {
                ss2 = "key word filtered: " + cnt2;                
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
        private Options opts; public ExtCollMng extCollMng;
        public void Init(ref Options _opts)
        {
            opts = _opts; // extCollMng = new ExtCollMng();
            // invariant limits
            numSegmentFrom.Minimum = 1; numSegmentTo.Minimum = 1; 
            numWordsMin.Minimum = 1; numWordsMax.Minimum = 2;
            numRandomSample.Minimum = 2; numRandomSample.Maximum = 1000;
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
            set { if (value) rectCover.Visibility = Visibility.Visible; else rectCover.Visibility = Visibility.Collapsed; } 
        }
        public ECquery GetQueryFromVisuals()
        {
            ECquery ecq = new ECquery()
            {
                SegmentFlag = Convert.ToBoolean(chkSegment.IsChecked.Value),
                SegmentFrom = numSegmentFrom.Value,
                SegmentTo = numSegmentTo.Value,

                categories = new HashSet<Categories>(),

                WordsFlag = Convert.ToBoolean(chkWords.IsChecked.Value),
                WordsMin = numWordsMin.Value,
                WordsMax = numWordsMax.Value,

                Filter = tbFilter.Text,

                RandomSampleFlag = Convert.ToBoolean(chkRandomSample.IsChecked.Value),
                RandomSampleSize = numRandomSample.Value
            };
            if (ecq.SegmentFrom > ecq.SegmentTo) opts.Log("Error: segment bondaries.");
            if (ecq.WordsMin > ecq.WordsMax) opts.Log("Error: words size limits.");
            return ecq;
        }
        public void SetQuery2Visuals(ECquery qry)
        {
            chkSegment.IsChecked = qry.SegmentFlag;
            numSegmentFrom.Value = qry.SegmentFrom;
            numSegmentTo.Value = qry.SegmentTo;

            // qry.categories -> later

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
                folderPath = _folderPath; ss = "(" + ecdesc.rowNumber + " lines)";
                numSegmentFrom.Maximum = ecdesc.rowNumber - 1; numSegmentTo.Maximum = ecdesc.rowNumber; 
            }
            else  folderPath = "";
            lbInfo.Content = "Extract cues from external prompt collection " + ss;
            string qfn = Path.Combine(folderPath, lastQuery); 
            if (File.Exists(qfn))
            {
                SetQuery2Visuals(JsonConvert.DeserializeObject<ECquery>(File.ReadAllText(qfn)));
            }
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

                List<string> cues = ec.ExtractCueList(folderPath, ecq);
                if (cues.Count == 0) { opts.Log("Warnning: No cues have been extracted."); return; }

                string fn = NextIdxFilePath();
                if (fn == "") return;
                if (File.Exists(fn)) File.Delete(fn);
                using (StreamWriter sw = File.AppendText(fn))
                {
                    sw.WriteLine("## query: " + ecq.ToJson()); sw.WriteLine("##");
                    foreach (string ss in cues)
                    {
                        sw.WriteLine(ss);
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
    }
}
