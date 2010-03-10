using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using SyncButler;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Controller.TestSingleInstance(e.Args))
            {
                Console.Out.WriteLine("OK IS SINGLE INSTANCE");
                new MainWindow().ShowDialog();
            }
            else
            {
                base.Shutdown(0);
            }
            base.OnStartup(e);
        }
	}
}