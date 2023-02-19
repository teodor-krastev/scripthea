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
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace scripthea
{
    public class Options
    {
        // general 
        public bool debug;
        // layout 
        public int Left;
        public int Top;
        public int Height;
        public int Width;
        public int LogColWidth;
        public bool LogColWaveSplit;
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
        // viewer
        public bool Autorefresh;
        public int ThumbZoom;
        public bool ThumbCue;
        public bool ThumbFilename;
    }

    public class Preferences
    {
        public Preferences()
        {

        }
        public string test;
        public void save(string configFilename)
        {
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(configFilename, json);
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
            if (File.Exists(configFilename))
            {
                string fileJson = File.ReadAllText(configFilename);
                prefs = JsonConvert.DeserializeObject<Preferences>(fileJson);
            }
            else prefs = new Preferences();
            prefs2visuals(); 
        }
        public string configFilename = Path.Combine(Utils.configPath, "Scripthea.cfg");  
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public Preferences prefs;
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt, "Warning", 3500);
        }
        public void prefs2visuals()
        {
            
        }
        public void visuals2prefs()
        {
            
        }
        /// <summary>
        /// Accepting and saving the changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e) // visual to internal 
        {
            prefs.save(configFilename); Hide();
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
