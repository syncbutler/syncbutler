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
using TetonWhitewaterKayak;
using SyncButler;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for WelcomeScreenControl.xaml
	/// </summary>
	public partial class WelcomeScreenControl : UserControl
	{
        private static string[] reserved = { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8" };
		public Controller controller;
		public WelcomeScreenControl()
		{
			this.InitializeComponent();
            
		}
		
		/// <summary>
		/// Button for Naming the Computer the first time
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void NameIt_Click(object sender,RoutedEventArgs e)
        {
			if(FirstTimeComputerNameText.Text.Trim().Length == 0)
			{
				CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please enter a valid computer name.");
			}
            else if (!ComputerNameChecker.IsComputerNameValid(FirstTimeComputerNameText.Text.Trim()))
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, FirstTimeComputerNameText.Text.Trim() + " is not a valid name");
            }
            else if (FirstTimeComputerNameText.Text.Length != 0)
            {
                controller = Controller.GetInstance();
                controller.SetFirstComputerName(FirstTimeComputerNameText.Text.Trim());

                VisualStateManager.GoToState(this, "HelpScreen1", false);
            }
            else
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please enter a valid computer name.");
            }
		}		
		public void GoToHelpScreen()
		{
			VisualStateManager.GoToState(this, "HelpScreen1", false);

		}
		/// <summary>
		/// Goes to the 2nd Screen of Help
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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