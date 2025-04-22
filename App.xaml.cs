using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UtilsNS;

namespace scripthea
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
			if (!createdNew) // another instance already running
			{
				if (Utils.isInVisualStudio) // if under development
                {
					if (Utils.ConfirmationMessageBox("Previous instance of Scripthea is already running.\n Close the current instance?")) Environment.Exit(0);
				}
				else // user case
                {
					Utils.TimedMessageBox("Previous instance of Scripthea is already running.", "Application halting!"); Current.Shutdown();
				}
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
