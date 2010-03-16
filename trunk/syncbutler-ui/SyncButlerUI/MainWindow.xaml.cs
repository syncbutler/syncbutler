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
            controller.SetWindow(this);
			this.homeWindow1.Controller = this.controller;
			}catch(Exception uIException){
				
			Console.WriteLine(uIException.Message);	
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
            List<string> DriveLetters = this.controller.GetDriveLetters();
            this.homeWindow1.ComputerNameTextBox.Text = this.controller.GetComputerName();
            
            this.homeWindow1.SBSWorkingDriveComboBox.Items.Clear();
            foreach(string s in DriveLetters)
            {
                this.homeWindow1.SBSWorkingDriveComboBox.Items.Add(s[0]);
            }
            if (this.homeWindow1.SBSWorkingDriveComboBox.Items.Contains(this.controller.GetSBSDriveLetter()))
            {
                this.homeWindow1.SBSWorkingDriveComboBox.SelectedItem = this.controller.GetSBSDriveLetter();
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