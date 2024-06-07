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
using scripthea.options;
using UtilsNS;

namespace scripthea.composer
{
    public enum ModifStatus
    {
        Off, Fixed, Scannable
    }
    /// <summary>
    /// Interaction logic for ModifItemUC.xaml
    /// </summary>
    public partial class ModifItemUC : UserControl
    {
        private Options opts;
        public ModifItemUC(ref Options _opts)
        {
            InitializeComponent(); modifStatus = ModifStatus.Off;
            opts = _opts;
        }
        public event RoutedEventHandler OnChange;
        protected void Change(object sender, RoutedEventArgs e)
        {
            if (OnChange != null) OnChange(sender, e);
        }
        public event Utils.LogHandler OnLog;
        protected void Log(string txt, SolidColorBrush clr = null)
        {
            if (OnLog != null) OnLog(txt, clr);
        }
        private ModifStatus _modifStatus;
        public ModifStatus modifStatus
        {
            get
            {
                return _modifStatus;
            } 
            set
            {
                _modifStatus = value;
                if (singleMode)
                {
                    if (value.Equals(ModifStatus.Off)) tbCheck.Text = "□";
                    else tbCheck.Text = "X"; 
                }
                else
                {
                    switch (value)
                    {
                        case ModifStatus.Off: tbCheck.Text = "□";                    
                            break;
                        case ModifStatus.Scannable: tbCheck.Text = "S"; 
                            break;
                        case ModifStatus.Fixed: tbCheck.Text = "F"; 
                            break;
                    }
                }
                switch (tbCheck.Text)
                {
                    case "□":
                    case "X":
                        gridCheck.Background = Brushes.Transparent; tbCheck.Foreground = Brushes.Black;
                        break;
                    case "S":
                        gridCheck.Background = Brushes.Coral; tbCheck.Foreground = Brushes.White;
                        break;
                    case "F":
                        gridCheck.Background = Brushes.CornflowerBlue; tbCheck.Foreground = Brushes.White;
                        break;
                }
                if (value == ModifStatus.Off)
                {
                    tbCheck.FontSize = 20; tbCheck.Margin = new Thickness(0, 0, 0, 3); gridMain.Background = null;
                }
                else
                {
                    tbCheck.FontSize = 12; tbCheck.Margin = new Thickness(0); gridMain.Background = Brushes.MintCream;
                }
                tbCheck.ToolTip = Convert.ToString(value);
                Change(this, null);
            }
        }
        private bool _marked = false;
        public bool marked 
        { 
            get { return _marked; }
            set { _marked = value; tbContent.Background = value ? Brushes.LemonChiffon : null; } 
        }
        public string Text
        {
            get { return tbContent.Text; }
            set { tbContent.Text = value; }
        }
        private bool _singleMode = false;
        public bool singleMode
        {
            get { return _singleMode; }
            set { _singleMode = value; modifStatus = _modifStatus; }
        }
        private void tbCheck_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwitchState(e.LeftButton.Equals(MouseButtonState.Pressed));
        }
        public void SwitchState(bool up)
        {           
            if (singleMode)
            {
                if (modifStatus.Equals(ModifStatus.Off)) modifStatus = ModifStatus.Fixed;
                else modifStatus = ModifStatus.Off;
            }
            else
            {
                int k = up ? 1 : 2;
                int index = (int)modifStatus;
                index = (index + k) % 3;
                modifStatus = (ModifStatus)index;
            }
        }
        private void tbContent_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            string input = opts.composer.ConfirmGoogling ? new InputBox("Ask Google about", tbContent.Text, "").ShowDialog() : tbContent.Text;
            if (input.Equals("")) return;
            Utils.AskTheWeb(input, true);
        }
    }
}
