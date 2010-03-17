using System;
using System.Collections.Generic;
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
using SyncButler;
namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for FirstTimeStartupScreen.xaml
	/// </summary>
	public partial class FirstTimeStartupScreen : Window
	{
		public FirstTimeStartupScreen()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.

			
		}
        public Controller controller;
		
		public void NameIt_Click(object sender,RoutedEventArgs e)
        {

            if (FirstTimeComputerNameText.Text.Length != 0)
            {
                controller = Controller.GetInstance();
                controller.SetComputerName(FirstTimeComputerNameText.Text);
                this.DialogResult = true;
            }
		}
		
		

	}
}