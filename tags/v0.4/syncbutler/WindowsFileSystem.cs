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
        protected String relativePath;
        protected String rootPath;
        protected FileSystemInfo nativeFileSystemObj;
        protected Partnership parentPartnership = null;

        /// <summary>
        /// Gets the name of the current folder/file. Additional info, such as the directory structure prior to this folder/file, is stripped away.
        /// </summary>
        /// <remarks>Calls Refresh() prior to getting the name.</remarks>
        public String Name
        {
            get
            {
                this.nativeFileSystemObj.Refresh();
                return this.nativeFileSystemObj.Name;
            }
        }

        /// <summary>
        /// Gets the relative path of the current folder/file. This is the full path with the root path stripped away.
        /// </summary>
        public String RelativePath {
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
        public static String StripPrefix(String prefix, String data)
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

        public bool Exists()
        {
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

        public abstract Error Delete();

        public abstract Error Merge(ISyncable item);

        public abstract bool HasChanged();

        public abstract bool Equals(ISyncable item);

        public abstract string EntityPath();

        public abstract ISyncable CreateChild(string entityPath);

        public abstract void SerializeXML(XmlWriter xmlData);
    }
}
