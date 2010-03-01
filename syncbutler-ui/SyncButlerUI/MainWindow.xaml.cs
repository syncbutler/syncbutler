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
			this.InitializeComponent();
			controller = new SyncButler.Controller();
			this.homeWindow1.Controller = this.controller;
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
			SortedList<string,string> mru = controller.GetMRU();
			foreach(string filenames in mru.Values)
			{
				this.homeWindow1.Favourites_List.Items.Add(filenames);
			}
			this.homeWindow1.WeirdFile_List.Items.Clear();
			this.homeWindow1.WeirdFile_List.Items.Add("C:\\xxx.jpg");
			this.homeWindow1.WeirdFile_List.Items.Add("C:\\xxx\\weird stuff.jpg");
		}
	}
}