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
		public MainWindow()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}
		private void goHome(object sender, RoutedEventArgs e)
		{
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"Home",false);
		}
		private void goToSyncButlerSync(object sender, RoutedEventArgs e)
		{
			//homeWindow1.goHome(sender,e);
			VisualStateManager.GoToState(homeWindow1,"SbsState1",false);
		}
	}
}