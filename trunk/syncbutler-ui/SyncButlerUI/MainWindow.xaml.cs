// Developer to contact: Ng Li Ying, Rachel
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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using SyncButler.Logging;
using SyncButler;
using SyncButler.Exceptions;

namespace SyncButlerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IGUI
    {
        private SyncButler.Controller controller;
        private ErrorList el;
        public MainWindow()
        {
            try
            {
                controller = Controller.GetInstance();
                if (Controller.IsFirstRun())
                {
                    FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
					if (!((bool)dialog.ShowDialog()))
                    {
                        throw new UserCancelledException();
                    }
                }
                this.InitializeComponent();

            }
            catch (UserCancelledException)
            {
                throw new UserCancelledException();
            }
            // Insert code required on object creation below this point.
            controller.SetWindow(this, true);
            this.homeWindow1.Controller = this.controller;
            el = new ErrorList();
            Controller.HandleStartupArgs();
        }

        #region IGUI
        public void AddToErrorList(string path, string error)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.SystemIdle,
                new Action(
                    delegate()
                    {
                        TableRow tr = new TableRow();
                        tr.FontSize = 10;
                        tr.Background = System.Windows.Media.Brushes.White;
                        tr.Cells.Add(new TableCell(new Paragraph(new Run(path))));
                        tr.Cells.Add(new TableCell(new Paragraph(new Run(error))));
                        this.el.errorTable.RowGroups[1].Rows.Add(tr);
                        this.el.Show();
                        this.el.Focus();
                    }
                    ));
        }

        public void GrabFocus(Controller.WinStates ws)
        {
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.SystemIdle,
                TimeSpan.FromSeconds(1),
                new Action(
                    delegate()
                    {
                        GrabFocus();
                        //switch to a screen based on ws.
                        switch (ws)
                        {
                            case Controller.WinStates.MiniPartnerships:
                                homeWindow1.ViewMiniPartnerships_Click(this, null);
                                break;
                            case Controller.WinStates.CreatePartnership:
                                homeWindow1.GoToCreatePartnership_Click(this, null);
                                if (this.Window.WindowState == WindowState.Minimized) this.Window.WindowState = WindowState.Normal;
                                break;
                            default:
                                break;
                        }
                    }
                    ));
        }

        /// <summary>
        /// Calling this will use the dispatcher to bring this window to the top.
        /// </summary>
        public void GrabFocus()
        {
            this.Activate();
            this.Topmost = true; //to bring to front
            this.Topmost = false; //to remove always on top
        }

        public void FillInCreatePartnership(string path)
        {
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                TimeSpan.FromSeconds(1),
                new Action(
                    delegate()
                    {
                        GrabFocus(Controller.WinStates.CreatePartnership);
                        homeWindow1.folderOneTextBox.Text = path;
                        homeWindow1.partnershipNameTextBox.Text = path;
                    }
                    ));
        }

        #endregion

        private void goHome(object sender, RoutedEventArgs e)
        {
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            if (!homeWindow1.StopExistingOperation()) return;
            //homeWindow1.goHome(sender,e);
            VisualStateManager.GoToState(homeWindow1, "HomeState", false);
            SetHomeActive();
            homeWindow1.CurrentState = HomeWindowControl.State.Home;
        }
        public void SetSBSActive()
        {
            SBSButtonHidden.Visibility = Visibility.Hidden;
            HomeButtonHidden.Visibility = Visibility.Visible;
        }
        public void SetHomeActive()
        {
            SBSButtonHidden.Visibility = Visibility.Visible;
            HomeButtonHidden.Visibility = Visibility.Hidden;
        }

        public void SetAllInActive()
        {
            SBSButtonHidden.Visibility = Visibility.Visible;
            HomeButtonHidden.Visibility = Visibility.Visible;
        }
        public void goToSyncButlerSync(object sender, RoutedEventArgs e)
        {

            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            String nextState = "SbsState";
            if (!homeWindow1.StopExistingOperation()) return;
          		if(Controller.IsFirstSBSRun() || !Controller.IsSBSEnable()){
					FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
					dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility=Visibility.Hidden;
                    dialog.Title = "Sync Butler - Help";
                    dialog.WelcomeScreenControl.GoToSBSScreen();
					dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
					dialog.ShowInTaskbar = false;
					dialog.Owner = this;
					dialog.ShowDialog();
                    if (dialog.WelcomeScreenControl.WantToShowSettingPage() && !Controller.IsSBSEnable())
                    {
                        homeWindow1.GoToSetting();
                        homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Visible;
                        SetSBSActive();
                        Controller.SetFirstSBSRun();
                        return;
                    }
                    Controller.SetFirstSBSRun();
				}
                    homeWindow1.CurrentState = HomeWindowControl.State.SBS;
                    VisualStateManager.GoToState(homeWindow1, nextState, false);
                    SetSBSActive();
					homeWindow1.LoadMRUs();
		}

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();

            dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility = Visibility.Hidden;
            dialog.Title = "Sync Butler - Help";
			dialog.ShowInTaskbar = false;
            dialog.WelcomeScreenControl.GoToHelpScreen();
			dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
			dialog.Owner = this;
            dialog.ShowDialog();

        }

        private void cleanUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.controller != null)
                this.controller.Shutdown();
        }
    }
}