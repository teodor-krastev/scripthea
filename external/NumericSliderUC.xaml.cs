﻿using System;
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
    /// Interaction logic for NumericSliderUC.xaml
    /// </summary>
    public partial class NumericSliderUC : UserControl
    {
        public NumericSliderUC()
        {
            InitializeComponent();
        }
        public void Init(string title, int initValue, int minValue = 0, int maxValue = 100, int incValue = 1)
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
            dblBox.Value = (int)slider.Value; ValueChanged(this, (double)dblBox.Value);
            changing = false;
        }
        private void numBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (changing) return;
            changing = true;
            slider.Value = dblBox.Value; ValueChanged(this, (double)dblBox.Value);
            changing = false;
        }
        public int Value 
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
