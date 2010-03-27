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
using WPF_Explorer_Tree;
using SyncButler;
using SyncButler.Exceptions;
using System.ComponentModel;

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

		private void goHome(object sender, RoutedEventArgs e)
		{
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"Home",false);
		}
        

		private void goToSyncButlerSync(object sender, RoutedEventArgs e)
		{
            if (Controller.GetInstance().GetSBSEnable().Equals("Enable"))
            {
                VisualStateManager.GoToState(homeWindow1, "SbsState1", false);
                homeWindow1.LoadMRUs();
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

		private void GoToSetting(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(homeWindow1, "Settings1",false);

            BackgroundWorker storageScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(storageScanWorker, "Loading Settings Page");
            progressWindow.HideTotalProgress();

            List<string> DriveLetters = null;
            bool noUSBDrives = false;

            storageScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
            {
                BackgroundWorker workerObj = (BackgroundWorker)worker;
                ProgressBar.ProgressBarInfo pinfo;

                pinfo.SubTaskPercent = 10;
                pinfo.taskDescription = "Searching for portable drives";
                pinfo.TotalTaskPercent = 0;
                workerObj.ReportProgress(0, pinfo);

                DriveLetters = this.controller.GetUSBDriveLetters();

                // if there is no usb drive, let user to select some local hard disk, warn the user as well.
                if (DriveLetters.Count == 0)
                {
                    pinfo.TotalTaskPercent = 50;
                    pinfo.taskDescription = "Searching for local drives";
                    DriveLetters = this.controller.GetNonUSBDriveLetters();
                    noUSBDrives = true;
                }

                pinfo.SubTaskPercent = 100;
                pinfo.taskDescription = "Finishing...";
                workerObj.ReportProgress(0, pinfo);
            });

            storageScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            {
                
            

                this.homeWindow1.ComputerNameTextBox.Text = this.controller.GetComputerName();
                this.homeWindow1.NoUSBWarningTextBlock.Visibility = Visibility.Hidden;
                this.homeWindow1.SBSWorkingDriveComboBox.Items.Clear();

                if (noUSBDrives) this.homeWindow1.NoUSBWarningTextBlock.Visibility = Visibility.Visible;

                foreach (string s in DriveLetters)
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.Items.Add(s[0]);
                }

                if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Contains(this.controller.GetSBSDriveLetter()))
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.SelectedItem = this.controller.GetSBSDriveLetter();
                }
                else if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Count != 0)
                {
                    this.homeWindow1.SBSWorkingDriveComboBox.SelectedIndex = 0;
                }

                this.homeWindow1.SBSWorkingDriveComboBox.IsEnabled = true;
                this.homeWindow1.SBSSettingComboBox.Items.Clear();
                this.homeWindow1.SBSSettingComboBox.Items.Add("Enable");
                this.homeWindow1.SBSSettingComboBox.Items.Add("Disable");
                this.homeWindow1.SBSSettingComboBox.SelectedItem = this.controller.GetSBSEnable();

                if (this.controller.GetSBSEnable().Equals("Enable"))
                {
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