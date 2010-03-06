using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SyncButler.Checksums;
using SyncButler.Exceptions;

namespace SyncButler
{
    /// <summary>
    /// Represents a file on the Windows file system.
    /// </summary>
    public class WindowsFile : WindowsFileSystem
    {
        protected FileInfo nativeFileObj;
        protected SyncableStatusMonitor statusMonitor = null;

        protected long checksumCache;
        protected bool checksumCacheFresh = false;

        /// <summary>
        /// Constructor that takes in two parameters, a root path and the full path.
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(String rootPath, String fullPath)
        {
            this.nativeFileObj = new FileInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeFileObj;
            this.rootPath = rootPath;
        }

        /// <summary>
        /// Constructor that takes in three parameters, a root path, the full path,
        /// and the parent partnership
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFile(String rootPath, String fullPath, Partnership parentPartnership)
        {
            this.nativeFileObj = new FileInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeFileObj;
            this.rootPath = rootPath;
            this.parentPartnership = parentPartnership;
        }

        public override void SetStatusMonitor(SyncableStatusMonitor statusMonitor)
        {
            this.statusMonitor = statusMonitor;
        }

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
        /// Reads the file and returns a byte array of data.
        /// </summary>
        /// <param name="start">The offset to start reading the bytes from.</param>
        /// <param name="wantedBufferSize">The desired size of the buffer. This is subject to the file's actual size.</param>
        /// <returns>A byte array of the file's contents.</returns>
        /// <exception cref="FileNotFoundException">If the file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Path is read-only or is a directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">The file is already open.</exception>
        /// <exception cref="NotSupportedException">The current stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The current stream is closed.</exception>
        public byte[] GetBytes(long start, long wantedBufferSize)
        {
            String fullPath = this.rootPath + this.relativePath;
            long actualBufferSize;

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Could not find file.", fullPath);
            }

            FileStream fileStream = this.nativeFileObj.OpenRead();

            if ((this.Length - start) >= wantedBufferSize)
                actualBufferSize = wantedBufferSize;
            else
                actualBufferSize = (this.Length - start);

            byte[] buffer = new byte[actualBufferSize];

            fileStream.Seek(start, 0);

            for (long i = 0; i < (actualBufferSize); i++)
                buffer[i] = (byte)fileStream.ReadByte();

            fileStream.Close();
            return buffer;
        }

        /// <summary>
        /// Attempt to overwrite its content to another file
        /// </summary>
        /// <param name="item">The target file to be overwrite</param>
        /// <returns>Error.NoError if there is no error. Error.InvalidPath if the path is not valid. Error.NoPermission if the user has no permission to overwrite this file. Error.PathTooLong if the path given is too long for this system to handle</returns>
        public override Error CopyTo(ISyncable item)
        {
            Debug.Assert(!item.GetType().Name.Equals("WindowsFiles"), "Different type, the given type is " + item.GetType().Name);

            WindowsFile windowFiles = (WindowsFile)item;

            try
            {
                nativeFileObj.CopyTo(windowFiles.rootPath + windowFiles.relativePath);
                return Error.NoError;
            }
            catch (ArgumentException)
            {
                return Error.InvalidPath;
            }
            catch (NotSupportedException)
            {
                return Error.InvalidPath;
            }
            catch (UnauthorizedAccessException)
            {
                return Error.NoPermission;
            }
            catch (PathTooLongException)
            {
                return Error.PathTooLong;
            }
                  
        }
        /// <summary>
        /// Attempts to delete this file.
        /// </summary>
        /// <returns>Error.NoError on no error. Error.NoPermission if users does not have permission to delete this file. Error.InvalidPath if the path is not valid</returns>
        public override Error Delete()
        {
            try
            {
                nativeFileObj.Delete();
            }
            catch (System.Security.SecurityException)
            {
                return Error.NoPermission;
            }
            catch (System.UnauthorizedAccessException)
            {
                return Error.InvalidPath;
            }
            return Error.NoError;
        }

        public override Error Merge(ISyncable item)
        {
            return Error.NotImplemented;
        }

        /// <summary>
        /// Calculates and returns the checksum of this file, based on a comparison of its contents.
        /// </summary>
        /// <remarks>
        /// A size of 2,048,000 bytes (approx. 2MB) is used.
        /// Adler32 is the preferred algorithm for calculating the checksum, as it has been proven to be fast, with an acceptable level of collision.
        /// 
        /// Future TODO: Cache the result so that checksum calculations do not need to be
        /// repeated.
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
            long start = 0;

            while (start < this.Length)
            {
                hashAlgorithm.Update(this.GetBytes(start, 2048000));
                start += 2048000;
            }

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
            catch (SyncableNotExistsException e)
            {
                return true; // assume the file has change if we know nothgin about it.
            }
        }

        /// <summary>
        /// Determine if the two file is the same in content.
        /// </summary>
        /// <param name="item">The file to compare with</param>
        /// <returns>true if the file content is the same, false otherwise</returns>
        public override bool Equals(ISyncable item)
        {
            if (!item.GetType().Name.Equals("WindowsFile"))
            {
                return false;
            }

            WindowsFile subject = (WindowsFile)item;
            
            return (subject.Checksum().Equals(Checksum()));
        }

        public override string EntityPath()
        {
            return "file:\\\\" + this.relativePath;
        }
        public override string ToString()
        {
            return this.rootPath + this.relativePath;
        }

        /// <summary>
        /// Synchronize this file with another. 
        /// IMPORTANT: This function should be called from lef tto right, ie. left.Sync(right);
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
        public override List<Conflict> Sync(ISyncable otherPair)
        {
            WindowsFile partner;

            // Temporary -- give basic functionality first.
            if (statusMonitor != null) statusMonitor(new SyncableStatus(this.EntityPath(), 0));

            if (otherPair is WindowsFile)// && this.EntityPath().Equals(otherPair.EntityPath()))
                partner = (WindowsFile)otherPair;
            else
                throw new InvalidPartnershipException();
            
            // Check if the files are in sync

            checksumCacheFresh = false; // Make sure we're really comparing with the current file

            List<Conflict> conflictList = new List<Conflict>();
            Conflict.Action recommendedAction = Conflict.Action.Unknown;
            
            if (!(this.nativeFileObj.Exists || partner.nativeFileObj.Exists))
                return conflictList;
            else if (!this.nativeFileObj.Exists)
                recommendedAction = Conflict.Action.CopyToLeft;
            else if (!partner.nativeFileObj.Exists)
                recommendedAction = Conflict.Action.CopyToRight;
            else
            {
                if (this.Length != partner.Length)
                    recommendedAction = Conflict.Action.Unknown;
                else if (this.LastWriteTime.Equals(partner.LastWriteTime))
                    return conflictList;
                //else if (HaveEqualChecksums(this, partner))
                else if (this.Checksum().Equals(partner.Checksum()))
                {
                    if (!parentPartnership.ChecksumExists(this))
                        this.UpdateStoredChecksum();
                    return conflictList;
                }
                
                Boolean leftChanged, rightChanged;
                leftChanged = this.HasChanged();
                rightChanged = partner.HasChanged();

                if (rightChanged ^ leftChanged)
                {
                    if (rightChanged)
                        recommendedAction = Conflict.Action.CopyToLeft;
                    else if (leftChanged)
                        recommendedAction = Conflict.Action.CopyToRight;
                }
            }

            conflictList.Add(new Conflict(this, partner, recommendedAction));
            return conflictList;
        }

        public override ISyncable CreateChild(string entityPath)
        {
            throw new ArgumentException();
        }

        public static bool HaveEqualChecksums(WindowsFile left, WindowsFile right)
        {
            if (left.Length != right.Length)
                return false;

            IRollingHash leftHash = new Adler32();
            IRollingHash rightHash = new Adler32();

            long start = 0;

            while (start < left.Length)
            {
                leftHash.Update(left.GetBytes(start, 2048000));
                rightHash.Update(right.GetBytes(start, 2048000));
                start += 2048000;

                if (leftHash.Value != rightHash.Value)
                    return false;
            }

            return true;
        }
        
    }
}
