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
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
            else Utils.TimedMessageBox(txt, "Info", 3500);
        }
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

            ValidatePythonInstall();
            chkPythonEnabled.IsEnabled = opts.sMacro.pythonValid;
            if (chkPythonEnabled.IsEnabled) chkPythonEnabled.IsChecked = opts.sMacro.pythonEnabled;
            else chkPythonEnabled.IsChecked = false;
            string pyVer = Utils.GetPythonVersion(); lbPyVer.Content = "Your base version of python is "+pyVer; lbPyVer.Foreground = Brushes.Navy;
            tcPythonLocation.IsEnabled = !pyVer.Equals("<none>"); tbValidLog.Text = "";
            if (!tcPythonLocation.IsEnabled)
            {
                vLog("No installation of Python is found!");
                vLog("You may go to https://www.python.org/downloads/ then download and run the last stable installation of Python.");
                vLog("After that reopen this dialog window.");
                lbPyVer.Foreground = Brushes.Red; return;
            } 
            switch (opts.sMacro.locationType)
            {
                case 0: tcPythonLocation.SelectedIndex = 0; rbIntegrated.IsChecked = true;
                    opts.sMacro.pyEnvLocation = Path.Combine(Utils.basePath, "stenv");
                    tbPyEnvLocation.Text = opts.sMacro.pyEnvLocation;                   
                    break;
                case 1: tcPythonLocation.SelectedIndex = 0; rbUserDef.IsChecked = true;
                    tbPyEnvLocation.Text = opts.sMacro.pyEnvLocation;
                    break;
                case 2: tcPythonLocation.SelectedIndex = 1;
                    tbPyBaseLocation.Text = opts.sMacro.pyBaseLocation;
                    break;
            }
            btnCreateLocEnv.IsEnabled = !validatePyEnv(Path.Combine(Utils.basePath, "stenv"), false);
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
            if (tcPythonLocation.SelectedIndex == 0 && rbIntegrated.IsChecked.Value) opts.sMacro.locationType = 0;
            if (tcPythonLocation.SelectedIndex == 0 && rbUserDef.IsChecked.Value) opts.sMacro.locationType = 1;
            if (tcPythonLocation.SelectedIndex == 1 ) opts.sMacro.locationType = 2;
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
        
        public void ValidatePythonInstall()
        {
            visuals2opts();
            opts.sMacro.ChangePythonLocation(); // try to setup python from python.NET               
        }
        private void btnValidatePython_Click(object sender, RoutedEventArgs e)
        {
            ValidatePythonInstall();
            if (opts.sMacro.pythonValid) vLog("Success: Python location has been validated");
            else vLog("Problem with Python location");
        }
        private void btnPyEnvLocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            
            dialog.Title = "Select a Python virtual environment location";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string fn = dialog.FileName;
                opts.sMacro.pyEnvLocation = fn; tbPyEnvLocation.Text = fn;
            }
        }
        private void btnPyBaseLocation_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            string pyHome =  Environment.GetEnvironmentVariable("PYTHONHOME");
            if (Directory.Exists(pyHome)) dialog.InitialDirectory = pyHome;
            else
            {
                string output, error;
                (output, error) =  Utils.ExecCommandLine("python - c \"import sys; print(sys.executable)\"");
                if (File.Exists(output)) dialog.InitialDirectory = Path.GetDirectoryName(output);
            }
            dialog.Title = "Select a Python dll file (e.g. python310.dll) at base location";
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string fn = dialog.FileName;
                if (!Path.GetExtension(fn).Equals(".dll", StringComparison.InvariantCultureIgnoreCase)) return;
                opts.sMacro.pyBaseLocation = fn; tbPyBaseLocation.Text = fn;
            }
        }
        public bool validatePyEnv(string pyEnvPath, bool showError = true)
        {
            void inLog(string txt) { if (showError) vLog(txt); }
            if (!Directory.Exists(pyEnvPath)) { inLog("Error: directory <" + pyEnvPath + "> does not exist!"); return false; }
            string fn = Path.Combine(pyEnvPath, "Scripts", "python.exe");
            if (!File.Exists(fn)) { inLog("Error: file <" + fn + "> does not exist!"); return false; }
            fn = Path.Combine(pyEnvPath, "Scripts", "activate.bat");
            if (!File.Exists(fn)) { inLog("Error: file <" + fn + "> does not exist!"); return false; }

            return true;
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

        public string installPyEnv2(string pyEnvParent) // python and pip must be installed already 
        {
            List<string> cmds = new List<string>() { "python -m venv stenv" };
            runCommand(pyEnvParent, cmds);
            cmds = new List<string>() { @"stenv\Scripts\activate", "pip install pythonnet", @"stenv\Scripts\desactivate" };
            runCommand(pyEnvParent, cmds);

            string ePath = Path.Combine(pyEnvParent, "stenv");
            if (validatePyEnv(ePath)) return ePath;
            else return "Error: creation of python virt.env. has failed!";
        }
        private void btnCreateLocEnv_Click(object sender, RoutedEventArgs e)
        {
            string ePath = installPyEnv2(Utils.basePath);
            btnCreateLocEnv.IsEnabled = !validatePyEnv(ePath, true);
            if (btnCreateLocEnv.IsEnabled) vLog("Problem creating python virt.env.");
            else vLog("Python virt.env. created in " + ePath);
            if (opts.sMacro.locationType == 0)
            { opts.sMacro.pythonValid = btnCreateLocEnv.IsEnabled; opts.sMacro.pyEnvLocation = ePath; }
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
