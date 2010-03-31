﻿using System;
using System.Collections.Generic;
using System.IO;
using SyncButler.Exceptions;
using System.Windows.Forms;
using SyncButler.MRU;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SyncButler
{
    /// <summary>
    /// This class is responsible for coordinating communications from and to the user interface as well as between internal logic classes
    /// </summary>
    public class Controller
    {
        SyncEnvironment syncEnvironment;
        SyncButler.IGUI mainWindow;
        private static Controller controller;
        /// <summary>
        /// Used by check and merged to see the total size of the files to be sync so far.
        /// </summary>
        private long totalSizeSoFar = 0;

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
            if(mainWindow != null)
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
        /// <summary>
        /// get a list of usb drive letters
        /// </summary>
        /// <returns>a list of usb drive letters</returns>
		public List<string> GetUSBDriveLetters()
		{
            
			return SyncButler.SystemEnvironment.StorageDevices.GetUSBDriveLetters();
		}

        /// <summary>
        /// Get a list of non usb drives letters
        /// </summary>
        /// <returns>a ist of non usb drive letters</returns>
        public List<string> GetNonUSBDriveLetters()
        {
            return SyncButler.SystemEnvironment.StorageDevices.GetNonUSBDriveLetters();
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
            int i = 0;
            foreach (string str in args) 
            {
                Logging.Logger.GetInstance().DEBUG(i++ + " " + str + " received");
            }

            
            // TODO: Process the arguements received
        }
        public bool IsSBSDriveEnough()
        {
            long required = GetUserLimit();
            string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId);
            if (driveLetter.Length == 0)
                return false;
            long avabilableSpace = SystemEnvironment.StorageDevices.GetAvailableSpace(driveLetter);
            return required <= avabilableSpace;
        }

        public long GetAvailableSpaceForDrive()
        {
            string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId);
            if (driveLetter.Length == 0)
                return 0;
            long AvailableSpace = SystemEnvironment.StorageDevices.GetAvailableSpace(driveLetter);
            string res = SyncEnvironment.Resolution;

            return GetSizeInResolution(res, AvailableSpace);
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
            string appPath = SyncEnvironment.AppPath;

            ContainsAppDirectory(leftPath, rightPath, appPath);

            syncEnvironment.AddPartnership(name, leftPath, rightPath);
        }

        /// <summary>
        /// Checks a path and throws an exception if it contains the application directory.
        /// </summary>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        /// <param name="appPath">Path to the application</param>
        /// <exception cref="UserInputException">Cannot create a partnership which contains the SyncButler directory</exception>
        private static void ContainsAppDirectory(String leftPath, String rightPath, string appPath)
        {
            if (WindowsFileSystem.PathsEqual(leftPath, appPath) || WindowsFileSystem.PathsEqual(rightPath, appPath))
                throw new UserInputException("Cannot create a partnership on the running SyncButler directory!");
        }

        /// <summary>
        /// Adds a minipartnership
        /// </summary>
        /// <param name="source">Full path to the source</param>
        public void AddMiniPartnership(string source)
        {
            syncEnvironment.AddMiniPartnership(source);
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
            string appPath = SyncEnvironment.AppPath;

            ContainsAppDirectory(leftPath, rightPath, appPath);

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
        /// Retrieves the list of mini partnerships from the sync environment. Allows for the user interface to display the list.
        /// </summary>
        /// <returns>The list of all mini partnerships</returns>
        public SortedList<String, Partnership> GetMiniPartnershipList()
        {
            return syncEnvironment.GetMiniPartnershipsList();
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
        public ConflictList SyncPartnership(String name, SyncableStatusMonitor monitor, SyncableErrorHandler errorhandler) 
        {
            Partnership curPartnership = syncEnvironment.GetPartnershipsList()[name];

            curPartnership.statusMonitor = monitor;
            curPartnership.errorHandler = errorhandler;
			List<Conflict> conflict = curPartnership.Sync();
            curPartnership.statusMonitor = null;

            return new ConflictList(conflict, name);
        }

        public void CleanUpOrphans(String partnershipName)
        {
            syncEnvironment.GetPartnershipsList()[partnershipName].CleanOrphanedChecksums();
        }

        public List<Conflict> RemoveAutoResolvableConflicts(ConflictList cl)
        {
            List<Conflict> resolvableConflicts = new List<Conflict>();
            for (int i = cl.conflicts.Count - 1; i >= 0; i--)
            {
                Conflict c = cl.conflicts[i];
                if (c.AutoResolveAction != Conflict.Action.Unknown)
                {
                    resolvableConflicts.Add(c);
                    cl.conflicts.RemoveAt(i);
                }
            }

            return resolvableConflicts;
        }

        public Resolved ResolveConflict(Conflict toResolve, SyncableStatusMonitor OnProgressUpdate, BackgroundWorker workerObj)
        {
            workerObj.ReportProgress(0, toResolve.GetPartnership().Name);
            toResolve.SetStatusMonitor(OnProgressUpdate);
            Resolved ret = toResolve.Resolve();
            toResolve.SetStatusMonitor(null);
            return ret;
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
        /// To check if size existing file list the given file list will within the limit given 
        /// by the users. Used by GetMonitoredFiles(..);
        /// </summary>
        /// <param name="ToMerge">The existing file list</param>
        /// <param name="FileListToCheck"> the file list that is new and required check</param>
        /// <param name="limit">the limit imposed by the user</param>
        /// <returns>if the new file list is merged with the existing file list</returns>
        private bool CheckAndMerge(SortedList<string, string> ToMerge, 
            SortedList<string, string> FileListToCheck, long limit)
        {
            long ListSize = WindowsFile.SizeOf(FileListToCheck.Values);
            if (ListSize + totalSizeSoFar <= limit)
            {
                totalSizeSoFar += ListSize;
                foreach (String key in FileListToCheck.Keys)
                {
                    ToMerge.Add(key, FileListToCheck[key]);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        private const long GIGABYTE = 1024 * 1024 * 1024;
        private const long MEGABYTE = 1024 * 1024;
        private const long KILOBYTE = 1024;

        private long GetSizeInResolution(String Resolution, long size)
        {
            if (Resolution.Equals("GB"))
            {
                return size / GIGABYTE;
            }
            else if (Resolution.Equals("MB"))
            {
                return size / MEGABYTE;
            }
            else if (Resolution.Equals("KB"))
            {
                return size / KILOBYTE;
            }
            else if (Resolution.Equals("Bytes"))
            {
                return size;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private long GetUserLimit()
        {
            String Resolution = this.GetResolution();
            long FreeSpaceTouse = (long)this.GetFreeSpaceToUse();
            if (Resolution.Equals("GB"))
            {
                return FreeSpaceTouse * GIGABYTE;
            }
            else if (Resolution.Equals("MB"))
            {
                return FreeSpaceTouse * MEGABYTE;
            }
            else if (Resolution.Equals("KB"))
            {
                return FreeSpaceTouse * KILOBYTE;
            }
            else if (Resolution.Equals("Bytes"))
            {
                return FreeSpaceTouse;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns a list of most recently used files.
        /// </summary>
        public SortedList<string,SortedList<string,string>> GetMonitoredFiles(SyncableStatusMonitor statusMonitor)
        {
            totalSizeSoFar = 0;
            long limit = GetUserLimit();
            
            SortedList<string, SortedList<string, string>> rtn = new SortedList<string, SortedList<string, string>>();
            SortedList<string, string> interesting = new SortedList<string, string>();
            MostRecentlyUsedFile.statusMonitor = statusMonitor;
            SortedList<string,SortedList<string,string>> splited = ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));
            if (CheckAndMerge(interesting, splited["interestingHigh"], limit))
            {
                if (CheckAndMerge(interesting, splited["interestingMedHigh"], limit))
                {
                    if (CheckAndMerge(interesting, splited["interestingMed"], limit))
                    {
                        if (CheckAndMerge(interesting, splited["interestingLowMed"], limit))
                        {
                            if (CheckAndMerge(interesting, splited["interestingLow"], limit))
                            {
                                CheckAndMerge(interesting, splited["interestingUltraLow"], limit);
                            }
                        }
                    }
                 
                }
            }
            SortedList<string, string> sensitive = splited["sensitive"];
            rtn.Add("sensitive", sensitive);
            rtn.Add("interesting", interesting);
            MostRecentlyUsedFile.statusMonitor = null;
            return rtn;
        }

        public SortedList<string, SortedList<string, string>> GetMonitoredFiles()
        {
            SortedList<string, SortedList<string, string>> ret = ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));
            return ret;
        }

        /// <summary>
        /// Get the username of the current logon user
        /// </summary>
        /// <returns>Return the user name</returns>
        public static String GetCurrentLogonUser()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }

        /// <summary>
        /// Sync the mrus that are listed. Please read MRUList to understand how file is actually saved.
        /// </summary>
        /// <param name="driveLetter"></param>
        public void SyncMRUs(SortedList<string, string> toSync, SyncableStatusMonitor statusMonitor, SyncableErrorHandler errorHandler)
        {
            char driveLetter = SyncEnvironment.SBSDriveLetter;
            string driveid = SyncEnvironment.SBSDriveId;
            if (SystemEnvironment.StorageDevices.GetDriveLetter(driveid).Length == 0)
            {
                errorHandler.Invoke(new Exception("Device not detected\nPlease plug in the device configured for SBS."));
            }
            else
            {
                string syncTo = driveLetter + ":\\SyncButler\\" + SyncEnvironment.ComputerName + "\\";
                if (!WindowsFolder.CheckIfUserHasRightsTo(syncTo, GetCurrentLogonUser()))
                {
                    errorHandler.Invoke(new Exception("Permisson denied\nPlease check if you have the rights to the folder for SBS at " + driveLetter + ":\\SyncButler\\"));
                }
                else
                {
                    driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(driveid)[0];

                    MRUList mruList = new MRUList();

                    mruList.SetStatusMonitor(statusMonitor);
                    mruList.SetErrorHandler(errorHandler);
                    mruList.Load(toSync);
                    mruList.Sync(SyncEnvironment.ComputerName, driveLetter);
                    SBSLogFile = syncTo + "logs.xml";
                    MRUList.SaveInfoTo(SBSLogFile, mruList);
                }
            }
        }
        public string SBSLogFile;

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
            return (SyncEnvironment.FirstRunComplete && SyncEnvironment.ComputerNamed);
        }
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ComputerName">Computer name of the user</param>
		/// <param name="EnableSBS">[Not in use]If the user wants sbs to be enabled</param>
		/// <param name="SBSDrive">The working drive letter</param>
        public void SaveSetting(string ComputerName, string EnableSBS, char SBSDrive, Double FreeSpaceToUse, String Resolution)
		{
            SyncEnvironment.ComputerName = ComputerName;
            SyncEnvironment.SBSDriveLetter = SBSDrive;
            SyncEnvironment.SBSEnable = EnableSBS;
            SyncEnvironment.FreeSpaceToUse = FreeSpaceToUse;
            SyncEnvironment.Resolution = Resolution;
            SyncEnvironment.SBSDriveId = SystemEnvironment.StorageDevices.GetDriveID(SBSDrive + ":");
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
        /// get the status of sbs
        /// </summary>
        /// <returns></returns>
        public string GetSBSEnable()
        {
            return SyncEnvironment.SBSEnable == null ? "Disable" : SyncEnvironment.SBSEnable;
        }

        /// <summary>
        /// set the status of sbs
        /// </summary>
        /// <param name="SBSEnable">the new sbs status</param>
        public void SetSBSEnable(string SBSEnable)
        {
            SyncEnvironment.SBSEnable = SBSEnable;
        }

        /// <summary>
        /// Used to set the initial computer name for the first time
        /// </summary>
        /// <param name="name">The computer name (new)</param>
        public void SetFirstComputerName(string name)
        {
            SetComputerName(name);
            SyncEnvironment.ComputerNamed = true;
            SyncEnvironment.GetInstance().updateComputerNamed();

        }

        /// <summary>
        /// Used to set the initial computer name or to replace an exisiting
        /// computer name
        /// </summary>
        public void SetFirstSBSRun()
        {
            SyncEnvironment.FirstSBSRun = false;
            SyncEnvironment.GetInstance().updateFirstSBSRun();
        }
        /// <summary>
        /// Used to get if SBS has runned before
        /// </summary>
        /// <returns></returns>
        public bool IsFirstSBSRun()
        {
            return SyncEnvironment.FirstSBSRun;
        }

        /// <summary>
        /// Get the sbs drive letter
        /// </summary>
        /// <returns>sbs drive letter</returns>
        public char GetSBSDriveLetter()
        {
            string driveletter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId);
            if(driveletter.Length == 0)
                return SyncEnvironment.SBSDriveLetter;
            return driveletter[0];
        }

        public void SetSBSDriveLetter(char driveLetter)
        {
            SyncEnvironment.SBSDriveLetter = driveLetter;
        }

        /// <summary>
        /// Get the amount of free space to use that the user allow
        /// </summary>
        /// <returns>the amount of free space allowed</returns>
        public double GetFreeSpaceToUse()
        {
            return SyncEnvironment.FreeSpaceToUse;
        }

        /// <summary>
        /// Set the amount of free space to use that the user allow
        /// </summary>
        /// <param name="FreeSpaceToUse">the amount of free space allowed</param>
        public void SetFreeSpaceToUse(double FreeSpaceToUse)
        {
            SyncEnvironment.FreeSpaceToUse = FreeSpaceToUse;
        }

        public String GetResolution()
        {
            return SyncEnvironment.Resolution;
        }

        public void SetResolution(String Resolution)
        {
            SyncEnvironment.Resolution = Resolution;
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
