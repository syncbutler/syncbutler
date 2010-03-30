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

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for HomeWindowControl.xaml
	/// </summary>
	public partial class HomeWindowControl : UserControl
	{
        protected enum ErrorReportingSource { Scanner, Resolver }

        protected struct ErrorReportingMessage
        {
            public Exception exceptionThrown;
            public ErrorReportingSource source;
            public Object failedObject;

            public ErrorReportingMessage(Exception exceptionThrown, ErrorReportingSource source, Object failedObject)
            {
                this.exceptionThrown = exceptionThrown;
                this.source = source;
                this.failedObject = failedObject;
            }
        }
    
	#region fields&Attributes 
    /// <summary>
    /// Defines the size of a single increment of the progress bar.
    /// </summary>
    //private int progressBarIncrement = 5;
	public ObservableCollection<ConflictList> mergedList;		
    private object dummyNode = null;
    public string SelectedImagePath { get; set; }
	public SyncButler.Controller Controller{get;set;}
	public enum State{Home,Page1OfCreate,Page2OfCreate,Page3OfCreate,Page4OfCreate,ViewPartnership,SBS,Conflict,Settings,Page1OfEdit,Page2OfEdit,Page3OfEdit,Page4OfEdit,Result};
    private string LastWorkingFreeSpace = "0.00";
	private State CurrentState;
    private const long GIGA_BYTE = 1024 * 1024 * 1024;
    private const long MEGA_BYTE = 1024 * 1024;
    private const long KILO_BYTE = 1024;
	#endregion

        public enum CurrentActionEnum { Scanning, Resolving, Idle }
        CurrentActionEnum CurrentAction = CurrentActionEnum.Idle;

        private string NewPartnershipName = "";

        int conflictsProcessed = 0;
        BackgroundWorker resolveWorker = null;
        BackgroundWorker scanWorker = null;
        bool operationCancelled = false;
        private Semaphore resolveLock = new Semaphore(1, 1);
        private Semaphore waitForErrorResponse = new Semaphore(0, 1);
        private Queue<Conflict> newConflicts = new Queue<Conflict>();
		
		public HomeWindowControl()
		{
			this.InitializeComponent();
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
        protected void DisplayProgress(Object workerObj, ProgressChangedEventArgs args) 
        {
            if (args.UserState is String)
            {
                PartnershipName.Text = (String)args.UserState;
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
                    //// Triage for Issue #83
                    //message = msg.exceptionThrown.Message + "\n\nSyncButler is unable to continue";

                    message = msg.exceptionThrown.Message + "\n\nWhat would you like me to do?";
                    msgTemplate = CustomDialog.MessageTemplate.SkipCancel;
                }
                else throw new NotImplementedException();
                
                switch (CustomDialog.Show(this, msgTemplate, CustomDialog.MessageType.Error, CustomDialog.MessageResponse.Cancel, message))
                {
                    case CustomDialog.MessageResponse.Cancel:
                        ((BackgroundWorker)workerObj).CancelAsync();
                        break;
                    case CustomDialog.MessageResponse.Retry:
                        System.Diagnostics.Debug.Assert(msg.source == ErrorReportingSource.Resolver, "Cannot Retry errors nor generated during conflict resolution");
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

            switch (status.actionType)
            {
                case SyncableStatus.ActionType.Checksum:
                case SyncableStatus.ActionType.Sync: 
                    verb = "Scanning: "; 
                    break;

                case SyncableStatus.ActionType.Copy: verb = "Copying: "; break;
                case SyncableStatus.ActionType.Delete: verb = "Deleting: "; break;
            }

            CurrentSyncingFile.Text = verb + status.EntityPath;
            SubProgressBar.Value = status.curTaskPercentComplete;

            if (CurrentAction == CurrentActionEnum.Resolving)
            {
                int processed = (conflictsProcessed > 0) ? conflictsProcessed - 1 : conflictsProcessed;

                TotalProgressBar.Value = (int)(((processed * 100) + status.curTaskPercentComplete) 
                    / (processed + newConflicts.Count + 1));
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
            AsyncStartSync(singletonList);
        }

        /// <summary>
        /// Starts a BackgroundWorker object for a Syncing (ie. Scan);
        /// </summary>
        /// <param name="partnershipNames">A collection of partnerships to scan</param>
        /// <returns></returns>
        private void AsyncStartSync(IEnumerable<string> partnershipNames)
        {

            VisualStateManager.GoToState(this, "ConflictState1", false);
            CurrentState = State.Conflict;
            if (scanWorker != null)
            {
                showMessageBox(CustomDialog.MessageType.Error, "There is already a scan " +
                    "in progress. Please stop the current scan before starting another.");
                
                return;
            }

            operationCancelled = false;
            conflictsProcessed = 0;

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
            CurrentSyncingFile.Text = "Initializing scan...";
            PartnershipName.Text = "";

            scanWorker.ProgressChanged += new ProgressChangedEventHandler(DisplayProgress);

            scanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object workerObj, RunWorkerCompletedEventArgs args)
            {
                if (operationCancelled)
                {
                    showMessageBox(CustomDialog.MessageType.Message, "Scan cancelled");
                    GoHome();

                    scanWorker = null;
                    CancelButton.IsEnabled = false;
                    TotalProgressBar.Value = 0;
                    SubProgressBar.Value = 0;

                    return;
                }

                List<Conflict> autoResolveConflicts = new List<Conflict>();

                foreach (ConflictList cl in mergedList)
                {
                    autoResolveConflicts.AddRange(this.Controller.RemoveAutoResolvableConflicts(cl));
                }

                ConflictList.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                ConflictList.ItemsSource = mergedList;
                ConflictList.Items.Refresh();
                ConflictList.IsEnabled = true;
                resolveButton.IsEnabled = true;
                doneButton.IsEnabled = true;
                TotalProgressBar.Value = 0;
                SubProgressBar.Value = 0;

                CurrentSyncingFile.Text = "Scan complete. Please look at the list of conflicts.";

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
                            worker.ReportProgress(status.percentComplete, status);
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
                        });

                        worker.ReportProgress(100, null);
                        mergedList.Add(cl);
                        this.Controller.CleanUpOrphans(friendlyName);
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        return;
                    }

                }
            });

            scanWorker.RunWorkerAsync();
            CurrentAction = CurrentActionEnum.Scanning;

            return;
        }
        List<Resolved> ResolvedConflicts = new List<Resolved>();
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

            TotalProgressBar.IsIndeterminate = true;
            SubProgressBar.IsIndeterminate = true;
            CurrentSyncingFile.Text = "Getting ready to resolve conflicts...";
            PartnershipName.Text = "";

            TotalProgressBar.Visibility = Visibility.Visible;
            CancelButton.IsEnabled = true;
            doneButton.IsEnabled = false;

            resolveWorker.DoWork += new DoWorkEventHandler(delegate(Object workerObj, DoWorkEventArgs args)
            {
                BackgroundWorker worker = (BackgroundWorker)workerObj;

                SyncableStatusMonitor reporter = delegate(SyncableStatus status)
                {
                    worker.ReportProgress(status.percentComplete, status);
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

                        ResolvedConflicts.Add(this.Controller.ResolveConflict(curConflict, reporter, worker));
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        return;
                    }
                    catch (IOException e)
                    {
                        exp = new Exception("There was a problem accessing a file while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        exp = new Exception("A permissions error was encountered while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (System.Security.SecurityException e)
                    {
                        exp = new Exception("A permissions error was encountered while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (InvalidActionException e)
                    {
                        exp = new Exception("An invalid action occurred while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (Exception e)
                    {
                        exp = new Exception("An problem was encountered while processing " + partnershipName + ": " + e.Message);
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
                CurrentSyncingFile.Text = "Scan complete. Conflicts processed so far: " + conflictsProcessed;
                partnershipNameTextBox.Text = "";

                if (operationCancelled)
                {
                    showMessageBox(CustomDialog.MessageType.Message, "Sync cancelled");
                    resolveButton.IsEnabled = true;
                    resolveWorker = null;
                    CancelButton.IsEnabled = false;
                    TotalProgressBar.Value = 0;
                    SubProgressBar.Value = 0;
                    return;
                }

                TotalProgressBar.Value = 0;
                SubProgressBar.Value = 0;

                resolveWorker = null;

                CancelButton.IsEnabled = false;
                doneButton.IsEnabled = true;
                if (newConflicts.Count > 0) AsyncStartResolve();

            });

            resolveWorker.RunWorkerAsync();
            CurrentAction = CurrentActionEnum.Resolving;

        }

	#region UIcode
	/// <summary>
    /// Interaction logic for Creating Partnership
    /// </summary>
		#region TreeView
		/// <summary>
		/// Populate the tree view with storage devices that are ready
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void clearTreeView()
        {
            try{
			    foldersItem.Items.Clear();
                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (d.IsReady)
                    {
                        string s = d.Name;
                        TreeViewItem item = new TreeViewItem();
                        item.Header = s;
                        item.Tag = s;
                        item.FontWeight = FontWeights.Normal;
                        item.Items.Add(dummyNode);
                        item.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Collapsed += new RoutedEventHandler(folder_Collapsed);

                        foldersItem.Items.Add(item);

                    }
                }
            }
            catch (Exception) { }

        }

        private void editPartnershipTreeView()
        {

        }
	
		/// <summary>
		/// remove subItems of the list and repopulate when collasped
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void folder_Collapsed(object sender, RoutedEventArgs e)
		{
            try
            {
                TreeViewItem item = (TreeViewItem)sender;
                item.Items.Clear();
                foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                {
                    TreeViewItem subitem = new TreeViewItem();
                    subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                    subitem.Tag = s;
                    subitem.FontWeight = FontWeights.Normal;
                    subitem.Items.Add(dummyNode);
                    subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                    subitem.Collapsed += new RoutedEventHandler(folder_Collapsed);
                    item.Items.Add(subitem);
                }
            }
            catch (Exception) { }
		}
		/// <summary>
		/// populate the list when folder is expanded
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

        private void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
						subitem.Collapsed += new RoutedEventHandler(folder_Collapsed);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception) { }
            }
        }
		
		/// <summary>
		/// populate the textbox with current selected value when folder is expanded
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void foldersItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tree = (TreeView)sender;
            TreeViewItem temp = ((TreeViewItem)tree.SelectedItem);

            if (temp == null)
                return;
            SelectedImagePath = "";
            string temp1 = "";
            string temp2 = "";
            while (true)
            {
                temp1 = temp.Header.ToString();
                if (temp1.Contains(@"\"))
                {
                    temp2 = "";
                }
                SelectedImagePath = temp1 + temp2 + SelectedImagePath;
                if (temp.Parent.GetType().Equals(typeof(TreeView)))
                {
                    break;
                }
                temp = ((TreeViewItem)temp.Parent);
                temp2 = @"\";
            }
            //show user selected path
			//destinationTextBox.Text=SelectedImagePath;
          	sourceTextBox.Text=SelectedImagePath;
       }
		#endregion

        private void GoHome()
        {
            VisualStateManager.GoToState(this, "Home", false);
            CurrentState = State.Home;
        }

		#region createPartnership
		
		/// <summary>
		/// Goes the 2nd Page of Create Partnership to set Destination Values
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToPartnershipDest_Click(object sender, RoutedEventArgs e){
		    try{
			    checkInput();
			    PartnershipTempData.sourcePath=sourceTextBox.Text;
		        clearTreeView();	
		        sourceTextBox.Text=PartnershipTempData.destinationPath;
			    VisualStateManager.GoToState(this,"CreatePartnershipState2",false);
				CurrentState = State.Page2OfCreate;
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		}
		
		/// <summary>
		/// go to the 1st page of create partnership to set source Textbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		
		private void GoToCreatePartnership_Click(object sender, RoutedEventArgs e){
		   clearTreeView();
           new PartnershipTempData();
		   VisualStateManager.GoToState(this,"CreatePartnershipState1",false);
			CurrentState = State.Page1OfCreate;
           this.sourceTextBox.Clear();
            
		}
		
		/// <summary>
		/// goes back to the 1st page from the 2nd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoBackToCreatePartnershipSrc_Click(object sender, RoutedEventArgs e){
		  	try{

			PartnershipTempData.destinationPath=sourceTextBox.Text;
		    sourceTextBox.Text=PartnershipTempData.sourcePath;
		    clearTreeView();	
			VisualStateManager.GoToState(this,"CreatePartnershipState1",false);
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		}
		/// <summary>
		/// goes to the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToCreatePartnershipName_Click(object sender, RoutedEventArgs e){
			try{
			checkInput();
			PartnershipTempData.destinationPath=sourceTextBox.Text;
            ValidateFoldersHierachy();
			sourceTextBox1.Text=PartnershipTempData.sourcePath;
			destinationTextBox1.Text=PartnershipTempData.destinationPath;
			partnershipNameTextBox.Text=PartnershipTempData.partnershipName;	
			VisualStateManager.GoToState(this,"CreatePartnershipState3",false);
			CurrentState = State.Page3OfCreate;
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}	
		}
		/// <summary>
		/// goes back to the 2nd page from the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoBackToCreatePartnershipDes_Click(object sender, RoutedEventArgs e){
		   PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
		   destinationTextBox1.Text=PartnershipTempData.destinationPath;
		   clearTreeView();	
		   VisualStateManager.GoToState(this,"CreatePartnershipState2",false);
			CurrentState = State.Page2OfCreate;
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
                if(partnershipNameTextBox.Text.Equals(""))
                    throw new UserInputException("Please input a partnership name");	

                PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
                sourceFolderPath.Text=PartnershipTempData.sourcePath;
                destinationFolderPath.Text=PartnershipTempData.destinationPath;
                partnerShipName.Text=PartnershipTempData.partnershipName;
                NewPartnershipName = partnerShipName.Text;

                this.Controller.AddPartnership(partnerShipName.Text,sourceFolderPath.Text,destinationFolderPath.Text);
                
                VisualStateManager.GoToState(this,"CreatePartnershipDone1",false);
				CurrentState = State.Page4OfCreate;
                sourceTextBox1.Text="";
                destinationTextBox1.Text="";
                sourceTextBox.Text="";
                PartnershipTempData.clear();
                partnershipList.Items.Refresh();
            }
            catch (UserInputException uIException)
            {
                showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
            }
            catch (ArgumentException argEx) 
            {
                showMessageBox(CustomDialog.MessageType.Error, argEx.Message);
            }
		}
		#endregion
		/// <summary>
		/// goes back to Home state
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoHome(object sender, RoutedEventArgs e){
            GoHome();
		}
		
		/// <summary>
		/// goes to view
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void GoToViewPartnerships_Click(object sender, RoutedEventArgs e)
        {
			VisualStateManager.GoToState(this,"ViewPartnership1",false);
            CurrentState = State.ViewPartnership;
			SortedList<string,Partnership> partnershiplist = this.Controller.GetPartnershipList();
			this.partnershipList.ItemsSource = partnershiplist.Values;
            this.partnershipList.Items.Refresh();
			
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
		  	    if(partnershipList.SelectedIndex<0)
                {
                    throw new UserInputException("Please select a partnership to delete.");
			    }

			    if (showMessageBox(CustomDialog.MessageType.Question,
                    "Are you sure you want to delete the \"" + 
                    partnershipList.Items[partnershipList.SelectedIndex] + 
                    "\" partnership?") == true)
                {
				    this.Controller.DeletePartnership(partnershipList.SelectedIndex);
				    partnershipList.Items.Refresh();
			    }
			}
            catch(UserInputException uIException)
            {
					showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		}
		
		/// <summary>
		/// Executes upon clicking resolve partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ResolvePartnership_Click(object sender, RoutedEventArgs e){
            foreach(ConflictList cl in mergedList) ThreadSafeAddResolve(cl.conflicts);
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
			showMessageBox(CustomDialog.MessageType.Message,"Exploring New Features is still under construction!");
		}
		
		/// <summary>
        /// DEPRECEATED. Left behind to not break existing code. Start using CustomDialog.Show() instead.
		/// </summary>
		/// <param name="messagetype">MessageType Enumerator, to tell what kind of message it is: Error, Question, Warning, Message</param>
		/// <param name="msg">String msg to tell what message the error is</param>
		private bool showMessageBox(CustomDialog.MessageType messageType, string msg){
            CustomDialog.MessageResponse def = CustomDialog.MessageResponse.Ok;
            CustomDialog.MessageTemplate template = CustomDialog.MessageTemplate.OkOnly;

            if (messageType == CustomDialog.MessageType.Question)
            {
                template = CustomDialog.MessageTemplate.YesNo;
                def = CustomDialog.MessageResponse.Yes;
            }

            CustomDialog.MessageResponse ret = CustomDialog.Show(this, template, messageType, def, msg);
            return (ret == CustomDialog.MessageResponse.Yes) || (ret == CustomDialog.MessageResponse.Ok);
		}

        /// <summary>
        /// When the user clicks the Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (showMessageBox(CustomDialog.MessageType.Question, "Are you sure you want to stop this scan?"))
            {
                if (scanWorker != null) scanWorker.CancelAsync();
                if (resolveWorker != null) resolveWorker.CancelAsync();
            }
        }

     	/// <summary>
		/// Executes when SyncAll button is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Sync(object sender, RoutedEventArgs e)
		{
            ResolvedConflicts = new List<Resolved>();
		    if(this.Controller.GetPartnershipList().Count < 1)
            {
                if (showMessageBox(CustomDialog.MessageType.Question, "There are no partnerships for me to sync. Would you like to create one now?") == true)
                {
                    clearTreeView();
                    VisualStateManager.GoToState(this, "CreatePartnershipState1", false);
                    CurrentState = State.Page1OfCreate;

                }
                else return;
			}
            else if (showMessageBox(CustomDialog.MessageType.Question,"Are you sure you want to sync all partnerships?") == true)
            {
                AsyncStartSync(this.Controller.GetPartnershipList().Keys);
		    }
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

                if (showMessageBox(CustomDialog.MessageType.Question, "Are you sure you want to sync this partnership?") == true)
                {
                    Partnership partnershipSelected = (Partnership)partnershipList.SelectedValue;

                    AsyncStartSync(partnershipSelected.Name);
                }
            }
            catch (UserInputException uIException)
            {
                showMessageBox(CustomDialog.MessageType.Error, uIException.Message);
            }

        }
        
        /// <summary>
        /// Syncing after creation of a partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncThisPartnership_Click(object sender, RoutedEventArgs e){
            ResolvedConflicts = new List<Resolved>();
            if (showMessageBox(CustomDialog.MessageType.Question,"Are you sure you want to sync now?")) {
                AsyncStartSync(NewPartnershipName);
			}
      
		}
		
        private void SavePartnership_Click(object sender, RoutedEventArgs e)
        {
			try
            {
				if(partnershipNameTextBox.Text.Equals(""))
                {
			        throw new UserInputException("Please input a partnership name");	
				}
			    PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
			    sourceFolderPath.Text=PartnershipTempData.sourcePath;
			    destinationFolderPath.Text=PartnershipTempData.destinationPath;
		        partnerShipName.Text=PartnershipTempData.partnershipName;
			    
                this.Controller.UpdatePartnership(PartnershipTempData.oldPartnershipName,partnerShipName.Text,sourceFolderPath.Text,destinationFolderPath.Text);
			    
                VisualStateManager.GoToState(this,"EditPartnershipDone1",false);
                CurrentState = State.Page4OfEdit;
			    sourceTextBox1.Text="";
			    destinationTextBox1.Text="";
			    sourceTextBox.Text="";
		        PartnershipTempData.clear();
			    partnershipList.Items.Refresh();
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}	
        }

		
		/// <summary>
		/// go to the 1st page of edit partnership to set source Textbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		
		private void GoToEditPartnership_Click(object sender, RoutedEventArgs e){
		   clearTreeView();
			try{
		  	if(partnershipList.SelectedIndex<0){
                throw new UserInputException("Please select a partnership to edit.");
			}
		 	new PartnershipTempData((Partnership)this.partnershipList.SelectedItem);
			 PartnershipTempData.oldPartnershipName= PartnershipTempData.partnershipName;
			sourceTextBox.Text=PartnershipTempData.sourcePath;
			 VisualStateManager.GoToState(this,"EditPartnershipState1",false);
             CurrentState = State.Page1OfEdit;
			}catch(UserInputException uIException){
					showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		  
		}
		/// <summary>
		/// go to 2nd page of edit partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToEditPartnershipDest_Click(object sender, RoutedEventArgs e){
		    try{
			checkInput();
			PartnershipTempData.sourcePath=sourceTextBox.Text;
		    clearTreeView();	
		    sourceTextBox.Text=PartnershipTempData.destinationPath;
			VisualStateManager.GoToState(this,"EditPartnershipState2",false);
            CurrentState = State.Page2OfEdit;
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		}
		
		
		/// <summary>
		/// goes back to the 1st page from the 2nd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoBackToEditPartnershipSrc_Click(object sender, RoutedEventArgs e){
		  	try{

			PartnershipTempData.destinationPath=sourceTextBox.Text;
		    sourceTextBox.Text=PartnershipTempData.sourcePath;
		    clearTreeView();	
			VisualStateManager.GoToState(this,"EditPartnershipState1",false);
            CurrentState = State.Page1OfEdit;
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}
		}
		/// <summary>
		/// goes to the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoToEditPartnershipName_Click(object sender, RoutedEventArgs e){
			try{
			checkInput();
			PartnershipTempData.destinationPath=sourceTextBox.Text;
            ValidateFoldersHierachy();
			sourceTextBox1.Text=PartnershipTempData.sourcePath;
			destinationTextBox1.Text=PartnershipTempData.destinationPath;
			partnershipNameTextBox.Text=PartnershipTempData.partnershipName;	
			VisualStateManager.GoToState(this,"EditPartnershipState3",false);
            CurrentState = State.Page3OfEdit;
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.Message);
			}	
		}
		/// <summary>
		/// goes back to the 2nd page from the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GoBackToEditPartnershipDes_Click(object sender, RoutedEventArgs e){
		   PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
		   destinationTextBox1.Text=PartnershipTempData.destinationPath;
		   clearTreeView();	
		   VisualStateManager.GoToState(this,"EditPartnershipState2",false);
           CurrentState = State.Page2OfEdit;
		}
		
		#endregion	
		
		/// <summary>
		/// Syncs MRUs
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void MRUSync(object sender, RoutedEventArgs e)
        {
            
            // Background worker to do the actual work
            BackgroundWorker mruWorker = new BackgroundWorker();
            mruWorker.WorkerSupportsCancellation = true;

            // Progress bar window
            ProgressBar progressWindow = new ProgressBar(mruWorker, "SyncButler, Sync!");

            bool cancelled = false;

            // Not using the Total progress indicator, so hide it.
            progressWindow.HideTotalProgress();

            mruWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            { // Code to run on completion
                if (!cancelled)
                {
                    CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, "Files were successfully synced and logged");
                }
                progressWindow.TaskComplete();
                SBSSync.IsEnabled = true;
                SBSDone.IsEnabled = true;
            });

            mruWorker.DoWork += new DoWorkEventHandler(delegate(Object worker, DoWorkEventArgs args) 
            { // Actual work gets done here
                BackgroundWorker workerObj = (BackgroundWorker)worker;
                ProgressBar.ProgressBarInfo pinfo;

                // Show some initial information on the progress window
                pinfo.SubTaskPercent = 0;
                pinfo.TotalTaskPercent = 0;
                pinfo.taskDescription = "Starting...";
                workerObj.ReportProgress(0, pinfo);

                try
                {
                    this.Controller.SyncMRUs(MRUs["interesting"], delegate(SyncableStatus status)
                    { // Status reporting - triggers whenever SyncMRU has made progress

                        pinfo.SubTaskPercent = status.curTaskPercentComplete;
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
                        else
                        {
                            // Define the parameters of the message box to show the user

                            info.message = "An error occured while syncing: " + exp.Message + "\n\nWhat would you like me to do?";
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

            SBSSync.IsEnabled = false;
            // Start the whole process
            progressWindow.Start();
        }
		
		private void SaveSetting(object sender, RoutedEventArgs e)
		{

            string ComputerName = this.ComputerNameTextBox.Text.Trim() ;
            string SBSEnable = (string)this.SBSSettingComboBox.SelectedItem;
            char DriveLetter = (char)this.SBSWorkingDriveComboBox.SelectedItem;
            double FreeSpaceToUse = double.Parse(this.LastWorkingFreeSpace);
            string Resolution = this.resolutionLabel.Content.ToString();
            if (FreeSpaceToUse <= 0 && SBSEnable.Equals("Enable"))
            {
                showMessageBox(CustomDialog.MessageType.Warning, "The free space catered for SBS is set to be too low, please check");
            }
            else if (!ComputerNameChecker.IsComputerNameValid(ComputerName))
            {
                CustomDialog.Show(this, CustomDialog.MessageTemplate.OkOnly, CustomDialog.MessageResponse.Ok, ComputerName + " is not a valid name");
            }
            else
            {
                Controller.GetInstance().SaveSetting(ComputerName, SBSEnable, DriveLetter, FreeSpaceToUse, Resolution);
                showMessageBox(CustomDialog.MessageType.Success, "The Setting has been changed");
            }
            
		}
		
		private void SBSSettingChanged(object sender, RoutedEventArgs e)
		{
            if (this.SBSSettingComboBox.SelectedItem != null)
            {
                this.SBSWorkingDriveComboBox.IsEnabled = this.SBSSettingComboBox.SelectedItem.Equals("Enable");
                if (this.SBSSettingComboBox.SelectedItem.Equals("Enable"))
                {
                    this.SpaceToUseSlide.IsEnabled = true;
                    this.SpaceToUseTextbox.IsEnabled = true;
                    SBSUpdateSpaceDetails(null, null);

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

        public bool IsLoadingSBS = true;
        private void SBSUpdateSpaceDetails(object sender, RoutedEventArgs e)
        {
            if (this.SBSSettingComboBox.SelectedIndex != -1 &&
                this.SBSSettingComboBox.SelectedItem.Equals("Enable") && !IsLoadingSBS)
            {
                if (SBSWorkingDriveComboBox.SelectedIndex != -1)
                {
                    SpaceToUseSlide.Value = 0;
                    DriveInfo di = new DriveInfo("" + (char)SBSWorkingDriveComboBox.SelectedItem);
                    long freespace = di.AvailableFreeSpace;
       
                    if (freespace / GIGA_BYTE > 10)
                    {
                        resolutionLabel.Content = "GB";
                        SpaceToUseSlide.Maximum = freespace / GIGA_BYTE;

                    }
                    else if (freespace / MEGA_BYTE > 500)
                    {
                        resolutionLabel.Content = "MB";
                        SpaceToUseSlide.Maximum = freespace / MEGA_BYTE;
                    }
                    else if (freespace / MEGA_BYTE > 2)
                    {
                        resolutionLabel.Content = "KB";
                        SpaceToUseSlide.Maximum = freespace / KILO_BYTE;
                    }
                    else
                    {
                        resolutionLabel.Content = "Bytes";
                        SpaceToUseSlide.Maximum = freespace;
                    }
                    SpaceToUseSlide.Value = 0.1 * SpaceToUseSlide.Maximum;
                }
            }
        }
        public void CheckIfEnoughSpace()
        {
            if (!Controller.GetInstance().IsSBSDriveEnough())
            {
                
                SBSSync.IsEnabled = false;
            }
        }
        private void SpaceToUseChanged(Object sender, KeyEventArgs e)
        {

            if (SpaceToUseTextbox.Text.Trim().Length != 0)
            {
                try
                {
                    if (double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture) <= SpaceToUseSlide.Maximum)
                    {
                        SpaceToUseTextbox.Text = String.Format("{0:F2}", SpaceToUseTextbox.Text);
                        SpaceToUseSlide.Value = double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture);
                    }
                    else if (double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture) > SpaceToUseSlide.Maximum)
                    {
                        SpaceToUseTextbox.Text = String.Format("{0:F2}", SpaceToUseTextbox.Text);
                        SpaceToUseTextbox.Text = String.Format("{0:F2}",SpaceToUseSlide.Maximum);
                        SpaceToUseSlide.Value = SpaceToUseSlide.Maximum;
                    }
                    LastWorkingFreeSpace = String.Format("{0:F2}", double.Parse(SpaceToUseTextbox.Text, CultureInfo.InvariantCulture));

                }
                catch (FormatException)
                {
                    // fall back to the last working value
                    SpaceToUseTextbox.Text = LastWorkingFreeSpace;
                }
            }
        }
        private void SpaceToUseSlided(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            SpaceToUseTextbox.Text = String.Format("{0:F2}",value);
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
		private void checkInput(){
            if (sourceTextBox.Text.Length > 266)
            {
                throw new UserInputException("Folder Path is too long");
            }
            else if (sourceTextBox.Text.Equals(""))
            {
                throw new UserInputException("Please select a Folder");
            }
            else if (!Directory.Exists(sourceTextBox.Text))
            {
                throw new UserInputException("No Such Folder");
            }
            else if (sourceTextBox.Text[0] != '\\') 
            {
                DriveInfo di = new DriveInfo(""+sourceTextBox.Text[0]);
                if (di.DriveType == DriveType.CDRom)
                {
                    throw new UserInputException("CD rom is not supported in this version");
                }
                else if (di.DriveType == DriveType.Network)
                {
                    throw new UserInputException("Network drive is not supported in this version");
                }
            }
           
		}
        private void ValidateFoldersHierachy()
        {

            string tempfolder1Name = PartnershipTempData.sourcePath;
            string tempfolder2Name = PartnershipTempData.destinationPath;
            if (!PartnershipTempData.sourcePath.EndsWith("\\"))
            {
                tempfolder1Name += "\\";
            }
            if (!PartnershipTempData.destinationPath.EndsWith("\\"))
            {
                tempfolder2Name += "\\";
            }
            if (tempfolder2Name.Equals(tempfolder1Name))
            {
                throw new UserInputException("Same Folders selected : Please pick another Folder");
            }
            else if (tempfolder1Name.IndexOf(tempfolder2Name) == 0)
            {
                throw new UserInputException("Error - 1st Folder is under the 2nd Folder  ");
            }
            else if (tempfolder2Name.IndexOf(tempfolder1Name) == 0)
            {
                throw new UserInputException("Error - 2nd Folder is under the 1st Folder  ");
            }
        }
        private void FocusMe(object sender, EventArgs e)
        {
            if (Favourites_List.IsFocused)
                WeirdFile_List.SelectedIndex = -1;
            else
                Favourites_List.SelectedIndex = -1;
        }

        private void SBSMoveToOther(object sender,EventArgs e )
        {
            SortedList<string, string> sensitive = MRUs["sensitive"];
            SortedList<string, string> interesting = MRUs["interesting"];
            // move from weird to fav
            if (WeirdFile_List.SelectedIndex != -1)
            {
                
                interesting.Add((String)WeirdFile_List.SelectedItem, (String)sensitive[(String)WeirdFile_List.SelectedItem]);
                sensitive.Remove((String)WeirdFile_List.SelectedItem);
            }
            // move from fav to weird
            else if (Favourites_List.SelectedIndex != -1)
            {
                sensitive.Add((String)Favourites_List.SelectedItem, (String)interesting[(String)Favourites_List.SelectedItem]);
                interesting.Remove((String)Favourites_List.SelectedItem);
            }

            Favourites_List.Items.Refresh();
            WeirdFile_List.Items.Refresh();
        }
        private SortedList<string, SortedList<string, string>> MRUs;

        public void LoadMRUs()
        {
            SBSDone.IsEnabled = false;

            BackgroundWorker sbsScanWorker = new BackgroundWorker();
            ProgressBar progressWindow = new ProgressBar(sbsScanWorker, "Loading SyncButler, Sync!");
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
                    pinfo.SubTaskPercent = status.curTaskPercentComplete;
                    ((BackgroundWorker)worker).ReportProgress(0, pinfo);
                    return true;
                });

                pinfo.SubTaskPercent = 100;
                pinfo.taskDescription = "Finishing...";
                ((BackgroundWorker)worker).ReportProgress(0, pinfo);
            });

            sbsScanWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object worker, RunWorkerCompletedEventArgs args)
            {
                Favourites_List.ItemsSource = MRUs["interesting"].Keys;
                WeirdFile_List.ItemsSource = MRUs["sensitive"].Keys;
                progressWindow.TaskComplete();
            });

            progressWindow.Start();
        }

        private void ShowResult(object sender, EventArgs e)
        {

            //ShowResult();
            VisualStateManager.GoToState(this, "Result1", false);
            CurrentState = State.Result;
            SyncResultListBox.ItemsSource = ResolvedConflicts;
        }

        private void SourceTextBox_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
			if (e.Key == Key.Return)
            {
				switch(CurrentState)
				{
					case State.Page1OfCreate:
						GoToPartnershipDest_Click(sender,e);
						break;
					case State.Page2OfCreate:
						GoToCreatePartnershipName_Click(sender,e);
						break;
					case State.Page1OfEdit:
						GoToEditPartnershipDest_Click(sender,e);
						break;
					case State.Page2OfEdit:
						GoToEditPartnershipName_Click(sender,e);
						break;
					
				}	
			}
        }
		private void PartnershipNameTextBox_Enter(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if(e.Key == Key.Return)
			{
				switch(CurrentState)
				{
					case State.Page3OfCreate:
						CreatePartnership_Click(sender,e);
						break;
					case State.Page3OfEdit:
						SavePartnership_Click(sender,e);
						break;
				}
			}
		}
	}
}