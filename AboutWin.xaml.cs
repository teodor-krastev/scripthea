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
using System.Windows.Shapes;
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
            Title = "About Scripthea  version " + UtilsNS.Utils.getAppFileVersion;
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
            Utils.CallTheWeb(@"http://scripthea.com");
        }

       
    }
}
