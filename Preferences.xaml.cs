using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UtilsNS;
using scripthea.viewer;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace scripthea
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
            public bool debug;
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
            public bool Maximazed;
            public int LogColWidth;
            public bool LogColWaveSplit;
        }
        public Composer composer;
        public class Composer
        {
            public int QueryRowHeight;
            public int QueryColWidth;
            public int ViewColWidth;
            // query single
            public bool SingleAuto;
            public bool OneLineCue;
            // query 
            public string ImageDepotFolder;        
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
        }
        public IDutilities iDutilities; // Image Depot utilities
        public class IDutilities
        {
            public bool MasterValidationAsk;
            public int MasterWidth;
            public bool MasterClearEntries;
            public int ImportWidth;
            public int ExportWidth;
        }
        public SMacro sMacro;
        public class SMacro
        {
            public string pythonLocation;
            public int FullWidth;
            public int CodeWidth;
            public int LogWidth;
            public bool pythonPanel;
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

            public delegate void Register2sMacroHandler(string moduleName, object moduleObject, List<Tuple<string, string>> help);
            public event Register2sMacroHandler OnRegister2sMacro;
            public void Register2sMacro(string moduleName, object moduleObject, List<Tuple<string, string>> help) 
            {
                if (OnRegister2sMacro != null) OnRegister2sMacro(moduleName, moduleObject, help);
            }
        }
    }

    /// <summary>
    /// Interaction logic, load & save for GeneralOptions genOptions
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public bool keepOpen = true;
        /// <summary>
        /// dialog box constructor; reads from file or creates new options object
        /// </summary>
        public PreferencesWindow()
        {
            InitializeComponent();         
        }
        Options opts;
        public void Init(ref Options _opts)
        {
            opts = _opts; tabControl.SelectedIndex = 0;
        } 
        public string configFilename = Path.Combine(Utils.configPath, "Scripthea.cfg");  
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt, "Warning", 3500);
        }
        public void opts2visuals()
        {
            if (!opts.general.NewVersion.Equals("")) lbNewVer.Content = "New version: " + opts.general.NewVersion;
            chkUpdates.IsChecked = opts.general.UpdateCheck;
            chkViewerRemoveImages.IsChecked = opts.viewer.RemoveImagesInIDF;
            chkClearEntriesImageDepot.IsChecked = opts.iDutilities.MasterClearEntries; ;
            chkValidationAsk.IsChecked = opts.iDutilities.MasterValidationAsk;
        }
        public void visuals2opts()
        {
            opts.general.UpdateCheck = chkUpdates.IsChecked.Value;
            opts.viewer.RemoveImagesInIDF = chkViewerRemoveImages.IsChecked.Value;
            opts.iDutilities.MasterClearEntries = chkClearEntriesImageDepot.IsChecked.Value; ImageDepotConvertor.ClearEntriesImageDepot = opts.iDutilities.MasterClearEntries;
            opts.iDutilities.MasterValidationAsk = chkValidationAsk.IsChecked.Value;
        }
        public void ShowWindow(int tabIdx)
        {
            //tabControl.SelectedIndex = Utils.EnsureRange(tabIdx, 0, 2) + 1;
            if (Utils.InRange(tabIdx, 0, 1)) tabControl.SelectedItem = tiGeneral;
            if (Utils.InRange(tabIdx, 2, 3)) tabControl.SelectedItem = tiIDutilities;
            opts2visuals();
            ShowDialog();
        }
        /// <summary>
        /// Accepting and saving the changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e) // visual to internal 
        {
            visuals2opts(); Hide();
        }

        /// <summary>
        /// Cancel without modifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
        private void wndSDOptions_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = keepOpen; Hide();
        }
    }
}
