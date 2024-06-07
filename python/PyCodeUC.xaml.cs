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
using Python.Runtime;
using Path = System.IO.Path;
using scripthea.options;
using UtilsNS;

namespace scripthea.python
{    
    public class St // default output
    {
        protected RichTextBox rtb;
        public St(ref RichTextBox _rtb)
        {
            rtb = _rtb; 
        }
        public void print(dynamic txt)
        {
            if (!(rtb is RichTextBox)) return;
            if (txt == null) Utils.log(rtb, "> NULL", Brushes.Red);
            else Utils.log(rtb,txt.ToString(),Brushes.Black);
        }
        public event Utils.LogHandler OnLog;
        public void log(dynamic txt)
        {
            if (OnLog != null) OnLog(txt.ToString());
            else Utils.TimedMessageBox(txt.ToString());
        }
        public string Input(string info, string defaultText = "") 
        { 
            return new InputBox(info, defaultText, "").ShowDialog();
        } 
        public List<Tuple<string, string>> help 
        { 
            get 
            {
                List<Tuple<string, string>> ls = new List<Tuple<string, string>>();
                ls.Add(new Tuple<string, string>( "print(dynamic text)", "replacement of python print to output into sMacro log panel. Mostly aimed for intermedia info and debuging." ));
                ls.Add(new Tuple<string, string>("log(dynamic text)", "output into the main log panel (left). Mostly aimed for the user "));
                ls.Add(new Tuple<string, string>("Input(string info, string defaultText)", "similar to standart input function in python. It will open dialog box for the user to enter text."));
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

        private void setup_py_venv_002(string PythonDLL, string pathToVirtualEnv)
        {
            Runtime.PythonDLL = PythonDLL;
            // be sure not to overwrite your existing "PATH" environmental variable.
            var path = Environment.GetEnvironmentVariable("PATH").TrimEnd(';');
            path = string.IsNullOrEmpty(path) ? pathToVirtualEnv : path + ";" + pathToVirtualEnv;
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PATH", pathToVirtualEnv, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONHOME", pathToVirtualEnv, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", $"{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib", EnvironmentVariableTarget.Process);
        }

        private void setup_py_venv_003(string PythonDLL, string pathToVirtualEnv)
        {
            Runtime.PythonDLL = ""; //PythonDLL @"C:\Python38\python38.dll";
            //var pathToVirtualEnv = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\venv"));
            Console.WriteLine(pathToVirtualEnv);

            Console.WriteLine(Runtime.PythonDLL);
            Console.WriteLine(PythonEngine.Platform);
            Console.WriteLine(PythonEngine.MinSupportedVersion);
            Console.WriteLine(PythonEngine.MaxSupportedVersion);
            Console.WriteLine(PythonEngine.BuildInfo);
            Console.WriteLine(PythonEngine.PythonPath);

            string additional = $"{pathToVirtualEnv};{pathToVirtualEnv}\\Lib\\site-packages;{pathToVirtualEnv}\\Lib";
            //PythonEngine.PythonPath = /*PythonEngine.PythonPath + ";" + */additional;
            Console.WriteLine(PythonEngine.PythonPath);

            //PythonEngine.Initialize();
            //PythonEngine.BeginAllowThreads();
        }

        public void OnChangePythonLocation() // (re)locate and validate python
        {

            opts.sMacro.pythonValid = false;
            if (!opts.common.pythonOn) return;
            
            if (PythonEngine.IsInitialized) PythonEngine.Shutdown();
            switch (opts.sMacro.locationType)
            {
                case 0: // ENV
                case 1:
                    string envPath = opts.sMacro.pyEnvLocation; //@"D:\Scripthea\stenv";
                    if (!Directory.Exists(envPath))
                        { Log("Error: directory <" + envPath + "> does not exist!"); return; }
                    //setup_py_venv_002(opts.sMacro.pyBaseLocation, envPath);
                    //Log("> "+Utils.RunCommand(Path.Combine(envPath, "Scripts"), "activate", false)); 
                    //Environment.SetEnvironmentVariable("PYTHONHOME", pyLoc);
                    //Environment.SetEnvironmentVariable("PYTHONPATH", Path.Combine(pyLoc, "Lib","site-packages") + ";" + Path.Combine(pyLoc, "Lib"));
                    //Environment.SetEnvironmentVariable("PYTHONHOME", envPath);
                    //Environment.SetEnvironmentVariable("PYTHONPATH", $@"{envPath}\Lib\site-packages;{envPath}\Lib");
                    //Environment.SetEnvironmentVariable("PATH", $@"{envPath}\Scripts;{envPath}\bin;{pathVar}");


                    //Environment.SetEnvironmentVariable("PYTHONPATH", "D:\\Scripthea\\stenv\\Lib;D:\\Scripthea\\stenv\\Lib\\site-packages");

                    //Environment.SetEnvironmentVariable("PYTHONHOME", "");//
                    string pathVar = Environment.GetEnvironmentVariable("PATH");
                    Environment.SetEnvironmentVariable("PYTHONHOME", "", EnvironmentVariableTarget.Process); // Unset
                    Environment.SetEnvironmentVariable("PYTHONPATH", $@"{envPath}\Scripts;{envPath}\Lib\site-packages;{envPath}\Lib", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PATH", $@"{envPath}\Scripts;{envPath}\bin;{pathVar}", EnvironmentVariableTarget.Process);


                    //Environment.SetEnvironmentVariable("PATH", $@"{virtualEnvPath}\Scripts;{Environment.GetEnvironmentVariable("PATH")}");


                    PythonEngine.Initialize();
                    //PythonEngine.PythonHome = pyLoc;
                    Log("HOME="+PythonEngine.PythonHome); // = opts.sMacro.pyEnvLocation;
                    Log("PATH="+PythonEngine.PythonPath); // Path.Combine(opts.sMacro.pyEnvLocation,"Lib")+ ";" + PythonEngine.PythonPath;
                    break;
                case 2: // base
                    if (!File.Exists(opts.sMacro.pyBaseLocation))
                        { Log("Error: file <" + opts.sMacro.pyBaseLocation + "> does not exist!"); return; }
                    try
                    {
                        Runtime.PythonDLL = opts.sMacro.pyBaseLocation;
                        PythonEngine.Initialize();
                    }
                    catch (PythonException e) { Log("Error[111]: " + e.Message); return; }
                    break;
                default:
                    Log("Error: internal #409a"); return;
            }
            opts.sMacro.pythonValid = Test1() && Test2(); 
            IsEnabled = opts.sMacro.pythonValid;
        }
        public void Init(ref Options _opts) // string pyPath = @"C:\Software\Python\Python310\python310.dll";
        {
            opts = _opts;
            if (!opts.common.pythonOn) return;
            colCodeWidth = opts.sMacro.CodeWidth; colLogWidth = opts.sMacro.LogWidth; colHelpWidth = opts.sMacro.HelpWidth;

            OnChangePythonLocation();
            opts.sMacro.OnChangePythonLocation -= OnChangePythonLocation; opts.sMacro.OnChangePythonLocation += OnChangePythonLocation;

            scripted = new Dictionary<string,object>();
            st = new St(ref tbLog);
            Register("st", st, st.help);
            if (File.Exists(Path.Combine(avalEdit.sMacroFolder, "_last.py")))
                Code = File.ReadAllText(Path.Combine(avalEdit.sMacroFolder, "_last.py"));
        }
        public void Finish()
        {
            if (Code.Trim() != "") File.WriteAllText(Path.Combine(avalEdit.sMacroFolder, "_last.py"), Code);
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
            if (!chkLog.IsChecked.Value) return;
            if (details && !chkDetails.IsChecked.Value) return;
            Utils.log(tbLog,txt+'\n', Brushes.Navy);
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLog.Document.Blocks.Clear();
        }
        public string Code { get { return avalEdit.Text;  }  set { avalEdit.Text = value; } }
        public PyObject Execute(string code)
        {
            if (!IsEnabled) return null;
            // create a Python scope
            using (dynamic scope = Py.CreateScope()) //
            {
                try
                {
                    foreach (var pair in scripted)
                    {
                        scope.Set(pair.Key, pair.Value.ToPython());
                    }                   
                    scope.Set("result", "Done.");
                }
                catch (PythonException e) { Log("Error[2]: " + e.Message); return null; }
                try
                {
                    scope.Exec(code);
                }
                catch (PythonException e) { Log("Error[3]: " + e.Message); return null; }
                return scope.result;
            }
        }
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            //tbLog.Tag = 0;
            //chkCancelRequest.IsChecked = false;
            PyObject po = Execute(Code); if (po != null) Log(po.ToString());
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
    }
}
