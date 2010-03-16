using System;
using System.Collections.Generic;
using System.IO;
using SyncButler.Exceptions;
using System.Windows.Forms;
using SyncButler.MRU;
using System.Xml;
using ISyncButler;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SyncButler
{
    public class Controller
    {
        SyncEnvironment syncEnvironment;
        IGUI mainWindow;
        private static Controller controller;

        /// <summary>
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of Controller.
        /// </summary>
        private Controller()
        {
            syncEnvironment = SyncEnvironment.GetInstance();
            //console = new SyncButlerConsole.Form1();
            //console.Show();
            Logging.Logger.GetInstance().DEBUG("Controller started up.");
        }

        public void SetWindow(IGUI win)
        {
            mainWindow = win;
        }
        public void GrabFocus()
        {
            mainWindow.GrabFocus();
        }

        /// <summary>
        /// Replaces the controller constructor with a method for other classes to retrieve the controller instead of creating one.
        /// </summary>
        /// <returns>The controller of the program</returns>
        public static Controller GetInstance()
        {
            if (controller == null)
                controller = new Controller();

            return controller;
        }
		public List<string> GetDriveLetters()
		{
			return SyncButler.SystemEnvironment.StorageDevices.GetUSBDriveLetters();
		}
        /// <summary>
        /// Tests for the existence of another instance and sets up single instance listener if this is the first instance
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static Boolean TestSingleInstance(String[] args)
        {
            // test if this is the first instance and register receiver, if so.
            if (SingleInstance.IsFirst(new SingleInstance.ReceiveDelegate(ReceiveAction)))
            {
                // This is the 1st instance.
                return true;
            }
            else
            {
                // send command line args to running app, then terminate
                SingleInstance.Send(args);
                SingleInstance.Cleanup(); // Cleanup for if path to be run during shutdown
                return false;
            }
        }

        /// <summary>
        /// Handles incoming data from other instances.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        private static void ReceiveAction(string[] args)
        {
            Controller.GetInstance().GrabFocus(); // grab focus
            string path="";
            foreach (string str in args) 
            {
                path = path + " " + str;
            }
            
            // TODO: Process the arguements received
        }

        /// <summary>
        /// Adds a partnership to the list of Partnerships based on 2 full paths
        /// (calls SyncEnvironment)
        /// </summary>
        /// <param name="name">Friendly name of a partnership</param>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        public void AddPartnership(String name, String leftPath, String rightPath) 
        {
            syncEnvironment.AddPartnership(name, leftPath, rightPath);
        }
        
        /// <summary>
        /// Delete a partnership from the list of partnerships based at an index.
        /// </summary>
        /// <param name="idx">Index of the partnership to be deleted.</param>
        public void DeletePartnership(int idx)
        {
            syncEnvironment.RemovePartnership(idx);
        }

        /// <summary>
        /// Delete a partnership from the list of partnerships based on the friendly name.
        /// </summary>
        /// <param name="name">The name of the partnership to be deleted.</param>
        public void DeletePartnership(string name)
        {
            syncEnvironment.RemovePartnership(name);
        }

        /// <summary>
        /// Updates the details of an existing partnership in the partnership list
        /// </summary>
        /// <param name="oldName">Old friendly name of a partnership</param>
        /// <param name="newName">New friendly name of a partnership</param>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        public void UpdatePartnership(string oldName, String newName, String leftPath, String rightPath)
        {
            syncEnvironment.UpdatePartnership(oldName, newName, leftPath, rightPath);
        }

        /// <summary>
        /// Retrieves the list of partnerships from the sync environment. Allows for the user interface to display the list.
        /// </summary>
        /// <returns>The list of all partnerships</returns>
        public SortedList<String,Partnership> GetPartnershipList()
        {
            return syncEnvironment.GetPartnershipsList();
        }

        /// <summary>
        /// It returns the partnership stored in the list with the given unique friendly
        /// name.
        /// </summary>
        /// <param name="friendlyName">Friendly name of partnership</param>
        /// <returns>The Partnership object with that unique friendly name</returns>
        public Partnership GetPartnership(string friendlyName)
        {
            return SyncEnvironment.GetInstance().GetPartnership(friendlyName);
        }

        /// <summary>
        /// Starts syncing a partnership.
        /// </summary>
        /// <param name="idx">Index of the partnership to be synced.</param>
        /// <returns>ObservableCollection conflict list with a list of conflicts. Will be null if there are no conflicts.</returns>
        public ObservableCollection<ConflictList> SyncPartnership(String name, SyncableStatusMonitor monitor) 
        {
		  	ObservableCollection<ConflictList> AllConflict = new ObservableCollection<ConflictList>();
            Partnership curPartnership = syncEnvironment.GetPartnershipsList()[name];

            curPartnership.statusMonitor = monitor;
			List<Conflict> conflict = curPartnership.Sync();
            curPartnership.statusMonitor = null;

			AllConflict.Add(new ConflictList(conflict,name));
			return AllConflict;
        }

        public void ResolveConflicts(ObservableCollection<ConflictList> mergedList, SyncableStatusMonitor OnProgressUpdate, BackgroundWorker workerObj)
        {
            foreach (ConflictList cl in mergedList)
            {
                workerObj.ReportProgress(0, cl.PartnerShipName);
                foreach (Conflict c in cl.conflicts)
                {
                        c.SetStatusMonitor(OnProgressUpdate);
                        c.Resolve();
                        c.SetStatusMonitor(null);
                }
            }
        }

        /// <summary>
        /// Synchronizes all partnerships.
        /// </summary>
        public ObservableCollection<ConflictList> SyncAll(SyncableStatusMonitor OnProgressUpdate, BackgroundWorker workerObj)
        {

            ObservableCollection<ConflictList> AllConflict = new ObservableCollection<ConflictList>();
            foreach (string name in GetPartnershipList().Keys)
            {
                workerObj.ReportProgress(0, name);
                Partnership curPartnership = syncEnvironment.GetPartnershipsList()[name];
                
                curPartnership.statusMonitor = OnProgressUpdate;
                List<Conflict> conflict = curPartnership.Sync();
                curPartnership.statusMonitor = null;

                AllConflict.Add(new ConflictList(conflict, name));
            }

            return AllConflict;
        }

        /// <summary>
        /// Not Implemented. Turns the recent file monitoring on/off.
        /// </summary>
        public void ToggleMonitor()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented. 
        /// </summary>
        public void EditMonitor()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a list of most recently used files.
        /// </summary>
        public SortedList<string,SortedList<string,string>> GetMonitoredFiles()
        {
            return ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));
        }
        
        /// <summary>
        /// Sync the mrus that are listed. Please read MRUList to understand how file is actually saved.
        /// </summary>
        /// <param name="driveLetter"></param>
        public void SyncMRUs(String driveLetter)
        {
            string syncTo = driveLetter + ":\\SyncButler\\" + SyncEnvironment.ComputerName + "\\";
            MRUList mruList = new MRUList();
            mruList.Load(GetMonitoredFiles()["interesting"]);
            mruList.Sync(SyncEnvironment.ComputerName, driveLetter);
            MRUList.SaveInfoTo(syncTo + "logs.xml", mruList);
        }

        /// <summary>
        /// This method is required to be run when the program is closed. It
        /// saves all the necessary state into memory
        /// </summary>
        public void Shutdown()
        {
            syncEnvironment.StoreEnv();
            SingleInstance.Cleanup();
        }

        /// <summary>
        /// This checks in with sync environment to check if the program has ran before.
        /// </summary>
        /// <returns>True if has ran before, false otherwise</returns>
        public bool IsProgramRanBefore()
        {
            return SyncEnvironment.FirstRunComplete;
        }
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ComputerName">Computer name of the user</param>
		/// <param name="EnableSBS">If the user wants sbs to be enabled</param>
		/// <param name="SBSDrive">The working drive letter</param>
		public void SaveSetting(string ComputerName, bool EnableSBS, char SBSDrive)
		{
			// do nothing?

            SyncEnvironment.ComputerName = ComputerName;

            SyncEnvironment.GetInstance().StoreSettings();
		}

        /// <summary>
        /// Get the friendly name of the comptuer
        /// </summary>
        /// <returns>The friendly name of the computer</returns>
        public string GetComputerName()
        {
            return SyncEnvironment.ComputerName;
        }

        /// <summary>
        /// Used to set the initial computer name or to replace an exisiting
        /// computer name
        /// </summary>
        /// <param name="name">The computer name (new)</param>
        public void SetComputerName(string name)
        {
            SyncEnvironment.ComputerName = name;
        }

        /// <summary>
        /// Remove the shell integration context menu from the registry
        /// and disable the settings
        /// </summary>
        public void RemoveDisableContextMenu()
        {
            //Registry action to remove key

            //Removing settings from history
            SyncEnvironment.EnableShellContext = false;
            SyncEnvironment.GetInstance().StoreSettings();
        }

        /// <summary>
        /// Add the shell integration context menu into the registry
        /// and activate the settings
        /// </summary>
        public void AddEnableContextMenu()
        {
            //Registry action to add key

            //Removing settings from history
            SyncEnvironment.EnableShellContext = true;
            SyncEnvironment.GetInstance().StoreSettings();
        }
    }
}
