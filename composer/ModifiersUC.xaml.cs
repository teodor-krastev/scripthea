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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UtilsNS;

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for ModifiersUC.xaml
    /// </summary>
    public partial class ModifiersUC : UserControl
    {
        public List<ModifListUC> modifLists;
        public List<ModifItemUC> modifItems
        {
            get 
            {
                List<ModifItemUC> mdf = new List<ModifItemUC>();
                if (!Utils.isNull(modifLists))
                {
                    foreach (ModifListUC mdl in modifLists)               
                        mdf.AddRange(mdl.modifList);                
                }
                return mdf;
            }
        }           
        public ModifiersUC()
        {
            InitializeComponent();
        }
        public event RoutedEventHandler OnChange;
        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (OnChange != null) OnChange(this, e);
        }
        public void Init()
        {
            separator = "; ";
            modifLists = new List<ModifListUC>();
            var files = new List<string>(Directory.GetFiles(Utils.configPath, "*.mdfr"));
            foreach (string fn in files)
            {
                ModifListUC cmu = new ModifListUC(fn); 
                cmu.OnChange += new RoutedEventHandler(Change);
                modifLists.Add(cmu);
                stackModifiers.Children.Add(cmu);
            }
            SetSingleScanMode(true);
        }
        public string separator { get; set; }
        public string Composite() // for single mode
        {
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Scannable))
                ss += separator + sc + " ";
            return FixItemsAsString()+ss;
        }

        public List<string> ModifItemsByType(ModifStatus ms)
        {
            List<string> ls = new List<string>();
            foreach (ModifListUC sm in modifLists)
            {
                if (!sm.enabled) continue;
                foreach (ModifItemUC mdf in sm.modifList)
                    if (mdf.modifStatus.Equals(ms)) ls.Add(mdf.Text);
            }
            return ls;
        }
        public string FixItemsAsString()
        {
            string ss = "";
            foreach (string sc in ModifItemsByType(ModifStatus.Fixed))
                ss += separator + sc + " ";
            return ss;
        }
        public void SetSingleScanMode(bool singleMode)
        {
            foreach (ModifItemUC mdf in modifItems)           
                mdf.singleMode = singleMode;            
        }
    }
}
