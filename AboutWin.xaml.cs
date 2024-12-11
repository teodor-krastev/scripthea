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
using System.Xml;
using System.Net;
using System.Xml.Linq;
using scripthea.options;
using UtilsNS;

namespace scripthea
{
    /// <summary>
    /// Interaction logic for AboutWin.xaml
    /// </summary>
    public partial class AboutWin : Window
    {
        public AboutWin()
        {
            InitializeComponent();
            Title = "About Scripthea version " + UtilsNS.Utils.getAppFileVersion;
        }
        Options opts;
        public bool closing = false;
        public void Init(ref Options _opts)
        {
            opts = _opts;
        }
        public void Check4Updates(int lastChecked) // the date of last check / -1 forced; returns the new check date; 
        {            
            if (!Utils.IsInternetConnectionAvailable()) // no i-net
            {
                if (lastChecked.Equals(-1)) Utils.TimedMessageBox("No internet connection");
                else
                {
                    lbMessage.Foreground = Brushes.Red; lbMessage.Content = "Problem with the internet connection"; 
                }                
                return; 
            }
            lbMessage.Foreground = Brushes.Green;            
            DateTime refDate = new DateTime(2023, 1, 1); // reference date
            TimeSpan timeSpan = DateTime.Today - refDate;  
            if ((timeSpan.Days - lastChecked) >= 14  || lastChecked.Equals(-1)) 
            {
                string msg; 
                bool bb = IsUpdateAvalaible(out msg); if (!bb && msg == "") return;
                lbMessage.Content = msg;
                if (bb) lbMessage.Foreground = Brushes.OrangeRed; 
                else lbMessage.Foreground = Brushes.Green;
                opts.general.LastUpdateCheck = timeSpan.Days; 
            }
            else
            {
                if (!opts.general.NewVersion.Equals(""))
                {
                    lbMessage.Foreground = Brushes.Tomato;
                    lbMessage.Content = "New release (" + opts.general.NewVersion + ") is available at Scripthea.com !"; 
                }
            }                        
        }
        public bool IsUpdateAvalaible(out string msg) // return a message to show
        {
            if (!Utils.CheckFileExists(@"https://scripthea.com/scripthea.xml"))
            {
                Utils.TimedMessageBox(@"Error[927]: pad file https://scripthea.com/scripthea.xml is not avalable!", "Error", 5000); msg = "";  return false;
            }
            string remVer = ""; string[] sl = Utils.getAppFileVersion.Split('.'); 
            string plus = "?b=" + ((sl.Length > 3) ? sl[3] : "");
            Dictionary<string, string> pad = readPAD(readXml(@"https://scripthea.com/scripthea.xml"+plus));
            bool ok = true;
            if (pad.ContainsKey("Program_Info.Program_Version")) remVer = pad["Program_Info.Program_Version"];
            else ok = false;
            ok &= pad.ContainsKey("Company_Info.Company_Name") && pad.ContainsKey("Program_Info.Program_Name");
            if (ok) ok = pad["Company_Info.Company_Name"].Equals("Teodor Krastev") && pad["Program_Info.Program_Name"].Equals("Scripthea");
            string[] sr = remVer.Split('.');
            if ((sr.Length != 3) || !ok) { msg = "Problem with accessing update info from Scripthea.com"; opts.general.NewVersion = ""; return false; }
            if (Utils.newerVersion(Utils.getAppFileVersion, remVer)) 
            {
                msg = "New release (" + remVer + ") is available at Scripthea.com !"; opts.general.NewVersion = remVer; return true;
            }
            msg = "Your version is up to date."; opts.general.NewVersion = ""; return false;
        }

        private void aboutWin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Hide();
        }
        private void tbSources_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.CallTheWeb(@"https://github.com/teodor-krastev/scripthea");
        }
        private void lbAuthor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.CallTheWeb(@"https://sicyon.com/survey/comment.html?sj=scripthea");
        }
        private void tbkWebsite_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Utils.CallTheWeb(@"https://scripthea.com");
        }
        public XmlDocument readXml(string url) 
        {
            string xmlContent = Utils.DownloadString(url);
            XDocument xDoc = XDocument.Parse(xmlContent);
            XmlReader reader = xDoc.CreateReader();
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(reader);
            return xmlDocument;
        }
        public Dictionary<string, string> readPAD(XmlDocument xmlDoc)
        {
            Dictionary<string, string> pad = new Dictionary<string, string>();

            // Access the root element
            XmlElement root = xmlDoc.DocumentElement;

            // Iterate through child elements
            foreach (XmlNode topNode in root.ChildNodes)
            {
                foreach (XmlNode node in topNode.ChildNodes)
                { 
                    // Do something with the node
                    //Console.WriteLine($"Element Name: {node.Name}");

                    if (node.Attributes != null)
                    {
                        string attr = "";
                        foreach (XmlAttribute attribute in node.Attributes)
                        {
                            // Do something with the attribute
                            //Console.WriteLine($"Attribute Name: {attribute.Name}, Value: {attribute.Value}");
                            attr += attribute.Name + "=" + attribute.Value + " ; ";
                        }
                        if (!attr.Equals("")) pad.Add(topNode.Name + "." + node.Name+":ATTR", attr);
                    }
                    // Access the node value (if any)
                    if (!string.IsNullOrEmpty(node.InnerText))
                    {
                        //Console.WriteLine($"Node Value: {node.InnerText}");
                        pad.Add(topNode.Name+"."+node.Name, node.InnerText);
                    }                
                }
            }            
            return pad;
        }
        private void AboutWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closing) return;
            // Cancel the window closing event
            e.Cancel = true;
            Hide();
        }
    }
}

/*




 */
