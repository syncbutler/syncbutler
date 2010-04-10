using System;
using System.Collections.Generic;
using System.IO;
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
using SyncButler.Logging;
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
                if (!(Controller.IsNotFirstRun()))
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
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            if (!homeWindow1.StopExistingOperation()) return;
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"HomeState",false);
            homeWindow1.CurrentState = HomeWindowControl.State.Home;
		}


		public void goToSyncButlerSync(object sender, RoutedEventArgs e)
		{
            
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
		
            if (!homeWindow1.StopExistingOperation()) return;
          		if(Controller.IsFirstSBSRun()){
					FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
					dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility=Visibility.Hidden;
					//VisualStateManager.GoToState(dialog.WelcomeScreenControl,"HelpScreen3",false);
					Controller.SetFirstSBSRun();
                    dialog.Title = "SyncButler - Help";
                    dialog.WelcomeScreenControl.GoToSBSScreen();
					dialog.ShowDialog();
				}
                    homeWindow1.CheckIfEnoughSpace();
                    homeWindow1.CurrentState = HomeWindowControl.State.SBS;
                    VisualStateManager.GoToState(homeWindow1, "SbsState", false);
					homeWindow1.LoadMRUs();

				


		}

		private void Help_Click(object sender, RoutedEventArgs e)
		{
			FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
            
            dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility = Visibility.Hidden;
            dialog.Title = "SyncButler - Help";
            
            dialog.WelcomeScreenControl.GoToHelpScreen();
            dialog.ShowDialog();
			
		}

		private void cleanUp(object sender,  System.ComponentModel.CancelEventArgs e)
		{
            if(this.controller != null)
			    this.controller.Shutdown();
		}
	}
}