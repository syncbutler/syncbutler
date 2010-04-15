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
            Controller.GetInstance().SetWindow(this, false);
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
        public void FillInCreatePartnership(string path)
        {
            //will not handle on 1st run.
        }

        #endregion


		private void CloseApp(Object sender, RoutedEventArgs e) {
            if (WelcomeScreenControl.CurrentState == WelcomeScreenControl.State.AllowClose)
                this.DialogResult = false;
            else
                this.DialogResult = true;
		}
    }
}