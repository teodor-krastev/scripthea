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

namespace scripthea.external
{
    public class SDoptions
    {
        public SDoptions()
        {

        }
        
        public void save(string configFilename)
        {

        }
    }
    /// <summary>
    /// Interaction logic, load & save for GeneralOptions genOptions
    /// </summary>
    public partial class SDoptionsWindow : Window
    {
        public bool keepOpen = true;
        /// <summary>
        /// dialog box constructor; reads from file or creates new options object
        /// </summary>
        public SDoptionsWindow()
        {
            InitializeComponent();         
            if (File.Exists(configFilename))
            {
                string fileJson = File.ReadAllText(configFilename);
                opts = JsonConvert.DeserializeObject<SDoptions>(fileJson);
            }
            else opts = new SDoptions();
            
        }
        public string configFilename = Path.Combine(Utils.configPath, "StableDiffusion.cfg");  
        /// <summary>
        /// the point of the dialog, readable everywhere
        /// </summary>
        public SDoptions opts;

        /// <summary>
        /// Accepting and saving the changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e) // visual to internal 
        {
            opts.save(configFilename);
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
