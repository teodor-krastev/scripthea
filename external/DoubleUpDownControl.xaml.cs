// http://www.CodeUnplugged.wordpress.com NotifyIcon for WPF
// Copyright (c) 2010 Inderpreet Gujral
// Contact and Information: http://www.CodeUnplugged.wordpress.com
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Code Project Open License (CPOL);
// either version 1.0 of the License, or (at your option) any later
// version.
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for NumericBox.xaml
    /// </summary>

    public partial class DoubleBox : UserControl
    {
        public double Maximum = double.MaxValue; public double Minimum = double.MinValue; public double Interval = 0.5; public string DoubleFormat = "F2";
        public DoubleBox()
        {
            InitializeComponent();
            TextBoxValue.Text = "0";
        }

        public event RoutedEventHandler OnValueChanged;
        protected void ValueChanged(object sender, RoutedEventArgs e) 
        {
            if (OnValueChanged != null) OnValueChanged(sender,e);
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
                double val;
                if (!double.TryParse(tb.Text, out val)) 
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
        private double _Value = 0;
        public double Value
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
                TextBoxValue.Text = _Value.ToString(DoubleFormat);
                
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
                double val; TextBox tb = (TextBox)sender;
                if (!double.TryParse(tb.Text, out val))
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

