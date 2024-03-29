﻿// Developer to contact: Chua Peng Chin, Benson
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
using System.IO;
using SyncButler.Exceptions;
using SyncButler.MRU;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Win32;

namespace SyncButler
{
    /// <summary>
    /// This class is responsible for coordinating communications from and to the user interface as well as between internal logic classes
    /// </summary>
    public class Controller
    {
        private const long KILOBYTE = 1024;
        private const long MEGABYTE = KILOBYTE * 1024;
        private const long GIGABYTE = MEGABYTE * 1024;
		public enum SBSstatus {	Enabled	,Disabled}
        private SyncEnvironment syncEnvironment;
        private IGUI mainWindow;
        private static List<string> errorList = new List<string>();
        private static Controller controller;
        private string sbsLogfile;
        private static string[] startupParams;

		public static int ConflictCount {get; set;}
        public enum WinStates { Main, MiniPartnerships, CreatePartnership }

        /// <summary>
        /// Used by check and merged to see the total size of the files to be sync so far.
        /// </summary>
        private long totalSizeSoFar;

        /// <summary>
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of Controller.
        /// </summary>
        private Controller()
        {
            syncEnvironment = SyncEnvironment.GetInstance();
            //console = new SyncButlerConsole.Form1();
            //console.Show();
            //AddRegistryKey();
        }

        /// <summary>
        /// Gets or sets the SBS log file path.
        /// </summary>
        public string SBSLogFile
        {
            get { return this.sbsLogfile; }
            set { this.sbsLogfile = value; }
        }

        /// <summary>
        /// Checks if the program is running from a CD drive.
        /// </summary>
        /// <returns>true if it is, false otherwise.</returns>
        public static bool IsOnCDRom()
        {
            return SystemEnvironment.StorageDevices.GetDeviceType(Environment.GetCommandLineArgs()[0][0] + ":").Equals(SystemEnvironment.StorageDevices.DeviceType.CDRom);
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
        /// Sets the reference to the window currently shown to the user.
        /// </summary>
        /// <param name="win">A window which implements IGUI</param>
        /// <param name="overwrite">Determines whether the value will be overwritten or not.</param>
        public void SetWindow(IGUI win, bool overwrite)
        {
            if (mainWindow == null || overwrite)
                mainWindow = win;
        }

        /// <summary>
        /// Attempts to grab focus.
        /// </summary>
        /// <param name="ws">The state (a page in the UI) to go into after grabbibg focus.</param>
        public void GrabFocus(WinStates ws)
        {
            if (mainWindow != null)
                mainWindow.GrabFocus(ws);
        }

        /// <summary>
        /// get a list of usb drive letters
        /// </summary>
        /// <returns>a list of usb drive letters</returns>
        public static List<WindowDriveInfo> GetUSBDriveLetters()
		{
			return WindowDriveInfo.GetDriveInfo(SyncButler.SystemEnvironment.StorageDevices.GetRemovableDeviceDriveLetters());
		}

        /// <summary>
        /// Get a list of non usb drives letters
        /// </summary>
        /// <returns>a ist of non usb drive letters</returns>
        public static List<WindowDriveInfo> GetNonUSBDriveLetters()
        {
            return WindowDriveInfo.GetDriveInfo(SyncButler.SystemEnvironment.StorageDevices.GetNonUSBDriveLetters());
        }

        /// <summary>
        /// Tests for the existence of another instance and sets up single instance listener if this is the first instance
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static bool TestSingleInstance(String[] args)
        {
            // test if this is the first instance and does the initialisation for the single instancing
            if (!SingleInstance.IsFirst(new SingleInstance.ReceiveDelegate(ReceiveAction)))
            {
                // send command line args to running app, then terminate
                SingleInstance.Send(args);
                SingleInstance.Cleanup();
                return false;
            }
            // This is the 1st instance. Handle the arguments later.
            SaveStartArguments(args);
            return true;
        }

        /// <summary>
        /// Saves the startup parameter to be handled later.
        /// </summary>
        /// <param name="args">Startup parameter</param>
        private static void SaveStartArguments(string[] args)
        {
            startupParams = args;
        }

        /// <summary>
        /// Handles incoming data from other instances.
        /// </summary>
        /// <param name="args">Command line arguments - Format: [flag] [path]</param>
        private static void ReceiveAction(string[] args)
        {
            Controller control = Controller.GetInstance();
            string output = "";
            foreach (string str in args)
                output = output + str + " ";
            output = output.TrimEnd();

            if (!(args.Length == 2))
            {
                Logging.Logger.GetInstance().WARNING("Invalid Command Line Arguments" + output);
                return;
            }
             
            Logging.Logger.GetInstance().DEBUG(output + " received");
            switch (args[0])
            {
                case "-addmini":
                    GetInstance().mainWindow.FillInCreatePartnership(TrimPath(args[1]));
                    break;
                default:
                    //unknown commands
                    Logging.Logger.GetInstance().WARNING("Unknown Command Line Arguments" + output);
                    break;
            }
        }

        /// <summary>
        /// Trims a path provided for use in FillInCreatePartnership.
        /// </summary>
        /// <param name="path">The string of the path to trim.</param>
        /// <returns>The path with invalid characters trimmed away.</returns>
        private static string TrimPath(string path)
        {
            path = path.Trim();
            path = path.Trim('"', '?', '*', '|', '>', '<');

            if (path.EndsWith(":"))
                path += "\\";

            return path;
        }

        /// <summary>
        /// Adds to the list of mini partnerships.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string AddToMiniPartnerships(string path)
        {
            try
            {
                SyncEnvironment.GetInstance().AddMiniPartnership(path);
            }
            catch (ArgumentException ae)
            {
                return ae.Message;
            }
            return "";
        }        

        /// <summary>
        /// Checks if the drive for SBS has enough space.
        /// </summary>
        /// <returns>true if the drive has enough space based on the set user limit, false otherwise</returns>
        public bool IsSBSDriveEnough()
        {
            long required = GetUserLimit();
            try
            {
                string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId, SyncEnvironment.SBSDrivePartition);
                if (driveLetter.Length == 0)
                    return false;
                long availableSpace = SystemEnvironment.StorageDevices.GetAvailableSpace(driveLetter);
                return required <= availableSpace;
            }
            catch (Exceptions.DriveNotSupportedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the Sync Butler, Sync! feature is enabled.
        /// </summary>
        /// <returns>true if it is enabled, false otherwise</returns>
        public static bool IsSBSEnable()
        {
            return SyncEnvironment.SBSEnable.Equals(SBSstatus.Enabled.ToString());
        }

        /// <summary>
        /// Checks if SBS can proceed
        /// </summary>
        /// <returns>true if the feature is enabled, auto sync is allowed and the external drive is not missing</returns>
        public static bool CanDoSBS()
        {
            List<WindowDriveInfo> DriveLetters = null;
            DriveLetters = Controller.GetUSBDriveLetters();
            if (DriveLetters.Count == 0)
                return false;
            else
                return Controller.IsSBSEnable() && DriveLetters.Contains(Controller.GetSBSDriveLetter());
        }

        /// <summary>
        /// Used to retrive the amount of space available for the SBS drive
        /// </summary>
        /// <returns>Space available for the SBS drive</returns>
        public long GetAvailableSpaceForDrive()
        {
            try
            {
                string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId,SyncEnvironment.SBSDrivePartition);
                if (driveLetter.Length == 0)
                    return 0;
                long AvailableSpace = SystemEnvironment.StorageDevices.GetAvailableSpace(driveLetter);
                string res = SyncEnvironment.Resolution;
                
                return GetSizeInResolution(res, AvailableSpace);
            }
            catch (Exceptions.DriveNotSupportedException)
            {
                return 0;
            }
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

            CheckValidPaths(leftPath, rightPath, appPath);

            syncEnvironment.AddPartnership(name, leftPath, rightPath);
        }

        /// <summary>
        /// Checks a path and throws an exception if it contains the application directory.
        /// </summary>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        /// <param name="appPath">Path to the application</param>
        /// <exception cref="UserInputException">Cannot create a partnership with the same directories</exception>
        /// <exception cref="UserInputException">Cannot create a partnership which contains the SyncButler directory</exception>
        private static void CheckValidPaths(String leftPath, String rightPath, string appPath)
        {
            if (WindowsFileSystem.PathsEqual(leftPath, rightPath))
            {
                Logging.Logger.GetInstance().WARNING("UserInputException reached in Controller.CheckValidPaths() which should have been caught in the UI.");
                throw new UserInputException("Cannot create a partnership with the same directories.");
            }

            if (WindowsFileSystem.PathsEqual(leftPath, appPath) || WindowsFileSystem.PathsEqual(rightPath, appPath))
                throw new UserInputException("Cannot create a partnership on the running Sync Butler directory!");
        }
        
        /// <summary>
        /// Delete a partnership from the list of partnerships based at an index.
        /// </summary>
        /// <param name="idx">Index of the partnership to be deleted.</param>
        public void DeletePartnership(int index)
        {
            syncEnvironment.RemovePartnership(index);
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
        /// Delete a partnership from the list of mini partnerships based at an index.
        /// </summary>
        /// <param name="index">Index of the partnership to be deleted.</param>
        public void DeleteMiniPartnership(int index)
        {
            syncEnvironment.RemoveMiniPartnership(index);
        }

        /// <summary>
        /// Delete a partnership from the list of mini partnerships based on the friendly name.
        /// </summary>
        /// <param name="name">The name of the mini partnership to be deleted.</param>
        public void DeleteMiniPartnership(string name)
        {
            syncEnvironment.RemoveMiniPartnership(name);
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

            CheckValidPaths(leftPath, rightPath, appPath);

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
        public static Partnership GetPartnership(string friendlyName)
        {
            return SyncEnvironment.GetInstance().GetPartnership(friendlyName);
        }

        /// <summary>
        /// Starts syncing a partnership.
        /// </summary>
        /// <param name="idx">Index of the partnership to be synced.</param>
        /// <returns>ObservableCollection conflict list with a list of conflicts. Will be null if there are no conflicts.</returns>
        public ConflictList SyncPartnership(String name, SyncableStatusMonitor monitor, SyncableErrorHandler errorHandler, SortedList<string, Partnership> partnershipList)
        {
			Partnership curPartnership = partnershipList[name];

            curPartnership.statusMonitor = monitor;
            curPartnership.errorHandler = errorHandler;
			List<Conflict> conflict = curPartnership.Sync();
			ConflictCount+= conflict.Count;
            curPartnership.statusMonitor = null;

            return new ConflictList(conflict, name);
        }

        /// <summary>
        /// Calls the method to clean up orphaned checksums.
        /// </summary>
        /// <param name="partnershipName"></param>
        /// <param name="partnershipList"></param>
        public void CleanUpOrphans(String partnershipName, SortedList<string,Partnership> partnershipList)
        {
            partnershipList[partnershipName].CleanOrphanedChecksums();
        }

        /// <summary>
        /// Removes all conflicts which can be auto resolved by the program and returns a list of conflicts which can't
        /// </summary>
        /// <param name="cl">a ConflictList containing all conflicts</param>
        /// <returns>A list of Conflict objects which require user intervention</returns>
        public static List<Conflict> RemoveAutoResolvableConflicts(ConflictList cl)
        {
            List<Conflict> resolvableConflicts = new List<Conflict>();
            for (int i = cl.Conflicts.Count - 1; i >= 0; i--)
            {
                Conflict c = cl.Conflicts[i];
                if (c.AutoResolveAction != Conflict.Action.Unknown)
                {
                    resolvableConflicts.Add(c);
                    cl.Conflicts.RemoveAt(i);
                }
            }

            return resolvableConflicts;
        }

        /// <summary>
        /// Resolves a conflict
        /// </summary>
        /// <param name="toResolve">The Conflict to resolve</param>
        /// <param name="onProgressUpdate">A SyncableStatusMonitor</param>
        /// <param name="worker">BackgroundWorker for threaded processing</param>
        /// <returns></returns>
        public static Resolved ResolveConflict(Conflict toResolve, SyncableStatusMonitor onProgressUpdate, BackgroundWorker worker)
        {
            worker.ReportProgress(0, toResolve.GetPartnership().Name);
            toResolve.SetStatusMonitor(onProgressUpdate);
            Resolved ret = toResolve.Resolve();
            toResolve.SetStatusMonitor(null);
            return ret;
        }

        /// <summary>
        /// To check if size existing file list the given file list will within the limit given 
        /// by the users. Used by GetMonitoredFiles(..);
        /// </summary>
        /// <param name="ToMerge">The existing file list</param>
        /// <param name="FileListToCheck"> the file list that is new and required check</param>
        /// <param name="limit">the limit imposed by the user</param>
        /// <returns>true if all the file to check is merged, false if no or some of the files are merged</returns>
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
                long CurrentFileSize;
                foreach (string key in FileListToCheck.Keys)
                {
                    CurrentFileSize = WindowsFile.SizeOf(FileListToCheck[key]);
                    if ((totalSizeSoFar += CurrentFileSize) <= limit)
                    {
                        ToMerge.Add(key, FileListToCheck[key]);
                        //return false;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Returns a given size based on the resolution
        /// </summary>
        /// <param name="resolution">string representation of the required resolution {GB,MB,KB,Bytes}</param>
        /// <param name="size">the size in bytes</param>
        /// <returns>the size in the specified resolution</returns>
        private static long GetSizeInResolution(string resolution, long size)
        {
            
            if (resolution.Equals("GB"))
            {
                return size / GIGABYTE;
            }
            else if (resolution.Equals("MB"))
            {
                return size / MEGABYTE;
            }
            else if (resolution.Equals("KB"))
            {
                return size / KILOBYTE;
            }
            else if (resolution.Equals("Bytes"))
            {
                return size;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns the limit set by the user in bytes
        /// </summary>
        /// <returns>size limit in bytes</returns>
        private long GetUserLimit()
        {
            String resolution = this.GetResolution();
            double freeSpaceTouse =  this.GetFreeSpaceToUse();
            if (resolution.Equals("GB"))
            {
                return  (long)(freeSpaceTouse * GIGABYTE);
            }
            else if (resolution.Equals("MB"))
            {
                return (long)(freeSpaceTouse * MEGABYTE);
            }
            else if (resolution.Equals("KB"))
            {
                return (long)(freeSpaceTouse * KILOBYTE);
            }
            else if (resolution.Equals("Bytes"))
            {
                return (long)freeSpaceTouse;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        String[] mrulevels = { "interestingHigh", "interestingMedHigh", "interestingMed", "interestingLowMed", "interestingLow", "interestingUltraLow" };

        /// <summary>
        /// Returns a list of most recently used files.
        /// </summary>
        public SortedList<string,SortedList<string,string>> GetMonitoredFiles(SyncableStatusMonitor statusMonitor, bool preview)
        {
            totalSizeSoFar = 0;

            long limit;
            if (preview)
                limit = 250 * 1024 * 1024;
            else
                limit = GetUserLimit();
            
            SortedList<string, SortedList<string, string>> rtn = new SortedList<string, SortedList<string, string>>();
            SortedList<string, string> interesting = new SortedList<string, string>();
            MostRecentlyUsedFile.statusMonitor = statusMonitor;
            SortedList<string,SortedList<string,string>> splited = ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));
            
            
            for (int i = 0; i < mrulevels.Length; i++)
            {
                CheckAndMerge(interesting, splited[mrulevels[i]], limit);
            }
            
            SortedList<string, string> sensitive = splited["sensitive"];
            rtn.Add("sensitive", sensitive);
            rtn.Add("interesting", interesting);
            MostRecentlyUsedFile.statusMonitor = null;
            return rtn;
        }

        /// <summary>
        /// Retrieves sorted lists of recently used files separated into bands of interest levels.
        /// </summary>
        /// <returns>A sorted list (based on levels of interest) of sorted lists of MRUs.</returns>
        public static SortedList<string, SortedList<string, string>> GetMonitoredFiles()
        {
            SortedList<string, SortedList<string, string>> ret = ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));
            return ret;
        }

        /// <summary>
        /// Get the username of the current logon user
        /// </summary>
        /// <returns>Return the user name</returns>
        public static String GetCurrentLogOnUser()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        }

        /// <summary>
        /// Get the path to the folder where SBS will store files.
        /// </summary>
        /// <returns>A string of the path.</returns>
        public static string GetSBSPath()
        {
            return GetSBSPath(SyncEnvironment.SBSDriveLetter);
        }

        /// <summary>
        /// Currently unused.
        /// </summary>
        /// <returns></returns>
        public string AutoSyncRecentFiles()
        {
            long limit = GetUserLimit();
            SortedList<string, SortedList<string, string>> MRUs = new SortedList<string, SortedList<string, string>>();
            SortedList<string, string> interesting = new SortedList<string, string>();
            SortedList<string, SortedList<string, string>> splited = ContentFilters.Spilt(MostRecentlyUsedFile.ConvertToSortedList(MostRecentlyUsedFile.GetAll()));


            for (int i = 0; i < mrulevels.Length; i++)
            {
                CheckAndMerge(interesting, splited[mrulevels[i]], limit);
            }
            MRUs.Add("interesting", interesting);
            return SyncMRUs(MRUs["interesting"]);
        }

        /// <summary>
        /// Currently unused.
        /// </summary>
        /// <param name="toSync"></param>
        /// <returns></returns>
        public string SyncMRUs(SortedList<string, string> toSync)
        {
            char driveLetter = SyncEnvironment.SBSDriveLetter;
            string driveid = SyncEnvironment.SBSDriveId;
            int drivePartition = SyncEnvironment.SBSDrivePartition;
            String errorMsg = "";
            try
            {
                if (SystemEnvironment.StorageDevices.GetDriveLetter(driveid, drivePartition).Length != 0)
                {
                    string syncTo = GetSBSPath(driveLetter);
                    if (WindowsFolder.CheckIfUserHasRightsTo(syncTo, GetCurrentLogOnUser()))
                    {
                        driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(driveid, drivePartition)[0];
                        MRUList mruList = new MRUList();
                        mruList.Load(toSync);
                        mruList.Sync(SyncEnvironment.ComputerName, driveLetter);
                        SBSLogFile = syncTo;
                        MRUList.SaveInfoTo(syncTo + "Open this Report in a Browser.xml", mruList);
                    }
                    else
                    {
                        errorMsg = "Permisson denied\nPlease check if you have the rights to the folder for SBS at " + driveLetter + ":\\SyncButler\\";
                    }

                }
                else
                {
                    errorMsg = "Device not detected\nPlease plug in the device configured for SBS.";
                }
            }
            catch (Exceptions.DriveNotSupportedException)
            {
                errorMsg = "Device not detected\nPlease plug in the device configured for SBS.";
            }
            return errorMsg;
        }

        /// <summary>
        /// Sync the mrus that are listed. Please read MRUList to understand how file is actually saved.
        /// </summary>
        /// <param name="driveLetter"></param>
        public void SyncMRUs(SortedList<string, string> toSync, SyncableStatusMonitor statusMonitor, SyncableErrorHandler errorHandler)
        {
            char driveLetter = SyncEnvironment.SBSDriveLetter;
            string driveid = SyncEnvironment.SBSDriveId;
            int drivePartition = SyncEnvironment.SBSDrivePartition;
            try
            {
                if (SystemEnvironment.StorageDevices.GetDriveLetter(driveid, drivePartition).Length == 0)
                {
                    errorHandler.Invoke(new Exception("Device not detected\nPlease plug in the device configured for SBS."));
                }
                else
                {
                    string syncTo = GetSBSPath(driveLetter);
                    if (!WindowsFolder.CheckIfUserHasRightsTo(syncTo, GetCurrentLogOnUser()))
                    {
                        errorHandler.Invoke(new Exception("Permisson denied\nPlease check if you have the rights to the folder for SBS at " + driveLetter + ":\\SyncButler\\"));
                    }
                    else
                    {
                        driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(driveid,drivePartition)[0];

                        MRUList mruList = new MRUList();

                        mruList.SetStatusMonitor(statusMonitor);
                        mruList.SetErrorHandler(errorHandler);
                        mruList.Load(toSync);
                        mruList.Sync(SyncEnvironment.ComputerName, driveLetter);
                        SBSLogFile = syncTo;
                        MRUList.SaveInfoTo(syncTo + "Open this Report in a Browser.xml", mruList);
                    }
                }
            }
            catch (Exceptions.DriveNotSupportedException)
            {
                errorHandler.Invoke(new Exception("Device not detected\nPlease plug in the device configured for SBS."));
            }
        }

        /// <summary>
        /// Get the path where SBS will store files, given the drive letter.
        /// </summary>
        /// <param name="driveLetter">The drive letter to use with SBS.</param>
        /// <returns>A string containing the path.</returns>
        private static string GetSBSPath(char driveLetter)
        {
            string syncTo = driveLetter + ":\\SyncButler\\" + SyncEnvironment.ComputerName + "\\";
            return syncTo;
        }

        /// <summary>
        /// Opens a file in its associated viewer.
        /// </summary>
        /// <param name="fileName">The path to the file name.</param>
        public static void OpenFile(string fileName)
        {
                System.Diagnostics.Process.Start(fileName);
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
        /// <returns>True if this is the first run, false otherwise</returns>
        public static bool IsFirstRun()
        {
            return !SyncEnvironment.FirstRunComplete || !SyncEnvironment.ComputerNamed;
        }

		/// <summary>
		/// Assigns the settings and stores it to disk.
		/// </summary>
		/// <param name="ComputerName">Computer name of the user</param>
		/// <param name="EnableSBS">[Not in use]If the user wants sbs to be enabled</param>
		/// <param name="SBSDrive">The working drive letter</param>
        public static void SaveSetting(string computerName, string enableSBS, char SBSDrive, Double freeSpaceToUse, String resolution)
        {

            SyncEnvironment.ComputerName = computerName;
            SyncEnvironment.SBSEnable = enableSBS;
            SyncEnvironment.FreeSpaceToUse = freeSpaceToUse;
            SyncEnvironment.Resolution = resolution;
            if (enableSBS.Equals(SBSstatus.Enabled.ToString()))
            {
                SyncEnvironment.SBSDriveLetter = SBSDrive;
                SyncEnvironment.SBSDriveId = SystemEnvironment.StorageDevices.GetDriveID(SBSDrive + ":");
                SyncEnvironment.SBSDrivePartition = SystemEnvironment.StorageDevices.GetDrivePartitionIndex(SBSDrive + ":");
            }
            else
            {
                SyncEnvironment.SBSDriveLetter = '1';
                SyncEnvironment.SBSDriveId = null;
                SyncEnvironment.SBSDrivePartition = -1;
            }
            

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
        public static void SetComputerName(string name)
        {
            SyncEnvironment.ComputerName = name;
        }

        /// <summary>
        /// Gets or sets the status of the SBS feature.
        /// </summary>
        public static string SBSEnable
        {
            get { return (SyncEnvironment.SBSEnable == null) ? SBSstatus.Disabled.ToString() : SyncEnvironment.SBSEnable; }
            set { SyncEnvironment.SBSEnable = value; }
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
        public static void SetFirstSBSRun()
        {
            SyncEnvironment.FirstSBSRun = false;
            SyncEnvironment.GetInstance().updateFirstSBSRun();
        }

        /// <summary>
        /// Used to get if SBS has runned before
        /// </summary>
        /// <returns></returns>
        public static bool IsFirstSBSRun()
        {
            return SyncEnvironment.FirstSBSRun;
        }

        /// <summary>
        /// Get the sbs drive letter
        /// </summary>
        /// <returns>sbs drive letter</returns>
        public static WindowDriveInfo GetSBSDriveLetter()
        {
            char sbsDriveLetter;

            if (SyncEnvironment.SBSDriveId == null || SyncEnvironment.SBSDriveId.Length == 0) return null;

            try
            {
                string driveletter = SystemEnvironment.StorageDevices.GetDriveLetter(SyncEnvironment.SBSDriveId, SyncEnvironment.SBSDrivePartition);

                if (driveletter.Length == 0)
                    return null;
                else sbsDriveLetter = driveletter[0];

                WindowDriveInfo wdi = null;
                try
                {
                    wdi = new WindowDriveInfo(sbsDriveLetter);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Equals("invalid drive"))
                        throw ex;
                }
                return wdi;
            }
            catch (Exceptions.DriveNotSupportedException)
            {
                return new WindowDriveInfo("" + SyncEnvironment.SBSDriveLetter);
            }
        }

        /// <summary>
        /// Sets the drive letter for SBS to store its files on.
        /// </summary>
        /// <param name="driveLetter">The drive letter of the path.</param>
        public static void SetSBSDriveLetter(char driveLetter)
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
        public static void SetFreeSpaceToUse(double FreeSpaceToUse)
        {
            SyncEnvironment.FreeSpaceToUse = FreeSpaceToUse;
        }


        /// <summary>
        /// Returns the current resolution
        /// </summary>
        /// <returns></returns>
        public string GetResolution()
        {
            return SyncEnvironment.Resolution;
        }

        /// <summary>
        /// Sets the resolution
        /// </summary>
        /// <param name="resolution">One of the following {GB,MB,KB,Bytes}</param>
        public static void SetResolution(string resolution)
        {
            SyncEnvironment.Resolution = resolution;
        }

        /// <summary>
        /// Remove the shell integration context menu from the registry
        /// and disable the settings
        /// </summary>
        public static void RemoveDisableContextMenu()
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
        
        /// <summary>
        /// Adds the registry key for the context menu
        /// </summary>
        /// <summary>
        /// Adds the registry key for Mini-Sync right-click function.
        /// </summary>
        //public void AddRegistryKey()
        //{
        //    try
        //    {
        //        RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Folder\shell");
        //        //key.SetValue(null, "open");
        //        RegistryKey sbs = key.CreateSubKey("Sync this folder!");
        //        sbs.CreateSubKey("command").SetValue(null, System.Reflection.Assembly.GetEntryAssembly().Location + " -addmini \"%1\" ");
        //        sbs.SetValue("icon", System.Reflection.Assembly.GetEntryAssembly().Location);
        //        sbs.SetValue("MultiSelectModel", "Single");
        //    }
        //    catch (Exception e)
        //    {
        //        Logging.Logger.GetInstance().WARNING("Error on adding registry key" + e.Message);
        //    }
        //}

        /// <summary>
        /// Called after initialisation to process the startup arguments.
        /// </summary>
        public static void HandleStartupArgs()
        {
            if (startupParams != null)
                Controller.ReceiveAction(startupParams);
        }
    }
}