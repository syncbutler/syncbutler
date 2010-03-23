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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SyncButler;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for WelcomeScreenControl.xaml
	/// </summary>
	public partial class WelcomeScreenControl : UserControl
	{
		public Controller controller;
		public WelcomeScreenControl()
		{
			this.InitializeComponent();
		}
		
		public void NameIt_Click(object sender,RoutedEventArgs e)
        {

            if (FirstTimeComputerNameText.Text.Length != 0)
            {
                controller = Controller.GetInstance();
                controller.SetFirstComputerName(FirstTimeComputerNameText.Text);
				VisualStateManager.GoToState(this,"HelpScreen1",false);
			}
            else
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please enter a computer name");
            }
		}		
		
		public void GoToHelpScreen2_Click(object sender,RoutedEventArgs e)
        {
        		VisualStateManager.GoToState(this,"HelpScreen2",false);
		}	
		public void ExitTutorial_Click(object sender,RoutedEventArgs e)
        {
        		Window.GetWindow(this).DialogResult=true;
		}			
		
		private void FirstTimeComputerNameText_Enter(object sender, System.Windows.Input.KeyEventArgs e)
		{
            if (e.Key == Key.Return)
            {
                NameIt_Click(sender, e);
            }
		}
	}
}