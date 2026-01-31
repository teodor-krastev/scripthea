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

namespace scripthea.cuestuff
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
            opts = _opts; 
            // invariant limits
            numSegmentFrom.Minimum = 1; numSegmentTo.Minimum = 1; 
            numWordsMin.Minimum = 1; numWordsMax.Minimum = 2;
            numRandomSample.Minimum = 2; numRandomSample.Maximum = 1000;
            numSemanticBest.Minimum = 0; numSemanticBest.Maximum = 1000;

            catChecks.Clear(); wpCats.Children.Clear();
            foreach (string cat in Enum.GetNames(typeof(Categories)))
            {
                CheckBox chk = new CheckBox() { Content = cat.Replace("_", "-").PadRight(18, ' '), FontSize = 13,  Margin = new Thickness(3) };
                catChecks.Add(chk); wpCats.Children.Add(chk);
            }
            cbiSemantic.IsEnabled = opts.composer.SemanticActive();
            if (!cbiSemantic.IsEnabled && cbWay2Match.SelectedIndex == 2) cbWay2Match.SelectedIndex = 0;
            cbWay2Match_SelectionChanged(null, null);
            eCollection = new ECollection(new Utils.LogHandler(opts.Log));
            eCollection.semanticActive = cbiSemantic.IsEnabled;
        }
        public void Finish()
        {
            if (!Directory.Exists(folderPath)) return;
            ECquery ecq = GetQueryFromVisuals(); if (ecq is null) return;
            File.WriteAllText(Path.Combine(folderPath, lastQuery), JsonConvert.SerializeObject(ecq));
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
            bool bb = ecdesc != null; // SJL file
            bool sf = _sjlFlag;
            if (bb)
            {
                bool? bc = ecdesc.sjlFlag();
                if (bc != null)
                { 
                    sf = (bool)bc;
                    if (sf != _sjlFlag) { opts.Log("Error: internal error #587"); return null; }
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
                Way2Match = cbWay2Match.SelectedIndex,
                SemanticBest = numSemanticBest.Value,

                RandomSampleFlag = Convert.ToBoolean(chkRandomSample.IsChecked.Value),
                RandomSampleSize = numRandomSample.Value
            };
            if (ecq.SegmentFlag)
                if (ecq.SegmentFrom > ecq.SegmentTo) { opts.Log("Error: segment limits."); return null; }
            if (ecq.WordsFlag)
                if (ecq.WordsMin > ecq.WordsMax) { opts.Log("Error: words size limits."); return null; }

            if (bb) bb = ecdesc.useCategories;
            if (sjlFlag && bb)
            {
                ecq.CatFlag = chkFilterByCat.IsChecked.Value;
                ecq.CatThreshold = (int)sliderThreshold.Value;
                ecq.categories = GetCategories();
                if (ecq.CatFlag && ecq.categories.Count == 0) { opts.Log("Error: no selected categories."); return null; }
            }
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
            cbWay2Match.SelectedIndex = qry.Way2Match;
            numSemanticBest.Value = qry.SemanticBest;

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
            if (ecdesc is null) return false;
            return ecdesc.Validate();
        }
        public bool IsCollectionFolder(string _folderPath)
        {
            if (!Directory.Exists(_folderPath)) return false;
            string dfn = Path.Combine(_folderPath, ECdesc.descName);
            if (!File.Exists(dfn)) return false;
            ECdesc ecd = ECdesc.OpenECdesc(_folderPath);
            if (ecd is null) return false;
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
        public event EventHandler NewCuesEvent; public ECollection eCollection = null;
        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            if (folderPath == "") { opts.Log("Error[911]: no valid folder"); return; }
            tbInfo.Text = "info"; 
            try
            {
                Mouse.OverrideCursor = Cursors.Wait; eCollection.cancelRequest = MessageBoxResult.No; 

                eCollection.OpenEColl(folderPath); 
                ECquery ecq = GetQueryFromVisuals(); if (ecq is null) return;
                semanticMatching = ecq.Way2Match.Equals(2); VisualHelper.SetButtonEnabled(btnSemStop, semanticMatching);
                ecq.SemanticProgress = tbSemProgress;

                List<ECprompt> cues = eCollection.ExtractCueList(folderPath, ecq);
                if (cues is null) return; // user cancelation
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
            } finally { Mouse.OverrideCursor = null; semanticMatching = false; VisualHelper.SetButtonEnabled(btnSemStop, false); }
        }       
        private void sliderThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lbThreshold.Content = "Cat.threshold (" + (int)sliderThreshold.Value + "%)";
        }
        private void cbWay2Match_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbBest is null) return;
            if (cbWay2Match.SelectedIndex == 2 && cbiSemantic.IsEnabled) spSemantic.Visibility = Visibility.Visible;  
            else spSemantic.Visibility = Visibility.Hidden;
            semanticMatching = cbWay2Match.SelectedIndex == 2 && cbiSemantic.IsEnabled;
        }
        private bool _semanticMatching = false;
        public bool semanticMatching // ongoing semantic matching
        {
            get { return _semanticMatching; }
            set
            {
                tbSemProgress.Visibility = Visibility.Hidden; btnSemStop.Visibility = Visibility.Hidden; // default state
                if (cbWay2Match.SelectedIndex != 2 && !cbiSemantic.IsEnabled) return;
                if (value) // going ON
                {
                    tbSemProgress.Visibility = Visibility.Visible; btnSemStop.Visibility = Visibility.Visible; 
                    eCollection.cancelRequest = MessageBoxResult.No; tbSemProgress.Text = "0%";
                }
                _semanticMatching = value;
            }
        }       
        private void btnSemStop_Click(object sender, RoutedEventArgs e)
        {
            if (!semanticMatching) return;
            eCollection.cancelRequest = MessageBox.Show(
                "Would you like to:\n- stop semantic matching at this stage (YES)\n- continue with the rest of the prompts (NO), or\n- CANCEL the extraction?",       
                "Semantic Matching Confirmation",                       
                MessageBoxButton.YesNoCancel,   // Three buttons
                MessageBoxImage.Question        // Icon
            );            
        }
        private void cbWay2Match_DropDownOpened(object sender, EventArgs e)
        {
            cbiSemantic.IsEnabled = opts.composer.SemanticActive();
            eCollection.semanticActive = cbiSemantic.IsEnabled;            
        }
    }
}
