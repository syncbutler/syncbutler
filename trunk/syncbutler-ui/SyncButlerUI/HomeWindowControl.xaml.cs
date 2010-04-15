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
using System.Linq;
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

using System.IO;
using System.Data;
using System.Threading;
using System.ComponentModel;
using WPF_Explorer_Tree;
using SyncButler;
using SyncButler.Exceptions;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;

namespace SyncButlerUI
{
    /// <summary>
    /// Interaction logic for HomeWindowControl.xaml
    /// </summary>
    public partial class HomeWindowControl : UserControl
    {

        #region ErrorReporting
        protected enum ErrorReportingSource { Scanner, Resolver }

        protected struct ErrorReportingMessage
        {
            public Exception exceptionThrown;
            public ErrorReportingSource source;
            public Object failedObject;

            public ErrorReportingMessage(Exception exceptionThrown, ErrorReportingSource source, Object failed)
            {
                this.exceptionThrown = exceptionThrown;
                this.source = source;
                this.failedObject = failed;
            }
        }
        #endregion

        #region fields&Attributes
        private ObservableCollection<ConflictList> mergedList;
        List<Resolved> ResolvedConflicts = new List<Resolved>();
        private SortedList<string, SortedList<string, string>> MRUs;
        public enum State { Home, Create, CreateDone, ViewPartnership,ViewMiniPartnership, SBS, Conflict, Settings, Edit, EditDone, Result };
        public State CurrentState;
        private string oldPartnershipName = "";
        private string NewPartnershipName = "";
        private string LastWorkingFreeSpace = "0.00";

        private int conflictsProcessed = 0;

        private BackgroundWorker resolveWorker = null;
        private BackgroundWorker scanWorker = null;
        private bool operationCancelled = false;
        private Semaphore resolveLock = new Semaphore(1, 1);
        private Semaphore waitForErrorResponse = new Semaphore(0, 1);
        private Queue<Conflict> newConflicts = new Queue<Conflict>();

        public enum Volume { MB = 1024 * 1024, KB = 1024, GB = 1024 * 1024 * 1024, Bytes = 1 };
        // Keeps track of last selected index of conflict list

        #region constantAttributes
        private const long GIGA_BYTE = 1024 * 1024 * 1024;
        private const long MEGA_BYTE = 1024 * 1024;
        private const long KILO_BYTE = 1024;
        #endregion

        #region getSetAttribute
        public string SelectedImagePath { get; set; }
        public SyncButler.Controller Controller { get; set; }
        #endregion

        public enum CurrentActions { Scanning, Resolving, Idle }
        CurrentActions CurrentAction = CurrentActions.Idle;

        #region CountersForUI
        private int autoResolveCount;
        private int manualResolveCount;
        #endregion

        public bool IsLoadingSBS = true;
        private int lastClickedIndex;

        #endregion


        public HomeWindowControl()
        {
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
        /// Indicates whether we're busy with a scan or resolve
        /// </summary>
        /// <returns></returns>
        public bool IsBusy()
        {
            if (resolveWorker != null && resolveWorker.IsBusy) return true;
            if (scanWorker != null && scanWorker.IsBusy) return true;
            return false;
        }

        /// <summary>
        /// Add a list of conflicts to the resolve queue
        /// </summary>
        /// <param name="newConflicts"></param>
        protected internal void ThreadSafeAddResolve(IEnumerable<Conflict> newConflicts)
        {
            resolveLock.WaitOne();
            foreach (Conflict newConflict in newConflicts) this.newConflicts.Enqueue(newConflict);
            resolveLock.Release();
        }

        /// <summary>
        /// Add one conflict to the resolve queue
        /// </summary>
        /// <param name="newConflict"></param>
        protected internal void ThreadSafeAddResolve(Conflict newConflict)
        {
            resolveLock.WaitOne();
            newConflicts.Enqueue(newConflict);
            resolveLock.Release();
        }

        /// <summary>
        /// Get one conflict to resolve
        /// </summary>
        /// <returns></returns>
        protected internal Conflict ThreadSafeGetNewResolve()
        {
            resolveLock.WaitOne();
            if (newConflicts.Count == 0)
            {
                resolveLock.Release();
                return null;
            }
            Conflict toReturn = newConflicts.Dequeue();
            conflictsProcessed++;
            resolveLock.Release();

            return toReturn;
        }

        /// <summary>
        /// Reports an error from Background Workers which use DisplayProgress
        /// to report progress.
        /// </summary>
        /// <param name="worker">The BackgroundWorker which represents this thread</param>
        /// <param name="exp">The exception to report</param>
        /// <returns>Returns false if the thread should attempt to continue, true if it should cancel operations</returns>
        private bool ReportError(BackgroundWorker worker, ErrorReportingMessage msg)
        {
            worker.ReportProgress(0, msg);
            waitForErrorResponse.WaitOne();
            return worker.CancellationPending;
        }
		
        /// <summary>
        /// Delegate to report progress of a Sync operation to the user
        /// </summary>
        /// <param name="workerObj"></param>
        /// <param name="args"></param>
        private void DisplayProgress(Object worker, ProgressChangedEventArgs args)
        {
            if (args.UserState is String)
            {
                PartnershipName.Text = "Syncing: " + (String)args.UserState;
                return;
            }

            if (args.UserState is ErrorReportingMessage)
            {
                ErrorReportingMessage msg = (ErrorReportingMessage)args.UserState;
                CustomDialog.MessageTemplate msgTemplate;

                string message;

                if (msg.source == ErrorReportingSource.Resolver)
                {
                    message = msg.exceptionThrown.Message + "\n\nWhat would you like me to do?";
                    msgTemplate = CustomDialog.MessageTemplate.SkipRetryCancel;
                    conflictsProcessed--;
                }
                else if (msg.source == ErrorReportingSource.Scanner)
                {
                    message = msg.exceptionThrown.Message + "\n\nWhat would you like me to do?";
                    msgTemplate = CustomDialog.MessageTemplate.SkipCancel;
                }
                else throw new NotImplementedException();

                switch (CustomDialog.Show(this, msgTemplate, CustomDialog.MessageType.Error, CustomDialog.MessageResponse.Cancel, message))
                {
                    case CustomDialog.MessageResponse.Cancel:
                        ((BackgroundWorker)worker).CancelAsync();
                        break;
                    case CustomDialog.MessageResponse.Retry:
                        System.Diagnostics.Debug.Assert(msg.source == ErrorReportingSource.Resolver, "I was unsuccessful in retrying your request. Some errors occured when I was fixing the issues.");
                        ThreadSafeAddResolve((Conflict)msg.failedObject);
                        break;
                }

                waitForErrorResponse.Release();
                return;
            }

            if (TotalProgressBar.IsIndeterminate)
            {
                TotalProgressBar.IsIndeterminate = false;
                SubProgressBar.IsIndeterminate = false;
            }

            if (args.UserState == null && args.ProgressPercentage == 100)
            {
                CurrentSyncingFile.Text = "Finalising...";
                return;
            }

            SyncableStatus status = (SyncableStatus)args.UserState;
            string verb = "";

            switch (status.Type)
            {
                case SyncableStatus.ActionType.Checksum:
                case SyncableStatus.ActionType.Sync:
                    verb = "Scanning ";
                    break;

                case SyncableStatus.ActionType.Copy: verb = "Copying "; break;
                case SyncableStatus.ActionType.Delete: verb = "Deleting "; break;
            }

            CurrentSyncingFile.Text = verb + status.FriendlyEntityPath;
            SubProgressBar.Value = status.CurTaskPercentComplete;

            if (CurrentAction == CurrentActions.Resolving)
            {
                int processed = (conflictsProcessed > 0) ? conflictsProcessed - 1 : conflictsProcessed;
                int total = processed + newConflicts.Count + 1;

                TotalProgressBar.Value = (int)((100 * processed / total));
            }
        }

        /// <summary>
        /// Starts a BackgroundWorker object for a Syncing (ie. Scan);
        /// </summary>
        /// <param name="partnershipName">The name of the partnerhsip to scan</param>
        /// <returns></returns>
        private void AsyncStartSync(string partnershipName)
        {
            List<string> singletonList = new List<string>();
            singletonList.Add(partnershipName);
            AsyncStartSync(singletonList, this.Controller.GetPartnershipList());
        }

        /// <summary>
        /// Starts a BackgroundWorker object for a Syncing (ie. Scan);
        /// </summary>
        /// <param name="partnershipNames">A collection of partnerships to scan</param>
        private void AsyncStartSync(IEnumerable<string> partnershipNames, SortedList<string, Partnership> partnershipList)
        {
            Controller.ConflictCount = 0;
            VisualStateManager.GoToState(this, "ConflictState", false);
            CurrentState = State.Conflict;
            if (scanWorker != null)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "There is already a sync " +
                    "in progress. Please stop the sync before starting another.");
                return;
            }

            operationCancelled = false;
            conflictsProcessed = 0;
            autoResolveCount = 0;
            manualResolveCount = 0;

            // Instantiates background worker 
            scanWorker = new BackgroundWorker();
            scanWorker.WorkerReportsProgress = true;
            scanWorker.WorkerSupportsCancellation = true;

            SubProgressBar.Maximum = 100;
            SubProgressBar.Minimum = 0;
            TotalProgressBar.Maximum = 100;
            TotalProgressBar.Minimum = 0;

            TotalProgressBar.Visibility = Visibility.Hidden;

            ConflictList.ItemsSource = new ObservableCollection<ConflictList>();
            ConflictList.Items.Refresh();
            resolveButton.IsEnabled = false;
            doneButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            goToResultPageButton.IsEnabled = false;
            CurrentSyncingFile.Text = "Preparing to sync...";
            PartnershipName.Text = "";

            bool NeedsUserIntervention = false;

            scanWorker.ProgressChanged += new ProgressChangedEventHandler(DisplayProgress);

            scanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object workerObj, RunWorkerCompletedEventArgs args)
            {
                TotalProgressBar.Value = 0;
                SubProgressBar.Value = 0;

                if (operationCancelled)
                {
                    CurrentSyncingFile.Text = "Scan Cancelled.";
                    scanWorker = null;
                    CancelButton.IsEnabled = false;
                    return;
                }

                List<Conflict> autoResolveConflicts = new List<Conflict>();

                foreach (ConflictList cl in mergedList)
                {
                    autoResolveConflicts.AddRange(Controller.RemoveAutoResolvableConflicts(cl));
                }

                ConflictList.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                ConflictList.ItemsSource = mergedList;
                ConflictList.Items.Refresh();
                ConflictList.IsEnabled = true;
                if (NeedsUserIntervention)
                {
                    resolveButton.IsEnabled = true;
                }
                doneButton.IsEnabled = true;
                CurrentSyncingFile.Text = "I am almost done with syncing.\nHowever, there are some outstanding issues which I need your help with.";

                ThreadSafeAddResolve(autoResolveConflicts);

                scanWorker = null;
                CancelButton.IsEnabled = false;
                AsyncStartResolve();
            });

            scanWorker.DoWork += new DoWorkEventHandler(delegate(Object workerObj, DoWorkEventArgs args)
            {
                mergedList = new ObservableCollection<ConflictList>();
                BackgroundWorker worker = (BackgroundWorker)workerObj;

                foreach (string friendlyName in partnershipNames)
                {
                    worker.ReportProgress(0, friendlyName);

                    try
                    {
                        ConflictList cl = this.Controller.SyncPartnership(friendlyName, delegate(SyncableStatus status)
                        {
                            worker.ReportProgress(status.PercentComplete, status);
                            if (worker.CancellationPending) return false;
                            return true;
                        },
                        delegate(Exception exp)
                        {
                            ErrorReportingMessage msg = new ErrorReportingMessage(exp, ErrorReportingSource.Scanner, null);

                            if (ReportError(worker, msg))
                            {
                                operationCancelled = true;
                                return false;
                            }
                            else return true;
                        }, partnershipList);

                        worker.ReportProgress(100, null);
                        foreach (Conflict c in cl.Conflicts) if (c.AutoResolveAction == SyncButler.Conflict.Action.Unknown) NeedsUserIntervention = true;
                        mergedList.Add(cl);
                        this.Controller.CleanUpOrphans(friendlyName, partnershipList);
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        return;
                    }

                }
            });


            scanWorker.RunWorkerAsync();
            CurrentAction = CurrentActions.Scanning;
            return;
        }
        /// <summary>
        /// Starts an asynchronous resolve operation, if it hasn't already been started.
        /// Conflicts to be resolved should be stored by calling ThreadSafeAddResolve()
        /// prior to calling this method.
        /// </summary>
        /// <returns>The BackgroundWorker used to run the resolutions</returns>
        private void AsyncStartResolve()
        {
            if (resolveWorker != null) return;

            operationCancelled = false;

            // Instantiates background worker 
            resolveWorker = new BackgroundWorker();
            resolveWorker.WorkerReportsProgress = true;
            resolveWorker.WorkerSupportsCancellation = true;

            CurrentSyncingFile.Text = "Getting ready to fix outstanding issues...";
            PartnershipName.Text = "";

            TotalProgressBar.Visibility = Visibility.Visible;
            CancelButton.IsEnabled = true;
            doneButton.IsEnabled = false;

            resolveWorker.DoWork += new DoWorkEventHandler(delegate(Object workerObj, DoWorkEventArgs args)
            {
                BackgroundWorker worker = (BackgroundWorker)workerObj;

                SyncableStatusMonitor reporter = delegate(SyncableStatus status)
                {
                    worker.ReportProgress(status.PercentComplete, status);
                    if (worker.CancellationPending) return false;
                    return true;
                };

                Conflict curConflict = ThreadSafeGetNewResolve();
                Exception exp;


                string partnershipName = "";
                while (curConflict != null)
                {
                    try
                    {
                        exp = null;
                        if (partnershipName != curConflict.GetPartnership().Name)
                        {
                            partnershipName = curConflict.GetPartnership().Name;
                            worker.ReportProgress(0, partnershipName);
                        }

                        ResolvedConflicts.Add(Controller.ResolveConflict(curConflict, reporter, worker));
                        if (curConflict.AutoResolveAction == SyncButler.Conflict.Action.Unknown) manualResolveCount++;
                        else autoResolveCount++;
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        return;
                    }
                    catch (IOException e)
                    {
                        if (e.Message.StartsWith("Could not find a part of the path "))
                            exp = new Exception("I could not be find the folder " + e.Message.Substring("Could not a find part of the path ".Length));
                        else
                            exp = new Exception("I am having a problem accessing a folder while syncing " + partnershipName + ":\n\n" + e.Message);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        exp = new Exception("I was denied permission to access a file while syncing " + partnershipName + ":\n\n" + e.Message);
                    }
                    catch (System.Security.SecurityException e)
                    {
                        exp = new Exception("I was denied permission to access a file while syncing " + partnershipName + ":\n\n" + e.Message);
                    }
                    catch (InvalidActionException e)
                    {
                        exp = new Exception("I might have done something I was not supposed to while syncing " + partnershipName + ":\n\n" + e.Message);
                    }
                    catch (Exception e)
                    {
                        exp = new Exception("There seems to be a problem syncing " + partnershipName + ":\n\n" + e.Message);
                    }

                    if (exp != null)
                    {
                        ErrorReportingMessage msg = new ErrorReportingMessage(exp, ErrorReportingSource.Resolver, curConflict);

                        if (ReportError(worker, msg))
                        {
                            operationCancelled = true;
                            return;
                        }
                    }

                    curConflict = ThreadSafeGetNewResolve();
                }
            });

            resolveWorker.ProgressChanged += new ProgressChangedEventHandler(DisplayProgress);

            resolveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object workerObj, RunWorkerCompletedEventArgs args)
            {
                int manualCount = 0;
                foreach (ConflictList cl in mergedList) manualCount += cl.Conflicts.Count;
                if ((manualResolveCount > 0) || (manualCount == 0))
                {
                    CurrentSyncingFile.Text = "Syncing Complete.";
                }
                else
                {
                    CurrentSyncingFile.Text = "I am almost done with syncing.\nHowever, there are some outstanding issues which I need your help with.";
                }
                
                partnershipNameTextBox.Text = "";

                TotalProgressBar.Value = 0;
                SubProgressBar.Value = 0;

                if (operationCancelled)
                {
                    CurrentSyncingFile.Text = "Sync Cancelled.";
                    if (Controller.ConflictCount != 0)
                    {
                        resolveButton.IsEnabled = true;
                    }
                    resolveWorker = null;
                    CancelButton.IsEnabled = false;
                    return;
                }

                resolveWorker = null;

                CancelButton.IsEnabled = false;
                doneButton.IsEnabled = true;
                if (conflictsProcessed > 0)
                {
                    goToResultPageButton.IsEnabled = true;
                }

                if (newConflicts.Count > 0) AsyncStartResolve();

            });

            resolveWorker.RunWorkerAsync();
            CurrentAction = CurrentActions.Resolving;

        }

        #region UIcode
        /// <summary>
        /// Expand and Collaspses the partnership conflicts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandCollapseCoflicts(object sender, RoutedEventArgs e)
        {
            if (lastClickedIndex == ConflictList.SelectedIndex)
            {
                ConflictList.SelectedIndex = -1;
                lastClickedIndex = -2;
              }
            else
            {
                lastClickedIndex = ConflictList.SelectedIndex;
            }
        }

        /// <summary>
        /// Refresh the list of device to user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshSBSSettingDriveList(object sender, RoutedEventArgs e)
		{
            BackgroundWorker storageScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(storageScanWorker, "Refreshing drive list", "Searching for removable storage devices");
            progressWindow.HideTotalProgress();
            progressWindow.IsIndeterminate = true;
            List<WindowDriveInfo> DriveLetters = null;
            bool noUSBDrives = false;
            
            storageScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
                {
                    #region search for portable devices
                    DriveLetters = Controller.GetUSBDriveLetters();

                    // if there is no usb drive, let user to select some local hard disk, warn the user as well.
                    if (DriveLetters.Count == 0)
                    {
                        //DriveLetters = Controller.GetNonUSBDriveLetters();
                        noUSBDrives = true;
                    }
                    #endregion
                });
            storageScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
                {
                    bool devicePluggedIn = false;
                    SBSWorkingDriveComboBox.Items.Clear();
                    if (noUSBDrives)
                    {
                        #region if there is no usb device found
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                            "Please plug in a portable storage device if you wish to use it with\nSync Butler, Sync!");
                        this.SBSWorkingDriveComboBox.IsEnabled = false;
                        this.SpaceToUseSlide.IsEnabled = false;
                        this.SpaceToUseTextbox.IsEnabled = false;
                        this.SBSSettingComboBox.IsEnabled = false;
                        this.DefaultSettingButton.IsEnabled = false;
                        this.SaveSettingButton.IsEnabled = false;
                        this.NoUSBWarningTextBlock.Visibility = Visibility.Visible;
                        this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Hidden;
                        #endregion
                    }
                    else
                    {
                        #region some usb device is/are found
                        this.SBSSettingComboBox.IsEnabled = true;
                        this.DefaultSettingButton.IsEnabled = true;
                        this.SaveSettingButton.IsEnabled = true;
                        SBSWorkingDriveComboBox.IsEnabled = true;
                        this.NoUSBWarningTextBlock.Visibility = Visibility.Hidden;
                        this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Hidden;
                        

                        foreach (WindowDriveInfo s in DriveLetters)
                        {
                            this.SBSWorkingDriveComboBox.Items.Add(s);
                        }
                        if (this.SBSWorkingDriveComboBox.Items.Contains(Controller.GetSBSDriveLetter()))
                        {
                            this.SBSWorkingDriveComboBox.SelectedItem = Controller.GetSBSDriveLetter();
                            devicePluggedIn = true;
                        }
                        else if (this.SBSWorkingDriveComboBox.Items.Count != 0)
                        {
                            this.SBSWorkingDriveComboBox.SelectedIndex = 0;
                        }
                        if (!devicePluggedIn)
                        {
                            this.SBSWorkingDriveComboBox.IsEnabled = false;
                            if (this.SBSSettingComboBox.Items.IsEmpty)
                            {
                                this.SBSSettingComboBox.Items.Add("Enable");
                                this.SBSSettingComboBox.Items.Add("Disable");
                            }
                            if (this.SBSWorkingDriveComboBox.Items.IsEmpty)
                            {
                                this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Visible;
                            }
                            this.SBSSettingComboBox.SelectedItem = "Disable";
                            this.SBSWorkingDriveComboBox.IsEnabled = false;
                        }
                        this.IsLoadingSBS = false;
                        #endregion
                    }
                    progressWindow.TaskComplete();
                });
            progressWindow.Start();
		}
		
        private void GoHome()
        {
            this.FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            VisualStateManager.GoToState(this, "HomeState", false);
            CurrentState = State.Home;
        }
        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            if (CurrentState == HomeWindowControl.State.Settings)
            {
                if (FirstTimeHelp.Visibility == Visibility.Visible)
                    FirstTimeHelp.Visibility = Visibility.Hidden;
                else
                    FirstTimeHelp.Visibility = System.Windows.Visibility.Visible;
            }
            else if (CurrentState == HomeWindowControl.State.SBS)
            {
                //GoToExploreFeatures_Click(null, null);
                FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
                dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility = Visibility.Hidden;
                Controller.SetFirstSBSRun();
                dialog.Title = "Sync Butler - Help";
                dialog.WelcomeScreenControl.GoToSBSHelpScreen();
				dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }


        #region createPartnership

        /// <summary>
        /// go to the 1st page of create partnership to set source Textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        internal void GoToCreatePartnership_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "CreatePartnershipState", false);
            CurrentState = State.Create;
            this.folderOneTextBox.Clear();
            this.folderTwoTextBox.Clear();
            this.partnershipNameTextBox.Clear();
            FocusControl(() => folderOneTextBox.Focus());			
        }
        /// <summary>
        /// GetPath of folder in directory browser dialog
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        private static String GetPath(String Path)
        {
            System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(Path))
            {
                fd.SelectedPath = Path;
            }
            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return null;
            return fd.SelectedPath;
        }




        /// <summary>
        /// done to submit the create partnership to controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreatePartnership_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                string folderOnePath = folderOneTextBox.Text.Trim();
                checkInput(folderOnePath);
                string folderTwoPath = folderTwoTextBox.Text.Trim();
                checkInput(folderTwoPath);
                ValidateFoldersHierachy(folderOnePath, folderTwoPath);
                string partnershipName = partnershipNameTextBox.Text.Trim();
                sourceFolderPath.Text = folderOnePath;
                destinationFolderPath.Text = folderTwoPath;
                partnerShipName.Text = partnershipName;
				NewPartnershipName = partnershipName;
                if (String.IsNullOrEmpty(partnershipNameTextBox.Text.Trim()))
                    throw new UserInputException("Please input a partnership name");

                this.Controller.AddPartnership(partnerShipName.Text, sourceFolderPath.Text, destinationFolderPath.Text);

                VisualStateManager.GoToState(this, "CreateDoneState", false);
                CurrentState = State.CreateDone;
                partnershipList.Items.Refresh();
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }
            catch (ArgumentException argEx)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, argEx.Message);
            }
        }
        #endregion


        /// <summary>
        /// goes back to Home state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoHome(object sender, RoutedEventArgs e)
        {
            GoHome();
        }

        /// <summary>
        /// goes to view partnerships
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoToViewPartnerships_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "ViewPartnershipState", false);
            CurrentState = State.ViewMiniPartnership;
            SortedList<string, Partnership> partnershiplist = this.Controller.GetPartnershipList();
       		this.partnershipList.ItemsSource = partnershiplist.Values;
			this.partnershipList.Items.Refresh();
        }
		/// <summary>
        /// goes to view for mini partnerships
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void ViewMiniPartnerships_Click(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "ViewMiniPartnershipState", false);
            CurrentState = State.ViewMiniPartnership;
            SortedList<string, Partnership> miniPartnershiplist = this.Controller.GetMiniPartnershipList();
			this.minipartnershiplist.ItemsSource = miniPartnershiplist.Values;
            this.minipartnershiplist.Items.Refresh();
            
        }
        
        /// <summary>
        /// Checks for the index selected and delete the mini partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteMiniPartnership_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (minipartnershiplist.SelectedIndex < 0)
                {
                    throw new UserInputException("Please select a mini partnership to delete.");
                }

                if(CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.No, "Are you sure you want to delete the \"" +
                    minipartnershiplist.Items[minipartnershiplist.SelectedIndex] +
                    "\" partnership?") == CustomDialog.MessageResponse.Yes)
                {
                    this.Controller.DeleteMiniPartnership(minipartnershiplist.SelectedIndex);
                    minipartnershiplist.Items.Refresh();
                }
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }
        }

        /// <summary>
        /// Checks for the index selected and delete the partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeletePartnership_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (partnershipList.SelectedIndex < 0)
                {
                    throw new UserInputException("Please select a partnership to delete.");
                }
                if(CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.No,"Are you sure you want to delete the \"" +partnershipList.Items[partnershipList.SelectedIndex] +"\" partnership?") == CustomDialog.MessageResponse.Yes)
                {
                    this.Controller.DeletePartnership(partnershipList.SelectedIndex);
                    partnershipList.Items.Refresh();
                }
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }
        }

        /// <summary>
        /// Executes upon clicking resolve partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResolvePartnership_Click(object sender, RoutedEventArgs e)
        {
            foreach (ConflictList cl in mergedList) ThreadSafeAddResolve(cl.Conflicts);
            ConflictList.IsEnabled = false;
            resolveButton.IsEnabled = false;
            AsyncStartResolve();
        }

        /// <summary>
        /// Executes when clicking on the explore features button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoToExploreFeatures_Click(object sender, RoutedEventArgs e)
        {
            FirstTimeStartupScreen dialog = new FirstTimeStartupScreen();
            
            dialog.WelcomeScreenControl.FirstTimeComputerNameText.Visibility = Visibility.Hidden;
            dialog.Title = "Sync Butler - Help";
            
            dialog.WelcomeScreenControl.GoToFeaturesScreen();
			dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.WelcomeScreenControl.WantToShowSettingPage() && !Controller.IsSBSEnable())
            {
                GoToSetting();
                FirstTimeHelp.Visibility = System.Windows.Visibility.Visible;
                return;
            }
        }


        /// <summary>
        /// Cancels the current scan or resolution
        /// </summary>
        public void CancelCurrentScan()
        {
            if (scanWorker != null) scanWorker.CancelAsync();
            if (resolveWorker != null) resolveWorker.CancelAsync();
        }

        /// <summary>
        /// When the user clicks the Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if(CustomDialog.Show(this,CustomDialog.MessageTemplate.YesNo,CustomDialog.MessageResponse.No,"Are you sure you want to stop this scan?") == CustomDialog.MessageResponse.Yes)
                CancelCurrentScan();
        }

        /// <summary>
        /// Executes when SyncAll button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sync(object sender, RoutedEventArgs e)
        {
            ResolvedConflicts = new List<Resolved>();
            bool canDoSBS = Controller.CanDoSBS();
            if (this.Controller.GetPartnershipList().Count < 1 && !canDoSBS)
            {
                if (CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.No, "There are no partnerships for me to sync. Would you like to create one now?") == CustomDialog.MessageResponse.Yes)
                {

                    VisualStateManager.GoToState(this, "CreatePartnershipState", false);
                    CurrentState = State.Create;
                    FocusControl(() => folderOneTextBox.Focus());

                }
            }
            else if(CustomDialog.Show(this,CustomDialog.MessageTemplate.YesNo,CustomDialog.MessageResponse.No,"Are you sure you want to sync all partnerships?") == CustomDialog.MessageResponse.Yes)
            {

                #region Sync Most Recently used files
                if (canDoSBS)
                {
                    #region find mrus
                    BackgroundWorker sbsScanWorker = new BackgroundWorker();
                    ProgressBar progressWindow = new ProgressBar(sbsScanWorker, "Searching for your recent files...");
                    progressWindow.HideTotalProgress();

                    sbsScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
                    {
                        ProgressBar.ProgressBarInfo pinfo;
                        pinfo.SubTaskPercent = 0;
                        pinfo.taskDescription = "Searching for your recent files...";
                        pinfo.TotalTaskPercent = 0;
                        ((BackgroundWorker)worker).ReportProgress(0, pinfo);

                        MRUs = Controller.GetInstance().GetMonitoredFiles(delegate(SyncableStatus status)
                        {
                            pinfo.SubTaskPercent = status.CurTaskPercentComplete;
                            ((BackgroundWorker)worker).ReportProgress(0, pinfo);
                            return true;
                        }, false);

                        pinfo.SubTaskPercent = 100;
                        pinfo.taskDescription = "Finishing...";
                        ((BackgroundWorker)worker).ReportProgress(0, pinfo);
                    });

                    sbsScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
                    {
                        progressWindow.TaskComplete();
                    });
                    progressWindow.Start();
                    
                    #endregion

                    SyncMRU(this.Controller.GetPartnershipList().Count > 0);

                }
                #endregion

                if (this.Controller.GetPartnershipList().Count > 0)
                {
                    AsyncStartSync(this.Controller.GetPartnershipList().Keys, this.Controller.GetPartnershipList());
                }
            }
            else
            {
                return;
            }
        }
		
 	     /// <summary>
        /// When the user clicks Sync in the MiniPartnership List view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncMiniPartnership_Click(object sender, RoutedEventArgs e)
        {
            ResolvedConflicts = new List<Resolved>();
            if (this.Controller.GetMiniPartnershipList().Count < 1)
                CustomDialog.Show(this,CustomDialog.MessageTemplate.OkOnly,CustomDialog.MessageResponse.Ok,"There are no mini partnerships.");
            else if(CustomDialog.Show(this,CustomDialog.MessageTemplate.YesNo,CustomDialog.MessageResponse.No,"Are you sure you want to sync all mini partnerships?") == CustomDialog.MessageResponse.Yes)
                AsyncStartSync(this.Controller.GetMiniPartnershipList().Keys, this.Controller.GetMiniPartnershipList());
        }
        /// <summary>
        /// When the user clicks Sync in the Partnership List view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncPartnership_Click(object sender, RoutedEventArgs e)
        {
            ResolvedConflicts = new List<Resolved>();
            try
            {
                if (partnershipList.SelectedIndex < 0)
                {
                    throw new UserInputException("Please select a partnership to sync.");
                }
                if(CustomDialog.Show(this,CustomDialog.MessageTemplate.YesNo,CustomDialog.MessageResponse.No, "Are you sure you want to sync this partnership?") == CustomDialog.MessageResponse.Yes)
                {
                    Partnership partnershipSelected = (Partnership)partnershipList.SelectedValue;

                    AsyncStartSync(partnershipSelected.Name);
                }
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }

        }

        /// <summary>
        /// Syncing after creation of a partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncThisPartnership_Click(object sender, RoutedEventArgs e)
        {
            ResolvedConflicts = new List<Resolved>();
            if(CustomDialog.Show(this,CustomDialog.MessageTemplate.YesNo,CustomDialog.MessageResponse.No,"Are you sure you want to sync now?") == CustomDialog.MessageResponse.Yes)
            {
                AsyncStartSync(NewPartnershipName);
            }

        }

        private void SavePartnership_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderOnePath = folderOneTextBox.Text.Trim();
                checkInput(folderOnePath);
                string folderTwoPath = folderTwoTextBox.Text.Trim();
                checkInput(folderTwoPath);
                ValidateFoldersHierachy(folderOnePath, folderTwoPath);

                string partnershipName = partnershipNameTextBox.Text.Trim();
                if (partnershipName.Equals(""))
                {
                    throw new UserInputException("Please input a partnership name");
                }
                sourceFolderPath.Text = folderOnePath;
                destinationFolderPath.Text = folderTwoPath;
                partnerShipName.Text = partnershipName;
				NewPartnershipName = partnershipName;
                this.Controller.UpdatePartnership(oldPartnershipName, partnershipName, folderOnePath, folderTwoPath);

                VisualStateManager.GoToState(this, "EditDoneState", false);
                CurrentState = State.EditDone;
                partnershipList.Items.Refresh();
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }
        }


        /// <summary>
        /// go to the 1st page of edit partnership to set source Textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void GoToEditPartnership_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (partnershipList.SelectedIndex < 0)
                {
                    throw new UserInputException("Please select a partnership to edit.");
                }
                Partnership currentPartnership = (Partnership)this.partnershipList.SelectedItem;
                folderOneTextBox.Text = currentPartnership.LeftFullPath;
                folderTwoTextBox.Text = currentPartnership.RightFullPath;
                partnershipNameTextBox.Text = currentPartnership.Name;
                oldPartnershipName = currentPartnership.Name;
                VisualStateManager.GoToState(this, "EditPartnershipState", false);
                CurrentState = State.Edit;
                FocusControl(() => folderOneTextBox.Focus());
            }
            catch (UserInputException uIException)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, uIException.Message);
            }

        }

        #endregion

        /// <summary>
        /// Syncs MRUs
        /// </summary>
        private void SyncMRU(bool SyncPartnershipIsNext)
        {


            // Background worker to do the actual work
            BackgroundWorker mruWorker = new BackgroundWorker();
            mruWorker.WorkerSupportsCancellation = true;

            // Progress bar window
            ProgressBar progressWindow = new ProgressBar(mruWorker, "Sync Butler, Sync!");

            bool cancelled = false;

            // Not using the Total progress indicator, so hide it.
            progressWindow.HideTotalProgress();

            mruWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            { // Code to run on completion
                if (!cancelled)
                {
                    if(!SyncPartnershipIsNext)
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Files were successfully copied to: \r\n" + Controller.GetSBSPath() + "\r\n\r\nA log was also been made in:\r\n" + Controller.GetInstance().SBSLogFile);
                }
                progressWindow.TaskComplete();

            });

            mruWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
            { // Actual work gets done here
                BackgroundWorker workerObj = (BackgroundWorker)worker;
                ProgressBar.ProgressBarInfo pinfo;

                // Show some initial information on the progress window
                pinfo.SubTaskPercent = 0;
                pinfo.TotalTaskPercent = 0;
                pinfo.taskDescription = "Starting to copy your recent files ...";
                workerObj.ReportProgress(0, pinfo);

                try
                {
                    this.Controller.SyncMRUs(MRUs["interesting"], delegate(SyncableStatus status)
                    { // Status reporting - triggers whenever SyncMRU has made progress

                        pinfo.SubTaskPercent = status.CurTaskPercentComplete;
                        pinfo.TotalTaskPercent = 0;
                        pinfo.taskDescription = status.EntityPath;
                        // Report the progress back to the progress bar
                        workerObj.ReportProgress(0, pinfo);

                        // User requested for cancellation
                        if (workerObj.CancellationPending)
                        {
                            cancelled = true;
                            return false;
                        }
                        else return true;
                    },
                    delegate(Exception exp)
                    { // Error handler - triggers whenever an exception is raised anywhere in SyncMRU

                        CustomDialog.MessageBoxInfo info = new CustomDialog.MessageBoxInfo();
                        if (exp.Message.Contains("Device not detected"))
                        {
                            info.message = exp.Message;
                            info.messageType = CustomDialog.MessageType.Error;
                            info.messageTemplate = CustomDialog.MessageTemplate.OkOnly;
                            info.parent = this;
                            progressWindow.RequestMessageDialog(workerObj, info);
                            cancelled = true;
                            return false;
                        }
                        else if (exp.Message.Contains("Permisson denied"))
                        {
                            info.message = exp.Message;
                            info.messageType = CustomDialog.MessageType.Error;
                            info.messageTemplate = CustomDialog.MessageTemplate.OkOnly;
                            info.parent = this;
                            progressWindow.RequestMessageDialog(workerObj, info);
                            cancelled = true;
                            return false;
                        }
                        else if (exp is SyncButler.Exceptions.UserCancelledException)
                        {
                            return false;
                        }
                        else
                        {
                            // Define the parameters of the message box to show the user

                            info.message = "I encountered a problem while syncing:\n\n" + exp.Message + "\n\nWhat would you like me to do?";
                            info.messageType = CustomDialog.MessageType.Error;
                            info.messageTemplate = CustomDialog.MessageTemplate.SkipCancel;
                            info.parent = this;

                            // Actually show the message box and respond to the even.
                            // Note: You cannot call CustomDialog directly here, the UI runs in a different thread.
                            if (progressWindow.RequestMessageDialog(workerObj, info) == CustomDialog.MessageResponse.Cancel)
                            {
                                cancelled = true;
                                return false;
                            }
                            else return true;
                        }
                    });
                }
                catch (UserCancelledException)
                {
                    cancelled = true;
                    return;
                }
            });

            // Start the whole process
            progressWindow.Start();
        }
        private void SBSSettingCancel(object sender, RoutedEventArgs e)
        {
            if (Controller.IsSBSEnable())
            {
                VisualStateManager.GoToState(this, "SbsState", false);
                LoadMRUs();
                CurrentState = State.SBS;
            }
            else
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Sync Butler, Sync! is currently not enabled.\r\n\r\nYou may turn on the feature later by click on the Sync Butler, Sync! button.");
                FirstTimeHelp.Visibility = Visibility.Hidden;
                VisualStateManager.GoToState(this, "HomeState", false);
                CurrentState = State.Home;
            }
            FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
        }
        private void SaveSetting(object sender, RoutedEventArgs e)
        {

            string ComputerName = this.ComputerNameTextBox.Text.Trim();
            string SBSEnable = (string)this.SBSSettingComboBox.SelectedItem;
            char DriveLetter = ((WindowDriveInfo)this.SBSWorkingDriveComboBox.SelectedItem).GetDriveLetter();
            double FreeSpaceToUse = double.Parse(this.LastWorkingFreeSpace);
            string Resolution = this.resolutionLabel.Content.ToString();
            double userrequestedspace = CalcuateUserRequestedSpace();

            if (SBSEnable.Equals("Enable") && userrequestedspace < 250 * MEGA_BYTE && userrequestedspace > 0)
            {
                if (CustomDialog.Show(this, CustomDialog.MessageTemplate.YesNo, CustomDialog.MessageResponse.No, "Sync Butler needs at least 250MB on your storage device to carry more of your recent files. It may not be able to carry the files you need, when you need them. Do you want to give Sync Bulter more storage space!") ==
                    CustomDialog.MessageResponse.No)
                {
                    Controller.SaveSetting(ComputerName, SBSEnable, DriveLetter, FreeSpaceToUse, Resolution);
                    
                    String ExtraMsg = String.Format("Sync Butler, Sync! will now save your recent files to:\n\n{0}", Controller.GetSBSPath());
                    CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "The settings has been changed.\r\n\r\n" + ExtraMsg);
                    CurrentState = State.SBS;
                    FirstTimeHelp.Visibility = Visibility.Hidden;
                    VisualStateManager.GoToState(this, "SbsState", false);
                    
                    LoadMRUs();
                    
                }
            }
            else if(SBSEnable.Equals("Enable") &&userrequestedspace <= 0)
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please give Sync Bulter some storage space to work with!");
            }
            
            else if (!ComputerNameChecker.IsComputerNameValid(ComputerName))
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, ComputerName + " is not a valid name");
            }
            else
            {
                if (!Directory.Exists(DriveLetter + ":\\") && SBSEnable.Equals("Enable"))
                {
                    CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Please check your the status of your portable storage device.\nSync Butler was unable to read it.");
                }
                else
                {
                    Controller.SaveSetting(ComputerName, SBSEnable, DriveLetter, FreeSpaceToUse, Resolution);
                    FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
                    if (SBSEnable.Equals("Enable"))
                    {
                        String ExtraMsg = String.Format("Sync Butler, Sync! will now save your recent files to:\n\n{0}", Controller.GetSBSPath());
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "The settings has been changed.\r\n\r\n" + ExtraMsg);
                        FirstTimeHelp.Visibility = Visibility.Hidden;
                        VisualStateManager.GoToState(this, "SbsState", false);
                        LoadMRUs();
                        CurrentState = State.SBS;
                    }
                    else
                    {
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Sync Butler, Sync! is currently not enabled.\r\n\r\nYou may turn on the feature later by click on the Sync Butler, Sync! button.");
                        VisualStateManager.GoToState(this, "HomeState", false);
                        CurrentState = State.Home;
                    }
                    

                }
            }

        }

        private void SBSSettingChanged(object sender, RoutedEventArgs e)
        {
            if (this.SBSSettingComboBox.SelectedItem != null)
            {
                this.SBSWorkingDriveComboBox.IsEnabled = this.SBSSettingComboBox.SelectedItem.Equals("Enable");
                if (this.SBSSettingComboBox.SelectedItem.Equals("Enable"))
                {
                    char driveletter = ((WindowDriveInfo)SBSWorkingDriveComboBox.SelectedItem).GetDriveLetter();
                    if (Directory.Exists(driveletter + ":\\"))
                    {
                        this.SpaceToUseSlide.IsEnabled = true;
                        this.SpaceToUseTextbox.IsEnabled = true;
                        SBSUpdateSpaceDetails(null, null);
                    }
                    else
                    {
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "A Portable Storage was not found.\nPlease check if the device is plugged in.");
                    }
                }
                else
                {
                    this.SpaceToUseSlide.Value = 0;
                    this.resolutionLabel.Content = "KB";
                    this.SpaceToUseSlide.IsEnabled = false;
                    this.SpaceToUseTextbox.IsEnabled = false;
                }

            }
        }

        private void SBSUpdateSpaceDetails(object sender, RoutedEventArgs e)
        {
            int minimumSize = 250;
            if (this.SBSSettingComboBox.SelectedIndex != -1 &&
                this.SBSSettingComboBox.SelectedItem.Equals("Enable") && !IsLoadingSBS)
            {
                if (SBSWorkingDriveComboBox.SelectedIndex != -1)
                {
                    SpaceToUseSlide.Value = 0;
                    DriveInfo di = new DriveInfo("" + ((WindowDriveInfo)SBSWorkingDriveComboBox.SelectedItem).GetDriveLetter());
                    SpaceToUseSlide.IsEnabled = true;
                    SpaceToUseTextbox.IsEnabled = true;
                    long freespace = di.AvailableFreeSpace;

                    if (freespace / GIGA_BYTE > 10)
                    {
                        resolutionLabel.Content = Volume.GB;
                        SpaceToUseSlide.Maximum = freespace / GIGA_BYTE;

                    }
                    else if (freespace / GIGA_BYTE <= 10 && freespace / MEGA_BYTE >= 2)
                    {
                        resolutionLabel.Content = Volume.MB;
                        SpaceToUseSlide.Maximum = freespace / MEGA_BYTE;
                    }
                    else if (freespace / MEGA_BYTE < 2 )
                    {
                        resolutionLabel.Content = Volume.KB;
                        SpaceToUseSlide.Maximum = freespace / KILO_BYTE;
                    }
                    else
                    {
                        resolutionLabel.Content = Volume.Bytes;
                        SpaceToUseSlide.Maximum = freespace;
                    }
                    if (freespace <= minimumSize * MEGA_BYTE)
                    {
                        CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                            "Sync Butler needs at least 250MB on your storage device to carry your recent files.\r\nIt may not be able to carry the files you need, when you need them.\r\nPlease use a device with a bigger space");
                    }
                        int preferredSize = (int) (freespace * 0.1 / MEGA_BYTE);
                        if (preferredSize < minimumSize) preferredSize = minimumSize;

                        Volume curRes = (Volume)resolutionLabel.Content;
                        switch (curRes)
                        {
                            case Volume.MB:
                                SpaceToUseSlide.Value = preferredSize;
                                break;
                            case Volume.KB:
                                SpaceToUseSlide.Value = preferredSize * KILO_BYTE;
                                break;
                            case Volume.GB:
                                SpaceToUseSlide.Value = preferredSize * GIGA_BYTE;
                                break;

                        }
                }
            }
        }

        private void SpaceToUseChanged(Object sender, KeyEventArgs e)
        {
            if (SpaceToUseTextbox.Text.Trim().Length != 0)
            {
                int current = SpaceToUseTextbox.SelectionStart;
                try
                {
                    SpaceToUseTextbox.Text = double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture).ToString();
                    if (double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture) <= SpaceToUseSlide.Maximum)
                    {
                        SpaceToUseTextbox.Text = String.Format("{0:F2}", SpaceToUseTextbox.Text);
                        SpaceToUseSlide.Value = double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture);
                    }
                    else if (double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture) > SpaceToUseSlide.Maximum)
                    {
                        SpaceToUseTextbox.Text = String.Format("{0:F2}", SpaceToUseSlide.Maximum);
                        SpaceToUseSlide.Value = SpaceToUseSlide.Maximum;
                    }
                    LastWorkingFreeSpace = String.Format("{0:F2}", double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture));

                }
                catch (FormatException)
                {
                    // fall back to the last working value
                    SpaceToUseTextbox.Text = String.Format("{0:F2}", LastWorkingFreeSpace);
                }

                SpaceToUseTextbox.SelectionStart = current;
                SpaceToUseTextbox.SelectionLength = 0;
                SpaceToUseTextbox.Focus();
            }
        }
        private double CalcuateUserRequestedSpace()
        {
            double value = double.Parse(this.SpaceToUseTextbox.Text);
            Volume curRes;
                if(resolutionLabel.Content is Volume)
                    curRes = (Volume)resolutionLabel.Content;
                else if(resolutionLabel.Content is String)
                    curRes = (Volume)Enum.Parse(typeof(Volume),(String)resolutionLabel.Content,true);
                else
                    return 0.0;
            switch (curRes)
            {
                case Volume.MB:
                    return value * MEGA_BYTE;
                case Volume.KB:
                    return value * KILO_BYTE;
                case Volume.GB:
                    return value * GIGA_BYTE;
            }
            return value;
        }

        private void SpaceToUseSlided(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            SpaceToUseTextbox.Text = String.Format("{0:F2}", value);
            LastWorkingFreeSpace = SpaceToUseTextbox.Text;

        }
        private void DefaultSetting(object sender, RoutedEventArgs e)
        {
            this.ComputerNameTextBox.Text = "Computer1";
            this.SBSSettingComboBox.SelectedItem = "Disable";
            this.SBSWorkingDriveComboBox.SelectedIndex = 0;
            this.SpaceToUseSlide.Value = 0;
            this.SpaceToUseSlide.IsEnabled = false;
            this.resolutionLabel.Content = "KB";
            this.SpaceToUseTextbox.IsEnabled = false;

        }
        /// <summary>
        /// Checks the sourceTextbox for values if its empty or if the directory exists
        /// </summary>
        private static void checkInput(string folderPath)
        {
            if (folderPath.Length > 266)
            {
                throw new UserInputException("The path to the folder is too long.");
            }
            else if (String.IsNullOrEmpty(folderPath))
            {
                throw new UserInputException("Please select a folder before continuing.");
            }
            else if (!Directory.Exists(folderPath))
            {
                throw new UserInputException("The folder you have given does not exist.");
            }
            else if (folderPath[0] != '\\')
            {
                DriveInfo di = new DriveInfo("" + folderPath[0]);
                if (di.DriveType == DriveType.CDRom)
                {
                    throw new UserInputException("Syncing with a CD ROM is not supported in this version");
                }
            }

        }
        private void ValidateFoldersHierachy(string path1, string path2)
        {
            FileInfo sourceFI = new FileInfo(path1);
            FileInfo destFI = new FileInfo(path2);
            char[] standard = { '\\', ' ' };
            string tempfolder1Name = sourceFI.FullName.TrimEnd(standard).ToLower() + "\\";
            string tempfolder2Name = destFI.FullName.TrimEnd(standard).ToLower() + "\\";

            if (tempfolder2Name.Equals(tempfolder1Name))
            {
                throw new UserInputException("The same folders were selected.\n\nPlease pick another folder.");
            }
            else if (tempfolder1Name.IndexOf(tempfolder2Name) == 0)
            {
                throw new UserInputException("The 1st folder is a subfolder of the 2nd folder.\n\nPlease select another folder.");
            }
            else if (tempfolder2Name.IndexOf(tempfolder1Name) == 0)
            {
                throw new UserInputException("The 2nd folder is a subfolder of the 1st folder.\n\nPlease select another folder.");
            }
        }

        public void LoadMRUs()
        {
            FindMRUs(false);
            Favourites_List.ItemsSource = MRUs["interesting"].Keys;
            Favourites_List.Items.Refresh();
        }

        /// <summary>
        /// Find a list of mru base on the space limit imposed by the user 
        /// or if any reason tt sbs is disable, a 250mb limit is imposed
        /// </summary>
        /// <param name="preview">If this list of mru is for preview or not</param>
        private void FindMRUs(bool preview)
        {

            if (!Controller.IsSBSEnable())
            {
                this.SBSPathLabel.Visibility = Visibility.Hidden;
                this.SBSPathTextBlock.Visibility = Visibility.Hidden;
				this.SBSWarningImage.Visibility = Visibility.Visible;
				this.SBSfeatureDisabledTextBlock.Visibility = Visibility.Visible;
                this.SBSDriveNotPluggedInDisabledTextBlock.Visibility = Visibility.Hidden;
                preview = true;
            }
            else
            {
                if (Controller.CanDoSBS())
                {

                    this.SBSWarningImage.Visibility = Visibility.Hidden;
                    this.SBSPathTextBlock.Visibility = Visibility.Visible;
                    this.SBSPathLabel.Content = Controller.GetSBSPath();
                    this.SBSPathLabel.Visibility = Visibility.Visible;
                    this.SBSfeatureDisabledTextBlock.Visibility = Visibility.Hidden;
                    this.SBSDriveNotPluggedInDisabledTextBlock.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.SBSPathLabel.Visibility = Visibility.Hidden;
                    this.SBSPathTextBlock.Visibility = Visibility.Hidden;
                    this.SBSWarningImage.Visibility = Visibility.Visible;
					this.SBSfeatureDisabledTextBlock.Visibility = Visibility.Hidden;
                    this.SBSDriveNotPluggedInDisabledTextBlock.Visibility = Visibility.Visible;
					preview = true;
                }
                preview = false;
            }

            BackgroundWorker sbsScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(sbsScanWorker, "Loading Sync Butler, Sync!");
            progressWindow.HideTotalProgress();

            sbsScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
            {
                ProgressBar.ProgressBarInfo pinfo;
                pinfo.SubTaskPercent = 0;
                pinfo.taskDescription = "Searching for files...";
                pinfo.TotalTaskPercent = 0;
                ((BackgroundWorker)worker).ReportProgress(0, pinfo);

                MRUs = Controller.GetInstance().GetMonitoredFiles(delegate(SyncableStatus status)
                {
                    pinfo.SubTaskPercent = status.CurTaskPercentComplete;
                    ((BackgroundWorker)worker).ReportProgress(0, pinfo);
                    return true;
                }, preview);

                pinfo.SubTaskPercent = 100;
                pinfo.taskDescription = "Finishing...";
                ((BackgroundWorker)worker).ReportProgress(0, pinfo);
            });

            sbsScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            {
                progressWindow.TaskComplete();
            });

            progressWindow.Start();
        }

        private void ShowResult(object sender, EventArgs e)
        {
            VisualStateManager.GoToState(this, "ResultState", false);
            CurrentState = State.Result;
            SyncResultListBox.ItemsSource = ResolvedConflicts;
        }

        private void SourceTextBox_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                switch (CurrentState)
                {
                    case State.Create:
                        CreatePartnership_Click(sender, e);
                        break;
                    case State.Edit:
                        SavePartnership_Click(sender, e);
                        break;

                }
            }
        }
        private void PartnershipNameTextBox_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                switch (CurrentState)
                {
                    case State.Create:
                        CreatePartnership_Click(sender, e);
                        break;
                    case State.Edit:
                        SavePartnership_Click(sender, e);
                        break;
                }
            }
        }
        private void GetFolderOneUserPath(object sender, RoutedEventArgs e)
        {
            String FolderPath;

            FolderPath = GetPath(this.folderOneTextBox.Text.Trim());
            if (FolderPath != null)
            {
                this.folderOneTextBox.Text = FolderPath;
            }
        }
        private void GetFolderTwoUserPath(object sender, RoutedEventArgs e)
        {
            String FolderPath;

            FolderPath = GetPath(this.folderTwoTextBox.Text.Trim());
            if (FolderPath != null)
            {
                this.folderTwoTextBox.Text = FolderPath;
            }
        }


        /// <summary>
        /// If there's a scan running in HomeWindowControl, ask the user
        /// if he wants to cancel. 
        /// </summary>
        /// <returns>True if the operation is stopped or cancelled</returns>
        public bool StopExistingOperation()
        {
            if (!IsBusy()) return true;

            CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                "I am working on something in this screen at the moment. Please click on Cancel Sync if you wish to leave this screen.");

            return false;
        }
        
        private void OpenSelectedOnList(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                String path = "";
                if (Favourites_List.SelectedIndex != -1)
                {
                    path = MRUs["interesting"][(String)Favourites_List.SelectedItem];
                }
                if (path.Length != 0 && File.Exists(path))
                    Controller.OpenFile(path);
            }
        }
        


        public void GoToSetting()
        {
            GoToSetting(null, null);
        }

        private void GoToSetting(object sender, RoutedEventArgs e)
        {
            CurrentState = State.Settings;
            if (!StopExistingOperation()) return;
            IsLoadingSBS = true;
            SpaceToUseSlide.Value = 0.0;
            #region show help if required
            if (sender != null && sender.GetType() == typeof(String) && (sender.Equals("FirstSBSRun")))
            {
                FirstTimeHelp.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                FirstTimeHelp.Visibility = System.Windows.Visibility.Hidden;
            }
            #endregion

            VisualStateManager.GoToState(this, "SettingsState", false);

            BackgroundWorker storageScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(storageScanWorker, "Retrieving your settings...", "Searching for Portable Storage Devices");
            progressWindow.HideTotalProgress();
            progressWindow.IsIndeterminate = true;

            List<WindowDriveInfo> DriveLetters = null;
            bool noUSBDrives = false;

            storageScanWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args)
            {
                #region Get List Of Device
                DriveLetters = Controller.GetUSBDriveLetters();

                // if there is no usb drive, let user to select some local hard disk, warn the user as well.
                if (DriveLetters.Count == 0)
                {
                    DriveLetters = Controller.GetNonUSBDriveLetters();
                    noUSBDrives = true;
                }
                #endregion
            });

            storageScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            {

                this.ComputerNameTextBox.Text = this.Controller.GetComputerName();
                this.NoUSBWarningTextBlock.Visibility = Visibility.Hidden;
                this.SBSWorkingDriveComboBox.Items.Clear();
                bool devicePluggedIn = false;
                if (noUSBDrives)
                {
                    #region if there is no usb device
                    this.NoUSBWarningTextBlock.Visibility = Visibility.Visible;
                    CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok,
                        "Please plug in a portable storage device if you wish to use it with\nSync Butler, Sync!");
                    if (this.SBSSettingComboBox.Items.IsEmpty)
                    {
                        this.SBSSettingComboBox.Items.Add("Enable");
                        this.SBSSettingComboBox.Items.Add("Disable");
                    }
                    this.SBSSettingComboBox.SelectedItem = "Disable";
                    this.SBSWorkingDriveComboBox.IsEnabled = false;
                    this.SpaceToUseTextbox.Text = "0.0";
                    this.SpaceToUseSlide.IsEnabled = false;
                    this.SpaceToUseTextbox.IsEnabled = false;
                    this.SBSSettingComboBox.IsEnabled = false;
                    this.DefaultSettingButton.IsEnabled = false;
                    this.SaveSettingButton.IsEnabled = false;
                    this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Hidden;
                    #endregion

                }
                else
                {
                    #region if some usb device is plugged in
                    this.SBSSettingComboBox.IsEnabled = true;
                    this.DefaultSettingButton.IsEnabled = true;
                    this.SaveSettingButton.IsEnabled = true;
                    foreach (WindowDriveInfo s in DriveLetters)
                    {
                        this.SBSWorkingDriveComboBox.Items.Add(s);
                    }
                    WindowDriveInfo wdi = Controller.GetSBSDriveLetter();

                    if (wdi != null)
                    {
                        #region if previously there is a sbs drive letter
                        if (this.SBSWorkingDriveComboBox.Items.Contains(Controller.GetSBSDriveLetter()))
                        {
                            this.SBSWorkingDriveComboBox.SelectedItem = Controller.GetSBSDriveLetter();
                            devicePluggedIn = true;
                        }
                        else if (this.SBSWorkingDriveComboBox.Items.Count != 0)
                        {
                            this.SBSWorkingDriveComboBox.SelectedIndex = 0;
                        }
                        #endregion
                    }
                    else if (this.SBSWorkingDriveComboBox.Items.Count != 0)
                    {
                        this.SBSWorkingDriveComboBox.SelectedIndex = 0;
                    }

                    
                    if (devicePluggedIn)
                    {
                        #region if the previously assigned device is plugged in
                        if (Controller.SBSEnable.Equals("Enable"))
                        {
                            this.SpaceToUseSlide.Maximum = this.Controller.GetAvailableSpaceForDrive();
                            this.SpaceToUseSlide.Value = this.Controller.GetFreeSpaceToUse();
                            this.resolutionLabel.Content = this.Controller.GetResolution();
                            this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            this.SpaceToUseSlide.Value = 0;
                            this.resolutionLabel.Content = "KB";
                            this.SpaceToUseSlide.IsEnabled = false;
                            this.SpaceToUseTextbox.IsEnabled = false;
                        }


                        this.SBSWorkingDriveComboBox.IsEnabled = true;
                        this.SBSSettingComboBox.Items.Clear();
                        this.SBSSettingComboBox.Items.Add("Enable");
                        this.SBSSettingComboBox.Items.Add("Disable");
                        this.SBSSettingComboBox.SelectedItem = Controller.SBSEnable;
                        #endregion
                    }
                    

                    else
                    {
                        #region if device is not plugged in
                        this.SBSWorkingDriveComboBox.IsEnabled = false;
                        this.SBSSettingComboBox.Items.Clear();
                        this.SBSSettingComboBox.Items.Add("Enable");
                        this.SBSSettingComboBox.Items.Add("Disable");
                        this.SBSSettingComboBox.SelectedItem = "Disable";
                        this.SpaceToUseTextbox.Text = "0.0";
                        if (this.SBSWorkingDriveComboBox.Items.Count > 0 && Controller.IsSBSEnable())
                            this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Visible;
                        else
                            this.SBSSettingDeviceNotFoundTextBox.Visibility = Visibility.Hidden;
                        #endregion
                    }
                    this.IsLoadingSBS = false;
                    #endregion
                }
                progressWindow.TaskComplete();
            });

            progressWindow.Start();
        }
    }
}