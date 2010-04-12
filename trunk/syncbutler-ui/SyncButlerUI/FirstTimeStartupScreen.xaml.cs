﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using SyncButler;
using System.Windows.Forms;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for FirstTimeStartupScreen.xaml
	/// </summary>
    public partial class FirstTimeStartupScreen : Window, IGUI
	{
        public FirstTimeStartupScreen()
        {
            this.InitializeComponent();
            this.ShowInTaskbar = true;
            Controller.GetInstance().SetWindow(this);
		}
        
        #region IGUI Members

        public void GrabFocus(Controller.WinStates ws)
        {
            GrabFocus(); //does not handle movement to other pages if the program has not been run before.
        }

        public void GrabFocus()
        {
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.SystemIdle,
                TimeSpan.FromSeconds(1),
                new Action(
                    delegate()
                    {
                        this.Activate();
                        this.Topmost = true; //to bring to front
                        this.Topmost = false; //to remove always on top
                    }
                    ));
        }

        public void AddToErrorList(string path, string error)
        {
            //will not handle on 1st run.
        }
        #endregion

		private void CloseApp(Object sender, RoutedEventArgs e){
            if (WelcomeScreenControl.CurrentState == WelcomeScreenControl.State.AllowClose)
                this.DialogResult = false;
            else
                this.DialogResult = true;
		}
    }
}