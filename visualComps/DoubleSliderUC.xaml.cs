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

namespace scripthea.visualComps
{
    /// <summary>
    /// Interaction logic for NumericSliderUC.xaml
    /// </summary>
    public partial class DoubleSliderUC : UserControl
    {
        public DoubleSliderUC()
        {
            InitializeComponent();
            dblBox.OnValueChanged += new RoutedEventHandler(numBox_ValueChanged);
        }
        public void Init(string title, double initValue, double minValue = 0, double maxValue = 100, double incValue = 1)
        {
            lbTitle.Content = title;
            dblBox.Minimum = minValue; slider.Minimum = minValue;
            dblBox.Maximum = maxValue; slider.Maximum = maxValue;
            dblBox.Interval = incValue; 
            Value = initValue; 
        }
        bool changing = false;
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (changing) return;
            changing = true;
            dblBox.Value = slider.Value; ValueChanged(this, dblBox.Value);
            changing = false;
        }
        private void numBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (changing) return;
            changing = true;
            slider.Value = dblBox.Value; ValueChanged(this, (double)dblBox.Value);
            changing = false;
        }
        public double Value 
        {
            get { return dblBox.Value; }
            set
            {
                changing = true;
                dblBox.Value = value; slider.Value = value; 
                changing = false;
            }
        }
        public delegate void ValueChangedHandler(object sender, double value);
        public event ValueChangedHandler OnValueChanged;
        protected void ValueChanged(object sender, double value)
        {
            if (OnValueChanged != null) OnValueChanged(sender,value);
        }
    }
}
