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
using System.Diagnostics;
using System.IO;
using System.Text;
using SyncButler.Checksums;
using SyncButler.Exceptions;
using System.Xml;
using Microsoft.VisualBasic.FileIO;

namespace SyncButler
{
    /// <summary>
    /// Represents a file on the Windows file system.
    /// </summary>
    public class WindowsFile : WindowsFileSystem
    {
        private FileInfo nativeFileObj;
        private FileStream fileStream = null;
        private long checksumCache;
        private bool checksumCacheFresh = false;

        /// <summary>
        /// Destructor. Makes sure fileStream is closed
        /// </summary>
        ~WindowsFile()
        {
            CloseFile();
        }

        /// <summary>
        /// Constructor to unserialise XML string
        /// </summary>
        /// <param name="xmlData">The XMLReader object to read the XML from.</param>
        /// <exception cref="ArgumentNullException">If, when parsing the boolean, the argument is null.</exception>
        /// <exception cref="FormatException">If, when parsing the boolean, the argument is in a format that is not recognised.</exception>
        public WindowsFile(XmlReader xmlData)
        {
            driveId = relativePath = rootPath = null;
            isPortableStorage = false;

            while ((xmlData.NodeType != XmlNodeType.Element) && (xmlData.Name != "WindowsFile"))
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
            if ((relativePath.Length > 0) && !rootPath.EndsWith("\\")) rootPath += "\\";

            if (this.driveLetter == "MISSING:")
            {
                nativeFileObj = new FileInfo("Z" + rootPath.Substring(7) + relativePath);
                nativeFileSystemObj = nativeFileObj;
            }
            else
            {
                nativeFileObj = new FileInfo(rootPath + relativePath);
                nativeFileSystemObj = nativeFileObj;
            }
        }

        /// <summary>
        /// Constructor that takes in two parameters, a root path and the full path.
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(string rootPath, string fullPath)
        {
            this.nativeFileObj = new FileInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeFileObj;
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
        /// Constructor that takes in three parameters, a root path, the full path
        /// and the parent folder
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(string rootPath, string fullPath, WindowsFolder parent)
        {
            this.nativeFileObj = new FileInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeFileObj;
            this.rootPath = rootPath;
            this.IsPortableStorage = parent.IsPortableStorage;
            this.DriveID = parent.DriveID;
            this.DriveLetter = parent.DriveLetter;
            this.PartitionIndex = parent.PartitionIndex;
        }

        /// <summary>
        /// Constructor that takes in one parameter, only the full path to the file.
        /// Calls the constructor that takes in two parameters and passes the full path.
        /// Useful when creating a file partnership.
        /// </summary>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(string fullPath) : this(fullPath, fullPath) { }

        /// <summary>
        /// Constructor that takes in three parameters, a root path, the full path,
        /// and the parent partnership
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(string rootPath, string fullPath, Partnership parentPartnership) : this(rootPath, fullPath)
        {
            this.parentPartnership = parentPartnership;
        }

        /// <summary>
        /// Gets the directory containing this file system object.
        /// </summary>
        /// <returns>The string of the directory.</returns>
        public override string GetContainingFolder()
        {
            return this.nativeFileObj.Directory.FullName;
        }

        /// <summary>
        /// Constructor that takes in four parameters, a root path, the full path,
        /// the parent partnership, and the parent folder
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(string rootPath, string fullPath, WindowsFolder parent, Partnership parentPartnership) : this(rootPath, fullPath, parent)
        {
            this.parentPartnership = parentPartnership;
        }

        /// <summary>
        /// Constructor that takes in two parameters, the full path to the file, and the containing partnerhsip.
        /// Useful when creating a file partnership.
        /// </summary>
        /// <param name="fullPath">Full path to this file</param>
        /// <param name="parentPartnership">The containing partnership</param>
        public WindowsFile(string fullPath, Partnership parentPartnership) : this(fullPath, fullPath, parentPartnership) { }

        /// <summary>
        /// Get the length of the file, in bytes.
        /// </summary>
        /// <remarks>
        /// Calls Refresh() first.
        /// </remarks>
        /// <exception cref="IOException">Refresh cannot update the state of the file or directory.</exception>
        /// <exception cref="FileNotFoundException">The file does not exist, or the Length property is called for a directory.</exception>
        public long Length
        {
            get
            {
                this.nativeFileObj.Refresh();
                return this.nativeFileObj.Length;
            }
        }

        /// <summary>
        /// Indicates whether the file (read through GetBytes) has reached the end
        /// </summary>
        public bool ReadEOF
        {
            get
            {
                if (fileStream == null) OpenFile();
                return fileStream.Position >= nativeFileObj.Length;
            }
        }

        /// <summary>
        /// Opens the file for reading
        /// </summary>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        public void OpenFile()
        {
            string fullPath = this.rootPath + this.relativePath;

            if (!File.Exists(fullPath)) throw new FileNotFoundException("Could not find file.", fullPath);

            fileStream = this.nativeFileObj.OpenRead();
        }

        /// <summary>
        /// Closes the file opened by OpenFile
        /// </summary>
        public void CloseFile()
        {
            if (fileStream != null) fileStream.Close();
            else fileStream = null;
        }

        /// <summary>
        /// Reads the file and returns a byte array of data.
        /// WARNING: The array returned may not be of the length wantedBufferSize if the end of file was reached!
        /// </summary>
        /// <param name="start">The offset to start reading the bytes from.</param>
        /// <param name="wantedBufferSize">The desired size of the buffer. This is subject to the file's actual size.</param>
        /// <returns>A byte array of the file's contents or null on EOF</returns>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed.</exception>
        public byte[] GetBytes(long start, long wantedBufferSize)
        {
            if (fileStream == null) OpenFile();

            fileStream.Seek(start, 0);

            return GetBytes(wantedBufferSize);
        }

        /// <summary>
        /// Reads the next wantedBufferSize bytes from the file
        /// WARNING: The array returned may not be of the length wantedBufferSize if the end of file was reached!
        /// </summary>
        /// <param name="wantedBufferSize"></param>
        /// <returns>The bytes read in or null on EOF</returns>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed.</exception>
        public byte[] GetBytes(long wantedBufferSize)
        {
            if (fileStream == null) OpenFile();

            long actualBufferSize;
            long start = fileStream.Position;

            if ((this.Length - start) >= wantedBufferSize) actualBufferSize = wantedBufferSize;
            else actualBufferSize = this.Length - start;

            if (actualBufferSize < 1) return null;

            byte[] buffer = new byte[actualBufferSize];

            for (long i = 0; i < actualBufferSize; i++)
                buffer[i] = (byte)fileStream.ReadByte();

            return buffer;
        }

        /// <summary>
        /// Attempt to overwrite its content to another file. It will not immedietly overwrite the destination
        /// file, but will copy the data to a temporary file first. Once that is completed, it will delete the
        /// old file and rename the temporary file to take its place.
        /// </summary>
        /// <param name="item">The target file to be overwrite</param>
        public override void CopyTo(ISyncable item)
        {
            Debug.Assert(!item.GetType().Name.Equals("WindowsFiles"), "Different type, the given type is " + item.GetType().Name);

            IRollingHash hashAlgorithm = new Adler32();
            WindowsFile destFile = (WindowsFile)item;

            // Make sure there's enough free space.
            if ((nativeFileObj.Length + 4096) > SystemEnvironment.StorageDevices.GetAvailableSpace(destFile.driveLetter))
                throw new IOException("There is insufficient space to copy the file to " + destFile.nativeFileObj.FullName);
            // If this is NTFS and the destination is FAT32 or FAT16. Then we need to check the file size.
            else if (((SystemEnvironment.StorageDevices.GetDriveFormat(GetDriveLetter(destFile.nativeFileObj.FullName)) == SyncButler.SystemEnvironment.StorageDevices.Format.FAT32)
                || (SystemEnvironment.StorageDevices.GetDriveFormat(GetDriveLetter(destFile.nativeFileObj.FullName)) == SyncButler.SystemEnvironment.StorageDevices.Format.FAT16))
                && (SystemEnvironment.StorageDevices.GetDriveFormat(GetDriveLetter(this.nativeFileObj.FullName)) == SyncButler.SystemEnvironment.StorageDevices.Format.NTFS))
            {
                if ((nativeFileObj.Length + 4096) > SystemEnvironment.StorageDevices.MAX_SIZE_FAT32)
                    throw new IOException("The file cannot be copied to " + destFile.nativeFileObj.FullName + " because its length is larger than what the destination drive could handle.");
            }

            int bufferSize = (int) SyncEnvironment.FileReadBufferSize;

            FileStream inputStream = nativeFileObj.OpenRead();
            FileStream outputStream = null;

            string tempName = null;
            for (int i = 0; i < 10000; i++)
            {
                //tempName = destFile.nativeFileObj.FullName + "." + i + ".syncbutler_safecopy";

                tempName = GetDriveLetter(destFile.nativeFileObj.FullName) + destFile.nativeFileObj.Name + "." + i + ".syncbutler_safecopy";

                if (File.Exists(tempName)) continue;
                outputStream = new FileStream(tempName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                break;
            }

            if (outputStream == null) throw new IOException("Could not create a temporary file to be used for safe copying");

            byte [] buf = new byte[bufferSize];
            long totalCopied = 0;
            int amountRead;
            float toPercent = 100f / nativeFileObj.Length;

            do
            {
                amountRead = inputStream.Read(buf, 0, bufferSize);
                if (amountRead > 0)
                {
                    outputStream.Write(buf, 0, amountRead);
                    if (!checksumCacheFresh) hashAlgorithm.Update(buf, 0, amountRead);
                }

                totalCopied += amountRead;

                if (statusMonitor != null)
                {
                    if (!statusMonitor(new SyncableStatus(EntityPath(), 0, (int)(totalCopied * toPercent), SyncableStatus.ActionType.Copy)))
                    {
                        inputStream.Close();
                        outputStream.Close();
                        File.Delete(tempName);
                        throw new UserCancelledException();
                    }
                }

            } while (amountRead > 0);

            inputStream.Close();
            outputStream.Close();

            if (destFile.nativeFileObj.Exists) destFile.nativeFileObj.Delete();
            File.Move(tempName, destFile.nativeFileObj.FullName);

            destFile.nativeFileSystemObj.LastWriteTime = nativeFileSystemObj.LastWriteTime;
            destFile.nativeFileSystemObj.CreationTime = nativeFileSystemObj.CreationTime;

            if (!checksumCacheFresh)
            {
                checksumCacheFresh = true;
                checksumCache = hashAlgorithm.Value;
            }

            destFile.checksumCacheFresh = checksumCacheFresh;
            destFile.checksumCache = checksumCache;
            this.UpdateStoredChecksum();
        }

        /// <summary>
        /// Attempts to delete this file.
        /// </summary>
        /// <exception cref="SecurityException">There was a permission error or the operation encountered an error and was cancelled by the user.</exception>
        /// <exception cref="IOException">The target file is open or memory-mapped on a computer running Microsoft Windows NT.</exception>
        public override void Delete(bool recoverable)
        {
            try
            {
                if (recoverable)
                    FileSystem.DeleteFile(nativeFileObj.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                else
                    nativeFileObj.Delete();

                this.RemoveStoredChecksum();
            }
            catch (OperationCanceledException)
            {
                throw new System.Security.SecurityException("There was no permission to delete this file");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Not Implemented: attempts to merge this file with another
        /// </summary>
        /// <param name="item"></param>
        public override void Merge(ISyncable item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates and returns the checksum of this file, based on a comparison of its contents.
        /// </summary>
        /// <remarks>
        /// The block size used is determined by the program settings
        /// Adler32 is the preferred algorithm for calculating the checksum, as it has been proven to be fast, with an acceptable level of collision.
        /// </remarks>
        /// <returns>A long of the checksum.</returns>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed.</exception>
        public override long Checksum()
        {
            if (checksumCacheFresh) return checksumCache;

            IRollingHash hashAlgorithm = new Adler32();
            long bufferSize = SyncEnvironment.FileReadBufferSize;
            double toPercent = 100.0 / nativeFileObj.Length;
            int percentComplete;
            long processed = 0;
            string curObj = EntityPath();

            this.OpenFile();

            while (!this.ReadEOF)
            {
                if (statusMonitor != null)
                {
                    percentComplete = (int)(processed * toPercent);

                    if (percentComplete > 100) percentComplete = 100;

                    if (!statusMonitor(new SyncableStatus(curObj, 0, percentComplete, SyncableStatus.ActionType.Checksum)))
                        throw new UserCancelledException();
                }

                hashAlgorithm.Update(this.GetBytes(bufferSize));
                processed += bufferSize;
            }

            this.CloseFile();

            checksumCacheFresh = true;
            checksumCache = hashAlgorithm.Value;

            return checksumCache;
        }

        /// <summary>
        /// Determine if the file has been changed since it was last synced.
        /// If no metadata is available, it assumes the file has been changed.
        /// </summary>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed.</exception>
        /// <returns>true if the file has been changed, false otherwise</returns>
        public override bool HasChanged()
        {
            Debug.Assert(parentPartnership != null, "parentPartnership not set! Cannot determine if this File has changed");

            try
            {
                return (parentPartnership.GetLastChecksum(this) != this.Checksum());
            }
            catch (SyncableNotExistsException)
            {
                return true; // assume the file has change if we know nothing about it.
            }
        }

        /// <summary>
        /// Determine if the two file is the same in content.
        /// </summary>
        /// <param name="item">The file to compare with</param>
        /// <returns>true if the file content is the same, false otherwise</returns>
        public override bool Equals(ISyncable item)
        {
            if (!item.GetType().Name.Equals("WindowsFile")) return false;

            WindowsFile subject = (WindowsFile)item;
            
            return (subject.Checksum().Equals(Checksum()));
        }

        /// <summary>
        /// Returns a string that represents this file in the context of the partnership
        /// </summary>
        /// <returns></returns>
        public override string EntityPath()
        {
            return PREF_FILE + this.relativePath;
        }

        /// <summary>
        /// Returns a string that represents this file in the context of the partnership
        /// </summary>
        /// <param name="extraAttributes">Additional attributes to tag on</param>
        /// <returns></returns>
        public override string EntityPath(string extraAttributes)
        {
            return extraAttributes + "," + PREF_FILE + this.relativePath;
        }

        /// <summary>
        /// Returns the full path to this file.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.rootPath + this.relativePath;
        }

        /// <summary>
        /// Synchronize this file with another. 
        /// IMPORTANT: This function should be called from left to right, ie. left.Sync(right);
        /// </summary>
        /// <param name="otherPair">The other ISyncable to sync with.</param>
        /// <exception cref="InvalidPartnershipException">Tried to sync a windows folder with something else</exception>
        /// <exception cref="FileNotFoundException">If the file is not found. (Probably while generating the checksum)</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory. (Probably while generating the checksum)</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive. (Probably while generating the checksum)</exception>
        /// <exception cref="IOException">The file is already open. (Probably while generating the checksum)</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading. (Probably while generating the checksum)</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed. (Probably while generating the checksum)</exception>
        /// <returns>A list of conflicts (may be empty)</returns>
        public override List<Conflict> Sync(ISyncable otherObj)
        {
            System.Diagnostics.Debug.Assert(otherObj is WindowsFile, "Other ISyncable passed is not a WindowsFile object.");

            WindowsFile partner = (WindowsFile)otherObj;

            if (this.statusMonitor != null)
            {
                if (!statusMonitor(new SyncableStatus(this.EntityPath(), 0, 0, SyncableStatus.ActionType.Sync)))
                    throw new UserCancelledException();
            }

            Exception exp = null;
            List<Conflict> conflictList = new List<Conflict>();
            Conflict.Action autoResolveAction = Conflict.Action.Unknown;
            Conflict.Action suggestedAction = Conflict.Action.Unknown;

            try
            {
                // Update the drive letters if needed.
                if (this.driveLetter == null) this.UpdateDriveLetter();
                if (partner.driveLetter == null) partner.UpdateDriveLetter();
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

            if (this.DoNotSync() || partner.DoNotSync())
            {
                conflictList.Add(new Conflict(this, partner, Conflict.Action.Ignore, Conflict.Action.Ignore));
                return conflictList;
            }

            try
            {
                checksumCacheFresh = false; // Invalidate checksum cache

                // Are we supposed to ignore this?
                if (Ignored())
                {
                    conflictList.Add(new Conflict(this, partner, Conflict.Action.Unknown, Conflict.Action.Ignore));
                    return conflictList;
                }

                // Left and right don't exist. Nothing to do.
                if (!(this.nativeFileObj.Exists || partner.nativeFileObj.Exists)) return conflictList;

                // Left or right exists, or both.
                else
                {
                    // Both files exist and checksums are the same
                    if ((this.Exists() && partner.Exists()) && (this.Checksum() == partner.Checksum()))
                    {
                        if (!parentPartnership.ChecksumExists(this)) this.UpdateStoredChecksum();
                        return conflictList;
                    }
                    else
                    {
                        // Both files exist
                        if (this.Exists() && partner.Exists())
                        {
                            bool leftChanged = this.HasChanged();
                            bool rightChanged = partner.HasChanged();

                            // Right OR Left changed (but not both)
                            if (rightChanged ^ leftChanged)
                            {
                                if (rightChanged) autoResolveAction = Conflict.Action.CopyToLeft;
                                else if (leftChanged) autoResolveAction = Conflict.Action.CopyToRight;
                            }
                            // Left and Right are both different
                            else
                            {
                                int timeDifference = this.LastWriteTime.CompareTo(partner.LastWriteTime);

                                // Left is earlier than right
                                if (timeDifference < 0) suggestedAction = Conflict.Action.CopyToLeft;
                                // Left is later than right
                                else if (timeDifference > 0) suggestedAction = Conflict.Action.CopyToRight;
                                // Left and right are the same
                                else suggestedAction = Conflict.Action.Unknown;
                            }
                        }
                        // One file exists only
                        else
                        {
                            // Left exists
                            if (this.Exists())
                            {
                                // Checksum does not exist - file is new
                                if (!this.parentPartnership.ChecksumExists(partner))
                                    autoResolveAction = Conflict.Action.CopyToRight;
                                else
                                {
                                    // File did not change - means a deletion was made on one side.
                                    if (this.Checksum() == this.parentPartnership.GetLastChecksum(this))
                                        autoResolveAction = Conflict.Action.DeleteLeft;
                                }
                            }
                            // Right exists
                            else if (partner.Exists())
                            {
                                // Checksum does not exist - file is new
                                if (!this.parentPartnership.ChecksumExists(this))
                                    autoResolveAction = Conflict.Action.CopyToLeft;
                                else
                                {
                                    // File did not change - means a deletion was made on one side.
                                    if (partner.Checksum() == this.parentPartnership.GetLastChecksum(partner))
                                        autoResolveAction = Conflict.Action.DeleteRight;
                                }
                            }
                        }
                    }
                }
            }
            catch (UserCancelledException)
            {
                throw new UserCancelledException();
            }
            catch (IOException e)
            {
                if (e.Message.StartsWith("Could not find a part of the path "))
                    exp = new Exception("I could not be find the folder " + e.Message.Substring("Could not find a part of the path ".Length));
                else
                    exp = new Exception("I am having a problem accessing a folder while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                exp = new Exception("I was denied permission to access a file while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
            }
            catch (System.Security.SecurityException e)
            {
                exp = new Exception("I was denied permission to access a file while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
            }
            catch (InvalidActionException e)
            {
                exp = new Exception("I might have done something I was not supposed to while syncing " + parentPartnership.Name + ":\n\n" + e.Message);
            }
            catch (Exception e)
            {
                exp = new Exception("There seems to be a problem syncing " + parentPartnership.Name + ":\n\n" + e.Message);
            }

            if (exp != null && errorHandler != null)
            {
                if (errorHandler(exp)) return conflictList;
                else throw new UserCancelledException();
            }
            else if (exp != null) throw exp;

            Conflict conflict = new Conflict(this, partner, autoResolveAction, suggestedAction);
            conflictList.Add(conflict);
            return conflictList;
        }

        /// <summary>
        /// Does not makes sense in the context of a file - will always throw Arguement Exception
        /// </summary>
        /// <param name="entityPath"></param>
        /// <returns></returns>
        public override ISyncable CreateChild(string entityPath)
        {
            throw new ArgumentException();
        }

        /// <summary>
        /// Checks whether two WindowsFile objects have equal checksums.
        /// The difference in this method from the object's Checksum() method is that while both updates a rolling hash,
        /// this method will stop the moment a difference is detected - which may save some cost.
        /// 
        /// In short -- a potentially more efficient way to compare two files checksums than left.Checksum() == right.Checksum()
        /// </summary>
        /// <param name="left">First WindowsFile object to compare with second</param>
        /// <param name="right">Second WindowsFile object to compare with first</param>
        /// <returns>True if the checksums are equal (after the whole file has been computed. False otherwise.</returns>
        public static bool HaveEqualChecksums(WindowsFile left, WindowsFile right)
        {
            if (left.Length != right.Length) return false;

            if (left.checksumCacheFresh && right.checksumCacheFresh) return (left.checksumCache == right.checksumCache);

            IRollingHash leftHash = new Adler32();
            IRollingHash rightHash = new Adler32();

            long bufferSize = SyncEnvironment.FileReadBufferSize;

            left.OpenFile();
            right.OpenFile();

            while (!left.ReadEOF)
            {
                leftHash.Update(left.GetBytes(bufferSize));
                rightHash.Update(right.GetBytes(bufferSize));

                if (leftHash.Value != rightHash.Value)
                {
                    left.CloseFile();
                    right.CloseFile();
                    return false;
                }
            }

            left.CloseFile();
            right.CloseFile();

            left.checksumCache = right.checksumCache = leftHash.Value;
            left.checksumCacheFresh = right.checksumCacheFresh = true;

            return true;
        }

        /// <summary>
        /// Serializes this file information
        /// </summary>
        /// <param name="xmlData"></param>
        public override void SerializeXML(XmlWriter xmlData)
        {
            string cleanRootPath = rootPath;
            if (cleanRootPath.StartsWith("MISSING:"))
                cleanRootPath = "Z" + cleanRootPath.Substring(7);

            xmlData.WriteStartElement("WindowsFile");
            xmlData.WriteAttributeString("RelativePath", relativePath);
            xmlData.WriteAttributeString("RootPath", cleanRootPath);
            xmlData.WriteAttributeString("IsPortableStorage", isPortableStorage.ToString());
            xmlData.WriteAttributeString("DriveID", driveId);
            xmlData.WriteAttributeString("PartitionIndex", partitionIndex.ToString());
            xmlData.WriteEndElement();
        }

        /// <summary>
        /// Return total file size of a given list of files
        /// </summary>
        /// <param name="files">the list of files</param>
        /// <returns>total file size</returns>
        public static long SizeOf(IList<String> files)
        {
            long totalSize = 0;
            foreach(String file in files)
            {
                if (File.Exists(file))
                    totalSize += (new FileInfo(file)).Length;
            }
            return totalSize;
        }

        public static long SizeOf(String filename)
        {
            return (new FileInfo(filename)).Length;
        }
    }
}
