using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Threading;
using SyncButler;
using SyncButler.Exceptions;
using System.IO;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve); 
            SyncButlerUI.App app = new SyncButlerUI.App();
            app.InitializeComponent();
            app.Run();           
        }
        /// <summary>
        ///  This method will be fire if some assembly is missing, can be used to spawn the missing files, if required.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            MessageBox.Show("Some important files are missing\r\nSBS will not run.");
            Environment.Exit(-1);
            return null;
        } 
        /// <summary>
        /// Overrides the default OnStartup to provide for testing of single instance.
        /// </summary>
        /// <param name="e">Contains arguments from the event; used to access command line parameters.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
                if (Controller.IsOnCDRom())
                {
                    MessageBox.Show("Running SBS on CD Rom is Not Supported", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(-1);
                }
                else
                {
                    if (Controller.TestSingleInstance(e.Args))
                    {
                        try
                        {
                            (new MainWindow()).ShowDialog();
                        }
                        catch (UserCancelledException)
                        {
                            base.Shutdown(0);
                        }

                    }
                    else
                    {
                        base.Shutdown(0);
                    }

                    base.OnStartup(e);
                }
        }
	}
}