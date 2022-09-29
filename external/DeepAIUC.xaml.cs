using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
using UtilsNS;
using DeepAI;
using System.Drawing;
using System.Drawing.Imaging;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class DeepAIUC : UserControl, interfaceAPI
    {
        public DeepAIUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
        }
        public Dictionary<string, string> opts { get; set; }
        public void ShowAccess(string prompt)
        {
            lbStatus.Content = "Status";
            if (!opts.ContainsKey("folder")) opts["folder"] = Utils.basePath + "\\images\\";
            if (!Directory.Exists(opts["folder"])) opts["folder"] = Utils.basePath + "\\images\\";
            tbIn.Text = prompt;
        }
        public bool isEnabled { get; }
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename) // returns image filename or error message if failed
        {
            string DeepAI_key = File.Exists(Utils.configPath + "DeepAI.key") ? File.ReadAllText(Utils.configPath + "DeepAI.key").Trim() : "quickstart-QUdJIGlzIGNvbWluZy4uLi4K";            
            DeepAI_API api = new DeepAI_API(apiKey: DeepAI_key); string imgName = ""; filename = "";
            bool bb = true;
            try
            {
                StandardApiResponse resp = api.callStandardApi("text2img", new
                {
                    text = prompt,
                });
                imgName = resp.output_url;
            }
            catch (Exception e) { bb = false; filename = e.Message; }
            if (bb)
            {
                Bitmap bm = Utils.GetImage(imgName);
                filename  = Utils.timeName() + ".jpg";
                bm.Save(imageDepotFolder + filename, ImageFormat.Jpeg);
            }
            return bb;
        }
        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(opts["folder"])) throw new Exception("Folder missing");            
            lbStatus.Content = "Image request sent."; Utils.DoEvents();
            string imgName = "";
            if (GenerateImage(tbIn.Text, opts["folder"], out imgName))
            {
                lbStatus.Content = "Response received";
                if (!File.Exists(opts["folder"] + imgName)) throw new Exception("File's missing -> " + opts["folder"] + imgName);
                imgOut.Source = new BitmapImage(new Uri(opts["folder"] + imgName));
            }
            else lbStatus.Content = imgName;
        }
    }
}
