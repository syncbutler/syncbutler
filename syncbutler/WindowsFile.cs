using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SyncButler.Checksums;
using SyncButler.Exceptions;

namespace SyncButler
{
    public enum Error { NoError, NoPermission, PathTooLong, DirectoryDoesNotExist, InvalidPath, NotImplemented };
    /// <summary>
    /// Represents a file on the Windows file system.
    /// </summary>
    public class WindowsFile : WindowsFileSystem, ISyncable
    {
        protected FileInfo nativeFileObj;
        protected SyncableStatusMonitor statusMonitor = null;

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

        public void SetStatusMonitor(SyncableStatusMonitor statusMonitor)
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
        public object CopyTo(ISyncable item)
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
        public object Delete()
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

        public object Merge(ISyncable item)
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
            IRollingHash hashAlgorithm = new Adler32();
            long start = 0;

            while (start < this.Length)
            {
                hashAlgorithm.Update(this.GetBytes(start, 2048000));
                start += 2048000;
            }

            return hashAlgorithm.Value;
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
        public bool HasChanged()
        {
            Debug.Assert(parentPartnership != null, "parentPartnership not set! Cannot determine if this File has changed");
            
            if (!parentPartnership.hashDictionary.ContainsKey(this.EntityPath())) return true;

            return (parentPartnership.hashDictionary[this.EntityPath()] != this.Checksum());
        }

        /// <summary>
        /// Determine if the two file is the same in content.
        /// </summary>
        /// <param name="item">The file to compare with</param>
        /// <returns>true if the file content is the same, false otherwise</returns>
        public bool Equals(ISyncable item)
        {
            if (!item.GetType().Name.Equals("WindowsFile"))
            {
                return false;
            }

            WindowsFile subject = (WindowsFile)item;
            
            return (subject.Checksum().Equals(Checksum()));
        }

        public string EntityPath()
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
        public List<Conflict> Sync(ISyncable otherPair)
        {
            WindowsFile partner;

            Debug.Assert(parentPartnership != null, "The parent partnership has not been set; cannot sync");

            // Temporary -- give basic functionality first.
            statusMonitor(new SyncableStatus(this.EntityPath(), 0));

            if (otherPair is WindowsFile)// && this.EntityPath().Equals(otherPair.EntityPath()))
            {
                partner = (WindowsFile)otherPair;
            }
            else
            {
                throw new InvalidPartnershipException();
            }
            
            // Check if the files are in sync
            List<Conflict> returnValue = new List<Conflict>();
            Conflict.Action recommendedAction = Conflict.Action.Unknown;

            if (!(this.nativeFileObj.Exists || partner.nativeFileObj.Exists))
            {
                return returnValue;
            }
            else if (!this.nativeFileObj.Exists) 
            {
                recommendedAction = Conflict.Action.CopyToLeft;
            }
            else if (!partner.nativeFileObj.Exists)
            {
                recommendedAction = Conflict.Action.CopyToRight;
            }
            else
            {
                if (this.Checksum().Equals(partner.Checksum())) return returnValue;
                
                Boolean leftChanged, rightChanged;
                leftChanged = this.HasChanged();
                rightChanged = partner.HasChanged();

                if (rightChanged ^ leftChanged)
                {
                    if (rightChanged) recommendedAction = Conflict.Action.CopyToLeft;
                    else if (leftChanged) recommendedAction = Conflict.Action.CopyToRight;
                }
            }

            returnValue.Add(new Conflict(this, partner, recommendedAction));
            return returnValue;
        }
        
    }
}
