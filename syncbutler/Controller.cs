using System;
using System.Collections.Generic;
using System.IO;
using SyncButler.Exceptions;
using System.Windows.Forms;
using SyncButler.MRU;
using System.Xml;

namespace SyncButler
{
    public class Controller
    {
        SyncEnvironment syncEnvironment;
        private static Controller controller;

        /// <summary>
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of Controller.
        /// </summary>
        private Controller()
        {
            syncEnvironment = SyncEnvironment.GetInstance();
            //console = new SyncButlerConsole.Form1();
            //console.Show();
            Logging.Logger.GetInstance().DEBUG("it's working!");
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

        /// <summary>
        /// Tests for the existence of another instance and sets up single instance listener if this is the first instance
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static Boolean TestSingleInstance(String[] args)
        {
            // test if this is the first instance and register receiver, if so.
            if (SingleInstance.IsFirst(new SingleInstance.ReceiveDelegate(receiveAction)))
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
        private static void receiveAction(string[] args)
        {
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
        /// Updates the details of an existing partnership in the partnership list
        /// </summary>
        /// <param name="name">The friendly name of the partnership</param>
        /// <param name="updated">The updated Partnership object</param>
        public void UpdatePartnership(string name, Partnership updated)
        {
            syncEnvironment.UpdatePartnership(name, updated);
        }

        /// <summary>
        /// Retrieves the list of partnerships from the sync environment. Allows for the user interface to display the list.
        /// </summary>
        /// <returns>The list of all partnerships</returns>
        public SortedList<String,Partnership> GetPartnershipList()
        {
            return syncEnvironment.GetPartnerships();
        }

        /// <summary>
        /// Starts syncing a partnership.
        /// </summary>
        /// <param name="idx">Index of the partnership to be synced.</param>
        /// <returns>A list of conflicts. Will be null if there are no conflicts.</returns>
        public List<Conflict> SyncPartnership(String name) 
        {
            return syncEnvironment.GetPartnerships()[name].Sync();
        }

        /// <summary>
        /// Synchronizes all partnerships.
        /// </summary>
        public SortedList<string, List<Conflict>> SyncAll()
        {
            SortedList<string, List<Conflict>> AllConflict = new SortedList<string, List<Conflict>>();
            foreach (string name in GetPartnershipList().Keys)
            {
                List<Conflict> conflict = SyncPartnership(name);
                AllConflict.Add(name,conflict);
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
        public SortedList<String,String> GetMonitoredFiles()
        {
            return MostRecentlyUsedFile.Get();
        }
        
        /// <summary>
        /// Sync the mrus that are listed. Please read MRUList to understand how file is actually saved.
        /// </summary>
        /// <param name="driveLetter"></param>
        public void SyncMRUs(String driveLetter)
        {
            string syncTo = driveLetter + ":\\SyncButler\\" + SyncEnvironment.GetComputerName() + "\\";
            MRUList mruList = new MRUList();
            mruList.Load();
            mruList.Sync(SyncEnvironment.GetComputerName(), driveLetter);
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
        public bool programRanBefore()
        {
            return syncEnvironment.FirstRunComplete;
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
		}
    }
}
