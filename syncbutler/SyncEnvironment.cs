using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.IO;
using SyncButler.ProgramEnvironment;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace SyncButler
{
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

    /// <summary>
    /// Contains methods to load and store settings as well as handling access to the list of partnerships
    /// </summary>
    public class SyncEnvironment
    {
        //List of constants used in the Sync Butler
        private const string DEFAULT_SBS_RELATIVE_PATH = "SyncButler\\";
        private const string DEFAULT_SBS_FILENAME_POSTFIX = "_Details.txt";
        
        ///List of persistence attributes
        private SortedList<String,Partnership> partnershipList;
        private bool allowAutoSyncForConflictFreeTasks;
        private bool firstRunComplete;
        private static long fileReadBufferSize = 2048000; //2MB, How much of the data file is read each cycle
        private System.Configuration.Configuration config;
        private string settingName = "systemSettings";
        private string partnershipName = "partnership";
        private SettingsSection storedSettings;
        private PartnershipSection storedPartnerships;
        private static SyncEnvironment syncEnv;
        private static Assembly syncButlerAssembly = null;
        
        /// <summary>
        /// This constructor will automatically restore a previous sessions or create new ones.
        /// This constructor should never be invoked directly. Use GetInstance() to obtain an instance of SyncEnvironment.
        /// </summary>
        private SyncEnvironment()
        {
            firstRunComplete = false;
            IntialEnv();
        }

        /// <summary>
        /// Returns an instance of SyncEnvironment
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
        /// Not implemented
        /// </summary>
        /// <returns></returns>
        public static string GetComputerName()
        {
            return "computer1";
        }

        /// <summary>
        /// Returns the partnership at the specified index
        /// </summary>
        /// <param name="name">The name (used as index) of the partnership that is to be loaded</param>
        /// <returns>A Partnership object (Possible to be null if not found)</returns>
        public Partnership LoadPartnership(string name)
        {
            return partnershipList[name];
        }

        /// <summary>
        /// Adds a properly created partner object into the list of partnership
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

            if (!CheckIsUniquePartnership(name, leftPath, rightPath))
                throw new ArgumentException("Friendly name already in used or such file/folder partnership already exist");

            partnershipList.Add(name, element);
            StoreEnv(); //Save the partnership immediately
        }

        /// <summary>
        /// Removes specified partnership in the partnership list
        /// </summary>
        /// <param name="idx">Removes the partnership at that particular index</param>
        public void RemovePartnership(int idx)
        {
            partnershipList.RemoveAt(idx);
            StoreEnv(); //Save the partnership immediately
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
        /// <param name="name">The position of the Partnership in the List of Partnerships</param>
        /// <param name="updated">The UPDATED Partnership object will replace the original one</param>
        public void UpdatePartnership(string name, Partnership updated)
        {
            Partnership backupElement = partnershipList[name];
            partnershipList.Remove(name);

            //Checks if the user try to update the partnership with existing partnerships
            if (CheckIsUniquePartnership(updated.Name, updated.LeftFullPath, updated.RightFullPath))
            {
                partnershipList.Add(name, backupElement);
                throw new ArgumentException("Such file/folder partnership already exist. Update failure. Previous Partnership restored");
            }

            partnershipList.Add(name,updated);
            StoreEnv(); //Save the partnership immediately
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

            // Write to file
            config.Save(ConfigurationSaveMode.Modified);
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
            partnershipList = new SortedList<String,Partnership>();
            
            // Add in default settings
            storedSettings.SystemSettings.AllowAutoSyncForConflictFreeTasks = true;
            storedSettings.SystemSettings.FirstRunComplete = true;
            storedSettings.SystemSettings.FileReadBufferSize = fileReadBufferSize; // 2MB
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
                if (storedSettings.SystemSettings.FirstRunComplete)
                {
                    createSettings = false;
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
        /// Rewrap the list of partnership in XML friendly format. The list is guaranted to
        /// have only unique partnerships.
        /// </summary>
        private void ConvertPartnershipList2XML()
        {
            //Clean up the database first
            storedPartnerships.PartnershipList.Clear();

            //Convert to store in XML format
            foreach(Partnership element in partnershipList.Values)
            {
                storedPartnerships.PartnershipList.Add(element);
            }
        }

        /// <summary>
        /// Unwrap the list of partnership from a XML friendly format. The list is guaranted to
        /// have only unique partnerships.
        /// </summary>
        private void ConvertXML2PartnershipList()
        {
            //Prepare the partnership list
            partnershipList = new SortedList<String,Partnership>();

            //Convert to store in XML format
            foreach (PartnershipElement element in storedPartnerships.PartnershipList)
            {
                Partnership newElement = element.obj;
                partnershipList.Add(element.friendlyName, newElement);
            } 
        }

        /// <summary>
        /// Gets whether the program is being executed for the first time or otherwise
        /// </summary>
        public bool FirstRunComplete
        {
            get
            {
                return firstRunComplete;
            }
        }

        /// <summary>
        /// Gets the size of the buffer to be used when reading from files.
        /// </summary>
        public long FileReadBufferSize
        {
            get
            {
                return fileReadBufferSize;
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
                    throw new ArgumentException("Folder cannot sync with a file");
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
        private bool CheckFilePartnerAbility(string leftpath, string rightpath)
        {
            //Ensuring the left and right files are the same
            int positionLeft = leftpath.LastIndexOf("\\");
            int positionRight = rightpath.LastIndexOf("\\");
            string filenameLeft = leftpath.Substring(positionLeft, leftpath.Length - positionLeft);
            string filenameRight = rightpath.Substring(positionRight, rightpath.Length - positionRight);
            filenameLeft = filenameLeft.ToLower();
            filenameRight = filenameRight.ToLower();

            //Meaning that an invalid pair has been specified
            return filenameLeft.Equals(filenameRight);
        }

        /// <summary>
        /// Checks if the Friendly Name is already in used. Also, the pair of partnership
        /// folders/files must be unique. Caps are ignored. The paths must fulfill the conditions below
        /// (Incoming Left != Left in List && Incoming Right != Right in List)
        /// (Incoming Left != Right in List && Incoming Right != Left in List)
        /// </summary>
        /// <param name="name">Friendly Name</param>
        /// <param name="leftPath">Full path to the incoming left folder or file</param>
        /// <param name="rightPath">Full path to the incoming right folder or file</param>
        /// <returns></returns>
        private bool CheckIsUniquePartnership(string name, string leftPath, string rightPath)
        {
            bool pathAlreadyExist1 = false; //Checks left with left, right with right
            bool pathAlreadyExist2 = false; //Checks left with right, right with left

            foreach (Partnership storedElement in partnershipList.Values)
            {
                pathAlreadyExist1 = false;
                pathAlreadyExist2 = false;

                if (storedElement.Name.ToLower().Equals(name.ToLower()))
                    throw new ArgumentException("Friendly name already in used");

                if (storedElement.LeftFullPath.ToLower().Equals(leftPath.ToLower()))
                    pathAlreadyExist1 = true;
                //Don't throw exception first, left and right must already exist

                if (pathAlreadyExist1 && storedElement.RightFullPath.ToLower().Equals(rightPath.ToLower()))
                    return false;

                if (storedElement.LeftFullPath.ToLower().Equals(rightPath.ToLower()))
                    pathAlreadyExist2 = true;
                //Don't throw exception first, left and right must already exist

                if (pathAlreadyExist2 && storedElement.RightFullPath.ToLower().Equals(leftPath.ToLower()))
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

            xmlData.Read();
            if (xmlData.NodeType != XmlNodeType.Element) throw new InvalidDataException();
            string className = xmlData.Name;
            xmlData = XmlTextReader.Create(new StringReader(xmlString));

            Type t;

            try
            {
                t = syncButlerAssembly.GetType("SyncButler." + className);
                return Activator.CreateInstance(t, xmlData);
            }
            catch (Exception e)
            {
                throw new InvalidDataException();
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
                                friendlyName + "\\" + friendlyName + DEFAULT_SBS_FILENAME_POSTFIX);

            // Read all the text inside
            content = tr.ReadToEnd();

            // close the stream
            tr.Close();

            return content;
        }
    }
}