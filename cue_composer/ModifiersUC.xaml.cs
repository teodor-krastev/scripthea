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

namespace scripthea
{
    /// <summary>
    /// Interaction logic for ModifiersUC.xaml
    /// </summary>
    public partial class ModifiersUC : UserControl
    {
        public List<Category> modifs; public List<CatModifUC> catModifs;
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
            modifs = new List<Category>(); catModifs = new List<CatModifUC>();            
            List<string> txtFile = new List<string>(File.ReadAllLines(Utils.configPath+"modifiers.txt"));
            List<string> modiFile = new List<string>();
            foreach (string ss in txtFile)
            {
                string st = ss.Trim();
                if (!st.Equals("")) modiFile.Add(st);
            }
            int i = 0; Category cat = null;
            while (i < modiFile.Count)
            {
                string ss = modiFile[i].Trim();
                if (ss.Equals("")) continue;
                if (ss[0].Equals('['))
                {
                    cat = new Category();                     
                    modifs.Add(cat);
                    char[] charsToTrim = { '[', ']' };
                    cat.title = ss.Trim(charsToTrim);
                    cat.enabled = false;
                    cat.content = new List<Tuple<string, string, bool>>();  
                    i++; continue;                 
                }
                if (!Utils.isNull(cat))
                {
                    string[] sa = ss.Split('=');
                    string s0 = ""; string s1 = "";
                    switch (sa.Length)
                    {
                        case 0: continue;
                        case 1: s0 = sa[0].Trim(); 
                            break;
                        case 2: s0 = sa[0].Trim(); s1 = sa[1].Trim();
                            break;
                    }
                    cat.content.Add(new Tuple<string, string, bool>(s0, s1, false));
                }                    
                i++;
            }
            foreach (Category mdf in modifs)
            {
                CatModifUC cmu = new CatModifUC(mdf); 
                cmu.OnChange += new RoutedEventHandler(Change);
                catModifs.Add(cmu);
                stackModifiers.Children.Add(cmu);
            }
        }
        public string separator { get; set; }
        public string Composite()
        {
            string ss = "";
            foreach (string sc in ScanItems())
                ss += separator + sc + " ";
            return ss;
        }

        public List<string> ScanItems()
        {
            List<string> ls = new List<string>();
            foreach (var sm in catModifs)
            {
                if (!sm.ctg.enabled) continue;
                foreach (var mdf in sm.ctg.content)
                    if (mdf.Item3) ls.Add(mdf.Item1);
            }
            return ls;
        }
    }
}
