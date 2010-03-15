using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SyncButler.Exceptions;
using SyncButler.Checksums;
using System.Collections;
using System.Diagnostics;
using System.Xml;

namespace SyncButler
{

    /// <summary>
    /// Represents a folder on the Windows file system.
    /// </summary>
    public class WindowsFolder : WindowsFileSystem
    {
        protected DirectoryInfo nativeDirObj;
        protected SyncableStatusMonitor statusMonitor = null;

        protected long checksumCache;
        protected bool checksumCacheFresh = false;

        /// <summary>
        /// Constructor to unserialise XML string and create an instance of itself.
        /// </summary>
        /// <param name="xmlData">The XMLReader object to read the XML from.</param>
        /// <exception cref="ArgumentNullException">If, when parsing the boolean, the argument is null.</exception>
        /// <exception cref="FormatException">If, when parsing the boolean, the argument is in a format that is not recognised.</exception>
        public WindowsFolder(XmlReader xmlData)
        {
            driveId = relativePath = rootPath = null;
            isPortableStorage = false;

            while ((xmlData.NodeType != XmlNodeType.Element) && (xmlData.Name != "WindowsFolder"))
            {
                if (!(xmlData.Read())) throw new InvalidDataException();
            }

            relativePath = xmlData.GetAttribute("RelativePath").Trim();
            rootPath = xmlData.GetAttribute("RootPath").Trim();
            driveId = xmlData.GetAttribute("DriveID").Trim();
            isPortableStorage = bool.Parse(xmlData.GetAttribute("IsPortableStorage").Trim());
            partitionIndex = int.Parse(xmlData.GetAttribute("PartitionIndex").Trim());

            // Update the drive letter immediately after parsing the XML
            if (isPortableStorage)
                this.UpdateDriveLetter();


            if (relativePath == null || rootPath == null) throw new InvalidDataException("Missing path");
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (!(rootPath + relativePath).EndsWith("\\")) relativePath += "\\";

            nativeDirObj = new DirectoryInfo(rootPath + relativePath);
            nativeFileSystemObj = nativeDirObj;
        }

        /// <summary>
        /// Constructor that takes in two parameters, a root path and the full path.
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFolder(string rootPath, string fullPath)
        {
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (!fullPath.EndsWith("\\")) fullPath += "\\";

            this.nativeDirObj = new DirectoryInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeDirObj;
            this.rootPath = rootPath;
            this.IsPortableStorage = SystemEnvironment.StorageDevices.IsUSBDrive(GetDriveLetter(fullPath));
            this.DriveID = SystemEnvironment.StorageDevices.GetDriveID(GetDriveLetter(fullPath));
            this.PartitionIndex = SystemEnvironment.StorageDevices.GetDrivePartitionIndex(GetDriveLetter(fullPath));
        }

        /// <summary>
        /// Constructor that takes in three parameters, a root path, the full path,
        /// and the parent partnership
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFolder(string rootPath, string fullPath, Partnership parentPartnership)
        {
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (!fullPath.EndsWith("\\")) fullPath += "\\";

            this.nativeDirObj = new DirectoryInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeDirObj;
            this.rootPath = rootPath;
            this.parentPartnership = parentPartnership;
            this.IsPortableStorage = SystemEnvironment.StorageDevices.IsUSBDrive(GetDriveLetter(fullPath));
            this.DriveID = SystemEnvironment.StorageDevices.GetDriveID(GetDriveLetter(fullPath));
            this.PartitionIndex = SystemEnvironment.StorageDevices.GetDrivePartitionIndex(GetDriveLetter(fullPath));
        }

        /// <summary>
        /// Used to create an instance of the topmost left or right IScynable WindowsFolder
        /// </summary>
        /// <param name="fullPath"></param>
        public WindowsFolder(string fullPath) : this(fullPath, fullPath)
        {

        }

        /// <summary>
        /// Used to create an instance of the topmost left or right IScynable WindowsFolder
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="parentPartnership"></param>
        public WindowsFolder(string fullPath, Partnership parentPartnership)
            : this(fullPath, fullPath, parentPartnership)
        {

        }

        public override void SetStatusMonitor(SyncableStatusMonitor statusMonitor)
        {
            this.statusMonitor = statusMonitor;
        }

        /// <summary>
        /// Copy the entire folder over
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException">ONe of the files/folders are read-only or we do not have security permissions</exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <exception cref="DirectoryNotFoundException">Possibly the file/folder structure changed while the operation was in progress</exception>
        /// <exception cref="FileNotFoundException">Possibly the file/folder structure changed while the operation was in progress</exception>
        /// <returns></returns>
        public override Error CopyTo(ISyncable dest)
        {
            WindowsFolder destFolder;

            if (dest is WindowsFolder)// && this.EntityPath().Equals(dest.EntityPath()))
            {
                destFolder = (WindowsFolder)dest;
            }
            else
            {
                throw new InvalidPartnershipException();
            }

            string srcPath, destPath;
            Queue<string> workingList = new Queue<string>(128);

            srcPath = this.rootPath + this.relativePath;
            destPath = destFolder.rootPath + destFolder.RelativePath;
            //srcPath = this.nativeDirObj.FullName; //--> RED FLAG
            //destPath = destFolder.nativeDirObj.FullName; //--> RED FLAG

            if (destFolder.nativeDirObj.Exists) destFolder.nativeDirObj.Delete(true);

            workingList.Enqueue(srcPath);

            string curDir;
            while (workingList.Count > 0)
            {
                curDir = workingList.Dequeue();

                foreach (string subFolder in Directory.GetDirectories(curDir))
                {
                    workingList.Enqueue(subFolder);
                }

                Directory.CreateDirectory(destPath + curDir.Substring(srcPath.Length));

                foreach (string file in Directory.GetFiles(curDir))
                {
                    File.Copy(file, destPath + file.Substring(srcPath.Length));
                }
            }

            return Error.NoError;
        }

        /// <summary>
        /// Deletes this folder
        /// </summary>
        /// <returns></returns>
        public override Error Delete()
        {
            try
            {
                nativeDirObj.Delete(true);
            }
            catch (IOException)
            {
                return Error.IsWorkingFolder;
            }
            catch (System.Security.SecurityException)
            {
                return Error.NoPermission;
            }
            return Error.NoError;

        }

        public override Error Merge(ISyncable item)
        {

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a checksum of the folder. Note that the folder checksum is
        /// only done over the file/folder names contained within it. In other
        /// words, if two folder checksum matches, it ONLY means that the number
        /// of files/folder and file/folder names are the same.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">This folder doesn't exist</exception>
        /// <returns>A long representing the checksum</returns>
        public override long Checksum()
        {
            if (checksumCacheFresh) return checksumCache;

            IRollingHash hashAlgorithm = new Adler32();
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            FileSystemInfo[] children = this.nativeDirObj.GetFileSystemInfos();

            if (children.Length == 0) return hashAlgorithm.Value;

            string[] childrenNames = new string[children.Length];

            for (int i = 0; i < children.Length; i++)
                childrenNames[i] = children[i].Name;

            Array.Sort(childrenNames);

            foreach (string childName in childrenNames)
                hashAlgorithm.Update(UTF8.GetBytes(("\\" + childName).ToCharArray()));

            checksumCache = hashAlgorithm.Value;
            return checksumCache;
        }

        /// <summary>
        /// Determine if the folder has been changed since it was last synced.
        /// If no metadata is available, it assumes the folder has been changed.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">This folder doesn't exist</exception>
        /// <returns></returns>
        public override bool HasChanged()
        {
            Debug.Assert(parentPartnership != null, "parentPartnership not set! Cannot determine if this Folder has changed");

            if (!parentPartnership.hashDictionary.ContainsKey(this.EntityPath())) return true;

            return (parentPartnership.hashDictionary[this.EntityPath()] != this.Checksum());
        }

        /// <summary>
        /// Determine if the number and names of the files and folders contained within
        /// this folders are the same.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Equals(ISyncable item)
        {
            if (!item.GetType().Name.Equals("WindowsFolder"))
                return false;

            WindowsFolder subject = (WindowsFolder)item;

            return (subject.Checksum().Equals(Checksum()));
        }

        public override string EntityPath()
        {
            return "folder:\\\\" + this.relativePath; 
        }

        public override string ToString()
        {
            return this.rootPath + this.relativePath;
        }



        /// <summary>
        /// Synchronizes this folder with another.
        /// 
        /// Implemented:
        /// * Detect missing files
        /// * Detect modified files
        /// * Detect deleted filesa
        /// </summary>
        /// <param name="otherPair"></param>
        /// <exception cref="InvalidPartnershipException">Tried to sync a windows folder with something else</exception>
        /// <exception cref="UnauthorizedAccessException">Could not read one of the files/folders because of security permissions</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive. (Probably while generating a file checksum)</exception>
        /// <exception cref="IOException">The file is already open. (Probably while generating a file checksum)</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading. (Probably while generating a file checksum)</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed. (Probably while generating a file checksum)</exception>
        /// <exception cref="PathTooLongException">The directory depth is too long!</exception>
        /// <returns>A list of conflicts detected</returns>
        public override List<Conflict> Sync(ISyncable otherPair)
        {
            
            WindowsFolder partner;

            System.Diagnostics.Debug.Assert(parentPartnership != null, "The parent partnership has not been set; cannot sync");

            if (otherPair is WindowsFolder)// && this.EntityPath().Equals(otherPair.EntityPath()))
                partner = (WindowsFolder)otherPair;
            else
                throw new InvalidPartnershipException();
            // check if the folder is created or not, else create

            // Update the drive letter.
            // It is actually faster to update the drive letter than to check whether it needs updating.
            // Unless we decide to store the drive letter, but then it's memory vs speed.
            this.UpdateDriveLetter();
            partner.UpdateDriveLetter();

            // check if the folder is created or not, else create
            if (!this.Exists())
            {
                Directory.CreateDirectory(rootPath + relativePath);
            }
            if (!partner.Exists())
            {
                Directory.CreateDirectory(partner.rootPath + partner.relativePath);
            }

            // Compare the files and folders under this directory
            List<Conflict> conflicts = new List<Conflict>();
            string leftPath, rightPath;
            Queue<string> workingList = new Queue<string>(128);

            leftPath = this.rootPath + this.relativePath;
            rightPath = partner.rootPath + partner.relativePath;
            //leftPath = this.nativeDirObj.FullName; //--> RED FLAG
            //rightPath = partner.nativeDirObj.FullName; //--> RED FLAG

            // Check Left to Right
            workingList.Enqueue("");

            checksumCacheFresh = false; // Make sure we're really comparing with the current folder

            string curDir;
            Conflict.Action autoResolveAction;

            while (workingList.Count > 0)
            {
                curDir = workingList.Dequeue();
                // Temporary -- give basic functionality first.
                if (statusMonitor != null) statusMonitor(new SyncableStatus("folder:\\\\" + curDir, 0, 0, SyncableStatus.ActionType.Sync));

                // Check if there are folders missing on the right. Otherwise, add it to the queue
                foreach (string subFolderLeft in Directory.GetDirectories(leftPath + curDir))
                {
                    string curFolderLeft = subFolderLeft.Substring(leftPath.Length) + "\\";

                    if (Directory.Exists(rightPath + curFolderLeft)) workingList.Enqueue(curFolderLeft);
                    else
                    {   // Folder exists only on the left
                        if (parentPartnership.ChecksumExists("folder:\\\\" + curFolderLeft))
                            autoResolveAction = Conflict.Action.DeleteLeft; // The folder was deleted from the right.
                        else
                            autoResolveAction = Conflict.Action.CopyToRight;

                        conflicts.Add(new Conflict(
                            new WindowsFolder(leftPath, subFolderLeft, this.parentPartnership),
                            new WindowsFolder(rightPath, rightPath + curFolderLeft, this.parentPartnership),
                            autoResolveAction
                        ));
                    }
                }

                // Check if there are folders missing on the left.
                foreach (string subFolderRight in Directory.GetDirectories(rightPath + curDir))
                {
                    string curFolderRight = subFolderRight.Substring(rightPath.Length) + "\\";

                    if (!Directory.Exists(leftPath + curFolderRight))
                    {   // Folder exists only on the right
                        if (parentPartnership.ChecksumExists("folder:\\\\" + curFolderRight))
                            autoResolveAction = Conflict.Action.DeleteRight; // The folder was deleted from the left
                        else
                            autoResolveAction = Conflict.Action.CopyToLeft;

                        conflicts.Add(new Conflict(
                            new WindowsFolder(leftPath, leftPath + curFolderRight, this.parentPartnership),
                            new WindowsFolder(rightPath, subFolderRight, this.parentPartnership),
                            autoResolveAction
                        ));
                    }
                }

                foreach (string subFileLeft in Directory.GetFiles(leftPath + curDir))
                {
                    string curFileLeft = subFileLeft.Substring(leftPath.Length);
                    WindowsFile leftFile = new WindowsFile(leftPath, subFileLeft, this.parentPartnership);

                    if (File.Exists(rightPath + curFileLeft))
                    {   // File exists on both sides. Check if they're the same
                        WindowsFile rightFile = new WindowsFile(rightPath, rightPath + curFileLeft, this.parentPartnership);

                        leftFile.SetStatusMonitor(statusMonitor);
                        conflicts.AddRange(leftFile.Sync(rightFile));
                    }
                    else
                    {   // File only exists on the left
                        if (parentPartnership.ChecksumExists("file:\\\\" + curFileLeft))
                        {
                            // File was deleted from the right
                            if (parentPartnership.GetLastChecksum(leftFile) == leftFile.Checksum())
                            {
                                autoResolveAction = Conflict.Action.DeleteLeft;
                            }
                            else
                            {
                                autoResolveAction = Conflict.Action.Unknown; // ...but the file was since modified
                            }
                        }
                        else
                        {
                            autoResolveAction = Conflict.Action.CopyToRight;
                        }
                        
                        conflicts.Add(new Conflict(
                            new WindowsFile(leftPath, subFileLeft, this.parentPartnership),
                            new WindowsFile(rightPath, rightPath + curFileLeft, this.parentPartnership),
                            autoResolveAction
                        ));
                    }
                }

                foreach (string subFileRight in Directory.GetFiles(rightPath + curDir))
                {
                    string curFileRight = subFileRight.Substring(rightPath.Length);
                    
                    if (!File.Exists(leftPath + curFileRight))
                    {   // File only exists on the right
                        if (parentPartnership.ChecksumExists("file:\\\\" + curFileRight))
                        {
                            WindowsFile rightFile = new WindowsFile(rightPath, subFileRight, this.parentPartnership);
                            // File was deleted from the left
                            if (parentPartnership.GetLastChecksum(rightFile) == rightFile.Checksum())
                                autoResolveAction = Conflict.Action.DeleteRight;
                            else
                                autoResolveAction = Conflict.Action.Unknown; // ...but the file was since modified
                        }
                        else
                        {
                            autoResolveAction = Conflict.Action.CopyToLeft;
                        }

                        conflicts.Add(new Conflict(
                            new WindowsFile(leftPath, leftPath + curFileRight, this.parentPartnership),
                            new WindowsFile(rightPath, subFileRight, this.parentPartnership),
                            autoResolveAction
                        ));
                    }
                }
            }

            return conflicts;
        }

        public override ISyncable CreateChild(string entityPath)
        {
            if (entityPath.StartsWith(@"file:\\"))
                return new WindowsFile(this.rootPath, this.rootPath + entityPath.Substring(7), this.parentPartnership);
            else if (entityPath.StartsWith(@"folder:\\"))
                return new WindowsFolder(this.rootPath, this.rootPath + entityPath.Substring(9), this.parentPartnership);
            else
                throw new ArgumentException();
        }

        public override void SerializeXML(XmlWriter xmlData)
        {
            xmlData.WriteStartElement("WindowsFolder");
            xmlData.WriteAttributeString("RelativePath", relativePath);
            xmlData.WriteAttributeString("RootPath", rootPath);
            xmlData.WriteAttributeString("IsPortableStorage", isPortableStorage.ToString());
            xmlData.WriteAttributeString("DriveID", driveId);
            xmlData.WriteAttributeString("PartitionIndex", partitionIndex.ToString());
            xmlData.WriteEndElement();
        }
    }
}
