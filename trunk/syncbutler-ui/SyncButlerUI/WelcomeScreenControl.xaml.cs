/*****************************************************************************/
// Copyright 2010 Sync Butler and its original developers.
// This file is part of Sync Butler (http://www.syncbutler.org).
// 
// Sync Butler is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sync Butler is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sync Butler.  If not, see <http://www.gnu.org/licenses/>.
//
/*****************************************************************************/

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
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please enter a valid computer name.\nOnly combination of Alphabets and Numbers are allowed.");
			}
            else if (!ComputerNameChecker.IsComputerNameValid(FirstTimeComputerNameText.Text.Trim()))
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, FirstTimeComputerNameText.Text.Trim() + " is not a valid name\nOnly combination of Alphabets and Numbers are allowed.");
            }
            else if (FirstTimeComputerNameText.Text.Length > 16)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please limit the length of the computer name to 16 characters.");
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
			FocusControl(() => FeatureHelpNextBtn.Focus());
		}
		public void GoToFeaturesSBSScreen()
		{
            CurrentState = State.AllowClose;
			VisualStateManager.GoToState(this, "FeatureSBSHelpState", false);
			FocusControl(() => HelpScreenCloseBtn.Focus());
		}
        public void GoToSBSScreen()
        {
            VisualStateManager.GoToState(this, "SBSHelpState", false);
            FocusControl(() => SBSHelpScreenNextBtn.Focus());
        }
		
		public void GoToDemoSBSScreen()
        {
            VisualStateManager.GoToState(this, "SBSHelpScreenState", false);
            FocusControl(() => HelpScreenCloseBtn.Focus());
        }
		/// <summary>
		/// Goes to the 2nd Screen of Help
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToHelpScreen2_Click(object sender,RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this,"SecondHelpScreenState",false);
            FocusControl(() => HelpScreenFinishBtn.Focus());
		}
	    private bool wantToShowSettingPage;
        private void ExitTutorial_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).DialogResult = true;
        }	
        private void ExitSBSTutorial_Click(object sender, RoutedEventArgs e)
        {
            wantToShowSettingPage = false;
            if (Controller.IsFirstSBSRun())
            {
                if (CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.No,
                                "Sync Butler, Sync! is currently not enabled.\n\nShould I show you to the Setting's screen so you may turn on Sync Butler, Sync! ?") == CustomDialog.MessageResponse.Yes)
                {
                    wantToShowSettingPage = true;
                }
            }
            Window.GetWindow(this).DialogResult = true;
		}

        public bool WantToShowSettingPage()
        {
            return wantToShowSettingPage;
        }
		private void FirstTimeComputerNameText_Enter(object sender, System.Windows.Input.KeyEventArgs e)
		{
            if (e.Key == Key.Return)
            {
                NameIt_Click(sender, e);
            }
		}
		/// <summary>
		/// Goes to the 2nd Screen of SBS Help
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToSBSDemoHelpScreen_Click(object sender,RoutedEventArgs e)
        {
			GoToDemoSBSScreen();
		}	
		/// <summary>
		/// Goes to the 2nd Screen of Feature
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToFeaturesSBSScreen_Click(object sender,RoutedEventArgs e)
        {
			GoToFeaturesSBSScreen();
		}	
		
		private void whatIsComputerName_Click(object sender,RoutedEventArgs e)
        {
			    CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                "Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.Chef fill this in.");

		}
	}
}