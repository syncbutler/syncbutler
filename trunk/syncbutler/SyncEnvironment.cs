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
        private List<Partnership> partnershipList;

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
        /// Returns the entire partnership list. This is useful when the controller
        /// needs to view what partnerships are there thus far.
        /// </summary>
        /// <returns>A List containing all existing Partnership</returns>
        public List<Partnership> GetPartnerships()
        {
            return partnershipList;
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
        /// <returns>True if the store operation was successful false otherwise.</returns>
        public bool StoreSettings()
        {
            return false;
        }

        /// <summary>
        /// This method is called during program startup to restore all
        /// the previous states of the program.
        /// </summary>
        public void IntialEnv()
        {
            //Get the location of the EXE and the config file SHOULD be there
            //otherwise react!
            string appName = Environment.GetCommandLineArgs()[0];
            string configFile = string.Concat(
                                appName.Substring(0, appName.Length - 4),
                                ".config");

            System.Configuration.Configuration config =
                ConfigurationManager.OpenExeConfiguration(configFile);

            //Console.WriteLine(config.FilePath);

            if (config == null)
            {
                //Console.WriteLine(
                //    "The configuration file does not exist.");
                //Console.WriteLine(
                //    "Use OpenExeConfiguration to create the file.");
                throw new FileNotFoundException("Application Config File not Found");
                //if it does not exist, opt to create!
            }

            // Prepare the retrieve the settings stored within the config files
            string settingName = "systemSettings";
            string partnershipName = "partnership";

            SettingsSection storedSettings =
                (SettingsSection)config.GetSection(settingName);

            PartnershipSection storedPartnerships =
                (PartnershipSection)config.GetSection(partnershipName);

            if(storedSettings == null)
                throw new ApplicationException("Could not load Stored Settings");

            if (storedPartnerships == null)
                throw new ApplicationException("Could not load Stored Partnership List");

            ConfigurationManager.RefreshSection(settingName);
            ConfigurationManager.RefreshSection(partnershipName);

            //Assign the respective program settings
            partnershipList = storedPartnerships.Partnership.PartnershipList;

        }

        /// <summary>
        /// When the program is used for the first time, this method
        /// is called to setup the program config file is expectedly,
        /// stored with the program.
        /// </summary>
        public void FirstRunEnvPrep()
        {
            System.Configuration.Configuration config =
                ConfigurationManager.OpenExeConfiguration(
                ConfigurationUserLevel.None);

            //Console.WriteLine(config.FilePath);

            if (config == null)
            {
                //Console.WriteLine(
                //    "The configuration file already exist! Overwriting");
            }

            // Create a new configuration file by saving 
            // the application configuration to a new file.
            string appName = Environment.GetCommandLineArgs()[0];

            string configFile = string.Concat(appName.Substring(0, appName.Length - 4),
                                ".config");
            config.SaveAs(configFile, ConfigurationSaveMode.Full);

            // Map the new configuration file.
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFile;

            // Get the mapped configuration file
            config =
            ConfigurationManager.OpenMappedExeConfiguration(
              configFileMap, ConfigurationUserLevel.None);

            // Make changes to the new configuration file. 
            // This is to show that this file is the 
            // one that is used.
            string settingName = "systemSettings";
            string partnershipName = "partnership";

            SettingsSection storedSettings =
                (SettingsSection)config.GetSection(settingName);

            PartnershipSection storedPartnerships =
                (PartnershipSection)config.GetSection(partnershipName);

            ConfigurationManager.RefreshSection(settingName);
            ConfigurationManager.RefreshSection(partnershipName);

            config.Save(ConfigurationSaveMode.Full);
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