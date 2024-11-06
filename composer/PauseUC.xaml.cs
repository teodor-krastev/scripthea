using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace scripthea.composer
{
    /// <summary>
    /// Interaction logic for PauseUC.xaml
    /// </summary>
    public partial class PauseUC : UserControl
    {
        private ManualResetEventSlim conditionsMetEvent;
        public PauseUC()
        {
            InitializeComponent();
            conditionsMetEvent = new ManualResetEventSlim(false);
        }
        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            chkPause.IsChecked = !chkPause.IsChecked.Value;
        }        
        private void chkPause_Unchecked(object sender, RoutedEventArgs e)
        {
            conditionsMetEvent?.Set();
        }
        public bool ConditionalPause(Action action) // return paused
        {
            if (!chkPause.IsChecked.Value) return chkPause.IsChecked.Value;
            conditionsMetEvent.Reset(); // Reset the event in case it was previously set
            ThreadPool.QueueUserWorkItem(_ =>
            {
                conditionsMetEvent.Wait(); // Wait until a condition (next control sequence of MM2 proc) is met

                // This code will run on a background thread after new sequence is about to start
                Dispatcher.Invoke(() =>
                {
                    // Access UI elements and continue your logic here
                    return chkPause.IsChecked.Value;
                });
            });
            return chkPause.IsChecked.Value;
        }
    }
}
