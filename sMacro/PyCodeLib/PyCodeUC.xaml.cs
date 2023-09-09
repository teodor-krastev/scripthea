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
using Python.Runtime;
using Path = System.IO.Path;
using UtilsNS;

namespace PyCodeLib
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
            Utils.log(rtb,txt.ToString(),Brushes.Black);
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
    public partial class PyCodeUC : UserControl
    {
        public PyCodeUC()
        {
            InitializeComponent();
            IsEnabled = false;
        }
        public St st; 
        public Dictionary<string,object> scripted; // register scripted components <name,component> HERE! 
        public double colCodeWidth { get { return colCode.Width.Value; } set { colCode.Width = new GridLength(value); } }
        public double colLogWidth { get { return colLog.Width.Value; } set { colLog.Width = new GridLength(value); } }
        
        public bool Init(string pyPath) // @"C:\Software\Python\Python310\python310.dll";
        {
            if (!File.Exists(pyPath)) { Log("Error: python path <"+pyPath+"> not found."); IsEnabled = false; return false; }
            //Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pyPath);
            try
            {
                Runtime.PythonDLL = pyPath;
                PythonEngine.Initialize();
            }
            catch (PythonException e) { Log("Error[1]: " + e.Message); return false; }
            IsEnabled = Test1() && Test2(); if (!IsEnabled) return false;
            scripted = new Dictionary<string,object>();
            st = new St(ref tbLog);
            Register("st", st, st.help);
            if (File.Exists(Path.Combine(avalEdit.sMacroFolder, "_last.py")))
                Code = File.ReadAllText(Path.Combine(avalEdit.sMacroFolder, "_last.py"));
            return IsEnabled;
        }
        public void Finish()
        {
            File.WriteAllText(Path.Combine(avalEdit.sMacroFolder, "_last.py"), Code);
        }
        public bool Register(string moduleName, object moduleObject, List<Tuple<string,string>> help) // method help -> <syntax,description> 
        {
            helpTree.SetModuleHelp(moduleName, help);
            if (moduleName.Equals("") || moduleObject == null) return false;
            scripted.Add(moduleName, moduleObject);
            return true;
        }
        public void Log(string txt, bool details = true)
        {
            if (!chkLog.IsChecked.Value) return;
            if (details && !chkDetails.IsChecked.Value) return;
            Utils.log(tbLog,txt, Brushes.Navy);
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
