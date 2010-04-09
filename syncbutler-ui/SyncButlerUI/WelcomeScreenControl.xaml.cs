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
using System.Windows.Threading;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for WelcomeScreenControl.xaml
	/// </summary>
	public partial class WelcomeScreenControl : UserControl
	{
        public enum State { AllowClose, OpenWindow }
        public State CurrentState;
        private static string[] reserved = { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8" };
		public Controller controller;
		public WelcomeScreenControl()
		{
            CurrentState = State.AllowClose;
			this.InitializeComponent();
            
		}
	    /// <summary>
        /// a fix to focus control, when wpf give change focus to another control instead 
        /// Source: http://stackoverflow.com/questions/1395887/wpf-cannot-set-focus/1401121#1401121
        /// </summary>
        /// <param name="a">the action "focus" of the textbox</param>
        private void FocusControl(Action a)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, a);
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
                CurrentState = State.OpenWindow;
                VisualStateManager.GoToState(this, "HelpScreenState", false);
				FocusControl(() => HelpScreen1NextBtn.Focus());
            }
            else
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please enter a valid computer name.");
            }
		}		
		public void GoToHelpScreen()
		{
            CurrentState = State.AllowClose;
			VisualStateManager.GoToState(this, "HelpScreenState", false);
			FocusControl(() => HelpScreen1NextBtn.Focus());
		}
		public void GoToFeaturesScreen()
		{
            CurrentState = State.AllowClose;
			VisualStateManager.GoToState(this, "FeatureHelpState", false);
			FocusControl(() => HelpScreen1NextBtn.Focus());
		}
        public void GoToSBSScreen()
        {
            VisualStateManager.GoToState(this, "SBSHelpScreenState", false);
            FocusControl(() => HelpScreenCloseBtn.Focus());
        }
		/// <summary>
		/// Goes to the 2nd Screen of Help
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void GoToHelpScreen2_Click(object sender,RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this,"SecondHelpScreenState",false);
            FocusControl(() => HelpScreenFinishBtn.Focus());
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