using System;
using System.Collections.Generic;
using System.IO;
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
using System.Diagnostics;
using System.Threading;
using Python.Runtime;
using Path = System.IO.Path;
using scripthea.options;
using scripthea.composer;
using UtilsNS;

namespace scripthea.python
{    
    public class St // default output
    {
        protected RichTextBox rtb; protected CheckBox details;
        public CancellationTokenSource cts;
        public St(ref RichTextBox _rtb, ref CheckBox _details)
        {
            rtb = _rtb; details = _details;
            cts = new CancellationTokenSource();
        }
        public void resetCancellation()
        {
            if (IsCancellationRequested) { cts?.Dispose(); cts = new CancellationTokenSource(); }               
        }
        public void print(dynamic txt, string color)
        {
            if (!(rtb is RichTextBox) || !(details is CheckBox)) return;
            if (!details.IsChecked.Value) return;
            if (txt == null) Utils.log(rtb, "> NULL", Brushes.Red);
            else
            {
                SolidColorBrush clr = (SolidColorBrush)new BrushConverter().ConvertFromString((string)color);
                if (clr == null) { Utils.log(rtb, "Error: wrong color", Brushes.Red); clr = Brushes.Black; }
                Utils.log(rtb,txt.ToString(),clr); 
            }                                 
        }
        
        public string Input(string info, string defaultText = "") 
        { 
            return new InputBox(info, defaultText, "").ShowDialog();
        } 
        public bool IsCancellationRequested 
        { 
            get
            {
                if (cts == null) return false;
                return cts.IsCancellationRequested;
            } 
        }
        public List<Tuple<string, string>> help 
        { 
            get 
            {
                List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
                ls.Add(new Tuple<string, string>( "print(dynamic text, string color)", "replacement of python print to output into sMacro log panel. Mostly aimed for intermedia info and debuging. color is string of the name or hexadecimal of the color" ));
                ls.Add(new Tuple<string, string>("log(dynamic text)", "output into the main log panel (left). Mostly aimed for the user "));
                ls.Add(new Tuple<string, string>("Input(string info, string defaultText)", "similar to standart input function in python. It will open dialog box for the user to enter text."));
                ls.Add(new Tuple<string, string>("IsCancellationRequested", "Check the value regularly in your macro and if true call sys.exit(<code>) to interupt the execution. the number <code> is printed out."));
                return ls;
            } 
        }
    }
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class PythonUC : UserControl
    {
        public PythonUC()
        {
            InitializeComponent();
            IsEnabled = false;
        }
        public Options opts;
        public St st; 
        public Dictionary<string,object> scripted; // register scripted components <name,component> HERE! 
        public double colCodeWidth { get { return colCode.Width.Value; } set { colCode.Width = new GridLength(value); } }
        public double colLogWidth { get { return colLog.Width.Value; } set { colLog.Width = new GridLength(value); } }
        public double colHelpWidth { get { return colHelp.Width.Value; } set { colHelp.Width = new GridLength(value); } }
        private const string backupMacro = "backupMacro.py";
        public void OnChangePythonLocation() // (re)locate and validate python
        {
            opts.sMacro.pythonValid = false;
            if (!opts.common.pythonOn) return;
            
            if (PythonEngine.IsInitialized) PythonEngine.Shutdown();
            if (!File.Exists(opts.sMacro.pyEmbedLocation))
                { inLog("Error: file <" + opts.sMacro.pyEmbedLocation + "> does not exist!", Brushes.Red); return; }
            try
            {
                Runtime.PythonDLL = opts.sMacro.pyEmbedLocation;
                PythonEngine.Initialize();
            }
            catch (PythonException e) { inLog("Error[111]: " + e.Message, Brushes.Red); return; }
            opts.sMacro.pythonValid = Test1(); // && Test2(); 
            IsEnabled = opts.sMacro.pythonValid;
            lbInfo.Content = (opts.sMacro.pythonIntegrated ? "Integrated" : "Custom") + " python version (" + (opts.sMacro.pythonValid ? "validated":"not validated") +")";
        }
        public void Init(ref Options _opts) 
        {
            opts = _opts;
            if (!opts.common.pythonOn) return;
            colCodeWidth = opts.sMacro.CodeWidth; colLogWidth = opts.sMacro.LogWidth; colHelpWidth = opts.sMacro.HelpWidth;
            avalEdit.DefaultDirectory = Path.Combine(Utils.basePath, "sMacro");
            if (!avalEdit.Open(opts.sMacro.pyLastFilename))
            {
                string last = Path.Combine(avalEdit.DefaultDirectory, backupMacro); // retrieve backup macro
                if (!avalEdit.Open(last)) Code = "";
            }
            OnChangePythonLocation();
            opts.sMacro.OnChangePythonLocation -= OnChangePythonLocation; opts.sMacro.OnChangePythonLocation += OnChangePythonLocation;

            scripted = new Dictionary<string,object>();
            st = new St(ref tbLog, ref chkDetails);
            Register("st", st, st.help);
        }
        public void Finish()
        {
            PythonEngine.Shutdown();
            if (Code.Trim() == "") return;
            if (avalEdit.Save(avalEdit.currentFileName)) opts.sMacro.pyLastFilename = avalEdit.currentFileName;
            else opts.sMacro.pyLastFilename = "";
            string fn = Path.Combine(avalEdit.DefaultDirectory, backupMacro);
            if (!fn.Equals(opts.sMacro.pyLastFilename,StringComparison.InvariantCultureIgnoreCase))
                File.WriteAllText(fn, Code); // backup macro in sMacrop folder
        }
        public bool Register(string moduleName, object moduleObject, List<Tuple<string,string>> help) // method help -> <syntax,description> 
        {
            helpTree.SetModuleHelp(moduleName, help);
            if (!opts.common.pythonOn || moduleName.Equals("") || moduleObject == null) return false;
            scripted.Add(moduleName, moduleObject);
            return true;
        }
        public void Log(string txt, bool details = true)
        {
            if (!chkPrint.IsChecked.Value) return;
            if (details && !chkDetails.IsChecked.Value) return;
            Utils.log(tbLog, txt, Brushes.Black);
        }
        protected void inLog(string txt, SolidColorBrush clr)
        {           
            Utils.log(tbLog, txt, clr); 
        }
        protected void printOut(string txt, SolidColorBrush clr)
        {
            if (chkPrint.IsChecked.Value) Utils.log(tbLog, txt, clr); 
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLog.Document.Blocks.Clear();
        }
        public string Code { get { return avalEdit.Text;  }  set { avalEdit.Text = value; } }
        public PyObject Execute(string code)
        {
            if (!IsEnabled) { inLog("Error[0]: python is disabled (see preferences)", Brushes.Red); return null; }
            // prepare
            st?.resetCancellation();
            // create a Python scope
            using (dynamic scope = Py.CreateScope()) 
            {
                try
                {                    
                    foreach (var pair in scripted) // scripthea objects
                        scope.Set(pair.Key, pair.Value.ToPython());
                    // redirect output
                    dynamic sys = Py.Import("sys");                 
                    var stdoutWriter = new StringWriter();
                    var stderrWriter = new StringWriter();
                    
                    var pyStdOut = new PyStdOut(); pyStdOut.OnLog += printOut;
                    sys.stdout = pyStdOut;
                    var pyStdErr = new PyStdOut() { colorBrush = Brushes.Red }; pyStdErr.OnLog += printOut;
                    sys.stdout = pyStdOut;
                    sys.stderr = pyStdErr;

                    scope.Set("result", "OK"); // the conditioning is fine
                }
                catch (PythonException e) { inLog("Error[2]: " + e.Message, Brushes.Red); return null; }
                try
                {
                    scope.Exec(code);
                }
                catch (PythonException e)
                {
                    if (Utils.isNumeric(e.Message)) inLog("Exit code: " + e.Message, Brushes.Blue);
                    else { inLog("Error[3]: " + e.Message, Brushes.Red); return null; }
                }
                return scope.result;
            }
        }
        private bool btnPressed 
        { 
            get { return btnRun.Content.Equals("Cancel");}
            set
            {
                if (value) { btnRun.Content = "Cancel"; btnRun.Background = Utils.ToSolidColorBrush("#FFFED17F"); }
                else { btnRun.Content = "R U N"; btnRun.Background = Utils.ToSolidColorBrush("#FFEBFCE5"); }
            }
        }
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (btnPressed) { st?.cts?.Cancel(); btnRun.IsEnabled = false; inLog("Cancellation requested", Brushes.Blue); return; }
            else
            {
                if (opts != null)
                    if (!opts.composer.QueryStatus.Equals(Status.Idle)) { inLog("Error[5784]: the composer is busy.", Brushes.Red); return; }
            }
            btnPressed = true;
            PyObject po = Execute(Code); 
            btnRun.IsEnabled = true; btnPressed = false; inLog("-=-=-=- end of macro -=-=-=-", Brushes.Green);
        }
        public bool Test1()
        {
            using (Py.GIL()) // acquire the Python GIL (Global Interpreter Lock)
            {
                dynamic py = Py.CreateScope();
                py.x = 10;
                py.y = 20;

                py.Exec("result = x + y");
                int result = py.result;
                return result.Equals(30); // should print 30
            }
        }
        public bool Test2()
        {
            using (Py.GIL())
            {
                dynamic numpy = Py.Import("numpy");
                var array = numpy.array(new int[] { 1, 2, 3, 4, 5 });
                int result = Convert.ToInt32(numpy.sum(array).ToString());
                return result.Equals(15); // should print 15               
            }
        }
        public void Test3(string code)
        {
            
        }       
        public class PyStdOut : IDisposable
        {
            private readonly System.IO.StringWriter buffer = new System.IO.StringWriter();

            public event Utils.LogHandler OnLog;
            protected void Log(string txt, SolidColorBrush clr = null)
            {
                if (OnLog != null) OnLog(txt, clr);
            }
            public SolidColorBrush colorBrush = Brushes.Navy;
            public void write(string message)
            {
                buffer.Write(message);
                if (message.Trim() != string.Empty) Log(message, colorBrush);
            }

            public void flush()
            {
                // Optionally implement if needed to mimic Python's flush functionality
                buffer.Flush();
            }

            public string Read()
            {
                return buffer.ToString();
            }

            /*public PyObject ToPython()
            {
                var pyStdOutClass = PythonEngine.ModuleFromString("pyStdOutModule", @"
                    class PyStdOut:
                        def __init__(self, net_object):
                            self.net_object = net_object

                        def write(self, message):
                            self.net_object.write(message)

                        def flush(self):
                            self.net_object.flush()
                    ").GetAttr("PyStdOut");

                return pyStdOutClass.Invoke(new PyObject[] { this.ToPython() });
            }*/

            public void Dispose()
            {
                buffer.Dispose();
            }
        }

        
    }
}
