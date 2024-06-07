using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
//using scripthea.viewer;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using UtilsNS;

namespace scripthea.options
{
    public class Options
    {
        public Options()
        {
            if (general == null) general = new General();
            if (layout == null) layout = new Layout();
            if (composer == null) composer = new Composer();
            if (viewer == null) viewer = new Viewer();
            if (iDutilities == null) iDutilities = new IDutilities();
            if (sMacro == null) sMacro = new SMacro();
            if (common == null) common = new Common();
        }
        public General general;
        public class General
        {
            public bool debug { get { return Utils.isInVisualStudio && Utils.localConfig; } }
            public bool UpdateCheck;  
            public int LastUpdateCheck;
            public string NewVersion;
            public string LastSDsetting;
            public bool AutoRefreshSDsetting;
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
            // query single
            public SingleAutoSet SingleAuto;
            public bool OneLineCue;
            // query
            public string WorkCuesFolder;
            public string ImageDepotFolder;
            public string StartupImageDepotFolder;
            public string API;
            // modifiers
            public string ModifPrefix;
            public bool AddEmptyModif;
            public bool ConfirmGoogling;
            public int ModifSample;
            public bool mSetsEnabled;
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
        }
        public IDutilities iDutilities; // Image Depot utilities
        public class IDutilities
        {
            public bool MasterValidationAsk;
            public int MasterWidth;
            public bool MasterClearEntries;
            public int ImportWidth;
            public int ExportWidth;
            // AddonGen
            public string AddonGenFolder;
        }
        public SMacro sMacro;
        public class SMacro
        {
            public bool pythonEnabled; // only the checkbox status
            public bool pythonValid; // actual validation
            public int locationType; // 0 - integrated env, 1 custom env, 2 - base location
            public string pyEnvLocation;
            public string pyBaseLocation;
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
            public bool pythonOn { get { return false; } }
            public delegate void Register2sMacroHandler(string moduleName, object moduleObject, List<Tuple<string, string>> help);
            public event Register2sMacroHandler OnRegister2sMacro;
            public void Register2sMacro(string moduleName, object moduleObject, List<Tuple<string, string>> help)
            {
                if (OnRegister2sMacro != null) OnRegister2sMacro(moduleName, moduleObject, help);
            }
        }
    }

}
