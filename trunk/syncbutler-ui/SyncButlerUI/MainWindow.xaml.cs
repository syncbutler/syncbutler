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
	public partial class MainWindow : Window
	{
		private SyncButler.Controller controller;
		public MainWindow()
		{
			try{
			this.InitializeComponent();
            controller = Controller.getInstance();
			this.homeWindow1.Controller = this.controller;
			}catch(Exception ex){
				
			Console.WriteLine(ex.Message);	
			}
			// Insert code required on object creation below this point.
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
		
		private void cleanUp(object sender,  System.ComponentModel.CancelEventArgs e)
		{
			this.controller.Shutdown();
		}
	}
}