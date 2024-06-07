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
using Newtonsoft.Json;
using OpenHWMonitor;
using Path = System.IO.Path;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Threading;
using UtilsNS;

namespace scripthea.external
{
    /// <summary>
    /// Interaction logic for tempRegulatorUC.xaml
    /// </summary>
    public partial class tempRegulatorUC : UserControl
    {
        public tempRegulatorUC()
        {
            InitializeComponent();
            nVidia = new NVidia();
        }
        SDoptions opts; private NVidia nVidia;
        public void Init(ref SDoptions _opts)
        {
            opts = _opts; opts2Visual(true); 
        }
        public void Finish()
        {
            dTimer?.Stop();
        }
        int timeLeft = 0;
        public bool tempRegulate() // false = timeout
        {
            bool bb = true;
            if (opts.GPUtemperature)
            {
                if (!dTimer.IsEnabled) dTimer?.Start();
                if (nVidiaAvailable)
                {
                    while (((currentTmp > opts.GPUThreshold) || (currentTmp == -1)) && opts.GPUtemperature) { Thread.Sleep(500); }
                }
                else
                {
                    timeLeft = opts.GPUThreshold;
                    while ((timeLeft > 0) && opts.GPUtemperature) { Thread.Sleep(500); }
                }
            }
            else { timeLeft = 0; dTimer?.Stop(); }
            return bb;
        }
        public bool nVidiaHWAvailable { get { return nVidia.IsAvailable(); } }
        public bool nVidiaAvailable { get; private set; }
        public void opts2Visual(bool first = false)
        {
            nVidiaAvailable = opts.measureGPUtemp && nVidiaHWAvailable;
            if (nVidiaAvailable)
            {
                gridTmpr.Visibility = Visibility.Visible;
                gridTmprDly.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridTmpr.Visibility = Visibility.Collapsed;
                gridTmprDly.Visibility = Visibility.Visible;
            }
            if (first)
            {           
                if (nVidiaAvailable)
                    {
                        chkTmpr.IsChecked = opts.GPUtemperature; numGPUThreshold.Value = opts.GPUThreshold;
                    }
                    else
                    {
                        chkTmprDly.IsChecked = opts.GPUtemperature; dTimer?.Stop();
                        numDly.Value = opts.GPUThreshold;
                    }
            }
        }
        int currentTmp = -1; double averTmp = -1; int maxTmp = -1;
        private List<int> tmpStack;
        private DispatcherTimer dTimer;
        private void chkTemp_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked.Value)
            {
                if (Utils.isNull(dTimer))
                {
                    dTimer = new DispatcherTimer();
                    dTimer.Tick += new EventHandler(dTimer_Tick);
                    dTimer.Interval = new TimeSpan(1000 * 10000); // 1 [sec]
                    tmpStack = new List<int>();
                }
                if (opts.GPUtemperature) timeLeft = opts.GPUThreshold;
                else timeLeft = 0;
                dTimer.Start();
            }
            else
            {
                dTimer?.Stop(); timeLeft = 0; lbTimeLeft.Content = "";
                chkTmpr.Foreground = Brushes.Black;
            }
        }
        private void dTimer_Tick(object sender, EventArgs e)
        {
            if (!nVidiaAvailable)
            {
                lbTimeLeft.Content = "Time left: " + timeLeft.ToString() + "[s]";
                if (timeLeft > 0) timeLeft--;
                return;
            }
            int? primeTmp = nVidia.GetGPUtemperature();
            if (Utils.isNull(primeTmp)) return;
            currentTmp = (int)primeTmp;
            if (currentTmp < opts.GPUThreshold) chkTmpr.Foreground = Brushes.Blue;
            else chkTmpr.Foreground = Brushes.Red;
            chkTmpr.Content = "GPU temp[°C] = " + currentTmp.ToString();
            tmpStack.Add(currentTmp);
            while (tmpStack.Count > opts.GPUstackDepth) tmpStack.RemoveAt(0);
            averTmp = tmpStack.ToArray().Average();
            maxTmp = -1;
            foreach (int t in tmpStack)
                maxTmp = Math.Max(t, maxTmp);
            lbTmpInfo.Content = "aver: " + averTmp.ToString("G3") + "  max: " + maxTmp.ToString();
        }
        private void numGPUThreshold_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (opts != null)
                opts.GPUThreshold = (sender as NumericBox).Value;
        }
    }
}
