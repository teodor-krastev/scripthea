using System;
using System.Windows;
using System.Windows.Input;
using System.Text;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
//using System.Deployment.Application;
using System.Reflection;
using System.Drawing;
using System.Net;
using System.Drawing.Imaging;

using Label = System.Windows.Controls.Label;
using FontFamily = System.Windows.Media.FontFamily;
//using NationalInstruments.Controls;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;


namespace UtilsNS
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            return temp.ToString();

        }
    }
    public static class Utils
    {
        static Random rand = new Random();
        /// <summary>
        /// ProcessMessages of the visual components 
        /// </summary>
        /// <param name="dp"></param>
        public static void DoEvents(DispatcherPriority dp = DispatcherPriority.Background)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(dp,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }
        public static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }
        /// <summary>
        /// Delaied Execution of some code
        /// ex. Utils.DelayExec(3000, () => { tbLog.Document.Blocks.Clear(); }); 
        /// </summary>
        /// <param name="Delay">Delay in ms</param>
        /// <param name="action">the code to be executed</param>
        public static void DelayExec(int Delay, Action action)
        {
            DispatcherTimer timer = null;
            timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, Delay),
                DispatcherPriority.Normal, (snd, ea) => { action(); timer.Stop(); }, Dispatcher.CurrentDispatcher);
        }
        public static void CallTheWeb(string query)
        {
            System.Diagnostics.Process.Start(query);
        }
        public static void AskGoogle(string query)
        {
            CallTheWeb("https://www.google.com/search?q=" + query.Replace(' ', '+'));
        }
        public static void Sleep(int milisec)
        {
            Thread.Sleep(milisec);
        }

        /// <summary>
        /// The developer computer. It shouldn't matter, but still..
        /// </summary>
        /// <returns></returns>
        public static bool TheosComputer()
        {
            string cn = (string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"];
            return (cn == "DESKTOP-U334RMA") || (cn == "THEOS") || (cn == "THEO-PC");
        }
        public static bool isSingleChannelMachine // only in Axel-hub
        {
            get
            {
                string cn = (string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"];
                return ((cn == "DESKTOP-U9GFG8U") || (cn == "CHAMELEON-HP")); // Plexal or Chameleon machines
            }
        }

        /// <summary>
        /// trace the program flow with this tool
        /// </summary>
        public static int traceMode = 3; // 0 - off; 1 - without time & file; 2 - without file; 3 - full
        /// <summary>
        /// output destination
        /// if null to Console (default)
        /// if RichTextBox write there
        /// if FileLogger write to a file (<timestamp>.trc)
        /// </summary>
        public static object traceDest = null;
        public static void Trace(string message = "",
                    [CallerLineNumber] int lineNumber = 0,
                    [CallerMemberName] string methodName = "",
                    [CallerFilePath] string fileName = "")
        {
            string txt = "";
            switch (traceMode)
            {
                case (1):
                    txt = string.Format("> {0} ({1}:{2})", message, methodName, lineNumber);
                    break;
                case (2):
                    txt = string.Format("{0:g}> {1} ({2}:{3})", DateTime.Now.TimeOfDay, message, methodName, lineNumber);
                    break;
                case (3):
                    txt = string.Format("{0:g}> {1} ({2}:{3})  in {4}", DateTime.Now.TimeOfDay, message, methodName, lineNumber, Path.GetFileName(fileName));
                    break;
            }
            if (!txt.Equals(""))
            {
                if (isNull(traceDest)) Console.WriteLine(txt);
                else
                {
                    if (traceDest is RichTextBox) log((RichTextBox)traceDest, txt, System.Windows.Media.Brushes.Maroon);
                    if (traceDest is FileLogger)
                    {
                        var fl = (traceDest as FileLogger);
                        if (!fl.Enabled) { fl.defaultExt = ".trc"; fl.Enabled = true; } // at first use
                        fl.log(txt);
                    }
                }
            }
        }
        /// <summary>
        /// Get the app version from Project properties -> Assembly Information
        /// </summary>
        /// <returns></returns>
        public static string getAppFileVersion
        {
            get
            {
                string FileLoc = Assembly.GetCallingAssembly().Location;
                return System.Diagnostics.FileVersionInfo.GetVersionInfo(FileLoc).ProductVersion.ToString();
            }
        }
        public static string getAppFileBuild
        {
            get
            {
                string FileLoc = Assembly.GetCallingAssembly().Location;
                return System.Diagnostics.FileVersionInfo.GetVersionInfo(FileLoc).FileBuildPart.ToString();
            }
        }
        /// <summary>
        /// Randomize List<T>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> Randomize<T>(List<T> list)
        {
            List<T> randomizedList = new List<T>();
            Random rnd = new Random();
            while (list.Count > 0)
            {
                int index = rnd.Next(0, list.Count); //pick a random item from the master list
                randomizedList.Add(list[index]); //place it at the end of the randomized list
                list.RemoveAt(index);
            }
            return randomizedList;
        }

        public static bool keyStatus(string key) // check for one of Shift; Ctrl; Alt
        {
            bool rslt = false;
            switch (key)
            {
                case "Shift": rslt = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
                    break;
                case "Ctrl": rslt = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
                    break;
                case "Alt": rslt = (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                    break;
            }
            return rslt;
        }
        /// <summary>
        /// The proper way to check if object is null
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static bool isNull(System.Object o)
        {
            return object.ReferenceEquals(null, o);
        }
        /// <summary>
        /// Check for variable name validity
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsIdentifier(string varName)
        {
            if (string.IsNullOrEmpty(varName))
                return false;
            if (!char.IsLetter(varName[0]) && varName[0] != '_')
                return false;
            for (int ix = 1; ix < varName.Length; ++ix)
                if (!char.IsLetterOrDigit(varName[ix]) && varName[ix] != '_')
                    return false;
            return true;
        }
        public static bool IsValidVarName(string varName)
        {
            if (string.IsNullOrWhiteSpace(varName))
                return false;
            TextBox textBox = new TextBox();
            try
            {
                textBox.Name = varName;
            }
            catch (ArgumentException ex)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Write in log to rich-text-box
        /// </summary>
        /// <param name="richText">The target rich-text-box</param>
        /// <param name="txt">the actual text to log</param>
        /// <param name="clr">color</param>
        public static void log(RichTextBox richText, string txt, SolidColorBrush clr = null)
        {
            if (isNull(System.Windows.Application.Current)) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
              new Action(() =>
              {
                  TextRange rangeOfText1 = new TextRange(richText.Document.ContentStart, richText.Document.ContentEnd);
                  string tx = rangeOfText1.Text;
                  int len = tx.Length; int maxLen = 10000; // the number of chars kept
                  if (len > (2 * maxLen)) // when it exceeds twice the maxLen
                  {
                      tx = tx.Substring(maxLen);
                      var paragraph = new Paragraph();
                      paragraph.Inlines.Add(new Run(tx));
                      richText.Document.Blocks.Clear();
                      richText.Document.Blocks.Add(paragraph);
                  }
                  rangeOfText1 = new TextRange(richText.Document.ContentEnd, richText.Document.ContentEnd);
                  rangeOfText1.Text = Utils.RemoveLineEndings(txt) + "\r";
                  SolidColorBrush clr1 = System.Windows.Media.Brushes.Black; // default
                  if (isNull(clr) && (txt.Length > 3))
                  {
                      if (txt.Substring(0, 3).Equals("Err")) clr1 = System.Windows.Media.Brushes.Red;
                      if (txt.Substring(0, 3).Equals("War")) clr1 = System.Windows.Media.Brushes.Tomato;
                  }
                  else clr1 = clr;
                  rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, clr1);
                  richText.ScrollToEnd();
              }));
        }
        public static void log(RichTextBox richText, List<string> txt, SolidColorBrush clr = null)
        {
            foreach (string tx in txt) log(richText, tx, clr);
        }

        /// <summary>
        /// Write in log to text-box
        /// </summary>
        /// <param name="tbLog">The target text-box</param>
        /// <param name="txt">the actual text to log</param>
        public static void log(TextBox tbLog, string txt)
        {
            tbLog.AppendText(txt + "\r\n");
            string text = tbLog.Text;
            int maxLen = 10000;
            if (text.Length > 2 * maxLen) tbLog.Text = text.Substring(maxLen);
            tbLog.Focus();
            tbLog.CaretIndex = tbLog.Text.Length;
            tbLog.ScrollToEnd();
        }
        public static void log(TextBox tbLog, List<string> txt)
        {
            foreach (string tx in txt) log(tbLog, tx);
        }
        public static Dictionary<string, string> dictDouble2String(Dictionary<string, double> dv, string prec)
        {
            Dictionary<string, string> ds = new Dictionary<string, string>();
            foreach (var item in dv)
                ds[item.Key] = item.Value.ToString(prec);
            return ds;
        }
        public static void dict2ListBox(Dictionary<string, string> ds, ListBox lbox)
        {
            lbox.Items.Clear();
            foreach (var item in ds)
            {
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = string.Format("{0}: {1}", item.Key, item.Value);
                lbox.Items.Add(lbi);
            }
        }
        /// <summary>
        /// If the name for file to save is unknown, make-up one as date-time stamp
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string timeName(string prefix = "")
        {
            if (prefix.Equals("")) return DateTime.Now.ToString("yy-MM-dd_H-mm-ss");
            else return DateTime.Now.ToString("yy-MM-dd_H-mm-ss") + "_" + prefix;
        }
        /*public static void copyGraphToClipboard(Graph gr, string filename)
        {
            Rect bounds; RenderTargetBitmap bitmap;
            bounds = System.Windows.Controls.Primitives.LayoutInformation.GetLayoutSlot(gr);
            bitmap = new RenderTargetBitmap((int)bounds.Width+50, (int)bounds.Height+50, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(gr);
            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                BitmapFrame frame = BitmapFrame.Create(bitmap);
                encoder.Frames.Add(frame);
                using (var stream = File.Create(filename))
                {
                    encoder.Save(stream);
                }
            }
        }*/

        public static Bitmap GetImage(string imageUrl) //, string filename, ImageFormat format)
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(imageUrl);
            Bitmap bitmap; bitmap = new Bitmap(stream);

            //if (bitmap != null) {  bitmap.Save(filename, format); }

            stream.Flush();
            stream.Close();
            client.Dispose();
            return bitmap;
        }
        /* PROBLEMS ??? 
        public static List<string> GetText(string textUrl) 
        {
            WebClient client = new WebClient();           
            string text = client.DownloadString(textUrl);
            string[] txtArr = text.Split('\r');
            List<string> ls = new List<string>(txtArr);           
            client.Dispose();
            return ls;
        }         
        public static BitmapImage ToBitmapImage(Bitmap bitmap, ImageFormat imageFormat)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, imageFormat);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }*/

        public static void VisualCompToPng(UIElement element, string filename = "")
        {
            var rect = new Rect(element.RenderSize);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new VisualBrush(element), null, rect);
            }
            var bitmap = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            if (filename.Equals(""))
            {
                Clipboard.SetImage(bitmap);
                Utils.TimedMessageBox("The image is in the clipboard");
            }
            else
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var file = File.OpenWrite(filename))
                {
                    encoder.Save(file);
                }
            }
        }
        public static string betweenStrings(string text, string start, string end = "")
        {
            int p1 = (start == "") ? 0 : text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if (end == "") return (text.Substring(p1));
            else return text.Substring(p1, p2 - p1);
        }
        public static string skimRem(string line)
        {
            if (isNull(line)) return line;
            string rslt = line;
            int found = line.IndexOf("#");
            if (found > -1)
                rslt = line.Substring(0, found);
            return rslt.Trim();
        }
        public static List<string> skimRem(List<string> text)
        {
            var ls = new List<string>();
            foreach (string ss in text)
                ls.Add(skimRem(ss));
            return ls;
        }
        /// <summary>
        /// Read text file in List of string
        /// </summary>
        /// <param name="filename">The text file</param>
        /// <param name="skipRem">If to skip # and empty lines</param>
        /// <returns></returns>
        public static List<string> readList(string filename, bool skipRem = true)
        {
            List<string> ls = new List<string>();
            foreach (string line in File.ReadLines(filename))
            {
                if (skipRem)
                {
                    string ss = skimRem(line);
                    if (!ss.Equals("")) ls.Add(ss);
                }
                else ls.Add(line);
            }
            return ls;
        }
        /// <summary>
        /// replace bunch of strings in a string/List
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dict"></param>
        /// <param name="bracket"> if bracket is space then brackets are included into the keys of the dict</param>
        /// <returns></returns>
        public static string replaceFromDict(string src, Dictionary<string, string> dict, char bracket) 
        {
            string rslt = src;
            foreach (var itm in dict)
            {
                string fct = bracket.Equals(' ') ? itm.Key : bracket + itm.Key + bracket;
                rslt.Replace(fct, itm.Value);
            }
            return rslt;
        }
        public static List<string> replaceFromDict(List<string> src, Dictionary<string, string> dict, char bracket)
        {
            List<string> rslt = new List<string>();
            foreach (string itm in src)
                rslt.Add(replaceFromDict(itm, dict, bracket));            
            return rslt;
        }

        public static Dictionary<string, List<string>> readStructList(string filename, bool skipRem = true)
        {
            List<string> ls = readList(filename, skipRem);
            return readStructList(ls, skipRem);
        }
        public static Dictionary<string,List<string>> readStructList(List<string> ls, bool skipRem = false)
        {
            int i, j; string section = "";   
            Dictionary<string, List<string>> rslt = new Dictionary<string, List<string>>();
            foreach (string ss in ls)
            {
                string st = skipRem ? skimRem(ss) : ss;
                i = st.IndexOf('['); j = st.IndexOf(']');
                if ((i == 0) && (j > -1) && (i < j))
                {
                    if (!section.Equals("") && !Utils.isNull(ls)) rslt[section] = ls;
                    section = st.Substring(i+1, j-i-1); ls = new List<string>();
                    continue;
                }
                if (!Utils.isNull(ls)) ls.Add(st);
            }
            if (!section.Equals("") && !Utils.isNull(ls)) rslt[section] = ls;
            return rslt;
        }
        public static List<string> writeStructList(string filename, Dictionary<string, List<string>> data)
        {
            List<string> ls = new List<string>();
            foreach (var section in data)
            {
                ls.Add("[" + section.Key + "]");
                foreach (string ss in section.Value)
                    ls.Add(ss);
            }
            if (!filename.Equals("")) writeList(filename, ls);
            return ls;
        }
        /// <summary>
        /// Write down in a text file a List of string
        /// </summary>
        /// <param name="filename">yes, you guessed right...</param>
        /// <param name="ls">The list in question</param>
        public static void writeList(string filename, List<string> ls)
        {
            File.WriteAllLines(filename, ls.ToArray());
        }
        // 
        /// <summary>
        /// Read text file in Dictionary of string, string
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="skipRem">If to skip # and empty lines</param>
        /// <returns></returns>
        public static Dictionary<string, string> readDict(string filename, bool skipRem = true)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (!File.Exists(filename))
            {
                Utils.TimedMessageBox("File not found: " + filename, "Warning", 3000); return dict;
            }
            List<string> ls = new List<string>();
            foreach (string line in File.ReadLines(filename))
            {
                string ss = skipRem ? skimRem(line) : line;
                if (ss.Equals("")) continue;
                string[] sb = ss.Split('=');
                if (sb.Length != 2) break;
                dict[sb[0]] = sb[1];
            }
            return dict;
        }

        /// <summary>
        /// Write dictionary(string,string) in key=value format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dict"></param>
        public static void writeDict(string filename, Dictionary<string, string> dict)
        {
            List<string> ls = new List<string>();
            foreach (var pair in dict)
            {
                ls.Add(pair.Key + "=" + pair.Value);
            }
            string fn = filename.Equals("") ? dataPath + timeName() + ".txt" : filename;
            File.WriteAllLines(fn, ls.ToArray());
        }

        /// <summary>
        /// Write dictionary(string,oblect) in key=value format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dict"></param>
        public static void writeDict(string filename, Dictionary<string, object> dict)
        {
            List<string> ls = new List<string>();
            foreach (var pair in dict)
            {
                ls.Add(pair.Key + "=" + Convert.ToString(pair.Value));
            }
            string fn = filename.Equals("") ? dataPath + timeName() + ".txt" : filename;
            File.WriteAllLines(fn, ls.ToArray());
        }

        /// <summary>
        /// Restrict Value to MinValue and MaxValue (double)
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static double EnsureRange(double Value, double MinValue, double MaxValue)
        {
            if (Value < MinValue) return MinValue;
            if (Value > MaxValue) return MaxValue;
            return Value;
        }
        /// <summary>
        /// Restrict Value to MinValue and MaxValue (int)
        /// 
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static int EnsureRange(int Value, int MinValue, int MaxValue)
        {
            if (Value < MinValue) return MinValue;
            if (Value > MaxValue) return MaxValue;
            return Value;
        }
        /// <summary>
        /// Check if Value is in range[MinValue..MaxValue] (double)
        /// no limits order required
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static bool InRange(double Value, double MinValue, double MaxValue)
        {
            if (MinValue > MaxValue) return InRange(Value, MaxValue, MinValue);
            return ((MinValue <= Value) && (Value <= MaxValue));
        }
        /// <summary>
        /// Check if Value is in range[MinValue..MaxValue] (int)
        /// no limits order required
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        public static bool InRange(int Value, int MinValue, int MaxValue)
        {
            if (MinValue > MaxValue) return InRange(Value, MaxValue, MinValue);
            return ((MinValue <= Value) && (Value <= MaxValue));
        }

        /// <summary>
        /// Convert string to bool with default value if cannot
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool Convert2BoolDef(string value, bool defaultValue = false)
        {
            bool result;
            return bool.TryParse(value, out result) ? result : defaultValue;
        }
        /// <summary>
        /// Convert string to int with default value if cannot
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int Convert2IntDef(string value, int defaultValue = 0)
        {
            int result;
            return int.TryParse(value, out result) ? result : defaultValue;
        }
        /// <summary>
        /// Convert string to double with default value if cannot
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double Convert2DoubleDef(string value, double defaultValue = 0)
        {
            double result;
            return double.TryParse(value, out result) ? result : defaultValue;
        }
        public static string Convert2StringDef(string value, string defaultValue = "0")
        {
            return String.IsNullOrEmpty(value) ? defaultValue : value ;
        }
        /// <summary>
        /// Random normaly distributed (mean:0 stDev:1) value
        /// </summary>
        /// <returns>random value</returns>
        public static double Gauss01()  
        {
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2);
            return randStdNormal;
        }
        public static double NextGaussian(double mu = 0, double sigma = 1)
        {
            var u1 = rand.NextDouble();  var u2 = rand.NextDouble();
            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                  Math.Sin(2.0 * Math.PI * u2);
            var rand_normal = mu + sigma * rand_std_normal;
            return rand_normal;
        }
        public static List<double> GaussSeries(int nData, double mean, double sigma)
        {
            List<double> ls = new List<double>();
            for (int i = 0; i<nData; i++)
            {
                ls.Add(Gauss01()*sigma+mean);
            }
            return ls;
        }

        /// <summary>
        /// Error outlet if no visual logs available
        /// </summary>
        /// <param name="errorMsg"></param>
        public static void errorMessage(string errorMsg)
        {
            Console.WriteLine("Error: " + errorMsg);
        }

        public static SolidColorBrush ToSolidColorBrush(string hex_code)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex_code);
        }

        /// <summary>
        /// Format double to another double with required format (e.g.precision)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static double formatDouble(double d, string format)
        {
            return Convert.ToDouble(d.ToString(format));
        }
        /// <summary>
        /// Format double array to another double array with required format (e.g.precision)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static double[] formatDouble(double[] d, string format)
        {
            double[] da = new double[d.Length];
            for (int i = 0; i < d.Length; i++) da[i] = Convert.ToDouble(d[i].ToString(format));
            return da;
        }
        public static bool isNumeric(System.Object o)
        {
            double test;
            return double.TryParse(Convert.ToString(o), out test);
        }

        /// <summary>
        /// Strip line endings
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(lineSeparator, string.Empty).Replace(paragraphSeparator, string.Empty);
        }

        /// <summary>
        /// Read dictionary(string,string) from file format key=value
        /// skip empty line, starting with [ or ;
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Dictionary<string, string> readINI(string filename)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (!File.Exists(filename)) throw new Exception("File not found: " + filename);           
            List<string> ls = new List<string>();
            string line;
            foreach (string wline in File.ReadLines(filename))
            {
                if (wline.Equals("")) continue;
                char ch = wline[0];
                if (ch.Equals('[')) continue;
                int sc = wline.IndexOf(';');
                if (sc > -1) line = wline.Remove(sc);
                else line = wline;
                if (line.Equals("")) continue;
                
                string[] sb = line.Split('=');
                if (sb.Length != 2) break;
                dict[sb[0]] = sb[1];
            }
            return dict;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern int MessageBoxTimeout(IntPtr hwnd, String text, String title, uint type, Int16 wLanguageId, Int32 milliseconds);
        /// <summary>
        /// Temporary message not to bother click OK
        /// </summary>
        /// <param name="text"></param>
        /// <param name="title"></param>
        /// <param name="milliseconds"></param>
        public static void TimedMessageBox(string text, string title = "Information", int milliseconds = 1500)
        {
            int returnValue = MessageBoxTimeout(IntPtr.Zero, text, title, Convert.ToUInt32(0), 1, milliseconds); 
            //return (MessageBoxReturnStatus)returnValue;
        }

        /// <summary>
        /// Main directory of current app: System.Reflection.Assembly.GetEntryAssembly().Location <-> Environment.GetCommandLineArgs()[0]
        /// </summary>
        public static string basePath = Directory.GetParent(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).Parent.FullName).FullName; 
        public static string configPath { get { return basePath + "\\Config\\"; } }

        public static bool extendedDataPath { get; set; } // defaults are: in AH - true / in MM2 - false
        public static string dataPath 
        { 
            get 
            { 
                string rslt = basePath + "\\Data\\";
                if (extendedDataPath)
                {                     
                    rslt += DateTime.Now.Month.ToString("D2")+"\\";
                    if (!Directory.Exists(rslt)) Directory.CreateDirectory(rslt);
                }
                return rslt;  
            } 
        }

        /// <summary>
        /// Random string (for testing purposes)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string randomString(int length)
        {
            int l8 = 1 + length / 8; string path, ss = "";
            for (int i = 0; i < l8; i++)
            {
                path = Path.GetRandomFileName();
                path = path.Replace(".", ""); // Remove period.
                ss += path.Substring(0, 8);  // Return 8 character string
            }
            return ss.Remove(length);
        }

        public static double tick2sec(long ticks)
        {
            TimeSpan interval = TimeSpan.FromTicks(ticks);
            return interval.TotalSeconds;
        }

        public static long sec2tick(double secs)
        {            
            return (long)(secs * 1000.0 * 10000.0);
        }
    }

    #region async file logger
    /// <summary> This is an obsolete version, please use FileLogger below instead !!!
    /// Async data storage device 
    /// first you set the full path of the file, otherwise it will save in data dir under date-time file name
    /// when you want the logging to start you set Enabled to true
    /// at the end you set Enabled to false (that will flush the buffer to HD)
    /// </summary>
    public class AutoFileLogger 
    {
        private const bool traceMode = false;
        public string header = ""; // that will be put as a file first line with # in front of it
        public string defaultExt = ".ahf";
        List<string> buffer;
        public int bufferLimit = 256; // number of items
        private int bufferCharLimit = 256000; // the whole byte/char size
        public int bufferSize { get { return buffer.Count; } }
        public int bufferCharSize { get; private set; }
        public string prefix { get; private set; } 
        public bool writing { get; private set; }
        public bool missingData { get; private set; }
        public Stopwatch stw;

        public AutoFileLogger(string _prefix = "", string Filename = "")
        {
            _AutoSaveFileName = Filename;
            prefix = _prefix;
            bufferCharSize = 0;
            buffer = new List<string>();
            stw = new Stopwatch();
        }

        public int log(List<string> newItems)
        {
            if (!Enabled) return buffer.Count;
            foreach (string newItem in newItems) log(newItem);
            return buffer.Count;
        }

        public int log(string newItem)
        {
            if (!Enabled) return buffer.Count;
            buffer.Add(newItem); bufferCharSize += newItem.Length;
            if ((buffer.Count > bufferLimit) || (bufferCharSize > bufferCharLimit)) 
                Flush();
            return buffer.Count;
        }
        public void DropLastChar()
        {
            if (buffer.Count == 0) return;
            string lastItem = buffer[buffer.Count - 1];
            buffer[buffer.Count - 1] = lastItem.Substring(0, lastItem.Length - 1);
        }

        private void ConsoleLine(string txt)
        {
            Console.WriteLine(txt);
        }

        public Task Flush() // do not forget to flush when exit (OR switch Enabled Off)
        {
            if (buffer.Count == 0) return null;
            if (traceMode) ConsoleLine("0h: " + stw.ElapsedMilliseconds.ToString());
            string strBuffer = "";
            for (int i = 0; i < buffer.Count; i++)
            {
                strBuffer += buffer[i] + "\n";
            }
            buffer.Clear(); bufferCharSize = 0;           
            var task = Task.Run(() => FileWriteAsync(AutoSaveFileName, strBuffer, true));
            return task;
        }

        private async Task FileWriteAsync(string filePath, string messaage, bool append = true)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write,
                                                                                               FileShare.None, 65536, true);
                using (StreamWriter sw = new StreamWriter(new BufferedStream(stream)))
                {
                    writing = true;
                    string msg = "1k: " + stw.ElapsedMilliseconds.ToString();                    
                    await sw.WriteAsync(messaage);
                    if(traceMode) ConsoleLine(msg);
                    if(traceMode) ConsoleLine("2p: " + stw.ElapsedMilliseconds.ToString());
                    writing = false;
                }
            }
            catch (IOException e)
            {
                ConsoleLine(">> IOException - " + e.Message);
                missingData = true;
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }

        private bool _Enabled = false;
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (value == _Enabled) return;
                if (value && !_Enabled) // when it goes from false to true
                {
                    string dir = "";
                    if (!_AutoSaveFileName.Equals("")) dir = Directory.GetParent(_AutoSaveFileName).FullName;
                    if (!Directory.Exists(dir))
                    {
                        _AutoSaveFileName = Utils.dataPath  + Utils.timeName(prefix) + defaultExt;
                    }                        

                    string hdr = "";
                    if (header != "") hdr = "# " + header + "\n";
                    var task = Task.Run(() => FileWriteAsync(AutoSaveFileName, hdr, false));

                    task.Wait();
                    writing = false;
                    missingData = false;
                    stw.Start();
                    _Enabled = true;
                }
                if (!value && _Enabled) // when it goes from true to false
                {
                    while (writing)
                    {
                        Thread.Sleep(100);
                    }
                    Task task = Flush();
                    if (task != null) task.Wait();
                    if (missingData) Console.WriteLine("Some data maybe missing from the log");
                    stw.Reset();
                    header = "";
                    _Enabled = false;
                }
            }
        }

        private string _AutoSaveFileName = "";
        public string AutoSaveFileName
        {
            get
            {
                return _AutoSaveFileName;
            }
            set
            {
                if (Enabled) throw new Exception("Logger.Enabled must be Off when you set AutoSaveFileName.");
                _AutoSaveFileName = value;
            }
        }
    }

    /// <summary>
    /// RECOMMENDED
    /// Async data storage device - new (dec.2018) optimized for speed (7x faster to AutoFilelogger) logger
    /// first you set the full path of the file, otherwise it will save in data dir under date-time file name
    /// when you want the logging to start you set Enabled to true
    /// at the end you set Enabled to false (that will flush the buffer to HD)
    /// </summary>
    public class FileLogger
    {
        private const bool traceMode = false;
        public string header = ""; // that will be put as a file first line with # in front of it
        public List<string> subheaders; 
        public string defaultExt = ".ahf";
        private ActionBlock<string> block;
        public string prefix { get; private set; }
        public string reqFilename { get; private set; }
        public bool writing { get; private set; }
        public bool missingData { get; private set; }
        public Stopwatch stw;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="_prefix"></param>
        /// <param name="_reqFilename"></param>
        public FileLogger(string _prefix = "", string _reqFilename = "") // if reqFilename is something it should contain the prefix; 
        // the usual use is only prefix and no reqFilename
        {
            subheaders = new List<string>();
            reqFilename = _reqFilename;
            prefix = _prefix;
            stw = new Stopwatch();
        }

        /// <summary>
        /// The main call if you have List of strings
        /// </summary>
        /// <param name="newItems"></param>
        public void log(List<string> newItems)
        {
            if (!Enabled) return;
            foreach (string newItem in newItems) log(newItem);
            return;
        }

        /// <summary>
        /// That's the main method 
        /// </summary>
        /// <param name="newItem"></param>
        public void log(string newItem)
        {
            if (!Enabled) return;
            if (writing)
            {
                missingData = true; return;
            }
            writing = true;
            block.Post(newItem);
            writing = false;
            return;
        }

        /// <summary>
        /// Optional (traceMode) console output - only for debug
        /// </summary>
        /// <param name="txt"></param>
        private void ConsoleLine(string txt)
        {
            if(traceMode) Console.WriteLine(txt);
        }

        /// <summary>
        /// Create actual asynchronious logger 
        /// </summary>
        /// <param name="filePath"></param>
        public void CreateLogger(string filePath)
        {
            block = new ActionBlock<string>(async message =>
            {
                using (var f = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    f.Position = f.Length;
                    using (var sw = new StreamWriter(f))
                    {
                        await sw.WriteLineAsync(message);
                    }
                }
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }

        private string _LogFilename = "";
        public string LogFilename
        {
            get { return _LogFilename; }
            private set
            {
                if (Enabled) throw new Exception("Logger.Enabled must be Off when you set LogFileName.");
                _LogFilename = value;
            }
        }

        /// <summary>
        /// Write a header and subheaders with # 
        /// </summary>
        public virtual void writeHeader()
        {
            if (!header.Equals("")) log("#" + header);
            for (int i=0; i<subheaders.Count; i++)
                log("#" + subheaders[i]);
            if(!header.Equals("") || (subheaders.Count > 0)) log("\n");
        }

        private bool _Enabled = false;
        /// <summary>
        /// Switch this on to create the file and start to accept logs
        /// switch it off to flash the buffer and close the file
        /// </summary>
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (value == _Enabled) return;
                if (value && !_Enabled) // when it goes from false to true
                {
                    string dir = "";

                    if (reqFilename.Equals("")) LogFilename = Utils.dataPath + Utils.timeName(prefix) + defaultExt;
                    else
                    {
                        dir = Directory.GetParent(reqFilename).FullName;
                        if (!Directory.Exists(dir)) dir = Utils.dataPath;
                        string fn = Path.GetFileName(reqFilename);
                        if (!prefix.Equals(""))
                        {
                            string ext = Path.GetExtension(fn);
                            if (ext.Equals("")) ext = defaultExt;
                            string fn1 = Path.ChangeExtension(fn, ""); fn1 = fn1.Remove(fn1.Length - 1, 1);
                            fn = Path.ChangeExtension(fn1 + "_" + prefix, ext);
                        }
                        LogFilename = dir + "\\" + fn;
                    }
                    if (File.Exists(LogFilename)) File.Delete(LogFilename);
                    CreateLogger(LogFilename);

                    writing = false;
                    missingData = false;
                    stw.Restart();
                    _Enabled = true;
                    writeHeader();
                }
                if (!value && _Enabled) // when it goes from true to false
                {
                    if (missingData) Console.WriteLine("Some data maybe missing from the log");
                    stw.Reset();
                    header = "";
                    _Enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// creates and logs in multicollumn table; record structure is defined in record List of string
    /// the column names must be set when created
    /// dictLog will extract only the keys with these names in that order
    /// </summary>
    public class DictFileLogger : FileLogger 
    {
        List<string> record;
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="_record">Required and fixed column names</param>
        /// <param name="_prefix">Ending in case of timestamp name</param>
        /// <param name="_reqFilename">If empty timestamp name is generated</param>
        public DictFileLogger(string[] _record, string _prefix = "", string _reqFilename = ""): base(_prefix, _reqFilename)
            // if reqFilename is something it should contain the prefix; 
            // the usual use is only prefix and no reqFilename
        {
            record = new List<string>(_record);
        }
        private readonly string[] titles = { "params", "steps" };
        /// <summary>
        /// When the group MMexec is known
        /// </summary>
        /// <param name="mme"></param>
       /* public void setMMexecAsHeader(MMexec mme)
        {
            header = ""; subheaders.Clear(); char q = '"';
            if (Utils.isNull(mme)) return;
            foreach (string ss in titles)
            {
                if (mme.prms.ContainsKey(ss))
                {
                    string sub = JsonConvert.SerializeObject(mme.prms[ss]);
                    mme.prms.Remove(ss);
                    subheaders.Add("{"+q+ss+q+":"+sub+"}");
                }
            }
            header = JsonConvert.SerializeObject(mme);
        }*/
        /// <summary>
        /// Write the header & subheaders and colunm names line
        /// </summary>
        public override void writeHeader()
        {
            base.writeHeader();
            if(Utils.isNull(record)) throw new Exception("The record list not set");
            if (record.Count.Equals(0)) throw new Exception("The record list is empty");
            string ss = "";
            foreach (string item in record) 
                ss += item + '\t';
            ss = ss.Remove(ss.Length - 1); 
            log(ss);
        }
       
        /// <summary>
        /// This main methods in three variations, but that is the basic one
        /// </summary>
        /// <param name="dict"></param>
        public void dictLog(Dictionary<string, string> dict)
        {
            string ss = "";
            foreach (string item in record)
            {
                if (dict.ContainsKey(item)) ss += dict[item];
                else ss += "<none>";
                ss += '\t';
            }
            ss = ss.Remove(ss.Length - 1);
            log(ss);
        }

        /// <summary>
        /// log that way with undefined Values type
        /// </summary>
        /// <param name="dict"></param>
        public void dictLog(Dictionary<string, object> dict)
        {
            Dictionary<string, string> dictS = new Dictionary<string, string>();
            foreach (var pair in dict)
            {
                dictS[pair.Key] = pair.Value.ToString();
                if (pair.Key.Length > 4)
                    if (pair.Key.Substring(0, 5).ToLower().Equals("index")) dictS[pair.Key] = Convert.ToInt32(pair.Value).ToString();

            }
            dictLog(dictS); 
        }

        /// <summary>
        /// log that way with double Values type with format
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="format"></param>
        public void dictLog(Dictionary<string, double> dict, string format = "")
        {
            Dictionary<string, string> dictS = new Dictionary<string, string>();
            foreach (var pair in dict)
            {
                if (Double.IsNaN(pair.Value)) dictS[pair.Key] = "NaN";
                else
                {
                    dictS[pair.Key] = pair.Value.ToString(format);
                    if (pair.Key.Length > 4)
                        if (pair.Key.Substring(0, 5).ToLower().Equals("index")) dictS[pair.Key] = Convert.ToInt32(pair.Value).ToString();
                }
            }
            dictLog(dictS); 
        }
    }

    /// <summary>
    /// Read dictionary from multi-column text file (tab separated)
    /// format first line #header (for conditions) - optional
    /// next line column names;
    /// header, subheaders & col names are read when instance is created 
    /// if _record = null then read this row in record
    /// if _record has items the record will be the cross-section of _record and column names list (fileRecord)
    /// </summary>
    public class DictFileReader
    {
        public string header;
        public List<string> subheaders; 
        StreamReader fileReader; public int counter = 0;
        public List<string> record, fileRecord;
        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="Filename">File must exits</param>
        /// <param name="strArr">Array of column names</param>
        public DictFileReader(string Filename, string[] strArr = null)
        {            
            if (!File.Exists(Filename)) throw new Exception("no such file: "+Filename);
            header = ""; subheaders = new List<string>();
            // Read file using StreamReader. Reads file line by line  
            fileReader = new StreamReader(Filename);  
            counter = 0;
            string ln = fileReader.ReadLine();
            while(ln.StartsWith("#"))
            {
                if (header.Equals("")) header = ln.Remove(0, 1);
                else subheaders.Add(ln.Remove(0, 1));
                ln = fileReader.ReadLine();
            }
            while (ln.Equals(""))
                ln = fileReader.ReadLine(); // read the next if empty, and again...

            string[] ns = ln.Split('\t');
            fileRecord = new List<string>(ns);
            if (Utils.isNull(strArr)) record = new List<string>(fileRecord);
            else
            {
                List<string> _record = new List<string>(strArr);
                if (_record.Count.Equals(0)) record = new List<string>(fileRecord);
                else
                {
                    record = new List<string>(_record);
                    for (int i = _record.Count-1; i > -1; i--)
                    {
                        int j = fileRecord.IndexOf(_record[i]);
                        if (j.Equals(-1)) record.RemoveAt(i); 
                    }
                }        
            }
        }
        
        /// <summary>
        /// returns one line (row) as (column.name , cell.value) dictionary
        /// </summary>
        /// <param name="rslt">one table row</param>
        /// <returns>if we can go again</returns>
        public bool stringIterator(ref Dictionary<string,string> rslt) // 
        {
            if (Utils.isNull(rslt)) rslt = new Dictionary<string, string>();
            else rslt.Clear();
            bool next = false; 
            string ln = fileReader.ReadLine();           
            if (Utils.isNull(ln))
            {
                fileReader.Close();
                return next;
            }
            while (ln.Equals("")) 
                ln = fileReader.ReadLine(); // read the next if empty, and again...
            string[] ns = ln.Split('\t');
            if(ns.Length != fileRecord.Count) throw new Exception("wrong number of columns");
            foreach (string ss in record)
            {
                int j = fileRecord.IndexOf(ss);
                if (j.Equals(-1)) throw new Exception("wrong column name: "+ss);
                rslt[ss] = ns[j];
            }
            counter++;
            next = true; return next;
        }

        /// <summary>
        /// same as above but Values are double
        /// </summary>
        /// <param name="rslt"></param>
        /// <returns></returns>
        public bool doubleIterator(ref Dictionary<string, double> rslt) // same as above but values in double (if possible)
        {
            rslt = new Dictionary<string, double>();
            Dictionary<string, string> strRslt = new Dictionary<string, string>();
            bool next = stringIterator(ref strRslt);
            foreach (var pair in strRslt)
            {
                try
                {
                    double dbl = Convert.ToDouble(pair.Value);
                    rslt[pair.Key] = dbl;
                }
                catch (FormatException)
                {
                    rslt[pair.Key] = Double.NaN;
                }               
            }
            return next;
        }
    }
    
    #endregion
    /// <summary>
    /// Hour-glass cursor while waiting for Godot
    /// </summary>
    public class WaitCursor : IDisposable
    {
        private System.Windows.Input.Cursor _previousCursor;
        public WaitCursor()
        {
            _previousCursor = Mouse.OverrideCursor;

            Mouse.OverrideCursor = Cursors.Wait;
        }

        #region IDisposable Members
        public void Dispose()
        {
            Mouse.OverrideCursor = _previousCursor;
        }
        #endregion
    }
    public class InputBox
    {
        Window Box = new Window();//window for the inputbox
        FontFamily font = new FontFamily("Segoe UI");//font for the whole inputbox
        int FontSize = 12;//fontsize for the input
        Grid grid = new Grid();// items container
        string title = "Message";//title as heading
        string boxcontent;//title, if windows type allows !
        string defaulttext = "";//default textbox content
        string errormessage = "Invalid text";//error messagebox content
        string errortitle = "Error";//error messagebox heading title
        string okbuttontext = "OK";//Ok button content
        string CancelButtonText = "Cancel";
        System.Windows.Media.Brush BoxBackgroundColor = System.Windows.Media.Brushes.WhiteSmoke;// Window Background
        System.Windows.Media.Brush InputBackgroundColor = System.Windows.Media.Brushes.MintCream;// Textbox Background
        bool clickedOk = false;
        TextBox input = new TextBox();
        Button ok = new Button();
        Button cancel = new Button();
        bool inputreset = false;
        public InputBox(string DefaultText)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            title = "Message";
            windowdef();
        }

        public InputBox(string Htitle, string DefaultText, string boxContent)
        {
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                boxcontent = boxContent;
            }
            catch { boxcontent = "Error!"; }
            windowdef();
        }

        public InputBox(string Htitle, string DefaultText, string Font, int Fontsize)
        {
            try
            {
                defaulttext = DefaultText;
            }
            catch
            {
                DefaultText = "Error!";
            }
            try
            {
                font = new FontFamily(Font);
            }
            catch { font = new FontFamily("Tahoma"); }
            try
            {
                title = Htitle;
            }
            catch
            {
                title = "Error!";
            }
            if (Fontsize >= 1)
                FontSize = Fontsize;
            windowdef();
        }
        private void windowdef()// window building - check only for window size
        {
            Box.Height = 120;// Box Height
            Box.Width = 450;// Box Width
            Box.Background = BoxBackgroundColor;
            Box.Title = title;
            Box.Content = grid;
            Box.Closing += Box_Closing;
            Box.WindowStyle = WindowStyle.None;
            Box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            TextBlock header = new TextBlock();
            header.TextWrapping = TextWrapping.Wrap;
            header.Background = null;
            header.HorizontalAlignment = HorizontalAlignment.Stretch;
            header.VerticalAlignment = VerticalAlignment.Top;
            header.FontFamily = font;
            header.FontSize = FontSize;
            header.Margin = new Thickness(10, 10, 10, 10);
            header.Text = title;
            grid.Children.Add(header);

            input.Background = InputBackgroundColor;
            input.FontFamily = font;
            input.FontSize = FontSize;
            input.Height = 25;
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            input.VerticalAlignment = VerticalAlignment.Top;
            input.Margin = new Thickness(10, 33, 10, 10);
            input.MinWidth = 200;
            input.MouseEnter += input_MouseDown;
            input.KeyDown += input_KeyDown;
            input.Text = defaulttext;
            grid.Children.Add(input);

            ok.Width = 65;
            ok.Height = 25;
            ok.HorizontalAlignment = HorizontalAlignment.Right;
            ok.VerticalAlignment = VerticalAlignment.Bottom;
            ok.Margin = new Thickness(0, 0, 10, 10);
            ok.Click += ok_Click;
            ok.Content = okbuttontext;

            cancel.Width = 65;
            cancel.Height = 25;
            cancel.HorizontalAlignment = HorizontalAlignment.Right;
            cancel.VerticalAlignment = VerticalAlignment.Bottom;
            cancel.Margin = new Thickness(0, 0, 85, 10);
            cancel.Click += cancel_Click;
            cancel.Content = CancelButtonText;

            grid.Children.Add(ok);
            grid.Children.Add(cancel);

            input.Focus();
        }
        void Box_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //validation
        }
        private void input_MouseDown(object sender, MouseEventArgs e)
        {
            if ((sender as TextBox).Text == defaulttext && inputreset)
            {
                (sender as TextBox).Text = null;
                inputreset = true;
            }
        }
        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && clickedOk == false)
            {
                e.Handled = true;
                ok_Click(input, null);
            }
            if (e.Key == Key.Escape)
            {
                cancel_Click(input, null);
            }
        }
        void ok_Click(object sender, RoutedEventArgs e)
        {
            clickedOk = true;
            if (input.Text == "")
                MessageBox.Show(errormessage, errortitle, MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                Box.Close();
            }
            clickedOk = false;
        }
        void cancel_Click(object sender, RoutedEventArgs e)
        {
            input.Text = "";
            Box.Close();
        }
        public string ShowDialog()
        {
            Box.ShowDialog();
            return input.Text;
        }
    }
}