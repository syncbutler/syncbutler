// Developer to contact: Chua Peng Chin, Benson
/*****************************************************************************/
// Copyright 2010 Sync Butler and its original developers.
// This file is part of Sync Butler (http://www.syncbutler.org).
// 
// Sync Butler is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sync Butler is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sync Butler.  If not, see <http://www.gnu.org/licenses/>.
//
/*****************************************************************************/

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
using System.Windows.Forms;

namespace SyncButlerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve); 
            SyncButlerUI.App app = new SyncButlerUI.App();
            app.InitializeComponent();
            app.Run();

        }

        /// <summary>
        /// This method is called if an unhandled exception is thrown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            String msg = "";
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                msg = "Exception Message: " + ex.Message + "\r\n";
                msg += "Stack Trace: \r\n" + ex.StackTrace;
                String filename =  DateTime.Now.ToString("yyyyMMddhhmmss") + ".log";
                TextWriter tw = new StreamWriter(filename);
                tw.WriteLine(msg);
                tw.Close();
                System.Windows.MessageBox.Show("Sorry an error has occured!\r\nPlease contact the developers with the following"
                      + " File:\n\n" + filename,
                      "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
            }
            catch (Exception)
            {
                if (String.IsNullOrEmpty(msg))
                {
                    System.Windows.MessageBox.Show("Sorry an unknown error has occured! Please download a new copy of a program and try again, if you get this message again please contact the developers\r\n"
                        ,"Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
                }
                else
                {
                    System.Windows.MessageBox.Show("Sorry an error has occured!\r\nPlease contact the developers with the screen shot of this error\r\n"
                          + msg,
                          "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
                }
            }
            finally
            {
                System.Windows.Forms.Application.Exit();
            }
        }

       
        /// <summary>
        /// [Disabled] This method will be fire if some assembly is missing, can be used to spawn the missing files, if required.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            System.Windows.Forms.MessageBox.Show("Some important files are missing\r\nSBS will not run.");
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
                System.Windows.Forms.MessageBox.Show("Running SBS on CD Rom is Not Supported", "Not Supported", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
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
                base.Shutdown(0);
            }
        }
    }
}