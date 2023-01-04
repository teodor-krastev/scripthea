using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Text;
using System.Threading.Tasks;
using UtilsNS;
using System.Windows;

namespace scripthea.master
{
    public interface iFocusControl // attached to all UC with folder needs
    {
        UserControl parrent { get; }
        GroupBox groupFolder { get; }
        TextBox textFolder { get; }        
    }
    public class FocusControl
    {
        public FocusControl()
        {
            iFoci = new List<iFocusControl>(); 
        }
        private List<iFocusControl> iFoci; 
        public void Register(iFocusControl iFocus)
        {
            if (iFocus == null) throw new Exception("null focusable component");
            iFoci.Add(iFocus);
            iFocus.parrent.GotFocus += new RoutedEventHandler(GotTheFocus);
        }
        private void Refocus(iFocusControl ifc, bool focus)
        {
            if (focus) ifc.groupFolder.BorderBrush = Utils.ToSolidColorBrush("#FF0A16E9");
            else ifc.groupFolder.BorderBrush = Utils.ToSolidColorBrush("#FFD5DFE5");
            ifc.groupFolder.BorderThickness = new Thickness(1.5);
        }
        public iFocusControl ifc; // the one with focus
        private void GotTheFocus(object sender, EventArgs e)
        {
            ifc = sender as iFocusControl;        
            foreach (iFocusControl foc in iFoci)
                Refocus(foc, foc.Equals(ifc));
        }
    }
}
