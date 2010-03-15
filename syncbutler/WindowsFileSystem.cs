using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SyncButler
{
    /// <summary>
    /// Represents a Windows File System object, such as a folder or a file.
    /// </summary>
    public abstract class WindowsFileSystem : ISyncable
    {
        protected static string PREF_FOLDER = @"folder:\\";
        protected static string PREF_FILE = @"file:\\";
        protected string driveLetter = null;
        protected string relativePath;
        protected string rootPath;
        protected string driveId;
        protected int partitionIndex;
        protected bool isPortableStorage;
        protected FileSystemInfo nativeFileSystemObj;
        protected Partnership parentPartnership = null;

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
        /// Gets/Sets the drive letter. This value is null by default.
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
        /// Gets/Sets the drive ID that uniquely identifies the drive. Used in conjunction with IsPortableStorage.
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
        /// Checks and returns whether the file or folder exists.
        /// Calls Refresh() on the underlying native file system object before attempting the check.
        /// </summary>
        /// <returns>True if the file system object (File or Folder) exists. False otherwise.</returns>
        public bool Exists()
        {
            this.nativeFileSystemObj.Refresh();
            return nativeFileSystemObj.Exists;
        }

        public void SetParentPartnership(Partnership parentPartnership)
        {
            this.parentPartnership = parentPartnership;
        }

        public Partnership GetParentPartnership()
        {
            return this.parentPartnership;
        }

        public long GetStoredChecksum()
        {
            return parentPartnership.GetLastChecksum(this);
        }

        public void UpdateStoredChecksum()
        {
            parentPartnership.UpdateLastChecksum(this);
        }

        public void RemoveStoredChecksum()
        {
            parentPartnership.RemoveChecksum(this);
        }

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

        public abstract long Checksum();

        public abstract void SetStatusMonitor(SyncableStatusMonitor monitor);

        public abstract List<Conflict> Sync(ISyncable otherPair);

        public abstract Error CopyTo(ISyncable item);

        //public abstract Error Delete();

        public abstract Error Merge(ISyncable item);

        public abstract bool HasChanged();

        public abstract bool Equals(ISyncable item);

        public abstract string EntityPath();

        public abstract ISyncable CreateChild(string entityPath);

        public abstract void SerializeXML(XmlWriter xmlData);

        /// <summary>
        /// Method that is used internally to update the drive letter of the root path, based on the current drive ID.
        /// </summary>
        public void UpdateDriveLetter()
        {
            string driveLetter = SystemEnvironment.StorageDevices.GetDriveLetter(this.DriveID, this.PartitionIndex);
            this.driveLetter = driveLetter;
            this.rootPath = ReplaceDriveLetter(this.rootPath, this.driveLetter);
        }

        /// <summary>
        /// Method that is used internally to update the drive letter of the root path.
        /// This method accepts a parameter that is the drive letter.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the format of C:</param>
        public void UpdateDriveLetter(string driveLetter)
        {
            this.driveLetter = driveLetter;
            this.rootPath = ReplaceDriveLetter(this.rootPath, this.driveLetter);
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


        public abstract Error Delete(bool recoverable);
        

        #endregion
    }
}
