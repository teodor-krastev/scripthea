using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UtilsNS;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace scripthea.composer
{
    
    public class ECwebRecord
    {
        public string name; // a must.   
        public string urlSource; // dataset card at huggingface or elsewhere (not now, maybe later)
        public int rowNumber; // must (total if multi-file)
        public string usage; // what's that
        public string version;
        public string comment; // what's new
        public string zipName;
        public int zipSize;
        public string zipMD5; // of the zip
        public static ECwebRecord OpenECwebRecord(string json)
        {
            return JsonConvert.DeserializeObject<ECwebRecord>(json);
        }               
    }

    /// <summary>
    /// Interaction logic for ExtCollMng.xaml
    /// </summary>
    public partial class ExtCollMng : Window
    {
        public ExtCollMng()
        {
            InitializeComponent();
        }
        protected Uri url = new Uri("https://scripthea.com/ext-collections/DESCRIPTION.jsonl");
        protected List<ECwebRecord> webRecords = new List<ECwebRecord>();
        public void ShowWindow() //string rootCuesFolder
        {
            string[] jsonl = new string[0]; 
            using (var client = new WebClient())
            {
                try
                {
                    jsonl = client.DownloadString(url).Split('\n');                       
                    // Parse with Newtonsoft.Json if you have a matching data class
                    // var myObject = JsonConvert.DeserializeObject<MyDataClass>(jsonString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}"); 
                    Utils.TimedMessageBox(@"Error[927]: collections description file "+url.AbsoluteUri+" is not found. ", "Error", 7000); this.Close(); return; 
                }
            }
            foreach (string rec in jsonl) { webRecords.Add(ECwebRecord.OpenECwebRecord(rec)); }
            ShowDialog();             
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var myDictionary = new Dictionary<string, string>
            {
                { "Apple", "Red" },
                { "Banana", "Yellow" },
                { "Grape", "Purple" },
                { "Orange", "Orange" }
            };

            // Convert dictionary to a list of KeyValuePair<string, string>
            // This allows the DataGrid to generate Key and Value columns automatically
            dGrid.ItemsSource = myDictionary.ToList();
        }

       
    }
}
