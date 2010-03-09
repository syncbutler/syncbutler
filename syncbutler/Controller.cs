using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SyncButler.Exceptions;
using System.Windows.Forms;

namespace SyncButler
{
    public class Controller
    {
        SyncEnvironment syncEnvironment;
        private static Controller controller;
        private SyncButlerConsole.Form1 console;

        /// <summary>
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of Controller.
        /// </summary>
        private Controller()
        {
            syncEnvironment = SyncEnvironment.GetInstance();
            this.console = new SyncButlerConsole.Form1();
            this.console.Show();
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
        /// Writes a line to the debugging console.
        /// </summary>
        /// <param name="text">Text to write</param>
        public void WriteDebug(string text)
        {
            this.console.WriteLine(text);
        }

        /// <summary>
        /// Clears the debugging console.
        /// </summary>
        public void ClearDebug()
        {
            this.console.ClearScreen();
        }

        /// <summary>
        /// Hide the debugging console.
        /// </summary>
        public void HideDebug()
        {
            this.console.Hide();
        }

        /// <summary>
        /// Show the debugging console.
        /// </summary>
        public void ShowDebug()
        {
            this.console.Show();
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
        public void SyncAll()
        {
            foreach (String name in GetPartnershipList().Keys)
            {
                List<Conflict> conflicts = SyncPartnership(name);
                foreach (Conflict conflict in conflicts)
                {
                    //If unknown ignore 1st
                    try
                    {
                        conflict.Resolve();
                    }
                    catch (InvalidActionException)
                    {
                    }
                }
            }
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
        /// Not Implemented. Returns a list of files which have been synced by the monitor.
        /// </summary>
        public SortedList<String,String> GetMonitoredFiles()
        {
            return MostRecentlyUsedFile.Get();
        }
        
        /// <summary>
        /// Sync the mrus that are listed.
        /// </summary>
        /// <param name="driveLetter"></param>
        public void SyncMRUs(String driveLetter)
        {
            string syncTo = driveLetter + ":\\SyncButler\\" + SyncEnvironment.GetComputerName() + "\\";
            foreach (string s in MostRecentlyUsedFile.Get().Values)
            {
                if (File.Exists(s))
                {
                    string filename = Path.GetFileName(s);
                    MostRecentlyUsedFile mruFiles = new MostRecentlyUsedFile(s, syncTo + filename);
                    mruFiles.Sync();
                }
            }
        }

        /// <summary>
        /// This method is required to be run when the program is closed. It
        /// saves all the necessary state into memory
        /// </summary>
        public void Shutdown()
        {
            syncEnvironment.StoreEnv();
        }

        /// <summary>
        /// This checks in with sync environment to check if the program has ran before.
        /// </summary>
        /// <returns>True if has ran before, false otherwise</returns>
        public bool programRanBefore()
        {
            return syncEnvironment.FirstRunComplete;
        }
    }
}
