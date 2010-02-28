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
    public class SyncEnvironment
    {
        /// <param name="storedSettings">This is a settings container that will be saved</param>
        /// <param name="settingName">This is an XML description require to write settings to the real XML page</param>
        /// <param name="storedPartnerships">This is a partnership container that will be saved</param>
        /// <param name="partnershipName">This is an XML description require to write partnership to the real XML page</param>

        //List of persistence attributes
        private List<Partnership> partnershipList;
        private bool allowAutoSyncForConflictFreeTasks;
        private bool firstRunComplete;
        private System.Configuration.Configuration config;
        private string settingName = "systemSettings";
        private string partnershipName = "partnership";
        private SettingsSection storedSettings;
        private PartnershipSection storedPartnerships;
        
        /// <summary>
        /// This constructor will automatically restore a previous sessions or
        /// create new ones
        /// </summary>
        public SyncEnvironment()
        {
            firstRunComplete = false;
            IntialEnv();
            //partnershipList = new List<Partnership>();
        }

        /// <summary>
        /// Returns the partnership at the specified index.
        /// </summary>
        /// <param name="idx">The integer index of the partnership to load.</param>
        /// <returns>A Partnership object</returns>
        public Partnership LoadPartnership(int idx)
        {
            return partnershipList[idx];
        }

        /// <summary>
        /// Adds a properly created partner object into the list of partnership
        /// </summary>
        /// <param name="partner">A properly created partner object</param>
        public void AddPartnership(Partnership partner)
        {
            partnershipList.Add(partner);
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
        public List<Partnership> GetPartnerships()
        {
            return partnershipList;
        }

        /// <summary>
        /// When there is a change in any Partnership, this method is called
        /// and supplied with the position of the Partnership object in the
        /// List of Partnerships.
        /// </summary>
        /// <param name="idx">The position fo the Partnership in the List of Partnerships</param>
        /// <param name="updated">The UPDATED Partnership object, that will replace the original one</param>
        public void UpdatePartnership(int idx, Partnership updated)
        {
            partnershipList.RemoveAt(idx);
            partnershipList.Insert(idx, updated);
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
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks =
                allowAutoSyncForConflictFreeTasks;

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
            allowAutoSyncForConflictFreeTasks =
                storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks;
        }

        /// <summary>
        /// If the settings files do not exist. A basic framework is created and stored. This
        /// will store the settings of a previous interations.
        /// </summary>
        public void CreateEnv()
        {
            //Create the list of partnerships
            partnershipList = new List<Partnership>();

            // Add in default settings
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = true;
            storedSettings.SystemSettings.FirstRunComplete = true;
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
            foreach(Partnership element in partnershipList)
            {
                //storedPartnerships.Partnership.Add(element. leftFullPath, element.rightFullPath);
            }
        }

        /// <summary>
        /// Unwrap the list of partnership from a XML friendly format
        /// </summary>
        private void ConvertXML2PartnershipList()
        {
            //Prepare the partnership list
            partnershipList = new List<Partnership>();

            //Convert to store in XML format
            foreach (PartnershipConfigElement element in storedPartnerships.Partnership)
            {
                Partnership newElement = CreatePartnership(element.LeftPath, element.RightPath);
                partnershipList.Add(newElement);
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
        private Partnership CreatePartnership(String leftPath, String rightPath)
        {
            FileInfo leftInfo = new FileInfo(leftPath);
            FileInfo rightInfo = new FileInfo(rightPath);
            bool isFolderLeft = leftInfo.Attributes.ToString().Equals("Directory");
            bool isFolderRight = rightInfo.Attributes.ToString().Equals("Directory");
            if (isFolderLeft && isFolderRight)
            {
                ISyncable left = new WindowsFolder(leftPath, leftPath);
                ISyncable right = new WindowsFolder(rightPath, rightPath);
                Partnership partner = new Partnership(left, right, null);
                return partner;
            }
            else if (isFolderLeft || isFolderRight)
            {
                throw new ArgumentException("Folder cannot sync with a non-folder");
            }
            else
            {
                ISyncable left = new WindowsFile(leftInfo.DirectoryName, leftPath);
                ISyncable right = new WindowsFile(rightInfo.DirectoryName, rightPath);
                Partnership partner = new Partnership(left, right, null);
                return partner;
            }
        }

        //There are 1001 options to change, need to revise this
        /*
        public void UpdateSystemSetting(SettingsConfigElement.Options option, )
        {
            
        }
        */
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