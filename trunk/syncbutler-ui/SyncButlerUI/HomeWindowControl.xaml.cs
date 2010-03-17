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
            public string partnershipName;
            public Object failedObject;

            public ErrorReportingMessage(Exception exceptionThrown, ErrorReportingSource source,
                string partnershipName, Object failedObject)
            {
                this.exceptionThrown = exceptionThrown;
                this.source = source;
                this.partnershipName = partnershipName;
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
			//Temporary testing link to Controller
            //controller = new Controller();
            //partnershipList.ItemsSource = controller.GetPartnershipList();
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

                if (msg.source == ErrorReportingSource.Resolver) msgTemplate = CustomDialog.MessageTemplate.SkipRetryCancel;
                else msgTemplate = CustomDialog.MessageTemplate.SkipCancel;
                
                switch (CustomDialog.Show(this, msgTemplate, CustomDialog.MessageType.Error, CustomDialog.MessageResponse.Cancel,
                    msg.exceptionThrown.Message + "\n\nWould you like to try and continue anyway?"))
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
                    showMessageBox(CustomDialog.MessageType.Message, "Scan cancelled by user");
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
                Exception exp;

                foreach (string friendlyName in partnershipNames)
                {
                    worker.ReportProgress(0, friendlyName);

                    exp = null;

                    try
                    {
                        ConflictList cl = this.Controller.SyncPartnership(friendlyName, delegate(SyncableStatus status)
                        {
                            worker.ReportProgress(status.percentComplete, status);
                            if (worker.CancellationPending) return false;
                            return true;
                        });

                        worker.ReportProgress(100, null);
                        mergedList.Add(cl);
                        this.Controller.CleanUpOrphans(friendlyName);
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        break;
                    }
                    catch (IOException e)
                    {
                        exp = new Exception("An I/O error was encountered while processing " + friendlyName + ": " + e.Message);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        exp = new Exception("A permissions error was encountered while processing " + friendlyName + ": " + e.Message);
                    }

                    if (exp != null)
                    {
                        ErrorReportingMessage msg = new ErrorReportingMessage(exp, ErrorReportingSource.Scanner, friendlyName, null);

                        if (ReportError(worker, msg))
                        {
                            operationCancelled = true;
                            break;
                        }
                    }

                }
            });

            scanWorker.RunWorkerAsync();
            CurrentAction = CurrentActionEnum.Scanning;

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

                        this.Controller.ResolveConflict(curConflict, reporter, worker);
                        curConflict = ThreadSafeGetNewResolve();
                    }
                    catch (UserCancelledException)
                    {
                        operationCancelled = true;
                        return;
                    }
                    catch (IOException e)
                    {
                        exp = new Exception("An I/O error was encountered while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        exp = new Exception("A permissions error was encountered while processing " + partnershipName + ": " + e.Message);
                    }
                    catch (InvalidActionException e)
                    {
                        exp = new Exception("An invalid action occurred while processing " + partnershipName + ": " + e.Message);
                    }

                    if (exp != null)
                    {
                        ErrorReportingMessage msg = new ErrorReportingMessage(exp, ErrorReportingSource.Resolver,
                            partnershipName, curConflict);

                        if (ReportError(worker, msg))
                        {
                            operationCancelled = true;
                            return;
                        }
                    }
                }
            });

            resolveWorker.ProgressChanged += new ProgressChangedEventHandler(DisplayProgress);

            resolveWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(Object workerObj, RunWorkerCompletedEventArgs args)
            {
                CurrentSyncingFile.Text = "Scan complete. Conflicts processed so far: " + conflictsProcessed;
                partnershipNameTextBox.Text = "";

                if (operationCancelled)
                {
                    showMessageBox(CustomDialog.MessageType.Message, "Sync cancelled by user");
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
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
		}
		
		
		/// <summary>
		/// done to submit the create partnership to controller
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CreatePartnership_Click(object sender, RoutedEventArgs e){
		 try{
			if(partnershipNameTextBox.Text.Equals("")){
			    throw new Exception("Please input a partnership name");	
			
			}
			PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
			sourceFolderPath.Text=PartnershipTempData.sourcePath;
			destinationFolderPath.Text=PartnershipTempData.destinationPath;
		    partnerShipName.Text=PartnershipTempData.partnershipName;
            NewPartnershipName = partnerShipName.Text;
			this.Controller.AddPartnership(partnerShipName.Text,sourceFolderPath.Text,destinationFolderPath.Text);
			VisualStateManager.GoToState(this,"CreatePartnershipDone1",false);
			sourceTextBox1.Text="";
			destinationTextBox1.Text="";
			sourceTextBox.Text="";
		    PartnershipTempData.clear();
			partnershipList.Items.Refresh();
         }
         catch (UserInputException uIException)
         {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
			try{
		  	if(partnershipList.SelectedIndex<0){
                throw new UserInputException("Please select a partnership to delete.");
			}
			if (showMessageBox(CustomDialog.MessageType.Question,"Are you sure?")==true){
				this.Controller.DeletePartnership(partnershipList.SelectedIndex);
				partnershipList.Items.Refresh();
			}
			}catch(UserInputException uIException){
					showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
		    if(this.Controller.GetPartnershipList().Count < 1)
            {
                if (showMessageBox(CustomDialog.MessageType.Question, "Sync Butler has detected that there are no partnerships created yet would you like to create one now?") == true)
                {
                    clearTreeView();
                    VisualStateManager.GoToState(this, "CreatePartnershipState1", false);

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
                showMessageBox(CustomDialog.MessageType.Error, uIException.message);
            }

        }
        
        /// <summary>
        /// Syncing after creation of a partnership
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncThisPartnership_Click(object sender, RoutedEventArgs e){
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
			    sourceTextBox1.Text="";
			    destinationTextBox1.Text="";
			    sourceTextBox.Text="";
		        PartnershipTempData.clear();
			    partnershipList.Items.Refresh();
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
			}catch(UserInputException uIException){
					showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
            }
            catch (UserInputException uIException)
            {
				showMessageBox(CustomDialog.MessageType.Error,uIException.message);
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
		}
		
		#endregion	
		
		/// <summary>
		/// Syncs MRUs
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void MRUSync(object sender, RoutedEventArgs e)
        {
            this.Controller.SyncMRUs();
            showMessageBox(CustomDialog.MessageType.Success, "Synced and logged!");
        }
		
		private void SaveSetting(object sender, RoutedEventArgs e)
		{
            
            string ComputerName = this.ComputerNameTextBox.Text;
            string SBSEnable = (string)this.SBSSettingComboBox.SelectedItem;
            char DriveLetter = (char)this.SBSWorkingDriveComboBox.SelectedItem;
			
            Controller.GetInstance().SaveSetting(ComputerName,SBSEnable,DriveLetter);

            showMessageBox(CustomDialog.MessageType.Success, "The Setting has been changed");
		}
		
		private void SBSSettingChanged(object sender, RoutedEventArgs e)
		{
            if (this.SBSSettingComboBox.SelectedItem != null)
			    this.SBSWorkingDriveComboBox.IsEnabled = this.SBSSettingComboBox.SelectedItem.Equals("Enable");
		}
		
		private void DefaultSetting(object sender, RoutedEventArgs e)
		{
            this.ComputerNameTextBox.Text = "Computer1";
            this.SBSSettingComboBox.SelectedItem = "Disable";
            this.SBSWorkingDriveComboBox.SelectedIndex = -1;
		}
		/// <summary>
		/// Checks the sourceTextbox for values if its empty or if the directory exists
		/// </summary>
		private void checkInput(){
			if(sourceTextBox.Text.Length>266){
                throw new UserInputException("Folder Path is too long");
			}else if(sourceTextBox.Text.Equals("")){
                throw new UserInputException("Please select a Folder");
			}else if(!Directory.Exists(sourceTextBox.Text)){
				throw new UserInputException("No Such Folder");
		
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
		

	}
}