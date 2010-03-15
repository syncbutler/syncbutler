using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SyncButler.Exceptions;
using SyncButler.Checksums;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.VisualBasic.FileIO;

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
        /// Constructor that takes in three parameters, a root path, the full path,
        /// and the parent folder.
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        /// <param name="parent">Parent Folder</param>
        public WindowsFolder(string rootPath, string fullPath, WindowsFolder parent)
        {
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (!fullPath.EndsWith("\\")) fullPath += "\\";

            this.nativeDirObj = new DirectoryInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeDirObj;
            this.rootPath = rootPath;
            this.IsPortableStorage = parent.IsPortableStorage;
            this.DriveID = parent.DriveID;
            this.PartitionIndex = parent.PartitionIndex;
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
            : this(rootPath, fullPath)
        {
            this.parentPartnership = parentPartnership;
        }

        /// <summary>
        /// Constructor that takes in four parameters, a root path, the full path,
        /// the parent partnership, and the parent folder
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        /// <param name="parent">Parent folder</param>
        /// <param name="parentPartnership">The containing partnership</param>
        public WindowsFolder(string rootPath, string fullPath, WindowsFolder parent, Partnership parentPartnership)
            : this(rootPath, fullPath, parent)
        {
            this.parentPartnership = parentPartnership;
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
        public override Error Delete(bool recoverable)
        {
            try
            {
                if (recoverable)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(nativeDirObj.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                else
                {
                    nativeDirObj.Delete(true);
                }
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
            return PREF_FOLDER + this.relativePath; 
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
        public override List<Conflict> Sync(ISyncable otherObj)
        {
            System.Diagnostics.Debug.Assert(parentPartnership != null, "The parent partnership has not been set.");
            System.Diagnostics.Debug.Assert(otherObj is WindowsFolder, "Other ISyncable passed is not a WindowsFolder object.");

            WindowsFolder partner = (WindowsFolder)otherObj;
            List<Conflict> conflictList = new List<Conflict>();
            Queue<string> workList = new Queue<string>();
            string leftPath, rightPath, currDir;

            // Update the drive letters if there is a need to.
            // Note: We do not store the driveLetter during serialisation because it is not useful to do so.
            if (driveLetter == null)
                this.UpdateDriveLetter();
            if (partner.driveLetter == null)
                partner.UpdateDriveLetter();

            leftPath = this.rootPath + this.relativePath;
            rightPath = partner.rootPath + partner.relativePath;
            this.checksumCacheFresh = false; // Invalidate the checksum cache

            workList.Enqueue("");

            // Work while the work queue has stuff.
            while (workList.Count > 0)
            {
                currDir = workList.Dequeue();

                if (this.statusMonitor != null)
                    this.statusMonitor(new SyncableStatus(PREF_FOLDER + currDir, 0, 0, SyncableStatus.ActionType.Sync));

                // Get sub-directories from the left folder
                List<string> leftFolders = new List<string>();
                leftFolders.AddRange(Directory.GetDirectories(leftPath + currDir));

                // Get sub-directories from the right folder
                List<string> rightFolders = new List<string>();
                rightFolders.AddRange(Directory.GetDirectories(rightPath + currDir));

                // Get files from left folder
                List<string> leftFiles = new List<string>();
                leftFiles.AddRange(Directory.GetFiles(leftPath + currDir));

                // Get files from right folder
                List<string> rightFiles = new List<string>();
                rightFiles.AddRange(Directory.GetFiles(rightPath + currDir));

                //---------------------------------------------------------------------------------
                // Check right folders against left
                //---------------------------------------------------------------------------------
                foreach (string leftFolder in leftFolders)
                {
                    Conflict.Action autoResolveAction = Conflict.Action.Unknown;
                    string leftFolderName = leftFolder.Substring(leftPath.Length) + @"\";

                    // The folder exists on both sides
                    if (Directory.Exists(rightPath + leftFolderName))
                    {
                        workList.Enqueue(leftFolderName);
                    }
                    // The folder exists on the left but not on the right
                    else
                    {
                        // If the checksum existed, then we infer that the folder was deleted from the right.
                        if (this.parentPartnership.ChecksumExists(PREF_FOLDER + leftFolderName))
                            autoResolveAction = Conflict.Action.DeleteLeft;
                        // Otherwise, we infer that the folder is newly created.
                        else
                            autoResolveAction = Conflict.Action.CopyToRight;

                        WindowsFolder leftObj = new WindowsFolder(leftPath, leftFolder, this, this.parentPartnership);
                        WindowsFolder rightObj = new WindowsFolder(rightPath, rightPath + leftFolderName, this, this.parentPartnership);
                        Conflict conflict = new Conflict(leftObj, rightObj, autoResolveAction);

                        // Set the drive letters to what we updated at the start, to save on WMI calls.
                        leftObj.UpdateDriveLetter(this.driveLetter);
                        rightObj.UpdateDriveLetter(partner.driveLetter);

                        conflictList.Add(conflict);
                        System.Diagnostics.Debug.Assert(autoResolveAction != Conflict.Action.Unknown);
                    }
                }

                //---------------------------------------------------------------------------------
                // Check left folders against right
                //---------------------------------------------------------------------------------
                foreach (string rightFolder in rightFolders)
                {
                    Conflict.Action autoResolveAction = Conflict.Action.Unknown;
                    string rightFolderName = rightFolder.Substring(rightPath.Length) + @"\";

                    // The folder exists on both sides
                    if (Directory.Exists(leftPath + rightFolderName))
                    {
                        workList.Enqueue(rightFolderName);
                    }
                    // The folder exists on the right but not on the left
                    else
                    {
                        // If the checksum existed, then we infer that the folder was deleted from the left.
                        if (this.parentPartnership.ChecksumExists(PREF_FOLDER + rightFolderName))
                            autoResolveAction = Conflict.Action.DeleteLeft;
                        // Otherwise, we infer that the folder is newly created.
                        else
                            autoResolveAction = Conflict.Action.CopyToRight;

                        WindowsFolder leftObj = new WindowsFolder(leftPath, leftPath + rightFolderName, this, this.parentPartnership);
                        WindowsFolder rightObj = new WindowsFolder(rightPath, rightFolder, this, this.parentPartnership);
                        Conflict conflict = new Conflict(leftObj, rightObj, autoResolveAction);

                        // Set the drive letters to what we updated at the start, to save on WMI calls.
                        leftObj.UpdateDriveLetter(this.driveLetter);
                        rightObj.UpdateDriveLetter(partner.driveLetter);

                        conflictList.Add(conflict);
                        System.Diagnostics.Debug.Assert(autoResolveAction != Conflict.Action.Unknown);
                    }
                }

                //---------------------------------------------------------------------------------
                // Check files
                //---------------------------------------------------------------------------------
                SortedList<string, string> relativeFilePaths = new SortedList<string, string>();

                // Check right against left
                foreach (string path in leftFiles)
                {
                    string relativePath = path.Substring(this.rootPath.Length);
                    relativeFilePaths.Add(relativePath, relativePath);
                    WindowsFile leftFileObj = new WindowsFile(this.rootPath, path, this, this.parentPartnership);
                    WindowsFile rightFileObj = new WindowsFile(partner.rootPath, partner.rootPath + relativePath, this, this.parentPartnership);

                    leftFileObj.UpdateDriveLetter(this.driveLetter);
                    rightFileObj.UpdateDriveLetter(partner.driveLetter);
                    leftFileObj.SetStatusMonitor(statusMonitor);
                    rightFileObj.SetStatusMonitor(statusMonitor);

                    conflictList.AddRange(leftFileObj.Sync(rightFileObj));
                }

                // Check left against right
                foreach (string path in rightFiles)
                {
                    string relativePath = path.Substring(partner.rootPath.Length);

                    // Ignore those that were checked when checking right against left (above)
                    if (!relativeFilePaths.ContainsKey(relativePath))
                    {
                        relativeFilePaths.Add(relativePath, relativePath);

                        WindowsFile leftFileObj = new WindowsFile(this.rootPath, this.rootPath + relativePath, this, this.parentPartnership);
                        WindowsFile rightFileObj = new WindowsFile(partner.rootPath, path, this, this.parentPartnership);

                        leftFileObj.UpdateDriveLetter(this.driveLetter);
                        rightFileObj.UpdateDriveLetter(partner.driveLetter);
                        conflictList.AddRange(leftFileObj.Sync(rightFileObj));
                    }
                }
            }

            return conflictList;
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
