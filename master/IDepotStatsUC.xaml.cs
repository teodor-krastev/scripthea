using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using scripthea.options;
using scripthea.viewer;
using UtilsNS;
using Path = System.IO.Path;

namespace scripthea.master
{
    /// <summary>
    /// Interaction logic for IDstatsUC.xaml
    /// </summary>
    public partial class IDepotStatsUC : UserControl
    {
        public IDepotStatsUC()
        {
            InitializeComponent();
        }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        public void Clear()
        {
            lbImgFolder.Items.Clear();
            lbImages.Items.Clear();
            lbRatings.Items.Clear();
            lbCommonParams.Items.Clear();
        }
        public ImageDepot iDepot { get; private set; } = null;
        protected List<FileInfo> fileInfos;
        public void OnChangeDepot(string ImageDepotPath)
        {
            iDepot = new ImageDepot(ImageDepotPath);
            iDepot?.Validate(null);
            Clear();
            ReadFileInfos(iDepot.path);
            if (fileInfos.Count == 0) { opts.Log("Error: image folder problem"); return; }
            FolderStats();
            ImagesStats();
            RatingsHisto();
            CommonParams();
        }
        public List<FileInfo> ReadFileInfos(string iDepotPath)
        {
            fileInfos = new List<FileInfo>();
            if (!Directory.Exists(iDepotPath) || iDepot == null) return fileInfos;
            foreach (ImageInfo ii in iDepot.items)
            {
                string filePath = Path.Combine(iDepotPath, ii.filename);              
                try
                {
                    // Create a FileInfo object for the file
                    FileInfo fileInfo = new FileInfo(filePath);
                    // Check if the file actually exists before trying to get its size
                    if (fileInfo.Exists) fileInfos.Add(fileInfo);                
                    else
                    {
                        opts.Log("Error: File not found at '{filePath}'");
                    }
                }
                catch (IOException ex)
                {
                    // Catch potential IO errors (e.g., path format issues, network problems)
                    opts.Log("Error: An IO error occurred: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Catch errors if you don't have permission to access the file's metadata
                    opts.Log("Error: Access denied: {ex.Message}");
                }
                catch (Exception ex) // Catch other potential exceptions
                {
                    opts.Log("Error: An unexpected error occurred: {ex.Message}");
                }
            }
            return fileInfos; 
        }
        private Dictionary<string, string> FolderStats()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (iDepot == null) return dict;
            if (iDepot.header != null)
                dict = new Dictionary<string, string>(iDepot.header);
            double sizeInK = 0;
            foreach (FileInfo fileInfo in fileInfos) sizeInK += fileInfo.Length / 1024.0; 
            dict.Add("total image space", sizeInK.ToString("F1") +" kB / "+ (sizeInK / 1024.0).ToString("F2")+" Mb");
            Utils.dict2ListBox(dict, lbImgFolder);
            return dict;
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
        private Dictionary<string, string> ImagesStats()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();           
            dict.Add("number of images", fileInfos.Count.ToString());
            DateTime earliestCreated = fileInfos.OrderBy(f => f.CreationTime).First().CreationTime;
            DateTime latestCreated = fileInfos.OrderBy(f => f.CreationTime).Last().CreationTime;
            string ss = earliestCreated.ToString("dd-MMM-yy HH:mm:ss") + " to " + latestCreated.ToString("dd-MMM-yyyy HH:mm:ss");
            if ((int)earliestCreated.ToOADate() == (int)latestCreated.ToOADate()) ss = earliestCreated.ToString("dd-MMM-yy HH:mm:ss") + " to " + latestCreated.ToString("HH:mm:ss");
            dict.Add("time of generation", ss);
            List<double> sizeInK = new List<double>();
            foreach (FileInfo fileInfo in fileInfos) sizeInK.Add(fileInfo.Length / 1024.0); 
            dict.Add("image size", (sizeInK.Average()).ToString("F0") + "  (" + sizeInK.Min().ToString("F0") + " . . " + sizeInK.Max().ToString("F0") + ") kB");
            List<double> promptSizeCh = new List<double>(); List<double> promptSizeWr = new List<double>();
            foreach (ImageInfo ii in iDepot.items)
                { promptSizeCh.Add(ii.prompt.Length); promptSizeWr.Add(WordCount(ii.prompt)); }
            dict.Add("prompt size in chars", (promptSizeCh.Average()).ToString("F1") + "  ("+ promptSizeCh.Min().ToString("F0") + " . . " + promptSizeCh.Max().ToString("F0") + ")");
            dict.Add("prompt size in words", (promptSizeWr.Average()).ToString("F1") + "  (" + promptSizeWr.Min().ToString("F0") + " . . " + promptSizeWr.Max().ToString("F0") + ")");
            Utils.dict2ListBox(dict, lbImages);
            return dict;
        }
        private Dictionary<string, string> RatingsHisto()
        {
            const double histoMax = 25.0;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            int Nr = 0; // number of rated
            double meanRating = 0;
            foreach (ImageInfo ii in iDepot.items)
                if (ii.rate > 0) { Nr++; meanRating += ii.rate; }
            dict.Add("number of rated images", Nr.ToString());
            if (Nr > 2)
                dict.Add("average rating (within rated)", (meanRating/Nr).ToString("F1"));
            if (Nr > histoMax)
            {
                double[] histo = new double[5];
                foreach (ImageInfo ii in iDepot.items)
                {
                    if (ii.rate == 0) continue;
                    for (int i = 0; i < 5; i++)
                        if ((2*i+1) == ii.rate || (2*i+2) == ii.rate) histo[i] += 1;
                }
                dict.Add("Histogram", "");
                double hmax = 0;
                for (int i = 0; i < 5; i++)
                    hmax = Math.Max(histo[i], hmax);
                if (hmax > 30)
                    for (int i = 0; i < 5; i++)
                        histo[i] *= histoMax / hmax;
                char bar = '░'; //▒
                for (int i = 0; i < 5; i++)
                    dict.Add((2 * i + 1).ToString()+" , "+ (2 * i + 2).ToString(), '\t' + new string(bar, (int)histo[i]));
            }
            Utils.dict2ListBox(dict, lbRatings);
            return dict;
        }
        public Dictionary<string, string> CommonParams()
        {          
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (iDepot == null) return dict;
            if (!iDepot.isEnabled) return dict;
            if (iDepot.items.Count == 0) return dict;
            dict = Utils.dictObject2String(iDepot.items[0].ToDictionary(true));
            dict.Remove("batch_size");
            foreach (ImageInfo ii in iDepot.items)
            {
                Dictionary<string, string> rd = Utils.dictObject2String(ii.ToDictionary(true));
                foreach(KeyValuePair<string,string> prms in rd)
                {
                    if (!dict.ContainsKey(prms.Key)) continue;
                    if (!dict[prms.Key].Equals(prms.Value,StringComparison.InvariantCultureIgnoreCase) || prms.Value.Trim().Equals(""))
                        dict.Remove(prms.Key);
                }
            }
            Utils.dict2ListBox(dict, lbCommonParams);
            return dict;
        }
    }
}
