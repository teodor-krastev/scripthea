using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using scripthea.composer;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Media;
using UtilsNS;

namespace scripthea.options
{
    public enum TripleReply { yes, ask, no }
    public enum InstallStages { unknown, first, second, regular }
    public class Options
    {
        public delegate bool SemanticHandler();
        public event Utils.LogHandler OnLog;
        public void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt, "Informaion", 3000);
        }
        public Options()
        {
            if (general is null) general = new General();
            if (layout is null) layout = new Layout();
            if (composer is null) composer = new Composer();
            if (llm is null) llm = new LLM();
            if (style is null) style= new StyleCreator();
            if (viewer is null) viewer = new Viewer();
            if (iDutilities is null) iDutilities = new IDutilities();
            if (sMacro is null) sMacro = new SMacro();
            if (common is null) common = new Common();
        }
        public General general;
        public class General
        {
            public bool UpdateCheck;  
            public int LastUpdateCheck;
            public string NewVersion;
            public string LastSDsetting;
            public bool AutoRefreshSDsetting;
            public string ComfyTemplate; // in config dir
            [JsonIgnore]
            public bool debug { get { return Utils.isInVisualStudio && Utils.localConfig; } }
            [JsonIgnore]
            public bool AppTerminating = false;
            [JsonIgnore]
            private InstallStages _installStage = InstallStages.unknown;
            [JsonIgnore]
            public InstallStages installStage 
            { 
                get
                {    
                    if (_installStage != InstallStages.unknown) return _installStage; // to be executed once per session
                    string f = Path.Combine(Utils.configPath, "first.install");
                    string s = Path.Combine(Utils.configPath, "second.install");
                    if (File.Exists(f) && !File.Exists(s)) { File.Move(f, s); _installStage = InstallStages.first; return _installStage; }
                    if (File.Exists(f) && File.Exists(s)) { File.Delete(f); _installStage = InstallStages.second; return _installStage;  }
                    if (!File.Exists(f) && File.Exists(s)) { _installStage = InstallStages.regular; return _installStage; }
                    return _installStage; 
                }             
            }
        }
        public Layout layout;
        public class Layout
        {
            public int Left;
            public int Top;
            public int Height;
            public int Width;
            public bool Maximized;
            public int LogColWidth;
            public bool LogColWaveSplit;
        }
        public enum SingleAutoSet { both, cue, modif, none }
        public Composer composer;
        public class Composer
        {
            public int QueryRowHeight;
            public int QueryColWidth;
            public int ViewColWidth;            
            public bool ShowCueMeta;
            [JsonIgnore]
            public Status QueryStatus; 
            // query single
            public SingleAutoSet SingleAuto;
            public bool OneLineCue;
            // query
            public string WorkCuesFolder; // the last active one
            public string ImageDepotFolder;
            public string StartupImageDepotFolder;
            // Fashioning
            public int FashionMode;
            // API
            public string API; // from combo-box items
            [JsonIgnore]
            public bool A1111 { get { return API.StartsWith("SD-A1111"); } }
            [JsonIgnore]
            public bool Comfy { get { return API.StartsWith("SD-Comfy"); } }
            public int SessionSpan; // in min
            public int TotalImageCount;
            public int TotalRatingCount;
            // modifiers
            public string ModifPrefix;
            public bool AddEmptyModif;
            public bool ConfirmGoogling;
            public int ModifSample;
            public bool mSetsEnabled;
            // semantic            
            public event SemanticHandler OnSemantic;           
            public bool SemanticActive()
            {
                if (OnSemantic != null) return OnSemantic();
                else return false;
            }
        }
        public LLM llm;
        public class LLM
        {
            public int SelectedPretext;
            public int SelectedQuestion;

            public bool ShowBoth; 
            public bool AutoAsk;
            public bool AllowAskingWhileGeneration;

            public string LMSlocation;
            public string LMSmodel;
            public string LMScontext;
            public double LMStemperature;
            public int LMSmax_tokens;
            [JsonIgnore]
            public scripthea.preview.LMstudioUC LMstudio;
        }
        public StyleCreator style;
        public class StyleCreator
        {
            public string StyleQuery;
            public string StyleMQuery;
            public double LMStemperature;
            public int LMSmax_tokens;

            public double col_0;
            public double col_1;
            public double row_1;
            public double row_2;
        }
        public Viewer viewer;
        public class Viewer
        {
            public bool Autorefresh;
            public int ThumbZoom;
            public bool ThumbCue;
            public bool ThumbFilename;
            public bool RemoveImagesInIDF;
            public int PicViewPromptH;
            public int PicViewMetaW;
            public bool BnWrate;
            [JsonIgnore]
            public string emptyText { get => "<empty>"; }
        }
        public IDutilities iDutilities; // Image Depot utilities
        public class IDutilities
        {
            public bool MasterValidationAsk;
            public int MasterWidth;
            public bool MasterClearEntries;
            public int ImportWidth;
            public int ExportWidth;
            [JsonIgnore]
            public bool IDFlocked;
            // AddonGen
            public string AddonGenFolder;
        }
        public SMacro sMacro;
        public class SMacro
        {
            public bool pythonOn { get { return true; } } // main python switch (for debug mostly)
            public bool pythonEnabled; // a wish for python to be on
            public bool pythonValid; // actual validation
            public bool pythonIntegrated;
            public string pyCustomLocation;
            public string pyLastFilename;

            [JsonIgnore] // !!! change the python version if NEW INTEGRATED VERSION IS INSTALLED
            public string pyEmbedLocation { get { return pythonIntegrated ? Path.Combine(Utils.basePath, "python-embed", "python312.dll") : pyCustomLocation; } } 
            public event Utils.SimpleEventHandler OnChangePythonLocation; // the result is back in pythonValid
            public void ChangePythonLocation()
            {
                OnChangePythonLocation?.Invoke();
            }
            public int CodeWidth;
            public int LogWidth;
            public int HelpWidth;
        }
        [JsonIgnore]
        public Common common; // similar to broadcast in ControlAPI, fire an event every time prop changes 
        public class Common
        {
            public delegate void CommonChangeHandler(Common common);
            public event CommonChangeHandler OnCommonChange;
            protected void Change() // only for radioMode
            {
                if (OnCommonChange != null) OnCommonChange(this);
            }
            private bool _wBool;
            public bool wBool { get { return _wBool; } set { _wBool = wBool; Change(); } }
            private int _wInt;
            public int wInt { get { return _wInt; } set { _wInt = wInt; Change(); } }
            // python
            public delegate void Register2sMacroHandler(string moduleName, object moduleObject, List<Tuple<string, string>> help);
            public event Register2sMacroHandler OnRegister2sMacro;
            public void Register2sMacro(string moduleName, object moduleObject, List<Tuple<string, string>> help)
            {
                if (OnRegister2sMacro != null) OnRegister2sMacro(moduleName, moduleObject, help);
            }
        }
        public class Mediator // design pattern for communication between different corners of an application
        {
            private static readonly Dictionary<string, Action<object>> _actions = new Dictionary<string, Action<object>>();
            public static void Register(string token, Action<object> callback)
            {
                if (!_actions.ContainsKey(token))
                {
                    _actions[token] = callback;
                }
                else
                {
                    _actions[token] += callback;
                }
            }
            public static void Unregister(string token, Action<object> callback)
            {
                if (_actions.ContainsKey(token))
                {
                    _actions[token] -= callback;
                }
            }
            public static void Send(string token, object args = null)
            {
                if (_actions.ContainsKey(token))
                {
                    _actions[token](args);
                }
            }
        }
        // Usage case
        // Mediator.Register("MyMessage", obj => MessageBox.Show("Message received: " + obj));
        // Mediator.Send("MyMessage", "Hello World!");
    }
}
