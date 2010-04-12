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
        private ErrorList el;
        public MainWindow()
        {
            try
            {
                controller = Controller.GetInstance();
                if (Controller.IsFirstRun())
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
            el = new ErrorList();
        }

        #region IGUI
        public void AddToErrorList(string path, string error)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.SystemIdle,
                new Action(
                    delegate()
                    {
                        TableRow tr = new TableRow();
                        tr.FontSize = 10;
                        tr.Background = System.Windows.Media.Brushes.White;
                        tr.Cells.Add(new TableCell(new Paragraph(new Run(path))));
                        tr.Cells.Add(new TableCell(new Paragraph(new Run(error))));
                        this.el.errorTable.RowGroups[1].Rows.Add(tr);
                        this.el.Show();
                        this.el.Focus();
                    }
                    ));
        }

        public void GrabFocus(Controller.WinStates ws)
        {
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.SystemIdle,
                TimeSpan.FromSeconds(1),
                new Action(
                    delegate()
                    {
                        GrabFocus();
                        //switch to a screen based on ws.
                        switch (ws)
                        {
                            case Controller.WinStates.MiniPartnerships:
                                homeWindow1.ViewMiniPartnerships_Click(this, null);
                                break;
                            case Controller.WinStates.CreatePartnership:
                                homeWindow1.GoToCreatePartnership_Click(this, null);
                                break;
                            default:
                                break;
                        }
                    }
                    ));
        }

        /// <summary>
        /// Calling this will use the dispatcher to bring this window to the top.
        /// </summary>
        public void GrabFocus()
        {
            this.Activate();
            this.Topmost = true; //to bring to front
            this.Topmost = false; //to remove always on top
        }

        public void FillInCreatePartnership(string path)
        {
            this.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                TimeSpan.FromSeconds(1),
                new Action(
                    delegate()
                    {
                        GrabFocus(Controller.WinStates.CreatePartnership);
                        homeWindow1.folderOneTextBox.Text = path;
                        homeWindow1.partnershipNameTextBox.Text = path;
                    }
                    ));
        }

        #endregion
        private void goHome(object sender, RoutedEventArgs e)
        {
            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            if (!homeWindow1.StopExistingOperation()) return;
            //homeWindow1.goHome(sender,e);
            VisualStateManager.GoToState(homeWindow1, "HomeState", false);
            homeWindow1.CurrentState = HomeWindowControl.State.Home;
        }


        public void goToSyncButlerSync(object sender, RoutedEventArgs e)
        {

            homeWindow1.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;

            if (!homeWindow1.StopExistingOperation()) return;
            if (Controller.IsFirstSBSRun())
            {
                FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
                dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility = Visibility.Hidden;
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

        private void cleanUp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.controller != null)
                this.controller.Shutdown();
        }
    }
}