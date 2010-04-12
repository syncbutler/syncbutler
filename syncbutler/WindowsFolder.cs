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
using System.IO;
using System.Text;
using SyncButler.Exceptions;
using SyncButler.Checksums;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.VisualBasic.FileIO;
using System.Security.AccessControl;

namespace SyncButler
{

    /// <summary>
    /// Represents a folder on the Windows file system.
    /// </summary>
    public class WindowsFolder : WindowsFileSystem
    {
        private DirectoryInfo nativeDirObj;
        private long checksumCache;
        private bool checksumCacheFresh = false;

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
                if (!(xmlData.Read())) throw new InvalidDataException();

            relativePath = xmlData.GetAttribute("RelativePath").Trim();
            rootPath = xmlData.GetAttribute("RootPath").Trim();
            driveId = xmlData.GetAttribute("DriveID").Trim();
            isPortableStorage = bool.Parse(xmlData.GetAttribute("IsPortableStorage").Trim());
            partitionIndex = int.Parse(xmlData.GetAttribute("PartitionIndex").Trim());

            // Update the drive letter immediately after parsing the XML
            try
            {
                if (isPortableStorage) this.UpdateDriveLetter();
            }
            catch (DriveNotFoundException)
            {
                this.driveLetter = "MISSING:";
                this.rootPath = ReplaceDriveLetter(this.rootPath, this.driveLetter);
            }

            if (relativePath == null || rootPath == null) throw new InvalidDataException("Missing path");
            if (!rootPath.EndsWith("\\")) rootPath += "\\";
            if (!(rootPath + relativePath).EndsWith("\\")) relativePath += "\\";

            if (this.driveLetter == "MISSING:")
            {
                nativeDirObj = new DirectoryInfo("Z" + rootPath.Substring(7) + relativePath);
                nativeFileSystemObj = nativeDirObj;
            }
            else
            {
                nativeDirObj = new DirectoryInfo(rootPath + relativePath);
                nativeFileSystemObj = nativeDirObj;
            }
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

            if (SystemEnvironment.StorageDevices.GetDeviceType(GetDriveLetter(fullPath)) == SyncButler.SystemEnvironment.StorageDevices.DeviceType.NetworkDrive)
            {
                this.DriveID = DRIVEID_NETWORK;
                this.PartitionIndex = 0;
            }
            else
            {
                try
                {
                    this.DriveID = SystemEnvironment.StorageDevices.GetDriveID(GetDriveLetter(fullPath));
                    this.PartitionIndex = SystemEnvironment.StorageDevices.GetDrivePartitionIndex(GetDriveLetter(fullPath));
                }
                catch (Exceptions.DriveNotSupportedException)
                {
                    this.DriveID = "";
                    this.PartitionIndex = -1;
                }
            }
        }

        /// <summary>
        /// Constructor that takes in three parameters, a root path, the full path,
        /// and the parent partnership
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFolder(string rootPath, string fullPath, Partnership parentPartnership) : this(rootPath, fullPath)
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
        public WindowsFolder(string rootPath, string fullPath, WindowsFolder parent, Partnership parentPartnership) : this(rootPath, fullPath, parent)
        {
            this.parentPartnership = parentPartnership;
        }

        /// <summary>
        /// Used to create an instance of the topmost left or right IScynable WindowsFolder
        /// </summary>
        /// <param name="fullPath"></param>
        public WindowsFolder(string fullPath) : this(fullPath, fullPath) { }

        /// <summary>
        /// Used to create an instance of the topmost left or right IScynable WindowsFolder
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="parentPartnership"></param>
        public WindowsFolder(string fullPath, Partnership parentPartnership) : this(fullPath, fullPath, parentPartnership) { }

        /// <summary>
        /// Gets the size of all the files in this folder and its subfolders.
        /// </summary>
        public long Length
        {
            get
            {
                return CalculateFolderSize();
            }
        }

        /// <summary>
        /// Method used internally and called when the property Length is accessed.
        /// It traverses the file system tree and sums the length of all files in subdirectories.
        /// </summary>
        /// <returns></returns>
        private long CalculateFolderSize()
        {
            Queue<string> workList = new Queue<string>();
            workList.Enqueue(this.rootPath + this.relativePath);

            string currDir;
            long totalSize = 0;

            while (workList.Count > 0)
            {
                currDir = workList.Dequeue();

                foreach (string dir in Directory.GetDirectories(currDir))
                    workList.Enqueue(dir);

                foreach (string file in Directory.GetFiles(currDir))
                    totalSize += (new FileInfo(file)).Length;
            }

            return totalSize;
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
        public override void CopyTo(ISyncable dest)
        {
            System.Diagnostics.Debug.Assert(dest is WindowsFolder, "dest is not a WindowsFolder");
            
            WindowsFolder destFolder = (WindowsFolder) dest;

            string srcPath, destPath;
            Queue<string> workingList = new Queue<string>(128);

            srcPath = this.rootPath + this.relativePath;
            destPath = destFolder.rootPath + destFolder.RelativePath;

            if (destFolder.nativeDirObj.Exists)
                destFolder.nativeDirObj.Delete(true);

            workingList.Enqueue(srcPath);

            string curDir;
            while (workingList.Count > 0)
            {
                curDir = workingList.Dequeue();

                foreach (string subFolder in Directory.GetDirectories(curDir))
                    workingList.Enqueue(subFolder);

                Directory.CreateDirectory(destPath + curDir.Substring(srcPath.Length));

                foreach (string file in Directory.GetFiles(curDir))
                {
                    WindowsFile srcObj = new WindowsFile(this.rootPath, file, this, this.parentPartnership);
                    WindowsFile destObj = new WindowsFile(destFolder.rootPath, destPath + file.Substring(srcPath.Length), destFolder, this.parentPartnership);

                    srcObj.statusMonitor = statusMonitor;
                    srcObj.errorHandler = errorHandler;
                    destObj.statusMonitor = statusMonitor;
                    destObj.errorHandler = errorHandler;

                    srcObj.CopyTo(destObj);
                    //File.Copy(file, destPath + file.Substring(srcPath.Length));
                }
            }

            this.UpdateStoredChecksum();
        }

        /// <summary>
        /// Deletes this folder
        /// </summary>
        /// <returns></returns>
        public override void Delete(bool recoverable)
        {
            if (recoverable)
            {
                FileSystem.DeleteDirectory(nativeDirObj.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            else
            {
                nativeDirObj.Delete(true);
            }

            this.RemoveStoredChecksum();
        }

        /// <summary>
        /// Attempts to merge the folder. Not Implemented.
        /// </summary>
        /// <param name="item"></param>
        public override void Merge(ISyncable item)
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

        /// <summary>
        /// Returns a string which represents the folder in the context of the partnership
        /// </summary>
        /// <returns></returns>
        public override string EntityPath()
        {
            return PREF_FOLDER + this.relativePath; 
        }

        /// <summary>
        /// Returns a string that represents this file in the context of the partnership
        /// </summary>
        /// <param name="extraAttributes">Additional attributes to tag on</param>
        /// <returns></returns>
        public override string EntityPath(string extraAttributes)
        {
            return extraAttributes + "," + PREF_FOLDER + this.relativePath;
        }

        /// <summary>
        /// Returns the full path to this folder.
        /// </summary>
        /// <returns></returns>
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
            Exception exp;

            try
            {
                // Update the drive letters if there is a need to.
                // Note: We do not store the driveLetter during serialisation because it is not useful to do so.
                if (driveLetter == null)
                    this.UpdateDriveLetter();

                if (partner.driveLetter == null)
                    partner.UpdateDriveLetter();
            }
            catch (DriveNotFoundException)
            {
                exp = new Exception("A storage device was not found. SyncButler cannot sync " + parentPartnership.Name);

                if (errorHandler == null) throw exp;
                else
                {
                    errorHandler(exp);
                    return conflictList;
                }
            }

            leftPath = this.rootPath + this.relativePath;
            rightPath = partner.rootPath + partner.relativePath;
            this.checksumCacheFresh = false; // Invalidate the checksum cache

            workList.Enqueue("");

            string curLeftDir, curRightDir;
            // Work while the work queue has stuff.
            while (workList.Count > 0)
            {
                exp = null;
                try
                {
                    currDir = workList.Dequeue();

                    if (this.statusMonitor != null)
                    {
                        if (!this.statusMonitor(new SyncableStatus(PREF_FOLDER + currDir, 0, 0, SyncableStatus.ActionType.Sync)))
                            throw new UserCancelledException();
                    }

                    curLeftDir = leftPath + currDir;
                    curRightDir = rightPath + currDir;

                    // Protect our home base!
                    if (curLeftDir.StartsWith(SyncEnvironment.AppPath) ||
                        curRightDir.StartsWith(SyncEnvironment.AppPath))
                        continue;

                    // Get sub-directories from the left folder
                    List<string> leftFolders = new List<string>();
                    leftFolders.AddRange(Directory.GetDirectories(curLeftDir));

                    // Get sub-directories from the right folder
                    List<string> rightFolders = new List<string>();
                    rightFolders.AddRange(Directory.GetDirectories(curRightDir));

                    // Get files from left folder
                    List<string> leftFiles = new List<string>();
                    leftFiles.AddRange(Directory.GetFiles(curLeftDir));

                    // Get files from right folder
                    List<string> rightFiles = new List<string>();
                    rightFiles.AddRange(Directory.GetFiles(curRightDir));

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
                            
                            if (!parentPartnership.ChecksumExists(PREF_FOLDER + leftFolderName))
                            {
                                WindowsFolder leftObj = new WindowsFolder(leftPath, leftFolder, this, this.parentPartnership);
                                parentPartnership.UpdateLastChecksum(leftObj);
                            }
                        }
                        // The folder exists on the left but not on the right
                        else
                        {
                            // If the checksum existed, then we infer that the folder was deleted from the right.
                            if (parentPartnership.ChecksumExists(PREF_FOLDER + leftFolderName))
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

                        // The folder exists on the right but not on the left
                        if (!Directory.Exists(leftPath + rightFolderName))
                        {
                            // If the checksum existed, then we infer that the folder was deleted from the left.
                            if (this.parentPartnership.ChecksumExists(PREF_FOLDER + rightFolderName))
                                autoResolveAction = Conflict.Action.DeleteRight;
                            // Otherwise, we infer that the folder is newly created.
                            else
                                autoResolveAction = Conflict.Action.CopyToLeft;

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
                        leftFileObj.SetErrorHandler(errorHandler);
                        rightFileObj.SetErrorHandler(errorHandler);

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
                catch (UserCancelledException)
                {
                    throw new UserCancelledException();
                }
                catch (IOException e)
                {
                    exp = new Exception("I am having a problem accessing a folder while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
                }
                catch (UnauthorizedAccessException e)
                {
                    exp = new Exception("I was denied permission to access a folder while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
                }
                catch (System.Security.SecurityException e)
                {
                    exp = new Exception("I was denied permission to access a folder while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
                }
                catch (InvalidActionException e)
                {
                    exp = new Exception("I might have done something I was not supposed to while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
                }
                catch (Exception e)
                {
                    exp = new Exception("There seems to be a problem syncing " + parentPartnership.Name + ":\n\n" + e.Message);
                }

                if (errorHandler == null && exp != null) throw exp;
                else if (exp != null)
                {
                    if (errorHandler(exp)) continue;
                    else throw new UserCancelledException();
                }
            }

            return conflictList;
        }

        /// <summary>
        /// Creates a WindowsFile/Folder object given the entity path, assuming the entity path refers
        /// to a filesystem object under this folder
        /// </summary>
        /// <param name="entityPath"></param>
        /// <returns></returns>
        public override ISyncable CreateChild(string entityPath)
        {
            if (entityPath.StartsWith(@"file:\\"))
                return new WindowsFile(this.rootPath, this.rootPath + entityPath.Substring(7), this, this.parentPartnership);
            else if (entityPath.StartsWith(@"folder:\\"))
                return new WindowsFolder(this.rootPath, this.rootPath + entityPath.Substring(9), this, this.parentPartnership);
            else
                throw new ArgumentException();
        }

        /// <summary>
        /// Gets the directory containing this file system object.
        /// </summary>
        /// <returns>The string of the directory.</returns>
        public override string GetContainingFolder()
        {
            return this.nativeDirObj.FullName;
        }

        /// <summary>
        /// Serializes this object into XML
        /// </summary>
        /// <param name="xmlData"></param>
        public override void SerializeXML(XmlWriter xmlData)
        {
            string cleanRootPath = rootPath;
            if (cleanRootPath.StartsWith("MISSING:"))
                cleanRootPath = "Z" + cleanRootPath.Substring(7);

            xmlData.WriteStartElement("WindowsFolder");
            xmlData.WriteAttributeString("RelativePath", relativePath);
            xmlData.WriteAttributeString("RootPath", cleanRootPath);
            xmlData.WriteAttributeString("IsPortableStorage", isPortableStorage.ToString());
            xmlData.WriteAttributeString("DriveID", driveId);
            xmlData.WriteAttributeString("PartitionIndex", partitionIndex.ToString());
            xmlData.WriteEndElement();
        }

        /// <summary>
        /// Check if the specific user has the right to create files in the directory
        /// </summary>
        /// <param name="DirectoryPath">The directory to check</param>
        /// <param name="User">The user</param>
        /// <returns>True if the user has the rights, false otherwise</returns>
        public static bool CheckIfUserHasRightsTo(String DirectoryPath, string User)
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return CheckIfUserHasRightsTo(Path.GetDirectoryName(DirectoryPath), User);
            }
            try
            {
                DirectoryInfo di = new DirectoryInfo(DirectoryPath);
                AuthorizationRuleCollection arc = di.GetAccessControl().GetAccessRules(true, false, typeof(System.Security.Principal.NTAccount));
                foreach (FileSystemAccessRule rule in arc)
                {
                    if ((rule.IdentityReference.ToString().ToLower().Trim().Contains("everyone") ||
                        rule.IdentityReference.ToString().ToLower().Trim().Contains(User)) &&
                        rule.AccessControlType == AccessControlType.Deny &&
                        (rule.FileSystemRights.ToString().ToLower().Trim().Contains("write")||
                        rule.FileSystemRights.ToString().ToLower().Trim().Contains("read") ||
                        rule.FileSystemRights.ToString().ToLower().Trim().Contains("fullcontrol")))
                    {
                        return false;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return true;

        }
    }
}
