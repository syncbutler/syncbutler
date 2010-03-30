using System;
using System.Collections.Generic;
using System.Linq;
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
using WPF_Explorer_Tree;
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
		public MainWindow()
		{
			try
            {
			    controller = Controller.GetInstance();
                if (!(controller.IsProgramRanBefore()))
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
            controller.SetWindow(this);
            this.homeWindow1.Controller = this.controller;
		}

        /// <summary>
        /// Calling this will use the dispatcher to bring this window to the top.
        /// </summary>
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

        /// <summary>
        /// If there's a scan running in HomeWindowControl, ask the user
        /// if he wants to cancel. 
        /// </summary>
        /// <returns>True if the operation is stopped or cancelled</returns>
        private bool StopExistingOperation()
        {
            if (!homeWindow1.IsBusy()) return true;

            CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                "There is currently an operation in progress. Please cancel the current operation before leaving this page.");
            
            // Without synchronization, cancelling the operation from here may be unpredictable
            // ie. The operation has started to be cancelled, but hasn't really ended yet while the
            // user has moved to a different page. For now, make the user cancel manually and wait until
            // the op is really cancelled.
            /* {
                homeWindow1.CancelCurrentScan();
                return true;
            } */

            return false;
        }

		private void goHome(object sender, RoutedEventArgs e)
		{
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            if (!StopExistingOperation()) return;
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"Home",false);
            homeWindow1.CurrentState = HomeWindowControl.State.Home;
		}


		private void goToSyncButlerSync(object sender, RoutedEventArgs e)
		{
            homeWindow1.CurrentState = HomeWindowControl.State.SBS;
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            if (!StopExistingOperation()) return;
            if (Controller.GetInstance().GetSBSEnable().Equals("Enable"))
            {
				if(controller.IsFirstSBSRun()){
					 FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
					VisualStateManager.GoToState(dialog.WelcomeScreenControl,"HelpScreen3",false);
					controller.SetFirstSBSRun();
					dialog.ShowDialog();
				}
                    homeWindow1.CheckIfEnoughSpace();
					VisualStateManager.GoToState(homeWindow1, "SbsState1", false);
					homeWindow1.LoadMRUs();
				
            }
            else
            {
                if (controller.IsFirstSBSRun())
                {
                    if (CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.Yes,
                       "Sync Butler has detected this is your first time using \"Sync Butler, Sync!\" and it is not enabled.\n\n"+
                       "Would you like to go to the settings screen with tips? ") == CustomDialog.MessageResponse.Yes)
                    {
                         this.GoToSetting("FirstSBSRun", null);
                    }

                }
                else
                {
                    if (CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.Yes,
                        "Sync Butler, Sync! is not enabled. Please enable this feature in the Settings screen\n\n" +
                        "Would you like to go to the settings screen?") == CustomDialog.MessageResponse.Yes)
                    {
                        this.GoToSetting(null, null);
                    }
                }
            }
		}

		private void GoToSetting(object sender, RoutedEventArgs e)
		{
            homeWindow1.CurrentState = HomeWindowControl.State.Settings;
            if (controller.IsFirstSBSRun())
                sender = "FirstSBSRun";
            if (!StopExistingOperation()) return;
            homeWindow1.IsLoadingSBS = true;
            if (sender != null && sender.GetType() == typeof(String) && (sender.Equals("FirstSBSRun")))
            {
                homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Visible;
			}
            else
            {
                homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            }
			
			VisualStateManager.GoToState(homeWindow1, "Settings1", false);
            
            BackgroundWorker storageScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(storageScanWorker, "Loading Settings Page", "Searching for removable storage devices");
            progressWindow.HideTotalProgress();
            progressWindow.IsInderteminate = true;

            List<string> DriveLetters = null;
            bool noUSBDrives = false;

            storageScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
            {
                DriveLetters = this.controller.GetUSBDriveLetters();

                // if there is no usb drive, let user to select some local hard disk, warn the user as well.
                if (DriveLetters.Count == 0)
                {
                    DriveLetters = this.controller.GetNonUSBDriveLetters();
                    noUSBDrives = true;
                }
            });

            storageScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            {

                this.homeWindow1.ComputerNameTextBox.Text = this.controller.GetComputerName();
                this.homeWindow1.NoUSBWarningTextBlock.Visibility = Visibility.Hidden;
                this.homeWindow1.SBSWorkingDriveComboBox.Items.Clear();
                bool devicePluggedIn = false;
                if (noUSBDrives) this.homeWindow1.NoUSBWarningTextBlock.Visibility = Visibility.Visible;

                foreach (string s in DriveLetters)
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.Items.Add(s[0]);
                }
                if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Contains(this.controller.GetSBSDriveLetter()))
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.SelectedItem = this.controller.GetSBSDriveLetter();
                    devicePluggedIn = true;
                }
                else if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Count != 0)
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.SelectedIndex = 0;
                }
                if (devicePluggedIn)
                {
                    if (this.controller.GetSBSEnable().Equals("Enable"))
                    {
                        this.homeWindow1.SpaceToUseSlide.Maximum = this.controller.GetAvailableSpaceForDrive();
                        this.homeWindow1.SpaceToUseSlide.Value = this.controller.GetFreeSpaceToUse();
                        this.homeWindow1.resolutionLabel.Content = this.controller.GetResolution();
                    }
                    else
                    {
                        this.homeWindow1.SpaceToUseSlide.Value = 0;
                        this.homeWindow1.resolutionLabel.Content = "KB";
                        this.homeWindow1.SpaceToUseSlide.IsEnabled = false;
                        this.homeWindow1.SpaceToUseTextbox.IsEnabled = false;
                    }


                    this.homeWindow1.SBSWorkingDriveComboBox.IsEnabled = true;
                    this.homeWindow1.SBSSettingComboBox.Items.Clear();
                    this.homeWindow1.SBSSettingComboBox.Items.Add("Enable");
                    this.homeWindow1.SBSSettingComboBox.Items.Add("Disable");
                    this.homeWindow1.SBSSettingComboBox.SelectedItem = this.controller.GetSBSEnable();
                }
                else
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.IsEnabled = false;
                    this.homeWindow1.SBSSettingComboBox.Items.Clear();
                    this.homeWindow1.SBSSettingComboBox.Items.Add("Enable");
                    this.homeWindow1.SBSSettingComboBox.Items.Add("Disable");
                    this.homeWindow1.SBSSettingComboBox.SelectedItem = "Disable";
                }
                homeWindow1.IsLoadingSBS = false;

                progressWindow.TaskComplete();
            });

            progressWindow.Start();
		}

		private void cleanUp(object sender,  System.ComponentModel.CancelEventArgs e)
		{
            if(this.controller != null)
			    this.controller.Shutdown();
		}
	}
}