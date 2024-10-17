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
using System.Diagnostics;
using Path = System.IO.Path;
using Microsoft.WindowsAPICodePack.Dialogs;
using scripthea.viewer;
using scripthea.options;
using UtilsNS;

namespace scripthea
{
 
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

        private List<string> history;
        
        public void opts2visuals()
        {
            if (!opts.general.NewVersion.Equals("")) { lbNewVer.Content = "New available release: " + opts.general.NewVersion; lbNewVer.Foreground = Brushes.Firebrick; }
            chkUpdates.IsChecked = opts.general.UpdateCheck;
            cbStartupImageDepotFolder.Text = opts.composer.StartupImageDepotFolder == null ? "": opts.composer.StartupImageDepotFolder;
            if (history != null)
            {
                foreach(string ss in history)
                {
                    cbStartupImageDepotFolder.Items.Add(new ComboBoxItem() { Content = ss });
                }
            }
            chkViewerRemoveImages.IsChecked = opts.viewer.RemoveImagesInIDF;
            tbCuesFolder.Text = opts.composer.WorkCuesFolder;
            chkClearEntriesImageDepot.IsChecked = opts.iDutilities.MasterClearEntries; ;
            chkValidationAsk.IsChecked = opts.iDutilities.MasterValidationAsk;
            //python
            if (!opts.common.pythonOn) tiPython.Visibility = Visibility.Collapsed;
            if (opts.sMacro.pythonIntegrated) rbIntegrated.IsChecked = true;
            else rbCustom.IsChecked = true;
            ValidatePythonLocation(false);
            chkPythonEnabled.IsEnabled = opts.sMacro.pythonValid;
            if (chkPythonEnabled.IsEnabled) chkPythonEnabled.IsChecked = opts.sMacro.pythonEnabled;
            else chkPythonEnabled.IsChecked = false;           
            tbValidLog.Text = ""; tbPyCustomLocation.Text = opts.sMacro.pyCustomLocation;   
            if (opts.sMacro.pythonValid) { vLog(""); vLog("Your Python location has been validated."); gbPyLoc.BorderBrush = Brushes.SeaGreen; }
            else
            {
                if (opts.sMacro.pythonIntegrated) { vLog("Broken Scripthea installation: <python-embed> folder is missing or damaged."); vLog(""); }
                else
                {
                    vLog("No installation of Python is found!"); vLog("");
                    vLog("If you don't have Python (embedded or standard) installed, you may go to https://www.python.org/downloads/windows/ then download and run Windows embeddable package of Python of your choosing."); vLog("");
                    vLog("After that browse to your python installation and validate the location.");
                }
                gbPyLoc.BorderBrush = Brushes.Red; return;
            }                     
        }
        private void vLog(string text)
        {
            Utils.log(tbValidLog, text); tbValidLog.UpdateLayout();
        }
        public void visuals2opts()
        {
            opts.general.UpdateCheck = chkUpdates.IsChecked.Value;
            opts.composer.StartupImageDepotFolder = cbStartupImageDepotFolder.Text;
            opts.viewer.RemoveImagesInIDF = chkViewerRemoveImages.IsChecked.Value;
            opts.iDutilities.MasterClearEntries = chkClearEntriesImageDepot.IsChecked.Value; ImageDepotConvertor.ClearEntriesImageDepot = opts.iDutilities.MasterClearEntries;
            opts.iDutilities.MasterValidationAsk = chkValidationAsk.IsChecked.Value;
            //python
            opts.sMacro.pythonEnabled = chkPythonEnabled.IsChecked.Value && chkPythonEnabled.IsEnabled;
            opts.sMacro.pythonIntegrated = rbIntegrated.IsChecked.Value;
            opts.sMacro.pyCustomLocation = tbPyCustomLocation.Text;
        }
        public void ShowWindow(int tabIdx, List<string> _history)
        {            
            history = new List<string>(_history);
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
        
        public bool ValidatePythonLocation(bool local)
        {
            if (local) visuals2opts();
            opts.sMacro.ChangePythonLocation(); // try to setup python from python.NET
            return opts.sMacro.pythonValid;
        }
        private void btnValidatePython_Click(object sender, RoutedEventArgs e)
        {
            if (ValidatePythonLocation(true)) { vLog("Success: Python location has been validated."); gbPyLoc.BorderBrush = Brushes.SeaGreen; }
            else { vLog("Problem: your Python location <" + opts.sMacro.pyEmbedLocation + "> is not valid."); gbPyLoc.BorderBrush = Brushes.Red; }
        }
        private void btnPyBaseLocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            string folder = Path.GetDirectoryName(opts.sMacro.pyEmbedLocation);
            if (Directory.Exists(folder)) dialog.InitialDirectory = folder;            
            dialog.Title = "Select a Python dll file (e.g. python310.dll)";
            dialog.IsFolderPicker = false; dialog.Multiselect = false;
            dialog.DefaultExtension = ".dll";
            dialog.Filters.Add(new CommonFileDialogFilter("Dll file", "dll"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string fn = dialog.FileName;
                if (!Path.GetExtension(fn).Equals(".dll", StringComparison.InvariantCultureIgnoreCase)) return;
                opts.sMacro.pyCustomLocation = fn; tbPyCustomLocation.Text = fn;
            }
            Activate();
            Topmost = true;  // important
            Topmost = false; // important
            Focus();         // important
        }

        private string runCommand(string workingDirectory, List<string> cmds, bool exitAtEnd = true)
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
                    if (output != "") vLog("> " + output);
                    string error = process.StandardError.ReadToEnd();
                    if (error != "") { vLog("Error: " + output); }

                    process.WaitForExit(); // Wait for the process to finish
                                           // Additional processing based on output can be added here
                                           //vLog("> " + $"Finished executing command: {command}");
                }
            }
            catch (Exception ex)
            {
                return "Error:" + ex.Message;
            }
            return output;
        }       
       

        /*public string CheckCuesFolder(string cuesFolder)
        {
        if (Directory.Exists(cuesFolder))
        {
        string cuesMap = Path.Combine(cuesFolder, "cue_pools.map");
        if (!File.Exists(cuesMap)) File.WriteAllText(cuesMap, "[{},{}]"); // write an empty "cue_pools.map"
        return cuesFolder;
        }
        string defaultCuesFolder = Path.Combine(Utils.basePath, "cues");
        if (!Directory.Exists(defaultCuesFolder)) throw new Exception("Error: Corrupt installation. Missing <" + defaultCuesFolder + "> folder");
        else Utils.TimedMessageBox("Warning: folder <" + cuesFolder + "> does not exist. \n\n Switch to default cues folder.");
        return defaultCuesFolder;
        }
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
        CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        dialog.InitialDirectory = Path.Combine(Utils.configPath, "Cues");
        dialog.Title = "Select a folder for the *.cues files";
        dialog.IsFolderPicker = true;
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
        opts.composer.WorkCuesFolder = CheckCuesFolder(dialog.FileName); tbCuesFolder.Text = opts.composer.WorkCuesFolder;
        if (chkAsDefault.IsChecked.Value) opts.composer.StartCuesFolder = opts.composer.WorkCuesFolder;
        }            
        }

        public string installPyEnv(string pyEnvParent) // python and pip must be installed already 
        {
            List<string> cmds = new List<string>() { "python -m venv stenv", @"stenv\Scripts\activate", "pip install pythonnet", @"stenv\Scripts\desactivate" };
                // Create a new process start info
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = pyEnvParent,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                // Start the process
                string msg = "Installation of Python virt.env."; vLog(msg); //Log(msg, Brushes.Blue);  
                string output, error;
                using (Process process = Process.Start(processStartInfo))
                {
                    foreach (string cmd in cmds)
                    {
                        vLog("> " + cmd);
                        // Execute the Python version command
                        process.StandardInput.WriteLine(cmd);
                        process.StandardInput.Flush();
                        //process.StandardInput.Close();
                        //process.WaitForExit(5000);
                        Utils.Sleep(3000);

                        // Read the output to get the Python version
                        output = process.StandardOutput.ReadToEnd();
                        if (output != "") vLog(": " + output);
                    }
                }
            string ePath = Path.Combine(pyEnvParent, "stenv");            
            if (validatePyEnv(ePath)) return ePath;
            else return "Error: creation of python virt.env. has failed!";
        }
        
         
         */
    }
}
