using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace SyncButler
{
    /// <summary>
    /// Represents a Windows File System object, such as a folder or a file.
    /// </summary>
    public abstract class WindowsFileSystem : ISyncable
    {
        public static string DRIVEID_NETWORK = @"network-drive";
        public static string PREF_FOLDER = @"folder:\\";
        public static string PREF_FILE = @"file:\\";
        protected string driveLetter = null;
        protected string relativePath;
        protected string rootPath;
        protected string driveId;
        protected int partitionIndex;
        protected bool isPortableStorage;
        protected FileSystemInfo nativeFileSystemObj;
        protected Partnership parentPartnership = null;

        protected internal SyncableStatusMonitor statusMonitor = null;
        protected internal SyncableErrorHandler errorHandler = null;

        /// <summary>
        /// Gets the name of the current folder/file. Additional info, such as the directory structure prior to this folder/file, is stripped away.
        /// </summary>
        /// <remarks>Calls Refresh() prior to getting the name.</remarks>
        public string Name
        {
            get
            {
                this.nativeFileSystemObj.Refresh();
                return this.nativeFileSystemObj.Name;
            }
        }
        
        /// <summary>
        /// Gets/Sets the drive letter. This value is null by default and will be set when UpdateDriveLetter is called.
        /// </summary>
        public string DriveLetter
        {
            get
            {
                return this.driveLetter;
            }
            set
            {
                this.driveLetter = value;
            }
        }

        /// <summary>
        /// Gets/Sets whether the file/folder is stored on a portable storage device.
        /// </summary>
        public bool IsPortableStorage
        {
            get
            {
                return this.isPortableStorage;
            }
            set
            {
                this.isPortableStorage = value;
            }
        }

        /// <summary>
        /// Gets/Sets the drive ID that uniquely identifies the drive. Used in conjunction with IsPortableStorage
        /// to update the drive letters of portable storage devices.
        /// </summary>
        public string DriveID
        {
            get
            {
                return this.driveId;
            }
            set
            {
                this.driveId = value;
            }
        }

        /// <summary>
        /// Gets/Sets the partition index of this drive. Useful when recovering the drive letter on a storage device with multiple partitions.
        /// </summary>
        public int PartitionIndex
        {
            get
            {
                return this.partitionIndex;
            }
            set
            {
                this.partitionIndex = value;
            }
        }

        /// <summary>
        /// Gets the relative path of the current folder/file. This is the full path with the root path stripped away.
        /// </summary>
        public string RelativePath {
            get
            {
                return this.relativePath;
            }
        }

        /// <summary>
        /// Gets the FileAttributes object containing the attributes of the folder/file.
        /// </summary>
        /// <remarks>
        /// Refresh() will be called first prior to returning the underlying attributes.
        /// </remarks>
        /// <exception cref="FileNotFoundException">The specified file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="ArgumentException">The caller attempts to set an invalid file attribute.</exception>
        /// <exception cref="IOException">Refresh cannot initialize the data. </exception>
        public FileAttributes Attributes
        {
            get
            {
                this.nativeFileSystemObj.Refresh();
                return this.nativeFileSystemObj.Attributes;
            }
        }

        /// <summary>
        /// Gets the time that the folder/file was last accessed.
        /// </summary>
        /// <remark>
        /// Accessing this property will call Refresh() on the underlying FileInfo object.
        /// </remark>
        /// <exception cref="IOException">Refresh cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Microsoft Windows NT or later.</exception>
        public DateTime LastAccessTime
        {
            get
            {
                this.nativeFileSystemObj.Refresh();
                return this.nativeFileSystemObj.LastAccessTime;
            }
        }

        /// <summary>
        /// Gets the time that the folder/file was last written to.
        /// </summary>
        /// <remark>
        /// Accessing this property will call Refresh() on the underlying FileInfo object.
        /// </remark>
        /// <exception cref="IOException">Refresh cannot initialize the data.</exception>
        /// <exception cref="PlatformNotSupportedException">The current operating system is not Microsoft Windows NT or later.</exception>
        public DateTime LastWriteTime
        {
            get
            {
                return this.nativeFileSystemObj.LastWriteTime;
            }
        }

        /// <summary>
        /// Strips a prefix from a given string. Used mainly for obtaining a relative path from the full path.
        /// </summary>
        /// <param name="prefix">String prefix to remove.</param>
        /// <param name="data">String to remove prefix from.</param>
        /// <returns>String with the prefix removed.</returns>
        public static string StripPrefix(string prefix, string data)
        {
            if (data.StartsWith(prefix))
            {
                return data.Substring(prefix.Length);
            }
            else
            {
                return data;
            }
        }

        /// <summary>
        /// Method that is used internally to update the drive letter of the root path, based on the current drive ID.
        /// Useful for correcting the drive letters of portable devices.
        /// </summary>
        public void UpdateDriveLetter()
        {
            if (this.DriveID != DRIVEID_NETWORK)
            {
                string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(this.DriveID, this.PartitionIndex);
                this.driveLetter = driveLetter;
                this.rootPath = ReplaceDriveLetter(this.rootPath, this.driveLetter);
            }
            else
            {
                this.driveLetter = GetDriveLetter(this.rootPath);
            }
        }

        /// <summary>
        /// Method that is used internally to update the drive letter of the root path.
        /// This method accepts a parameter that is the drive letter.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the format of C:</param>
        public void UpdateDriveLetter(string driveLetter)
        {
            if (this.DriveID != DRIVEID_NETWORK)
            {
                this.driveLetter = driveLetter;
                this.rootPath = ReplaceDriveLetter(this.rootPath, this.driveLetter);
            }
            else
            {
                this.driveLetter = GetDriveLetter(this.rootPath);
            }
        }

        /// <summary>
        /// Returns the drive letter in the format of C:\
        /// It can return the drive letter from any path that contains it.
        /// </summary>
        /// <param name="somePath">A path containing the drive letter</param>
        /// <returns>The drive letter in the format of C:</returns>
        /// <exception cref="Exceptions.InvalidPathException">If the drive letter could not be obtained from the path.</exception>
        public static string GetDriveLetter(string somePath)
        {
            somePath = somePath.Trim();
            string[] parts = somePath.Split(':');

            if ((parts.Length > 0) && (parts[0].Length > 0))
                return (parts[0] + @":");
            else
                throw new Exceptions.InvalidPathException("The drive letter could not be obtained from the path '" + somePath + "'");
        }

        /// <summary>
        /// Returns the provided path with the drive letter changed to the provided drive letter.
        /// </summary>
        /// <param name="somePath">The path containing the drive letter to change</param>
        /// <param name="driveLetter">The drive letter to change to, in the format of C:</param>
        /// <returns>The path with drive letter replaced</returns>
        /// <exception cref="ArgumentOutOfRangeException">If when calculating the substring, the index was out of range.</exception>
        public static string ReplaceDriveLetter(string somePath, string driveLetter)
        {
            somePath = somePath.Trim();
            int stopIndex = somePath.IndexOf(':');
            return (driveLetter + somePath.Substring(stopIndex + 1));
        }

        #region ISyncable Members

        /// <summary>
        /// Checks and returns whether the file or folder exists.
        /// Calls Refresh() on the underlying native file system object before attempting the check.
        /// </summary>
        /// <returns>True if the file system object (File or Folder) exists. False otherwise.</returns>
        public bool Exists()
        {
            this.nativeFileSystemObj.Refresh();
            return nativeFileSystemObj.Exists;
        }

        /// <summary>
        /// Set a reference back to the containing partnership
        /// </summary>
        /// <param name="parentPartnership">The containing partnership</param>
        public void SetParentPartnership(Partnership parentPartnership)
        {
            this.parentPartnership = parentPartnership;
        }

        /// <summary>
        /// Returns a reference to the containing partnership
        /// </summary>
        /// <returns>The containing partnership</returns>
        public Partnership GetParentPartnership()
        {
            return this.parentPartnership;
        }

        /// <summary>
        /// Gets the checksum stored from a previous scan. Returns null if the checksum does 
        /// not exist.
        /// </summary>
        /// <returns></returns>
        public long GetStoredChecksum()
        {
            return parentPartnership.GetLastChecksum(this);
        }

        /// <summary>
        /// Indicates whether this file/folder is ignored
        /// </summary>
        /// <returns></returns>
        public bool Ignored()
        {
            Debug.Assert(parentPartnership != null, "Cannot check Ignored until parent partnership is set");
            return parentPartnership.hashDictionary.ContainsKey(EntityPath("ignored"));
        }

        /// <summary>
        /// Sets whether this file/folder should be ignored
        /// </summary>
        /// <param name="value"></param>
        public void Ignored(bool value)
        {
            if (value)
            {
                RemoveStoredChecksum();
                parentPartnership.hashDictionary.Add(EntityPath("ignored"), 0);
            }
            else
            {
                parentPartnership.hashDictionary.Remove(EntityPath("ignored"));
            }
        }

        /// <summary>
        /// Updates the sotred checksum with the file/folder's current checksum.
        /// </summary>
        public void UpdateStoredChecksum()
        {
            Debug.Assert(!Ignored(), "Updating checksum on an ignored FileSystem object");
            parentPartnership.UpdateLastChecksum(this);
        }

        /// <summary>
        /// Removes the stored checksum for this file/folder form the checksum dictinary.
        /// </summary>
        public void RemoveStoredChecksum()
        {
            parentPartnership.RemoveChecksum(this);
        }

        /// <summary>
        /// Serializes this object into XML
        /// </summary>
        /// <param name="xmlData"></param>
        public abstract void SerializeXML(XmlWriter xmlData);

        /// <summary>
        /// Serializes this object into a string
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.OmitXmlDeclaration = true;
            xmlSettings.Indent = true;
            XmlWriter xmlData = XmlWriter.Create(output, xmlSettings);

            xmlData.WriteStartDocument();
            SerializeXML(xmlData);
            xmlData.WriteEndDocument();
            xmlData.Close();

            return output.ToString();
        }

        /// <summary>
        /// Prepares for a sync. Sets the driveLetter to null so that it will be rechecked.
        /// </summary>
        /// <exception cref="Exceptions.NetworkDriveException">Thrown when the original network drive cannot be found.</exception>
        public void PrepareSync()
        {
            if (this.DriveID == DRIVEID_NETWORK)
            {
                driveLetter = GetDriveLetter(this.rootPath);

                if (!SystemEnvironment.StorageDevices.GetAllDrives().Contains(driveLetter + @"\"))
                {
                    throw new Exceptions.NetworkDriveException("The network drive could not be found.");
                }
                else if (SystemEnvironment.StorageDevices.GetDeviceType(driveLetter) != SyncButler.SystemEnvironment.StorageDevices.DeviceType.NetworkDrive)
                {
                    throw new Exceptions.NetworkDriveException("The original network drive could not be found.");
                }
            }
            else
            {
                // Force drive letter to be rechecked
                driveLetter = null;
            }

        }


        /// <summary>
        /// Returns the checksum for this file/folder
        /// </summary>
        /// <returns></returns>
        public abstract long Checksum();

        /// <summary>
        /// Sets the delegate which reports the progress of a sync
        /// </summary>
        /// <param name="statusMonitor"></param>
        public void SetStatusMonitor(SyncableStatusMonitor statusMonitor)
        {
            this.statusMonitor = statusMonitor;
        }

        /// <summary>
        /// Sets the delegate used to report an error encountered while scanning
        /// </summary>
        /// <param name="handler"></param>
        public void SetErrorHandler(SyncableErrorHandler handler)
        {
            this.errorHandler = handler;
        }

        /// <summary>
        /// Scans the File/Folders and reports a list of conflicts
        /// </summary>
        /// <param name="otherPair"></param>
        /// <returns></returns>
        public abstract List<Conflict> Sync(ISyncable otherPair);

        /// <summary>
        /// Copies this file/folder onto another file/folder
        /// </summary>
        /// <param name="item"></param>
        public abstract void CopyTo(ISyncable item);

        /// <summary>
        /// Merges two files. NOT IMPLEMENTED.
        /// </summary>
        /// <param name="item"></param>
        public abstract void Merge(ISyncable item);

        /// <summary>
        /// Delete this file/folder
        /// </summary>
        /// <param name="recoverable"></param>
        public abstract void Delete(bool recoverable);

        /// <summary>
        /// Indicates whether this file/folder has changed since the last scan.
        /// </summary>
        /// <returns></returns>
        public abstract bool HasChanged();

        /// <summary>
        /// Indicates whether the two file/folders are identical.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public abstract bool Equals(ISyncable item);

        /// <summary>
        /// Returns a string that represents this file/folder in the context of the
        /// containing partnership
        /// </summary>
        /// <returns></returns>
        public abstract string EntityPath();

        /// <summary>
        /// Returns a string that represents this file/folder in the context of the
        /// containing partnership
        /// </summary>
        /// <param name="extraAttributes">Additional attributes to add to the EntityPath</param>
        /// <returns></returns>
        public abstract string EntityPath(string extraAttributes);

        /// <summary>
        /// Creates the file/folder based onthe entity path given and the 
        /// containing partnership.
        /// </summary>
        /// <param name="entityPath"></param>
        /// <returns></returns>
        public abstract ISyncable CreateChild(string entityPath);

        #endregion

        /// <summary>
        /// Checks if 2 paths are similar by comparing using FileInfo after standardising.
        /// </summary>
        /// <param name="path1">1st path</param>
        /// <param name="path2">2nd path</param>
        /// <returns>true if they are equal</returns>
        internal static bool PathsEqual(string path1, string path2)
        {
            FileSystemInfo fsi1 = new FileInfo(path1);
            FileSystemInfo fsi2 = new FileInfo(path2);
            char[] standard = { '\\', ' ' };
            return fsi1.FullName.TrimEnd(standard).ToLower() == fsi2.FullName.TrimEnd(standard).ToLower();
        }
    }
}
