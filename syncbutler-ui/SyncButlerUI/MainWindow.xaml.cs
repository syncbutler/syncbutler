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
using ISyncButler;

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
			this.homeWindow1.Favourites_List.Items.Clear();
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"SbsState1",false);
			SortedList<string,string> mru = controller.GetMonitoredFiles();
			foreach(string filenames in mru.Values)
			{
				
				this.homeWindow1.Favourites_List.Items.Add(filenames.Substring(filenames.LastIndexOf('\\')+1));
			}
			this.homeWindow1.WeirdFile_List.Items.Clear();
			this.homeWindow1.WeirdFile_List.Items.Add("C:\\secret.jpg");
			this.homeWindow1.WeirdFile_List.Items.Add("C:\\abc co\\secret stuff.jpg");
		}
		private void GoToSetting(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(homeWindow1, "Settings1",false);
		}

		private void cleanUp(object sender,  System.ComponentModel.CancelEventArgs e)
		{
            if(this.controller != null)
			    this.controller.Shutdown();
		}
	}
}