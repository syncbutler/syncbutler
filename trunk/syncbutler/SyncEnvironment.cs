using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.IO;
using SyncButler.ProgramEnvironment;

namespace SyncButler
{
    /// <summary>
    /// Contains methods to load and store settings as well as handling access to the list of partnerships
    /// </summary>

    /// <summary>
    /// Represents a key used in the checksum dictionary
    /// </summary>
    public struct ChecksumKey
    {
        public string entityPath, relativePath, partnershipName;
        public ChecksumKey(string ep, string rp, string pn)
        {
            entityPath = ep;
            relativePath = rp;
            partnershipName = pn;
        }
    }

    //To Do: Allow the restoration of dictionary object
    public class SyncEnvironment
    {
        ///List of persistence attributes
        /// <param name="storedSettings">This is a settings container that will be saved</param>
        /// <param name="settingName">This is an XML description require to write settings to the real XML page</param>
        /// <param name="storedPartnerships">This is a partnership container that will be saved</param>
        /// <param name="partnershipName">This is an XML description require to write partnership to the real XML page</param>
        private SortedList<String,Partnership> partnershipList;
        private bool allowAutoSyncForConflictFreeTasks;
        private bool firstRunComplete;
        private long fileReadBufferSize;
        private System.Configuration.Configuration config;
        private string settingName = "systemSettings";
        private string partnershipName = "partnership";
        private SettingsSection storedSettings;
        private PartnershipSection storedPartnerships;
        private static SyncEnvironment syncEnv;
        
        /// <summary>
        /// This constructor will automatically restore a previous sessions or create new ones.
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of SyncEnvironment.
        /// </summary>
        private SyncEnvironment()
        {
            firstRunComplete = false;
            IntialEnv();
            //partnershipList = new List<Partnership>();
        }

        /// <summary>
        /// Returns an instance of SyncEnvironment.
        /// Creates a new instance if necessary. Otherwise, it will use an already available instance.
        /// </summary>
        /// <returns>An instance of SyncEnvironment.</returns>
        public static SyncEnvironment GetInstance()
        {
            if (syncEnv == null)
                syncEnv = new SyncEnvironment();

            return syncEnv;
        }

        /// <summary>
        /// Returns the partnership at the specified index.
        /// </summary>
        /// <param name="idx">The integer index of the partnership to load.</param>
        /// <returns>A Partnership object</returns>
        public Partnership LoadPartnership(string name)
        {
            return partnershipList[name];
        }

        /// <summary>
        /// Adds a properly created partner object into the list of partnership
        /// </summary>
        /// <param name="partner">A properly created partner object</param>
        /// <exception cref="ArgumentNullException">Thrown if the key is a null reference.</exception>
        /// <exception cref="ArgumentException">Thrown if the name of the partnership already exists.</exception>
        public void AddPartnership(string name, String leftPath, String rightPath)
        {
            System.Diagnostics.Debug.Assert((name != null) && (name.Length > 0));
            Partnership element = CreatePartnership(name, leftPath, rightPath);
            partnershipList.Add(name, element);
        }

        /// <summary>
        /// Removes specified partnership in the partnership list
        /// </summary>
        /// <param name="idx">Removes the partnership at that particular index</param>
        public void RemovePartnership(int idx)
        {
            partnershipList.RemoveAt(idx);
        }

        /// <summary>
        /// Returns the entire partnership list. This is useful when the controller
        /// needs to view what partnerships are there thus far.
        /// </summary>
        /// <returns>A List containing all existing Partnership</returns>
        public SortedList<String,Partnership> GetPartnerships()
        {
            return partnershipList;
        }

        /// <summary>
        /// When there is a change in any Partnership, this method is called
        /// and supplied with the position of the Partnership object in the
        /// List of Partnerships.
        /// </summary>
        /// <param name="idx">The position of the Partnership in the List of Partnerships</param>
        /// <param name="updated">The UPDATED Partnership object will replace the original one</param>
        public void UpdatePartnership(string name, Partnership updated)
        {
            partnershipList.Remove(name);
            partnershipList.Add(name,updated);
        }

        /// <summary>
        /// Not implemented. Gets the settings for the program.
        /// </summary>
        /// <returns>A Dictionary object with strings as keys and value (subject to change).</returns>
        public Dictionary<String, String> ReadSettings()
        {
            return null;
        }

        /// <summary>
        /// Not implemented. Stores the settings for the program to persistent storage
        /// during program shut down
        /// </summary>
        public void StoreEnv()
        {
            //Update the settings in the config file
            ConvertPartnershipList2XML();
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = allowAutoSyncForConflictFreeTasks;
            storedSettings.SystemSettings.FileReadBufferSize = fileReadBufferSize;
            // Write to file
            config.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// This method is called during program startup to restore all
        /// the previous states of the program.
        /// </summary>
        /// <param name="storedSettings">This is a settings container that will be restored</param>
        /// <param name="settingName">This is an XML description require to read settings to the real XML page</param>
        /// <param name="storedPartnerships"></param>
        /// <param name="partnershipName">This is an XML description require to read partnership to the real XML page</param>
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
        }

        /// <summary>
        /// If the settings files do not exist. A basic framework is created and stored. This
        /// will store the settings of a previous interations.
        /// </summary>
        public void CreateEnv()
        {
            //Create the list of partnerships
            partnershipList = new SortedList<String,Partnership>();

            // Add in default settings
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = true;
            storedSettings.SystemSettings.FirstRunComplete = true;
            storedSettings.SystemSettings.FileReadBufferSize = 2048000; // 2MB
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
        public void IntialEnv()
        {
            //This will detect if settings are already stored
            bool createSettings = true;

            // The config file will be the name of our app, less the extension
            string configFilename = GetSettingsFileName();
            
            // Map the new configuration file
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFilename;

            // Create out own configuration file
            config = ConfigurationManager.OpenMappedExeConfiguration(
                configFileMap, ConfigurationUserLevel.None);

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
                //throw new ApplicationException("Could not load Stored Settings");

                if (storedSettings.SystemSettings.FirstRunComplete)
                {
                    createSettings = false;
                    firstRunComplete = true;
                    //It remains as false for the rest of the execution of the program
                    //during the real run
                }

                //else
                //Console.WriteLine(
                //    "A invalid settings file was found, recreating one");
            }

            storedPartnerships = new PartnershipSection();

            if (createSettings)
                CreateEnv();

            else
                RestoreEnv();
        }

        /// <summary>
        /// This method is widely reused. It obtains the name of the config file
        /// by detecting name of the app.
        /// </summary>
        /// <returns>A String in the format of 'appname.settings'</returns>
        private string GetSettingsFileName()
        {
            string appName = Environment.GetCommandLineArgs()[0];
            int extensionPoint = appName.LastIndexOf('.');
            string configFilename = string.Concat(appName.Substring(0, extensionPoint),
                                        ".settings");
            return configFilename;
        }

        /// <summary>
        /// Rewrap the list of partnership in XML friendly format
        /// </summary>
        private void ConvertPartnershipList2XML()
        {
            //Clean up the database first
            storedPartnerships.Partnership.Clear();

            //Convert to store in XML format
            foreach(Partnership element in partnershipList.Values)
            {
                storedPartnerships.Partnership.Add(element.Name, element.LeftFullPath, element.RightFullPath);
            }
        }

        /// <summary>
        /// Unwrap the list of partnership from a XML friendly format
        /// </summary>
        private void ConvertXML2PartnershipList()
        {
            //Prepare the partnership list
            partnershipList = new SortedList<String,Partnership>();

            //Convert to store in XML format
            foreach (PartnershipConfigElement element in storedPartnerships.Partnership)
            {
                Partnership newElement = CreatePartnership(element.FriendlyName, element.LeftPath,
                                            element.RightPath);
                partnershipList.Add(element.FriendlyName, newElement);
            }   
        }

        /// <summary>
        /// This will determine whether the program is being executed for the
        /// first time or otherwise
        /// </summary>
        /// <returns></returns>
        public bool isFirstRunComplete()
        {
            return firstRunComplete;
        }

        /// <summary>
        /// Creates a new Partnership based on 2 full paths.
        /// </summary>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>        
        private Partnership CreatePartnership(String name, String leftPath, String rightPath)
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
                    throw new ArgumentException("Folder cannot sync with a file");
                }
                else
                {
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
                    return CreateFilePartner(leftInfo.DirectoryName, rightInfo.DirectoryName, leftPath, rightPath, name);
                }
   
            }
        }

        private Partnership CreateFolderPartner(String leftpath, String rightpath, String name)
        {
            ISyncable left = new WindowsFolder(leftpath, leftpath);
            ISyncable right = new WindowsFolder(rightpath, rightpath);
            Partnership partner = new Partnership(name, left, right, null);
            return partner;
        }

        private Partnership CreateFilePartner(String leftdir, String rightdir, String leftpath, String rightpath, String name)
        {
            ISyncable left = new WindowsFile(leftdir, leftpath);
            ISyncable right = new WindowsFile(rightdir, rightpath);
            Partnership partner = new Partnership(name, left, right, null);
            return partner;
        }
        //There are 1001 options to change, need to revise this
        /*
        public void UpdateSystemSetting(SettingsConfigElement.Options option, )
        {            
        }
        */

        /// <summary>
        /// Decodes a dictinoary key (string) into its components
        /// </summary>
        /// <param name="key">Dictionary key to decode</param>
        /// <returns>The decoded key</returns>
        /// <exception cref="ArguementExcpetion">The key provided was not a valid Checksum Dictionary key</exception>
        public static ChecksumKey DecodeChecksumKey(string key)
        {
            int pos = key.IndexOf(':');

            // Possible corruption of the dictionary?
            if (pos < 0) throw new ArgumentException();

            ChecksumKey returnValue;

            returnValue.partnershipName = key.Substring(0, pos);
            returnValue.entityPath = key.Substring(pos + 1);

            pos = returnValue.entityPath.IndexOf(":\\\\");
            if (pos < 0) throw new ArgumentException();

            returnValue.relativePath = returnValue.entityPath.Substring(pos + 3);

            return returnValue;
        }
    }
}
/*
Environment

Attributes:

partnershipList:List<partnership>

Methods:

loadPartnership(input:int):Partnership
readSettings():Dictionary
storeSettings():boolean
*/