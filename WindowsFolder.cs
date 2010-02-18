using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a folder on the Windows file system.
    /// </summary>
    public class WindowsFolder : WindowsFileSystem, ISyncable
    {
        protected DirectoryInfo nativeDirObj;
        protected SortedList<String, WindowsFolder> subFolders;
        protected SortedList<String, WindowsFile> files;

        /// <summary>
        /// Constructor that takes in two parameters, a root path and the full path.
        /// </summary>
        /// <param name="rootPath">Path of the root directory</param>
        /// <param name="fullPath">Full path to this file</param>
        public WindowsFolder(String rootPath, String fullPath)
        {
            this.nativeDirObj = new DirectoryInfo(fullPath);
            this.relativePath = StripPrefix(rootPath, fullPath);
            this.nativeFileSystemObj = this.nativeDirObj;
            this.subFolders = new SortedList<String, WindowsFolder>();
            this.files = new SortedList<String, WindowsFile>();
            this.rootPath = rootPath;
        }

        /// <summary>
        /// Gets a sorted list of sub folders in this directory. Sorted by the name of the folder.
        /// </summary>
        public SortedList<String, WindowsFolder> SubFolders
        {
            get { return this.subFolders; }
        }

        /// <summary>
        /// Gets a sorted list of files in this directory. Sorted by the name of the file.
        /// </summary>
        public SortedList<String, WindowsFile> Files
        {
            get { return this.files; }
        }

        /// <summary>
        /// Gets the number of sub folders in this directory.
        /// </summary>
        public long SubFolderCount
        {
            get { return this.subFolders.Count; }
        }

        /// <summary>
        /// Gets the number of files in this directory.
        /// </summary>
        public long FileCount
        {
            get { return this.files.Count; }
        }

        /// <summary>
        /// Add a file to this directory.
        /// </summary>
        /// <remarks>
        /// If the file is already contained in this directory, nothing is done.
        /// </remarks>
        /// <param name="file">The file to add.</param>
        public void AddFile(WindowsFile file)
        {
            if (!this.files.ContainsKey(file.Name))
                this.files.Add(file.Name, file);
        }

        /// <summary>
        /// Add a sub folder to this directory.
        /// </summary>
        /// <remarks>
        /// If the folder is already contained in this directory, nothing is done.
        /// </remarks>
        /// <param name="folder">The folder to add.</param>
        public void AddSubFolder(WindowsFolder folder)
        {
            if (!this.subFolders.ContainsKey(folder.Name))
                this.subFolders.Add(folder.Name, folder);
        }

        public bool Copy(ISyncable item)
        {
            throw new NotImplementedException();
        }

        public bool Delete()
        {
            throw new NotImplementedException();
        }

        public bool Merge(ISyncable item)
        {
            throw new NotImplementedException();
        }

        public override long Checksum()
        {
            throw new NotImplementedException();
        }
    }
}
