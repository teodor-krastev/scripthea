using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace image_cues
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		private static System.Threading.Mutex _mutex = null;

		protected override void OnStartup(StartupEventArgs e)
		{
			_mutex = new System.Threading.Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-73F04E6BDE8F}", out bool createdNew);
			if (!createdNew)
			{
				MessageBox.Show("Previous instance of Scripthea is already running.", "Application Halted");
				Current.Shutdown();
			}
			else Exit += CloseMutexHandler;
			base.OnStartup(e);
		}
		protected virtual void CloseMutexHandler(object sender, EventArgs e)
		{
			_mutex?.Close();
		}

	}
}
