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
        public enum Error { IsWorkingFolder, NoPermission, NoError };
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

        /// <summary>
        /// attempt to copy the content of the file over
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public object CopyTo(ISyncable item)
        {
            WindowsFolder subject = (WindowsFolder)item;
            
            Directory.SetCurrentDirectory(subject.rootPath);
            List<string> fileList = new List<string>();
            List<String> totalDirectoryListing = new List<string>();
            List<String> workingList = new List<string>();
            List<String> subFolders = new List<string>();

            
            foreach (DirectoryInfo dir in nativeDirObj.GetDirectories())
            {
                String dirName = dir.FullName;
                workingList.Add(StripPrefix(rootPath, dirName));
                
            }
            foreach (FileInfo fi in nativeDirObj.GetFiles())
            {
                string filename = fi.FullName;
                fileList.Add(filename);
            }
            
            do
            {
                totalDirectoryListing.AddRange(workingList);
                // for each level
                foreach (string dir in workingList)
                {
                    DirectoryInfo workingdir = new DirectoryInfo(Path.Combine(rootPath,dir));
                    // add each subfolders into list
                    foreach (DirectoryInfo subfolders in workingdir.GetDirectories())
                    {
                        subFolders.Add(StripPrefix(rootPath, subfolders.FullName));
                        foreach (FileInfo fi in subfolders.GetFiles())
                        {
                            string filename = fi.FullName;
                            fileList.Add(filename);
                        }
                        
                    }
                }
                workingList.AddRange(subFolders);
                
            } while (subFolders.Count != 0);

            foreach (string dir in totalDirectoryListing)
            {
                Directory.CreateDirectory(Path.Combine(subject.rootPath, dir));
            }

            foreach (string file in fileList)
            {
                File.Copy(file, Path.Combine(subject.rootPath, StripPrefix(rootPath, file)));
            }
            return Error.NoError;
        }

        public object Delete()
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

        public object Merge(ISyncable item)
        {
            
            throw new NotImplementedException();
        }

        public override long Checksum()
        {
            throw new NotImplementedException();
        }

        public bool  HasChanged()
        {
 	        throw new NotImplementedException();
        }

        public bool  Equals(ISyncable item)
        {
 	        throw new NotImplementedException();
        }

        public List<ISyncable> GetChildren()
        {
            throw new NotImplementedException();
        }

        
    }
}
