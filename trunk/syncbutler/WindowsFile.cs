using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using SyncButler.Checksums;

namespace SyncButler
{

    public enum Error { NoError, NoPermission, PathTooLong, DirectoryDoesNotExist, InvalidPath, NotImplemented };
    /// <summary>
    /// Represents a file on the Windows file system.
    /// </summary>
    public class WindowsFile : WindowsFileSystem, ISyncable
    {
        protected FileInfo nativeFileObj;

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

            return buffer;
        }

        /// <summary>
        /// Attempt to overwrite its content to another file
        /// </summary>
        /// <param name="item">The target file to be overwrite</param>
        /// <returns>Error.NoError if there is no error. Error.InvalidPath if the path is not valid. Error.NoPermission if the user has no permission to overwrite this file. Error.PathTooLong if the path given is too long for this system to handle</returns>
        public object CopyTo(ISyncable item)
        {
            Debug.Assert(item.GetType().Name.Equals("WindowsFiles"), "Different type, the given type is " + item.GetType().Name);

            WindowsFile windowFiles = (WindowsFile)item;
            try
            {
                nativeFileObj.CopyTo(windowFiles.relativePath + windowFiles.Name);
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
        /// </summary>
        /// <returns>true if the file has been changed, false otherwise</returns>
        public bool HasChanged()
        {
            throw new NotImplementedException();
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

        public List<Conflict> Sync(ISyncable otherPair)
        {
            // TODO: Implement
            return null;
        }
        
    }
}
