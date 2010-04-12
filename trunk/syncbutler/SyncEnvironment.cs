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
using System.Configuration;
using System.IO;
using SyncButler.ProgramEnvironment;
using SyncButler.Exceptions;
using System.Xml;
using System.Reflection;
using System.Diagnostics;

namespace SyncButler
{
    /// <summary>
    /// Contains methods to load and store settings as well as handling access to any environmental attributes
    /// </summary>
    public class SyncEnvironment
    {
        //List of constants used in the Sync Butler
        private const string DEFAULT_SBS_RELATIVE_PATH = "SyncButler\\";
        private const string DEFAULT_SBS_FILENAME_POSTFIX = "_Details.txt";
        private const string SYNCBUTLER_SETTINGS_EXTENSION = ".butler";
        
        ///List of persistence attributes
        private SortedList<string,Partnership> partnershipList;
        private SortedList<string,Partnership> miniPartnershipList;
        private static bool allowAutoSyncForConflictFreeTasks;
        private static bool firstRunComplete;
        private static bool computerNamed;
        private static bool enableShellContext;//Defaults to false
        private static long fileReadBufferSize = 2048000; //2MB, How much of the data file is read each cycle. Default value.
        private static string computerName;
        private static bool firstSBSRun;
        private static string sbsEnable;
        private static string sbsDriveId;
        private static int sbsDrivePartition;
        private static char sbsDriveLetter;
        private static string resolution;
        private static double freeSpaceToUse;
        //private static List<String> unwanted;

        //List of runtime variables
        private static System.Configuration.Configuration config;
        private static string settingName = "systemSettings";
        private static string partnershipName = "partnership";
        private static SettingsSection storedSettings;
        private static PartnershipSection storedPartnerships;
        private static SyncEnvironment syncEnv;
        private static Assembly syncButlerAssembly;
        private static string _appPath;

        /// <summary>
        /// Path to thumbdrive for mini partnerships
        /// </summary>
        private string miniSyncPath 
        {
            get
            {
                return sbsDriveLetter + @":\" + DEFAULT_SBS_RELATIVE_PATH + computerName + @"\";
            }
        }

        /// <summary>
        /// This constructor will automatically restore a previous sessions or create new ones if one is not found.
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of SyncEnvironment.
        /// </summary>
        private SyncEnvironment()
        {
            _appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            IntialEnv();
            
        }

        /// <summary>
        /// Returns an instance of SyncEnvironment (ensures 1 to 1 coupling with controller)
        /// Creates a new instance if necessary, otherwise, it will use an already available instance
        /// </summary>
        /// <returns>An instance of SyncEnvironment</returns>
        public static SyncEnvironment GetInstance()
        {
            if (syncEnv == null)
                syncEnv = new SyncEnvironment();

            return syncEnv;
        }

        /// <summary>
        /// Returns the entire partnership list. This is useful when the controller
        /// needs to view what partnerships are there thus far.
        /// </summary>
        /// <returns>A List containing all existing Partnership</returns>
        public SortedList<string, Partnership> GetPartnershipsList()
        {
            return partnershipList;
        }

        /// <summary>
        /// Returns the mini partnership list.
        /// </summary>
        /// <returns>A sorted list of mini partnerships</returns>
        public SortedList<string, Partnership> GetMiniPartnershipsList()
        {
            return miniPartnershipList;
        }

        /// <summary>
        /// Returns the partnership with unique key (the friendly name)
        /// </summary>
        /// <param name="name">The name (used as an index) of the partnership that is to be loaded</param>
        /// <returns>A Partnership object (possible to be null if not found)</returns>
        public Partnership GetPartnership(string name)
        {
            return partnershipList[name];
        }

        /// <summary>
        /// Adds a properly created Partnership object into the list of partnership
        /// </summary>
        /// <param name="name">Friendly name of the partnership, must be unique, caps ignored</param>
        /// <param name="leftPath">Full path of the left folder of the partnership, must be unique, caps ignored</param>
        /// <param name="rightPath">Full path of the right folder of the partnership, must be unique, caps ignored</param>
        /// <exception cref="ArgumentNullException">Thrown if the key is a null reference.</exception>
        /// <exception cref="ArgumentException">Thrown if the name of the partnership already exists.</exception>
        public void AddPartnership(string name, string leftPath, string rightPath)
        {
            System.Diagnostics.Debug.Assert((name != null) && (name.Length > 0));
            Partnership element = CreatePartnership(name, leftPath, rightPath);

            if (!IsValidSystemObject(leftPath))
                throw new ArgumentException("Folder 1 is a system folder which cannot be synchronised.\n \nPlease select another folder.");

            if (!IsValidSystemObject(rightPath))
                throw new ArgumentException("Folder 2 is a system folder which cannot be synchronised.\n \nPlease select another folder.");

            if (!IsUniquePartnershipName(name, partnershipList))
                throw new ArgumentException("The name is already in use.\n \nPlease input another name.");

            //Usually CheckIsUniquePartnership will check for IsUniquePartnershipName too
            //since we checked it already, the exception caught will be more specified
            if (!IsUniquePartnershipPath(name, leftPath, rightPath, partnershipList))
                throw new ArgumentException("This partnership already exists.");

            partnershipList.Add(name, element);
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// Checks if a given path to see if it can be synchronised.
        /// </summary>
        /// <param name="path">Path to be checked</param>
        /// <returns>False if it belongs to a special system folder.</returns>
        private bool IsValidSystemObject(string path)
        {
            List<string> specialItems = new List<string>();
            specialItems.Add(@":\$RECYCLE.BIN"); // Recycle Bins
            specialItems.Add(@":\SYSTEM VOLUME INFORMATION"); // System Volume Information
            specialItems.Add(@":\HIBERFIL.SYS"); // Hibernation file
            specialItems.Add(@":\PAGEFILE.SYS"); // Page file
            string comparatorPath = path.ToUpper();

            foreach (string str in specialItems)
            {
                if (comparatorPath.Contains(str))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Adds a Mini Partnership to the list of mini partnerships
        /// </summary>
        /// <param name="source">full path to the source</param>
        public void AddMiniPartnership(string source)
        {
            string name = source;
            bool fileExist = File.Exists(source);
            bool directoryExist = Directory.Exists(source);
            //Check that the path exists.
            if (!(fileExist || directoryExist))
                throw new ArgumentException("This path does not exist.");

            if (!IsValidSystemObject(source))
                throw new ArgumentException("Selected folder is a system folder which cannot be synchronised.");
            
            System.Diagnostics.Debug.Assert((name != null) && (name.Length > 0));
            Partnership element;

            if (fileExist)
            {
                FileInfo fi = new FileInfo(source);
                element = CreatePartnership(name, source, miniSyncPath + fi.Name);            
            }
            else //(directoryExist)
            {
                DirectoryInfo di = new DirectoryInfo(source);
                element = CreatePartnership(name, source, miniSyncPath + di.Name);
            }
           
            //The name is the path.
            if (!IsUniquePartnershipName(name, miniPartnershipList))
                throw new ArgumentException("This path is already in use.");

            //Usually CheckIsUniquePartnership will check for IsUniquePartnershipName too
            //since we checked it already, the exception caught will be more specified
            if (!IsUniquePartnershipPath(name, source, miniSyncPath, miniPartnershipList))
                throw new ArgumentException("This partnership already exists.");

            miniPartnershipList.Add(name, element);
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// Removes specified partnership in the partnership list
        /// </summary>
        /// <param name="idx">Removes the partnership at that particular index</param>
        public void RemovePartnership(int idx)
        {
            partnershipList.RemoveAt(idx);
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// Removes specified mini partnership in the mini partnership list
        /// </summary>
        /// <param name="idx">Removes the mini partnership at that particular index</param>
        public void RemoveMiniPartnership(int idx)
        {
            miniPartnershipList.RemoveAt(idx);
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// Removes specified partnership in the partnership list
        /// </summary>
        /// <param name="name">Name of the partnership to remove.</param>
        public void RemovePartnership(string name)
        {
            partnershipList.Remove(name);
            StoreEnv();
        }

        /// <summary>
        /// Removes specified mini partnership in the mini partnership list
        /// </summary>
        /// <param name="idx">Removes the mini partnership at a given path</param>
        public void RemoveMiniPartnership(string path)
        {
            miniPartnershipList.Remove(path);
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// When there is a change in any Partnership, this method is called
        /// and supplied with the name of the Partnership object in the
        /// List of Partnerships.
        /// </summary>
        /// <param name="oldName">Old friendly name of a partnership</param>
        /// <param name="newName">New friendly name of a partnership</param>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        public void UpdatePartnership(string oldname, string newname, string leftPath, string rightPath)
        {
            //Prepare to write to Partnership list
            Partnership backupElement = partnershipList[oldname];
            Partnership updated = CreatePartnership(newname, leftPath, rightPath);
            //remove the old partnership
            partnershipList.Remove(oldname);

            //Conditions check
            bool nameUnchanged = newname.Trim().ToLower() == oldname.Trim().ToLower();
            bool pathUnchanged = false;
            try
            {
                if (!IsValidSystemObject(leftPath))
                    throw new UserInputException("Folder 1 is a system folder which cannot be synchronised.\n \nPlease select another folder.");

                if (!IsValidSystemObject(rightPath))
                    throw new UserInputException("Folder 2 is a system folder which cannot be synchronised.\n \nPlease select another folder.");

                //Checks if the user try to update the partnership with paths which is similar to existing partnerships
                if (!IsUniquePartnershipPath(updated.Name, updated.LeftFullPath, updated.RightFullPath, partnershipList))
                {
                    throw new UserInputException("Partnership already exists.\nUnable to update this partnership.");
                }

                //See if the friendly name is already in used (even after it is removed)
                if (!IsUniquePartnershipName(newname, partnershipList))
                {
                    throw new UserInputException("The selected name is already in use.\n \nPlease select another name.");
                }

                if (WindowsFileSystem.PathsEqual(leftPath, backupElement.LeftFullPath)
                    &&
                    WindowsFileSystem.PathsEqual(rightPath, backupElement.RightFullPath))
                    pathUnchanged = true;
            }
            catch (Exception e)
            {
                partnershipList.Add(oldname, backupElement);
                throw e;
            }
            
            //Path is unchanged, only name is changed
            if (nameUnchanged == false && pathUnchanged == true)
            {
                //Partnership checksum dictionary is retained
                backupElement.Name = newname;
                partnershipList.Add(newname, backupElement);
            }
            // 3 cases:
            // Nothing is changed.
            // Name is the same, only path is changed.
            // Both changed, old element is removed and new one is added.
            else
            {
                partnershipList.Add(newname, updated);
            }
            
            StoreEnv(); //Save the partnership to disk immediately
        }

        /// <summary>
        /// Stores the settings for the program to persistent storage
        /// during program shut down
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Throws ConfigurationErrorsException if the program
        /// is unable to write to disk</exception>
        public void StoreEnv()
        {
            //Update the settings in the config file
            ConvertPartnershipList2XML();
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = allowAutoSyncForConflictFreeTasks;
            storedSettings.SystemSettings.FileReadBufferSize = fileReadBufferSize;
            storedSettings.SystemSettings.EnableShellContext = enableShellContext;
            storedSettings.SystemSettings.ComputerName = computerName;
            storedSettings.SystemSettings.SBSDriveLetter = SBSDriveLetter;
            storedSettings.SystemSettings.SBSEnable = SBSEnable;
            storedSettings.SystemSettings.FreeSpaceToUse = freeSpaceToUse;
            storedSettings.SystemSettings.Resolution = resolution;
            storedSettings.SystemSettings.SBSDriveId = sbsDriveId;
            storedSettings.SystemSettings.SBSDrivePartition = sbsDrivePartition;

            // Write to file
            if (SearchForSettingsFile() == null)
            {
                ReIntialEnv();
                StoreEnv();
            }
            else
            {
                config.Save(ConfigurationSaveMode.Modified);
            }
        }

        /// <summary>
        /// This is a light weight version of the store environment. It only
        /// saves the updated settings
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Throws ConfigurationErrorsException if the program
        /// is unable to write to disk</exception>
        public void StoreSettings()
        {
            //Update the settings in the config file
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = allowAutoSyncForConflictFreeTasks;
            storedSettings.SystemSettings.FileReadBufferSize = fileReadBufferSize;
            storedSettings.SystemSettings.EnableShellContext = enableShellContext;
            storedSettings.SystemSettings.ComputerName = computerName;
            storedSettings.SystemSettings.SBSDriveLetter = SBSDriveLetter;
            storedSettings.SystemSettings.SBSEnable = SBSEnable;
            storedSettings.SystemSettings.FreeSpaceToUse = FreeSpaceToUse;
            storedSettings.SystemSettings.Resolution = Resolution;
            storedSettings.SystemSettings.SBSDriveId = SBSDriveId;
            storedSettings.SystemSettings.SBSDrivePartition = SBSDrivePartition;

            // Write to file
            if (SearchForSettingsFile() == null)
            {
                ReIntialEnv();
                StoreSettings();
            }
            else
            {
                config.Save(ConfigurationSaveMode.Modified);
            }
        }

        /// <summary>
        /// This method is called during program startup to restore all
        /// the previous states of the program.
        /// </summary>
        public void RestoreEnv()
        {
            //Restore partnership stored in the XML settings file
            storedPartnerships = (PartnershipSection)config.GetSection(partnershipName);

            if (storedPartnerships == null)
                throw new ApplicationException("Could not load Stored Partnership List");

            //This restores the partnership list
            ConvertXML2PartnershipList();

            //This one restores general program settings
            allowAutoSyncForConflictFreeTasks = storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks;

            //This one restores the buffer size for reading files
            fileReadBufferSize = storedSettings.SystemSettings.FileReadBufferSize;

            //Gets the last stored friendly name of the computer
            computerName = storedSettings.SystemSettings.ComputerName;

            //Gets last stored status of namedComputer
            computerNamed = storedSettings.SystemSettings.ComputerNamed;

            //Gets the last stored status of whether SBS has runned before
            firstSBSRun = storedSettings.SystemSettings.FirstSBSRun;

            //Get the sbs drive letter
            SBSDriveLetter = storedSettings.SystemSettings.SBSDriveLetter;

            //Get the status of sbs
            SBSEnable = storedSettings.SystemSettings.SBSEnable;

            //Get the free space
            freeSpaceToUse = storedSettings.SystemSettings.FreeSpaceToUse;

            //Get the resolution
            resolution = storedSettings.SystemSettings.Resolution;

            //Get sbs drive id
            sbsDriveId = storedSettings.SystemSettings.SBSDriveId;

            //Get partition id
            sbsDrivePartition = storedSettings.SystemSettings.SBSDrivePartition;
        }

        /// <summary>
        /// If the settings files do not exist. A basic framework is created and stored. This
        /// will store the settings of a previous interations. 
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Throws ConfigurationErrorsException if the program
        /// is unable to write to disk</exception>
        public void CreateEnv()
        {
            //Create the list of partnerships 
            partnershipList = new SortedList<string,Partnership>();
            miniPartnershipList = new SortedList<string, Partnership>();
            
            // Add in default settings to XML file
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = true;
            storedSettings.SystemSettings.FirstRunComplete = true;
            storedSettings.SystemSettings.EnableShellContext = enableShellContext;
            storedSettings.SystemSettings.FileReadBufferSize = fileReadBufferSize;
            storedSettings.SystemSettings.ComputerName = "computer1";
            storedSettings.SystemSettings.ComputerNamed = false;
            storedSettings.SystemSettings.FirstSBSRun = true;
            storedSettings.SystemSettings.SBSDriveLetter = 'c';
            storedSettings.SystemSettings.SBSEnable = "Disable";
            storedSettings.SystemSettings.Resolution = "KB";
            storedSettings.SystemSettings.FreeSpaceToUse = 0;
            storedSettings.SystemSettings.SBSDriveId = "";
            storedSettings.SystemSettings.SBSDrivePartition = -1;

            ConvertPartnershipList2XML();

            // Add the custom sections to the config
            config.Sections.Add(settingName, storedSettings);
            config.Sections.Add(partnershipName, storedPartnerships);

            // Write to file
            config.Save(ConfigurationSaveMode.Modified);

            // Just to reload the configuration
            ConfigurationManager.RefreshSection(settingName);
            ConfigurationManager.RefreshSection(partnershipName);
        }

        /// <summary>
        /// When the program is used for the first time, this method
        /// is to update if the computer is named
        /// </summary>

        public void updateComputerNamed()
        {

            ConvertPartnershipList2XML();
            storedSettings.SystemSettings.ComputerNamed = computerNamed;
            config.Save(ConfigurationSaveMode.Modified);

        }


        /// <summary>
        /// when SBS is used for the first time,this method is to update if
        /// that it ran before
        /// </summary>
        public void updateFirstSBSRun()
        {
            ConvertPartnershipList2XML();
            storedSettings.SystemSettings.FirstSBSRun = firstSBSRun;
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// Used to restore deleted config file during run time
        /// </summary>
        public void ReIntialEnv()
        {
            if (config != null)
            {
                config.Sections.Clear();
            }

            ConfigurationSetup();

            // Prepare to read the custom sections (Pre declared needed for valid settings
            // file check. (Hint, the first run complete is hidden in the xml file)
            if (config != null)
            {
                storedSettings =
                    (SettingsSection)config.GetSection(settingName);

                
                if (storedSettings == null)
                {
                    storedSettings = new SettingsSection();
                    storedSettings.SystemSettings.FirstRunComplete = true;
                    storedSettings.SystemSettings.ComputerNamed = true;
                }

            }
            ConvertPartnershipList2XML();

            // Add the custom sections to the config
            config.Sections.Add(settingName, storedSettings);
            config.Sections.Add(partnershipName, storedPartnerships);
            
            // Write to file
            config.Save(ConfigurationSaveMode.Modified);

            // Just to reload the configuration
            ConfigurationManager.RefreshSection(settingName);
            ConfigurationManager.RefreshSection(partnershipName);

        }
        /// <summary>
        /// When the program is used for the first time, this method
        /// is called to setup the program config file is expectedly,
        /// stored with the program.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Throws ConfigurationErrorsException if the program
        /// is unable to write to disk</exception>
        public void IntialEnv()
        {
            // This will detect if settings are already stored
            bool createSettings = true;

            sbsDriveLetter = 'c';
            computerName = "Computer1";
            computerNamed = false;
            sbsEnable = "Disable";
            freeSpaceToUse = 0;
            resolution = "KB";
            firstSBSRun = true;
            sbsDrivePartition = -1;
            ConfigurationSetup();

            // Prepare to read the custom sections (Pre declared needed for valid settings
            // file check. (Hint, the first run complete is hidden in the xml file)
            if (config != null)
            {
                //Console.WriteLine(
                //    "A Settings file was found, checking its validity");

                storedSettings =
                    (SettingsSection)config.GetSection(settingName);

                if (storedSettings == null)
                {
                    //Console.WriteLine(
                    //    "A invalid settings file was found, recreating one");
                    storedSettings = new SettingsSection();
                }

                //Not necessary an error, we will just create the .settings file
                if (storedSettings.SystemSettings.FirstRunComplete)
                {
                    createSettings = false; //Means old settings file found
                    firstRunComplete = true;
                    //It remains as false for the rest of the execution of the program
                    //during the real run
                }
            }

            storedPartnerships = new PartnershipSection();

            if (createSettings)
                CreateEnv();

            else
                RestoreEnv();
        }
        private void ConfigurationSetup()
        {
            // The config file will be the name of our app, less the extension
            // It might not be so if the user has changed the filename for some reason
            // Attempt to locate the settings file (null if not found)
            string configFilename = SearchForSettingsFile();
            if (configFilename == null)
                configFilename = GetSettingsFileName();
            else
                configFilename = _appPath + @"\" + configFilename;

            // Map the new configuration file
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFilename;

            // Create out own configuration file
            config = ConfigurationManager.OpenMappedExeConfiguration(
                configFileMap, ConfigurationUserLevel.None);
        }

        /// <summary>
        /// Rewrap the list of partnership in XML friendly format. The list is guaranted to
        /// have only unique partnerships.
        /// </summary>
        private void ConvertPartnershipList2XML()
        {
            //Clean up the database first
            storedPartnerships.PartnershipList.Clear();
            storedPartnerships.MiniPartnershipList.Clear();

            //Convert to store in XML format
            foreach(Partnership element in partnershipList.Values)
            {
                storedPartnerships.PartnershipList.Add(element);
            }
            foreach (Partnership element in miniPartnershipList.Values)
            {
                storedPartnerships.MiniPartnershipList.Add(element);
            }
        }

        /// <summary>
        /// Unwrap the list of partnership from a XML friendly format. The list is guaranted to
        /// have only unique partnerships.
        /// </summary>
        private void ConvertXML2PartnershipList()
        {
            //Prepare the partnership list
            partnershipList = new SortedList<string,Partnership>();
            miniPartnershipList = new SortedList<string, Partnership>();
            //Convert to store in XML format
            foreach (PartnershipElement element in storedPartnerships.PartnershipList)
            {
                Partnership newElement = element.obj;
                partnershipList.Add(element.friendlyName, newElement);
            }
            foreach (PartnershipElement element in storedPartnerships.MiniPartnershipList)
            {
                Partnership newElement = element.obj;
                miniPartnershipList.Add(element.friendlyName, newElement);
            }
        }

        /// <summary>
        /// Creates a new Partnership based on 2 full paths. Note it merely creates a partnership
        /// object. It is not its place to check if there are duplicated partnerships.
        /// </summary>
        /// <param name="name">Friendly name of a partnership</param>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>        
        private Partnership CreatePartnership(string name, string leftPath, string rightPath)
        {
            FileInfo leftInfo = new FileInfo(leftPath);
            FileInfo rightInfo = new FileInfo(rightPath);
            bool isFolderLeft = leftInfo.Attributes.ToString().Contains("Directory");
            bool isFolderRight = rightInfo.Attributes.ToString().Contains("Directory");
            if (leftInfo.Exists && rightInfo.Exists) // true if both filesystem objects exist
            {
                if (isFolderLeft && isFolderRight)
                {
                    return CreateFolderPartner(leftPath, rightPath, name);
                }
                else if (isFolderLeft || isFolderRight) //when one side is a folder and one is a file
                {
                    throw new ArgumentException("A folder cannot sync with a file");
                }
                else
                {
                    //Ensuring the left and right files are the same
                    if (!CheckFilePartnerAbility(leftPath, rightPath))
                        throw new ArgumentException("Left file not the same as right file for windows file partnership");

                    return CreateFilePartner(leftInfo.DirectoryName, rightInfo.DirectoryName, leftPath, rightPath, name);
                }
            }
            else 
            {
                //assumed if both sides are missing that they are folder pairs
                if (!leftInfo.Exists && !rightInfo.Exists)
                    return CreateFolderPartner(leftPath, rightPath, name);

                if (leftInfo.Exists && isFolderLeft)
                {
                    return CreateFolderPartner(leftPath, rightPath, name);
                }
                else if (rightInfo.Exists && isFolderRight)
                {
                    return CreateFolderPartner(leftPath, rightPath, name);
                }
                else
                {
                    //Ensuring the left and right files are the same
                    if (!CheckFilePartnerAbility(leftPath, rightPath))
                        throw new ArgumentException("Left file not the same as right file for windows file partnership");
                    return CreateFilePartner(leftInfo.DirectoryName, rightInfo.DirectoryName, leftPath, rightPath, name);
                }
            }
        }

        /// <summary>
        /// This method will only create a partnership for the same file on the two folders.
        /// Required to be the same for it to work
        /// </summary>
        /// <param name="leftpath">Left Full Path to the file on the left</param>
        /// <param name="rightpath">Left Full Path to the file on the left</param>
        /// <param name="name">Friendly name of the partnership</param>
        /// <returns>A well formed Partnership object</returns>
        private Partnership CreateFolderPartner(string leftpath, string rightpath, string name)
        {
            ISyncable left = new WindowsFolder(leftpath, leftpath);
            ISyncable right = new WindowsFolder(rightpath, rightpath);
            Partnership partner = new Partnership(name, left, right, null);
            return partner;
        }
        
        /// <summary>
        /// Creates a Partnership object between two files. Ideally, between two SAME file
        /// but in different location. (Like Micro Partnership)
        /// </summary>
        /// <param name="leftdir">Root Path of the left file</param>
        /// <param name="rightdir">Root Path of the right file</param>
        /// <param name="leftpath">Full Path of the left file</param>
        /// <param name="rightpath">Full Path of the right file</param>
        /// <param name="name">Friendly name of the partnership</param>
        /// <returns>A well formed Partnership object</returns>
        private Partnership CreateFilePartner(string leftdir, string rightdir, string leftpath, string rightpath, string name)
        {
            //Meaning that an invalid pair has been specified
            System.Diagnostics.Debug.Assert(CheckFilePartnerAbility(leftpath, rightpath));

            ISyncable left = new WindowsFile(leftdir, leftpath);
            ISyncable right = new WindowsFile(rightdir, rightpath);
            Partnership partner = new Partnership(name, left, right, null);
            return partner;
        }

        /// <summary>
        /// Checks if the path supplied for file partnership creation are for the same file,
        /// ignores case
        /// </summary>
        /// <param name="leftPath">Full Path of the left file</param>
        /// <param name="rightPath">Full Path of the right file</param>
        /// <returns>True if they are compatible</returns>
        private static bool CheckFilePartnerAbility(string leftpath, string rightpath)
        {
            //Ensuring the left and right files are the same
            int positionLeft = leftpath.LastIndexOf(@"\");
            int positionRight = rightpath.LastIndexOf(@"\");
            string filenameLeft = leftpath.Substring(positionLeft, leftpath.Length - positionLeft);
            string filenameRight = rightpath.Substring(positionRight, rightpath.Length - positionRight);
            filenameLeft = filenameLeft.ToLower();
            filenameRight = filenameRight.ToLower();

            //Meaning that an invalid pair has been specified
            return filenameLeft.Equals(filenameRight);
        }

        /// <summary>
        /// Tthe pair of partnership folders/files must be unique. Caps are ignored.
        /// The paths must fulfill the conditions below:
        /// (Incoming Left != Left in List && Incoming Right != Right in List)
        /// (Incoming Left != Right in List && Incoming Right != Left in List)
        /// </summary>
        /// <param name="name">Friendly Name</param>
        /// <param name="leftPath">Full path to the incoming left folder or file</param>
        /// <param name="rightPath">Full path to the incoming right folder or file</param>
        /// <returns>True if it is a pair of unique partnership path, false otherwise</returns>
        private bool IsUniquePartnershipPath(string name, string leftPath, string rightPath, SortedList<string,Partnership> partnerList)
        {
            foreach (Partnership storedElement in partnerList.Values)
            {
                //For (Incoming Left != Left in List && Incoming Right != Right in List)
                bool leftLeft = WindowsFileSystem.PathsEqual(storedElement.LeftFullPath, leftPath);
                bool leftRight = WindowsFileSystem.PathsEqual(storedElement.LeftFullPath, rightPath);
                bool rightLeft = WindowsFileSystem.PathsEqual(storedElement.RightFullPath, leftPath);
                bool rightRight = WindowsFileSystem.PathsEqual(storedElement.RightFullPath, rightPath);

                //check left path with left of stored partnerships and right with right
                //For (Incoming Left != Right in List && Incoming Right != Left in List)
                if ((leftLeft && rightRight) || (leftRight && rightLeft))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if the desired friendly name is already in used
        /// </summary>
        /// <param name="name">Desired friendly name of the partnership</param>
        /// <param name="isMini">true if checking the mini partnership list; else checks the main partnership list</param>
        /// <returns>True if it is unique, false otherwise</returns>
        private bool IsUniquePartnershipName(string name, SortedList<string,Partnership> partnerList)
        {
            foreach (Partnership storedElement in partnerList.Values)
            {
                if (storedElement.Name.Trim().ToLower().Equals(name.Trim().ToLower()))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Decodes a dictionary key (string) into its components
        /// </summary>
        /// <param name="key">Dictionary key to decode</param>
        /// <returns>The decoded key</returns>
        /// <exception cref="ArguementExcpetion">The key provided was not a valid Checksum Dictionary key</exception>
        public static ChecksumKey DecodeChecksumKey(string key)
        {
            int pos;
            ChecksumKey returnValue = new ChecksumKey();

            returnValue.EntityPath = key;

            pos = returnValue.EntityPath.IndexOf(@":\\");
            if (pos < 0) throw new ArgumentException("Malformed Key for Partnership Record");

            returnValue.RelativePath = returnValue.EntityPath.Substring(pos + 3);

            return returnValue;
        }

        /// <summary>
        /// Unserializes an object. Depends on the name of the root element to figure out
        /// what class the object is. Also assumes the object implements a constructor 
        /// which takes one arguement - an XmlReader. The actual unserialization takes place
        /// in that constructor
        /// </summary>
        /// <param name="xmlString">The XML to unserialize</param>
        /// <returns>An Object of the correct type</returns>
        /// <exception cref="InvalidDataException">The XML was not valid</exception>
        public static Object ReflectiveUnserialize(string xmlString)
        {
            if (syncButlerAssembly == null) InitSyncButlerAssembly();

            XmlReader xmlData = XmlTextReader.Create(new StringReader(xmlString));

            while (xmlData.NodeType != XmlNodeType.Element) xmlData.Read();
            if (xmlData.NodeType != XmlNodeType.Element) throw new InvalidDataException();
            string className = xmlData.Name;
            //xmlData = XmlTextReader.Create(new StringReader(xmlString));
            
            Type t;

            try
            {
                t = syncButlerAssembly.GetType("SyncButler." + className);
                return Activator.CreateInstance(t, xmlData);
            }
            catch (Exception e)
            {
                throw new InvalidDataException("Unable to Create Instance of Deserialised Object", e);
            }
        }

        /// <summary>
        /// This is required for reflective unserialise. It will determine what
        /// class this is from.
        /// </summary>
        private static void InitSyncButlerAssembly()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("SyncButler,"))
                {
                    syncButlerAssembly = asm;
                    break;
                }
            }

            if (syncButlerAssembly == null)
                throw new Exception("Unable to load the backend assembly!");
        }

        /// <summary>
        /// This method is widely reused. It obtains the name of the config file
        /// by detecting name of the app.
        /// </summary>
        /// <returns>A String in the format of 'appname.settings'</returns>
        private string GetSettingsFileName()
        {
            string appName = Environment.GetCommandLineArgs()[0];
            //string appName = System.Reflection.Assembly.GetExecutingAssembly().
            appName = appName.Substring(appName.LastIndexOf('\\')+1);
            int extensionPoint = appName.LastIndexOf('.');
            string configFilename = _appPath + @"\" + string.Concat(appName.Substring(0, extensionPoint), SYNCBUTLER_SETTINGS_EXTENSION);
            return configFilename;
        }

        /// <summary>
        /// It attempts to look for the settings file (with the default extension
        /// .butler) in the same directory as the program
        /// </summary>
        /// <returns>The filename of the settings file</returns>
        private string SearchForSettingsFile()
        {
            //string appName = Environment.GetCommandLineArgs()[0];
            //int parentDirectory = appName.LastIndexOf('\\');
            //string programDirectory = appName.Substring(0, parentDirectory);
            DirectoryInfo programPath = new DirectoryInfo(_appPath);
            if (programPath.Exists)
            {
                FileInfo[] fileList = programPath.GetFiles();

                foreach (FileInfo file in fileList)
                {
                    if (file.Extension.ToLower().Equals(SYNCBUTLER_SETTINGS_EXTENSION))
                    {
                        //parentDirectory index position can be reused cause it is in the same directory
                        return file.FullName.Substring(file.FullName.LastIndexOf('\\') + 1);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This reads MRU details from the textfile stored in the MRU folder.
        /// 
        /// For eg, friendly name = partnership, drive prefix = D:\,
        /// The folder and file name is = D:\SyncButler\partnership\parnership_Details.txt
        /// </summary>
        /// <param name="drivePrefix">The drive of the SBS folder, it has to be in the form
        /// of "X:\\"</param>
        /// <param name="friendlyName">The friendly name of the MRU parent</param>
        /// <param name="content">Contents in a MRU file. May include date/time, original
        /// location, present filename (maybe renamed forcibly if there are same file names)</param>
        /// <exception cref="IOException">An I/O error occurs</exception>
        private void writeMRUFile(string drivePrefix, string friendlyName, string content)
        {
            // Create a writer and open the file
            // For eg, friendly name = partnership, drive prefix = D:\,
            // The folder and file name is = D:\SyncButler\partnership\parnership_Details.txt
            TextWriter tw = new StreamWriter(drivePrefix + DEFAULT_SBS_RELATIVE_PATH +
                                friendlyName + "\\" + friendlyName + DEFAULT_SBS_FILENAME_POSTFIX);

            // Write a line of text to the file
            tw.WriteLine(content);

            // Close the stream
            tw.Close();
        }

        /// <summary>
        /// This reads MRU details from the textfile stored in the MRU folder.
        /// 
        /// For eg, friendly name = partnership, drive prefix = D:\,
        /// The folder and file name is = D:\SyncButler\partnership\parnership_Details.txt
        /// </summary>
        /// <param name="drivePrefix">The drive of the SBS folder, it has to be in the form
        /// of "X:\\"</param>
        /// <param name="friendlyName">The friendly name of the MRU parent</param>
        /// <returns>Contents of the MRU file</returns>
        /// <exception cref="IOException">An I/O error occurs</exception>
        /// <exception cref="OutOfMemoryException">There is insufficient memory to allocate a
        /// buffer for the returned string</exception>
        /// <exception cref="ArgumentOutOfRangeException">The number of characters in the next line is
        /// larger than MaxValue (Int32, Signed)</exception>
        private string readMRUFile(string drivePrefix, string friendlyName)
        {
            // The content of the textfile, less the first date/time
            string content = "";

            // Create reader & open file
            TextReader tr = new StreamReader(drivePrefix + DEFAULT_SBS_RELATIVE_PATH +
                                friendlyName + @"\" + friendlyName + DEFAULT_SBS_FILENAME_POSTFIX);

            // Read all the text inside
            content = tr.ReadToEnd();

            // close the stream
            tr.Close();

            return content;
        }

        /// <summary>
        /// Gets whether the computer name is for the first time or otherwise 
        /// </summary>
        public static bool ComputerNamed
        {
            get
            {
                return computerNamed;
            }
            set
            {
                computerNamed = value;
            }
        }

        /// <summary>
        /// Gets whether the SBS is runed for the first time or otherwise 
        /// </summary>
        public static bool FirstSBSRun
        {
            get
            {
                return firstSBSRun;
            }
            set
            {
                firstSBSRun = value;
            }
        }
        /// <summary>
        /// Gets whether the program is being executed for the first time or otherwise
        /// </summary>
        public static bool FirstRunComplete
        {
            get
            {
                return firstRunComplete;
            }
        }

        /// <summary>
        /// Gets the size of the buffer to be used when reading from files.
        /// </summary>
        public static long FileReadBufferSize
        {
            get
            {
                return fileReadBufferSize;
            }
        }

        /// <summary>
        /// the current computer name
        /// </summary>
        /// <returns></returns>
        public static string ComputerName
        {
            get
            {
                return computerName;
            }
            set
            {
                computerName = value;
            }
        }
        /// <summary>
        /// The current status of sbs
        /// </summary>
        public static string SBSEnable
        {
            get
            {
                return sbsEnable;
            }
            set
            {
                sbsEnable = value;
            }
        }
        public static int SBSDrivePartition
        {
            get
            {
                return sbsDrivePartition;
            }
            set
            {
                sbsDrivePartition = value;
            }
        }
        public static string SBSDriveId
        {
            get
            {
                return sbsDriveId;
            }
            set
            {
                sbsDriveId = value;
            }
        }

        /// <summary>
        /// The resolution of the free space
        /// </summary>
        public static string Resolution
        {
            get
            {
                return resolution;
            }
            set
            {
                resolution = value;
            }
        }
        /// <summary>
        /// Free space that the user allow sbs to use
        /// </summary>
        public static double FreeSpaceToUse
        {
            get
            {
                return freeSpaceToUse;
            }
            set
            {
                freeSpaceToUse = value;
            }
        }


        /// <summary>
        /// SBS working drive
        /// </summary>
        public static char SBSDriveLetter
        {
            get
            {
                return sbsDriveLetter;
            }
            set
            {
                sbsDriveLetter = value;
            }
        }

        /// <summary>
        /// Determines if shell integration context menu is to be kept on or off
        /// </summary>
        public static bool EnableShellContext
        {
            get
            {
                return enableShellContext;
            }
            set
            {
                enableShellContext = value;
            }
        }

        /// <summary>
        /// Path to this application
        /// </summary>
        public static string AppPath
        {
            get
            {
                Debug.Assert(_appPath != null, "Cannot provide app path before constructor of SyncEnviroment is run");
                return _appPath;
            }
        }
    }
}