using System;
using System.Collections.Generic;
using System.Linq;
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

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for CraiyonWebUC.xaml
    /// </summary>
    public partial class CraiyonWebUC : UserControl, interfaceAPI
    {
        public CraiyonWebUC()
        {
            InitializeComponent();
            opts = new Dictionary<string, string>();
        }

        public Dictionary<string, string> opts { get; set; }

        public void Init(string prompt) {  }
        public void Finish() { }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        public bool isDocked { get { return false; } }
        public UserControl userControl { get { return this as UserControl; } }
        public bool isEnabled { get { return true; } }
        public bool GenerateImage(string prompt, string imageDepotFolder, out string filename)
        {
            Utils.CallTheWeb("https://craiyon.com/?prompt=" + prompt.Trim().Replace(' ', '+'));
            filename = "";
            return true;
        }

    }
}
