﻿using System;
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
        /// <summary>
        /// Overrides the default OnStartup to provide for testing of single instance.
        /// </summary>
        /// <param name="e">Contains arguments from the event; used to access command line parameters.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Controller.TestSingleInstance(e.Args))
            {
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