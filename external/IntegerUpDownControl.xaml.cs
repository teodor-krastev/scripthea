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

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for IntegerUpDownControl.xaml
    /// </summary>
    public partial class IntegerBox : UserControl
    {
        public int Maximum = int.MaxValue; public int Minimum = int.MinValue; public int Interval = 1; 

        public IntegerBox()
        {
            InitializeComponent();
            TextBoxValue.Text = "0";
        }
        public event RoutedEventHandler OnValueChanged;
        protected void ValueChanged(object sender, RoutedEventArgs e)
        {
            if (OnValueChanged != null) OnValueChanged(sender, e);
        }

        private void ResetText(TextBox tb)
        {
            tb.Text = 0 < Minimum ? Minimum.ToString() : "0";
            tb.SelectAll();
        }
        bool lockText = false;
        private void value_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            //if (!_numMatch.IsMatch(tb.Text)) ResetText(tb);
            lockText = true;
            try
            {
                int val;
                if (!int.TryParse(tb.Text, out val))
                {
                    TextBoxValue.Foreground = Brushes.Red; return;
                }
                if ((val < Minimum) || (val > Maximum))
                {
                    TextBoxValue.Foreground = Brushes.Red; return;
                }
                TextBoxValue.Foreground = Brushes.Black;
                _Value = val;
                ValueChanged(this, new RoutedEventArgs());
            }
            finally { lockText = false; }
        }

        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += Interval;
                //ValueChange(this, new RoutedEventArgs());
            }
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= Interval;
                //ValueChanged(this, new RoutedEventArgs());
            }
        }

        /// <summary>The Value property represents the TextBoxValue of the control.</summary>
        /// <returns>The current TextBoxValue of the control</returns>      
        private int _Value = 0;
        public int Value
        {
            get
            {
                return _Value;
            }
            set
            {
                if (lockText) return;
                _Value = value;
                if (value < Minimum) _Value = Minimum;
                if (value > Maximum) _Value = Maximum;
                TextBoxValue.Text = _Value.ToString();

                ValueChanged(this, new RoutedEventArgs());
            }
        }
        /// <summary>
        /// Minimum value of the numeric up down conrol.
        /// </summary>


        /// <summary>
        /// Checking for Up and Down events and updating the value accordingly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void value_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsDown && e.Key == Key.Up && Value < Maximum)
            {
                Value += Interval;
                e.Handled = true;
            }
            if (e.IsDown && e.Key == Key.Down && Value > Minimum)
            {
                Value -= Interval;
                e.Handled = true;
            }
            if (e.IsDown && e.Key == Key.Enter)
            {
                int val; TextBox tb = (TextBox)sender;
                if (!int.TryParse(tb.Text, out val))
                {
                    TextBoxValue.Foreground = Brushes.Red; return;
                }
                else Value = val;
                e.Handled = true;
            }
        }

        private void TextBoxValue_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) Increase_Click(sender, null);
            if (e.Delta < 0) Decrease_Click(sender, null);
        }
    }
}
