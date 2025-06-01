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
using ExtCollMng;
using Newtonsoft.Json.Linq;

namespace scripthea.composer
{

    /// <summary>
    /// Interaction logic for ExtCollectionsUC.xaml
    /// </summary>
    public partial class ExtCollectionsUC : UserControl
    {
        public ExtCollectionsUC()
        {
            InitializeComponent();
        }
        private Options opts; private List<CheckBox> catChecks = new List<CheckBox>();
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
            if (!Directory.Exists(folderPath)) return;
            File.WriteAllText(Path.Combine(folderPath, lastQuery), JsonConvert.SerializeObject(GetQueryFromVisuals()));
        }
        public bool CoverOn 
        { 
            get { return rectCover.Visibility == Visibility.Visible; } 
            set { if (value) rectCover.Visibility = Visibility.Visible; else rectCover.Visibility = Visibility.Collapsed; } 
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
            bool bb = ecdesc != null;
            bool sf = _sjlFlag;
            if (bb)
            {
                bool? bc = ecdesc.sjlFlag();
                if (bc != null)
                { 
                    sf = (bool)bc;
                    if (sf != _sjlFlag) opts.Log("Error: internal error #587");
                }
            }
            ECquery ecq = new ECquery()
            {
                sjlFlag = sf,

                SegmentFlag = Convert.ToBoolean(chkSegment.IsChecked.Value),
                SegmentFrom = numSegmentFrom.Value,
                SegmentTo = numSegmentTo.Value,

                CatFlag = false,

                WordsFlag = Convert.ToBoolean(chkWords.IsChecked.Value),
                WordsMin = numWordsMin.Value,
                WordsMax = numWordsMax.Value,

                Pattern = tbPatternMatching.Text,
                RegExFlag = chkRegEx.IsChecked.Value,

                RandomSampleFlag = Convert.ToBoolean(chkRandomSample.IsChecked.Value),
                RandomSampleSize = numRandomSample.Value
            };
            
            if (bb) bb = ecdesc.useCategories;
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

            tbPatternMatching.Text = qry.Pattern;
            chkRegEx.IsChecked = qry.RegExFlag;

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
                if (bb) bb = ecdesc.useCategories;
                if (value && bb) rowDefCats.Height = new GridLength(77); 
                else rowDefCats.Height = new GridLength(0);
            } 
        } 
        public bool Validate()
        {
            if (ecdesc == null) return false;
            return ecdesc.Validate();
        }
        public bool IsCollectionFolder(string _folderPath)
        {
            if (!Directory.Exists(_folderPath)) return false;
            string dfn = Path.Combine(_folderPath, ECdesc.descName);
            if (!File.Exists(dfn)) return false;
            ECdesc ecd = ECdesc.OpenECdesc(_folderPath);
            if (ecd == null) return false;
            return ecd.Validate();
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
                string fn = Path.Combine(folderPath, ecdesc.coreName) + "-" + i.ToString("D2") + ".cues";
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
                ECollection ec = new ECollection(new Utils.LogHandler(opts.Log));
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
        private void sliderThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lbThreshold.Content = "Cat.threshold (" + (int)sliderThreshold.Value + "%)";
        }        
    }
}
