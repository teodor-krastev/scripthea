using System;
using System.Windows;
using System.Windows.Input;
using System.Text;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Reflection;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using MessageBox = System.Windows.Forms.MessageBox;

using Label = System.Windows.Controls.Label;
using FontFamily = System.Windows.Media.FontFamily;
using System.Linq;
using System.Net.Http;

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
        public delegate void SimpleEventHandler();

        static Random rand = new Random(RandomnSeed);
        /// <summary>
        /// ProcessMessages of the visual components 
        /// </summary>
        /// <param name="dp"></param>
        public static void DoEvents(DispatcherPriority dp = DispatcherPriority.Background)
        {
            try
            {
                DispatcherFrame frame = new DispatcherFrame();

                Dispatcher.CurrentDispatcher.BeginInvoke(dp,
                    new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);
            }
            finally { }
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
                DispatcherPriority.Normal, (snd, ea) => { timer.Stop(); action(); }, Dispatcher.CurrentDispatcher);
        }
        public static bool UrlExists(string url)
        {
            try
            {
                // Create a web request to the URL
                var request = WebRequest.Create(url);
                request.Method = "HEAD"; // Use HEAD request to avoid downloading the whole file

                // Get the response
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    // Check if the status code indicates success
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch (WebException ex)
            {
                // If the file doesn't exist, a WebException will be thrown
                if (ex.Status == WebExceptionStatus.ProtocolError &&
                    ex.Response is HttpWebResponse errorResponse &&
                    errorResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                // Handle other exceptions as needed
                TimedMessageBox($"Error: checking URL {ex.Message}");
            }

            return false;
        }
        public static void CallTheWeb(string query)
        {
            System.Diagnostics.Process.Start(query);
        }
        public enum SearchEngine { google, bing, duckduckgo }
        public static SearchEngine searchEngine = SearchEngine.google;
        public static void AskTheWeb(string query, bool imagesTab = false)
        {
            string imageSwitch = imagesTab ? "&tbm=isch" : "";
            CallTheWeb("https://www." + Convert.ToString(searchEngine) + ".com/search?q=" + query.Trim().Replace(' ', '+') + imageSwitch);
        }
        /// <summary>
        /// start batch file and when it's time to close it: 
        /// 
        /// //Close the batch file
        /// //Try to close gracefully
        /// if (!process.CloseMainWindow())
        /// {
        /// //If the main window cannot be closed, kill the process
        ///    process.Kill();
        /// }
        /// </summary>
        /// <param name="pathToBatchFile"></param>
        /// <returns></returns>
        public static Process RunBatchFile(string pathToBatchFile, bool showShell = true)
        {
            Process process = null;
            try
            {
                if (!File.Exists(pathToBatchFile))
                {
                    string msg = "No batch file: " + pathToBatchFile;
                    TimedMessageBox(msg, "Problem", 3500); Console.WriteLine(msg);
                    return null;
                }
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    FileName = pathToBatchFile,
                    WorkingDirectory = Path.GetDirectoryName(pathToBatchFile),

                    RedirectStandardOutput = !showShell,
                    RedirectStandardError = !showShell,

                    UseShellExecute = showShell,
                    CreateNoWindow = !showShell
                };
                process = Process.Start(processStartInfo);
                if (!showShell) // later if needed
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            return process;
        }
        public static string RunCommand(string workingDirectory, string cmd, bool exitAtEnd = true)
        {
            var cmds = new List<string>() { cmd };
            return RunCommands(workingDirectory, cmds, exitAtEnd);
        }

        public static string RunCommands(string workingDirectory, List<string> cmds, bool exitAtEnd = true)
        {
            string output;
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    if (Directory.Exists(workingDirectory)) process.StartInfo.WorkingDirectory = workingDirectory;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    // Send command to cmd.exe
                    using (StreamWriter sw = process.StandardInput)
                    {
                        if (sw.BaseStream.CanWrite)
                        {
                            foreach (string command in cmds)
                            {
                                sw.WriteLine(command);
                            }
                            if (exitAtEnd) sw.WriteLine("exit"); // Ensure the cmd closes after executing the command
                        }
                    }

                    // Read output from cmd.exe
                    output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // Wait for the process to finish
                                           // Additional processing based on output can be added here
                                           //vLog("> " + $"Finished executing command: {command}");
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
            return output;
        }

        public static (string, string) ExecCommandLine(string cl)
        {
            try
            {
                // Create a new process start info
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                // Start the process
                using (Process process = Process.Start(processStartInfo))
                {
                    // Execute the Python version command
                    process.StandardInput.WriteLine(cl);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    process.WaitForExit();

                    // Read the output to get the Python version
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    return (output, error);
                }
            }
            catch (Exception ex)
            {
                return ("", "Error:" + ex.Message);
            }
        }
        public static string GetPythonVersion()
        {
            string output, error;
            (output, error) = ExecCommandLine("python --version");
            if (!string.IsNullOrEmpty(output))
            {
                int i = output.IndexOf("Python ");
                if (i < 0) return "<none>";
                string ss = output.Substring(i); i = ss.IndexOf('\r');
                if (i < 0) return "<none>";
                return ss.Substring(7, i - 7);
            }
            if (!string.IsNullOrEmpty(error))
            {
                return "Error: " + error;
            }
            return "";
        }

        /// <summary>
        /// Get Python version if there
        /// </summary>
        /// <returns> python version #.#.#; <none> or starting with "Error:"</returns>         
        public static string PythonVersion()
        {
            string rslt = "";
            // Command to check Python version
            string pythonCommand = "python --version";
            try
            {
                // Create a new process start info
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                // Start the process
                using (Process process = Process.Start(processStartInfo))
                {
                    // Execute the Python version command
                    process.StandardInput.WriteLine(pythonCommand);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();
                    process.WaitForExit();

                    // Read the output to get the Python version
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Print the Python version if available
                    if (!string.IsNullOrEmpty(output))
                    {
                        int i = output.IndexOf("Python ");
                        if (i < 0) return "<none>";
                        string ss = output.Substring(i); i = ss.IndexOf('\r');
                        if (i < 0) return "<none>";
                        rslt = ss.Substring(7, i - 7);
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        rslt = "Error: " + error;
                    }
                }
            }
            catch (Exception ex)
            {
                rslt = "Error:" + ex.Message;
            }
            return rslt;
        }

        public static string my_python_path = @"c:\Program Files (x86)\Microsoft Visual Studio\Shared\Python37_64\python.exe";
        // https://ernest-bonat.medium.com/using-c-to-run-python-scripts-with-machine-learning-models-a82cff74b027
        // working directory for the script file is executable direcory of the c# application
        // you can change PYTHONSTARTUP for permanently set working directory location
        public static string CallPython(string scriptFile, string args, out string stderr, string python_path = "")
        {            
            string pp = python_path.Equals("") ? my_python_path : python_path;
            if (!File.Exists(pp)) { throw new Exception(pp + " is not there"); }
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = pp;
            string ss = args.Equals("") ? start.Arguments = string.Format("\"{0}\"", scriptFile) : start.Arguments = string.Format("\"{0}\" \"{1}\"", scriptFile, args); 
            start.UseShellExecute = false;// Do not use OS shell
            start.CreateNoWindow = true; // We don't need new window
            start.RedirectStandardOutput = true;// Any output, generated by application will be redirected back
            start.RedirectStandardError = true; // Any error in standard output will be redirected back (for example exceptions)
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
                    string result = reader.ReadToEnd(); // Here is the result of StdOut(for example: print "test")
                    return result;
                }
            }
        }
        public static void Sleep(int milisec)
        {
            Thread.Sleep(milisec);
        }
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        public static bool GoToSleep()
        {
            return SetSuspendState(false, false, false);
        }
        /// <summary>
        /// The developer computer. It shouldn't matter, but still..
        /// </summary>
        /// <returns></returns>
        public static bool TheosComputer()
        {
            string cn = (string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"];
            return (cn == "DESKTOP-3UQQHSO") || (cn == "DESKTOP-M3GM68M") || (cn == "DESKTOP-U334RMA") || (cn == "THEOS") || (cn == "THEO-PC");
        }
        public static bool isSingleChannelMachine // only in Axel-hub
        {
            get
            {
                string cn = (string)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"];
                return ((cn == "DESKTOP-U9GFG8U") || (cn == "CHAMELEON-HP")); // Plexal or Chameleon machines
            }
        }
        public static bool isInVisualStudio
        {
            get
            {
                /* var myId = Process.GetCurrentProcess().Id;
                 var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {myId}";
                 var search = new ManagementObjectSearcher("root\\CIMV2", query);
                 var results = search.Get().GetEnumerator();
                 results.MoveNext();
                 var queryObj = results.Current;
                 var parentId = (uint)queryObj["ParentProcessId"];
                 var parent = Process.GetProcessById((int)parentId);
                 return parent.ProcessName.ToLower().EndsWith("devenv"); //"msvsmon";
                */
                
                // Check if the debugger is attached
                if (Debugger.IsAttached)
                {
                    // Check for Visual Studio process
                    var visualStudioProcessName = "devenv";
                    var visualStudioProcesses = Process.GetProcessesByName(visualStudioProcessName);

                    if (visualStudioProcesses.Any())
                    {
                        return true;
                    }

                    // Check for Visual Studio Code process
                    var vscodeProcessName = "Code";
                    var vscodeProcesses = Process.GetProcessesByName(vscodeProcessName);

                    if (vscodeProcesses.Any())
                    {
                        return true;
                    }
                }
                return false;               
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
            Random rnd = new Random(RandomnSeed);
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
        #region LOG
        public delegate void LogHandler(string txt, SolidColorBrush clr = null);

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
                  rangeOfText1.Text = Utils.RemoveLineEndings(txt) + "\r"; //Environment.NewLine;
                  SolidColorBrush clr1 = System.Windows.Media.Brushes.Black; // default
                  if (isNull(clr))
                  {
                      if (txt.StartsWith("Err")) clr1 = System.Windows.Media.Brushes.Red;
                      if (txt.StartsWith("War")) clr1 = System.Windows.Media.Brushes.Tomato;
                  }
                  else clr1 = clr;
                  rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, clr1);
                  richText.ScrollToEnd(); richText.UpdateLayout();
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
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    tbLog.AppendText(txt + "\r\n");
                    string text = tbLog.Text;
                    int maxLen = 10000;
                    if (text.Length > 2 * maxLen) tbLog.Text = text.Substring(maxLen);
                    tbLog.Focus();
                    tbLog.CaretIndex = tbLog.Text.Length;
                    tbLog.ScrollToEnd();
                }));
        }
        public static void log(TextBox tbLog, List<string> txt)
        {
            foreach (string tx in txt) log(tbLog, tx);
        }
        #endregion LOG

        public static Dictionary<string, string> dictObject2String(Dictionary<string, object> dv, string prec = "G5")
        {
            Dictionary<string, string> ds = new Dictionary<string, string>(); double rslt;
            foreach (var item in dv)
            {
                if (item.Value is int || item.Value is long) 
                    { ds.Add(item.Key, item.Value.ToString()); continue; }

                if (Double.TryParse(Convert.ToString(item.Value), out rslt)) ds.Add(item.Key, Convert.ToDouble(rslt).ToString(prec));
                else
                {
                    if (isNull(item.Value)) continue;
                    ds.Add(item.Key, item.Value.ToString());
                }  
            }                  
            return ds;
        }

        public static Dictionary<string, string> dictDouble2String(Dictionary<string, double> dv, string prec = "G5")
        {
            Dictionary<string, string> ds = new Dictionary<string, string>();
            foreach (var item in dv)
                ds.Add(item.Key, item.Value.ToString(prec));
            return ds;
        }

        public static void dict2ListBox(Dictionary<string, string> ds, ListBox lbox)
        {
            //lbox.Items.Clear();
            int cnt = lbox.Items.Count; 
            for (int i = 0; i<cnt; i++)
                if (lbox.Items[lbox.Items.Count - 1] is ListBoxItem) lbox.Items.RemoveAt(lbox.Items.Count - 1);
            foreach (var item in ds)
            {
                ListBoxItem lbi = new ListBoxItem();
                if (item.Value.Equals("")) lbi.Content = item.Key;
                else lbi.Content = string.Format("{0}: {1}", item.Key, item.Value);
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
            if (prefix.Equals("")) return DateTime.Now.ToString("yy-MM-dd_HH-mm-ss");
            else return DateTime.Now.ToString("yy-MM-dd_HH-mm-ss") + "_" + prefix;
        }
        // template (usually html) with fields $fct$ to be replaced by res depending of the dict.type 
        // if the type is List<string> -> multiline substitution
        public static List<string> CreateFromTemplate(List<string> templ, Dictionary<string, object> rep)
        {
            List<string> singleLine(string sl, Dictionary<string, object> rp)
            {
                List<string> rs = new List<string>(); string s = sl; bool found = false;
                foreach (var pair in rp)
                {
                    string brKey = "$" + pair.Key + "$";
                    string rp1 = "";
                    if (sl.IndexOf(brKey) > -1)
                    {
                        Type t = pair.Value.GetType();
                        if (Object.ReferenceEquals(t, typeof(List<string>))) // list must be alone in line and all caps
                            { rs.AddRange((List<string>)pair.Value); break; }
                        if (Object.ReferenceEquals(t, typeof(int))) { rp1 = ((int)pair.Value).ToString(); found = true; }
                        if (Object.ReferenceEquals(t, typeof(double))) { rp1 = ((double)pair.Value).ToString("G5"); found = true; }
                        if (Object.ReferenceEquals(t, typeof(string))) { rp1 = (string)pair.Value; found = true; }
                        if (found) s = s.Replace(brKey, rp1);
                        else TimedMessageBox("Error[739]: " + s);
                    }
                }
                if (found) rs.Add(s);
                return rs;
            }
            List<string> res = new List<string>();
            foreach (string ss in templ)
                if (ss.IndexOf("$") > -1) res.AddRange(singleLine(ss, rep));
                else res.Add(ss);
            return res;
        }
        public static bool IsInternetConnectionAvailable()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send("8.8.8.8", 2200); // Google public DNS, 1000ms timeout
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
        private static Dictionary<string,string> FakeBrowserHeaders()
        {
            Dictionary<string, string> webHeaders = new Dictionary<string, string>();
            webHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml");
            //client.Headers.Add("Accept-Encoding", "gzip, deflate");
            webHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            webHeaders.Add("Accept-Charset", "ISO-8859-1");
            return webHeaders;
        }
        public static string DownloadString(string address)
        {
            string ss = "";
            try
            {
                using (var client = new WebClient())
                {
                    foreach (var pr in FakeBrowserHeaders())
                        client.Headers.Add(pr.Key, pr.Value);
                    ss = client.DownloadString(address);
                }
            }
            catch (WebException we) { TimedMessageBox(we.Message); ss = ""; }
            return ss;
        }
        public static bool DownloadFile(string address, string fileName)
        {
            bool bb = true;
            try
            {
                using (var client = new WebClient())
                {
                    foreach (var pr in FakeBrowserHeaders())
                        client.Headers.Add(pr.Key, pr.Value);
                    client.DownloadFile(address, fileName);
                }
            }
            catch (WebException we) { TimedMessageBox(we.Message); bb = false; }
            return bb;
        }

        public static bool CheckFileExists(string url) 
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    // Create a HEAD request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url);

                    // Send the request synchronously
                    HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();

                    // Return true if the status code is 200 (OK)
                    return response.IsSuccessStatusCode;
                }
            }
            catch (HttpRequestException)
            {
                // Handle request exceptions (e.g., invalid URL, no network)
                return false;
            }
        }


        /* PROBLEMS ??? 
         * https://www.csharp-examples.net/download-files/#:~:text=The%20simply%20way%20how%20to,want%20to%20save%20the%20file
         * https://stackoverflow.com/questions/525364/how-to-download-a-file-from-a-website-in-c-sharp
         * https://jonathancrozier.com/blog/how-to-download-files-using-c-sharp */
        public static List<string> GetWebText(string textUrl) 
        {
            string text;
            try 
            { 
                using (var client = new WebClient())
                {
                    foreach (var pr in FakeBrowserHeaders())
                        client.Headers.Add(pr.Key, pr.Value);

                    text = client.DownloadString(textUrl);                   
                }
            }
            catch (WebException we) { TimedMessageBox(we.Message); return null; }
            string[] txtArr = text.Split('\r');
            List<string> ls = new List<string>(txtArr);                      
            return ls;
        }        
        public static string GetMD5Checksum(string filename) // https://makolyte.com/csharp-get-a-files-checksum-using-any-hashing-algorithm-md5-sha256/
        {
            if (!File.Exists(filename)) return "";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
        public static string AvoidOverwrite(string filename)
        {
            if (!File.Exists(filename)) return filename; // it's safe
            string ext = Path.GetExtension(filename);
            string fp = Path.ChangeExtension(filename, null);
            string fn; int k = 0;
            do
            {
                fn = fp + "-" + k.ToString();
                fn = Path.ChangeExtension(fn, ext);
                k++;
            } while (File.Exists(fn));            
            return fn;
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
            return rslt;
        }
        public static List<string> skimRem(List<string> text)
        {
            var ls = new List<string>();
            foreach (string ss in text)
                ls.Add(skimRem(ss));
            return ls;
        }
        public static List<string> listFlatTextBox(TextBox textBox, bool noComment)
        {
            string[] sa = textBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            List<string> ls = new List<string>(sa); List<string> lt;
            if (noComment) lt = skimRem(ls);
            else lt = new List<string>(ls);
            return lt;
        }
        public static string stringFlatTextBox(TextBox textBox, bool noComment)
        {
            string st = "";
            List<string> ls = listFlatTextBox(textBox, noComment);
            foreach (var line in ls)
            {
                string ss = line.Trim();
                if (ss.StartsWith("#") && !noComment) { st += ss + Environment.NewLine; continue; }
                st += ss + " ";
            }                        
            return st.Replace("  ", " ").Trim();
        }
        private static String WildCardToRegular(String value) // If you want to implement both "*" and "?"
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
        public static bool IsWildCardMatch(string text, string searchPattern, bool strict = false) // 
        {
            string sp = searchPattern;
            if (!strict)
            {
                if (sp.IndexOf('*').Equals(-1) && sp.IndexOf('?').Equals(-1)) sp = '*' + sp + '*';
            }
            return Regex.IsMatch(text, WildCardToRegular(sp), RegexOptions.IgnoreCase);
        }
        public static List<string> ReadMultilineTextFromClipboard(bool RemoveEmptyEntries = true)
        {
            List<string> lines = new List<string>();
            if (Clipboard.ContainsText())
            {
                string[] clipboardLines = Clipboard.GetText().Split(new[] { '\n', '\r' }, RemoveEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
                foreach (string ss in clipboardLines) lines.Add(ss.Trim());
            }
            return lines;
        }
        /// <summary>
        /// Read text file in List of string
        /// </summary>
        /// <param name="filename">The text file</param>
        /// <param name="skipRem">If to skip # and empty lines</param>
        /// <returns></returns>
        public static List<string> readList(string filename, bool skipRem)
        {
            if (!File.Exists(filename)) return null;
            List<string> ls = new List<string>();
            try
            {
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
            catch { return null; }
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
        public static Dictionary<string, List<string>> readStructList(List<string> ls, bool skipRem = false)
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
                    section = st.Substring(i + 1, j - i - 1); ls = new List<string>();
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
        public static void writeList(string filename, List<string> ls, bool skipIfEmpty = true)
        {
            if (isNull(ls)) return;
            if (skipIfEmpty)
            {
                if (ls.Count == 0) return;
                if (string.Join("", ls.ToArray()).Trim().Equals("")) return;
            }
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
            //List<string> ls = new List<string>();
            foreach (string line in File.ReadLines(filename))
            {
                string ss = skipRem ? skimRem(line) : line;
                if (ss.Equals("")) continue;
                string[] sb = ss.Split('=');
                if (sb.Length != 2) break;
                dict.Add(sb[0].Trim(), sb[1].Trim());
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
        public static bool dblEqualsZero(double d, double eps = 1e-12)
        {
            if (d.Equals(Double.NaN)) return false;
            return InRange(d, -eps, eps);
        }
        /// <summary>
        /// Check if Value is in range[MinValue..MaxValue] (double)
        /// no limits order required
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static bool InRange(double Value, double MinValue, double MaxValue, bool ordered = false)
        {
            if (ordered) return (MinValue <= Value) && (Value <= MaxValue);
            else
            {
                if (MinValue > MaxValue) return InRange(Value, MaxValue, MinValue, true);
                return InRange(Value, MinValue, MaxValue, true);
            }                    
        }
        /// <summary>
        /// Check if Value is in range[MinValue..MaxValue] (int)
        /// no limits order required
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="MinValue"></param>
        /// <param name="MaxValue"></param>
        public static bool InRange(int Value, int MinValue, int MaxValue, bool ordered = false)
        {
            if (ordered) return (MinValue <= Value) && (Value <= MaxValue);
            else
            {
                if (MinValue > MaxValue) return InRange(Value, MaxValue, MinValue, true);
                return InRange(Value,  MinValue,MaxValue, true);
            }
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
            return String.IsNullOrEmpty(value) ? defaultValue : value;
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
            var u1 = rand.NextDouble(); var u2 = rand.NextDouble();
            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                  Math.Sin(2.0 * Math.PI * u2);
            var rand_normal = mu + sigma * rand_std_normal;
            return rand_normal;
        }
        public static List<double> GaussSeries(int nData, double mean, double sigma)
        {
            List<double> ls = new List<double>();
            for (int i = 0; i < nData; i++)
            {
                ls.Add(Gauss01() * sigma + mean);
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
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex_code.StartsWith("#") ? hex_code : "#"+hex_code );
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
        public static bool ConfirmationMessageBox(string question)
        {
            return MessageBox.Show(question, "Confirmation", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes;
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
        public static string appFullPath = System.Reflection.Assembly.GetEntryAssembly().Location;
        public static string appName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
        public static bool localConfig = File.Exists(Path.ChangeExtension(System.Reflection.Assembly.GetEntryAssembly().Location, ".PDB"));

        public static string GetParrentDirectory(string path)
        {
            DirectoryInfo parentDir = Directory.GetParent(path);
            return parentDir.FullName;
        }
        public enum BaseLocation { oneUp, twoUp, appData, auto }
        public static BaseLocation baseLocation = BaseLocation.auto;
        public static string GetBaseLocation(BaseLocation bl)
        {
            switch (bl)
            {
                case BaseLocation.oneUp:
                    return Directory.GetParent(appFullPath).Parent.FullName;
                case BaseLocation.twoUp:
                    return Directory.GetParent(Directory.GetParent(appFullPath).Parent.FullName).FullName;
                case BaseLocation.appData:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
                default:
                    return "";
            }
        }
        public static string basePath
        {
            get
            {
                string requiredFolder = "Config"; // or maybe "bin"
                if (baseLocation == BaseLocation.auto)
                {
                    if (localConfig)
                    {
                        if (Directory.Exists(Path.Combine(GetBaseLocation(BaseLocation.oneUp), requiredFolder))) baseLocation = BaseLocation.oneUp;
                        if (Directory.Exists(Path.Combine(GetBaseLocation(BaseLocation.twoUp), requiredFolder))) baseLocation = BaseLocation.twoUp;
                    }
                    else baseLocation = BaseLocation.appData;
                }
                if (!Directory.Exists(Path.Combine(GetBaseLocation(baseLocation), requiredFolder)))
                    TimedMessageBox(Path.Combine(GetBaseLocation(baseLocation), requiredFolder) + " is expected but does not exist.", "Error", 3000);
                return GetBaseLocation(baseLocation);
            }
        }
        public static string configPath { get { return Path.Combine(basePath,"Config")+"\\"; } }
        public static bool extendedDataPath { get; set; } // defaults are: in AH - true / in MM2 - false
        public static string dataPath 
        { 
            get 
            { 
                string rslt = Path.Combine(basePath,"Data")+"\\";
                if (extendedDataPath)
                {                     
                    rslt += DateTime.Now.Month.ToString("D2")+"\\";
                    if (!Directory.Exists(rslt)) Directory.CreateDirectory(rslt);
                }
                return rslt;  
            } 
        }
        public static bool comparePaths(string path1, string path2)
        {
            if (path1.Equals("") || path2.Equals("")) return false;
            return Path.GetFullPath(path1).TrimEnd('\\').Equals(Path.GetFullPath(path2).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool newerVersion(string ver1, string ver2) // check if ver2 is later than ver1
        {
            string[] sa = ver1.Split('.'); string[] sb = ver2.Split('.');
            if (sa.Length == 4 && sb.Length == 4) return Convert.ToInt32(sa[3]) < Convert.ToInt32(sb[3]);
            if (sa.Length < 3 || sb.Length < 3) return false;
            if (Convert.ToInt32(sa[0]) < Convert.ToInt32(sb[0])) return true;
            if (Convert.ToInt32(sa[0]) > Convert.ToInt32(sb[0])) return false;
            if (Convert.ToInt32(sa[1]) < Convert.ToInt32(sb[1])) return true;
            if (Convert.ToInt32(sa[1]) > Convert.ToInt32(sb[1])) return false;
            if (Convert.ToInt32(sa[2]) < Convert.ToInt32(sb[2])) return true;
            if (Convert.ToInt32(sa[2]) > Convert.ToInt32(sb[2])) return false;
            return false;
        }

        public static int RandomnSeed
        {
            get 
            {                
                return  Convert.ToInt32(DateTime.Now.TimeOfDay.Ticks % int.MaxValue); 
            }
        }
        /// <summary>
        /// Random string (for testing purposes or random fileName)
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string randomString(int length, bool numOnly = false )
        {
            if (length < 1) return "";
            if (numOnly)
            {
                Random rnd = new Random(RandomnSeed);
                string rslt = "";
                for (int i = 0; i < length; i++)
                    rslt += rnd.Next(0, 9).ToString();
                return rslt;
            }
            else
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
        }
        public static bool validFileName(string fileName)
        {
            bool isValid = true;        
            if (string.IsNullOrEmpty(fileName) || Path.GetInvalidFileNameChars().Any(x => fileName.Contains(x)))
            {
                isValid = false;
            }
            // Check if the file name is too long
            if (fileName.Length > 260)
            {
                isValid = false;
            }
            return isValid;
        }
        public static string correctFileName(string fileName)
        {
            string correctedFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            // Truncate the file name if it is too long
            if (correctedFileName.Length > 260)
            {
                correctedFileName = correctedFileName.Substring(0, 260);
            }
            return correctedFileName;
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

                    if (reqFilename.Equals("")) LogFilename = Path.Combine(Utils.dataPath, Path.ChangeExtension(Utils.timeName(prefix), defaultExt));
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
                        LogFilename = LogFilename = Path.Combine(dir, Path.ChangeExtension(fn, defaultExt));
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
 

}