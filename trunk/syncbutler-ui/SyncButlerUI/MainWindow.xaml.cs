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
			try{
			this.InitializeComponent();
            controller = Controller.GetInstance();
			if(!(controller.IsProgramRanBefore())){
            FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
					
				if((bool)dialog.ShowDialog()){
           			 controller.SetWindow(this);
					this.homeWindow1.Controller = this.controller;
				}else{
                    throw new UserCancelledException();
				}
			}
			}catch (UserCancelledException)
            {
                throw new UserCancelledException();
			}
			// Insert code required on object creation below this point.
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
		private void goHome(object sender, RoutedEventArgs e)
		{
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"Home",false);
		}
        private SortedList<string,SortedList<string, string>> MRUs;

		private void goToSyncButlerSync(object sender, RoutedEventArgs e)
		{
			this.homeWindow1.Favourites_List.Items.Clear();
			VisualStateManager.GoToState(homeWindow1,"SbsState1",false);
            MRUs = controller.GetMonitoredFiles();
            foreach (string filenames in MRUs["interesting"].Keys)
			{
                this.homeWindow1.Favourites_List.Items.Add(filenames);
			}
            this.homeWindow1.WeirdFile_List.Items.Clear();
            foreach (string filenames in MRUs["sensitive"].Keys)
            {
                this.homeWindow1.WeirdFile_List.Items.Add(filenames);
            }
		}
		private void GoToSetting(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(homeWindow1, "Settings1",false);
            
            this.homeWindow1.ComputerNameTextBox.Text = this.controller.GetComputerName();
            this.homeWindow1.NoUSBWarningTextBlock.Visibility=Visibility.Hidden;
            this.homeWindow1.SBSWorkingDriveComboBox.Items.Clear();
            List<string> DriveLetters = this.controller.GetUSBDriveLetters();
            // if there is no usb drive, let user to select some local hard disk, warn the user as well.
            if (DriveLetters.Count == 0)
            {
                DriveLetters = this.controller.GetNonUSBDriveLetters();
                this.homeWindow1.NoUSBWarningTextBlock.Visibility = Visibility.Visible;
            }
            foreach(string s in DriveLetters)
            {
                this.homeWindow1.SBSWorkingDriveComboBox.Items.Add(s[0]);
            }

            if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Contains(this.controller.GetSBSDriveLetter()))
            {
                this.homeWindow1.SBSWorkingDriveComboBox.SelectedItem = this.controller.GetSBSDriveLetter();
            }
            else if(this.homeWindow1.SBSWorkingDriveComboBox.Items.Count != 0)
            {
                this.homeWindow1.SBSWorkingDriveComboBox.SelectedIndex = 0;
            }
			this.homeWindow1.SBSWorkingDriveComboBox.IsEnabled = true;
		}

		private void cleanUp(object sender,  System.ComponentModel.CancelEventArgs e)
		{
            if(this.controller != null)
			    this.controller.Shutdown();
		}
	}
}