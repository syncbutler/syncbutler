using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Reflection;

namespace SyncButler.MRU
{
    /// <summary>
    /// A service class to retrive most recently used file for windows
    /// </summary>
    public class MostRecentlyUsedFile
    {
        [DllImport("shell32.dll")]
        private static extern Int32 SHGetPathFromIDListW(
            UIntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);

        [DllImport("shell32.dll")]
        private static extern IntPtr ILCombine(
            IntPtr pidl1, IntPtr pidl2);

        [DllImport("shell32.dll")]
        private static extern void ILFree(
            IntPtr pidl);

        public static SyncableStatusMonitor statusMonitor = null;

        public static List<string> GetAll()
        {
            int depth = 2;
            int days = 5;

            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 7, SyncableStatus.ActionType.Sync));
            List<string> mergedList = (MostRecentlyUsedFile.Get());

            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 14, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), depth, days));

            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 21, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), depth, days));
            
            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 28, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), depth, days));
            
            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 35, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), depth, days));
            
            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 42, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.Personal), depth, days));
            
            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 49, SyncableStatus.ActionType.Sync));
            mergedList.AddRange(MostRecentlyUsedFile.Scan(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), depth, days));
            
            if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, 56, SyncableStatus.ActionType.Sync));
            List<string> drives = SystemEnvironment.StorageDevices.GetNonUSBDriveLetters();
            
            drives.AddRange(SystemEnvironment.StorageDevices.GetUSBDriveLetters());

            int done = 1;
            double toPercent = 0; 
            if (drives.Count > 0) toPercent = (100 - 56) / drives.Count;

            foreach (string drive in drives)
            {
                if (statusMonitor != null) statusMonitor(new SyncableStatus("", 0, ((int)(done * toPercent)) + 56, SyncableStatus.ActionType.Sync));
                mergedList.AddRange(MostRecentlyUsedFile.Scan(drive, depth, days));
                done++;
            }
            
            return CleanUP(mergedList);
        }

        public static SortedList<string, string> ConvertToSortedList(List<string> MRUs)
        {
            SortedList<string, string> MRUSorted = new SortedList<string, string>();
            foreach (string mru in MRUs)
            {
                string filename = Path.GetFileName(mru);
                int count = 2;
                while (MRUSorted.Keys.Contains(filename))
                {
                    filename = filename.Substring(0, filename.LastIndexOf('.')) + count + Path.GetExtension(filename);
                    count++;
                }
                MRUSorted.Add(filename, mru);
            }
            return MRUSorted;
        }

        /// <summary>
        /// Scan for recently used file
        /// </summary>
        /// <param name="path">Start path to be scanned</param>
        /// <param name="depth">To scan how deep</param>
        /// <param name="howRecent">How many day old and below</param>
        /// <returns>A list of recently use file</returns>
        public static List<string> Scan(string path, int depth, int howRecent)
        {
            List<string> files = new List<string>();
            DirectoryInfo di = new DirectoryInfo(path);
            List<string> upperDirectory = new List<string>();
            List<string> working = new List<string>();
            upperDirectory.Add(path);
            for (int i = 0; i < depth; i++)
            {
                foreach (string s in upperDirectory)
                {
                    di = new DirectoryInfo(s);
                    try
                    {
                        foreach (FileInfo fi in di.GetFiles())
                        {
                            if (((fi.Attributes & FileAttributes.System) |
                                (fi.Attributes & FileAttributes.Hidden) |
                                (fi.Attributes & FileAttributes.Encrypted) |
                                (fi.Attributes & FileAttributes.Offline) |
                                (fi.Attributes & FileAttributes.Temporary)) == 0)
                            {
                                if ((DateTime.Now - fi.LastWriteTime).TotalDays <= (howRecent))
                                {
                                    files.Add(fi.FullName);
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing
                    }
                    try
                    {
                        working.Clear();
                        foreach (DirectoryInfo idi in di.GetDirectories())
                        {
                            working.Add(idi.FullName);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing
                    }
                }
                upperDirectory.Clear();
                upperDirectory.AddRange(working);
            }
            return files;
        }

        /// <summary>
        /// Do a cleanup of the given list of MRUs, 
        /// to remove non existance files as well to remove shortcuts
        /// </summary>
        /// <param name="MRUs">the list of MRUs</param>
        /// <returns>the cleaned list of MRU</returns>
        private static List<string> CleanUP(List<string> MRUs)
        {
            List<string> filenames = new List<string>();
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetAssembly(typeof(MostRecentlyUsedFile)).Location));

            foreach (string filename in MRUs)
            {
                if (!filenames.Contains(filename) )
                {
                    if (File.Exists(filename))
                    {
                        // not dir
                        if (!di.FullName.Equals(Path.GetDirectoryName(filename)))
                        {
                            // see if is a shotcut
                            FileInfo fi = new FileInfo(filename);
                            
                            if (fi.Length != 0)
                            {
                                if (!IsShortcut(filename))
                                {
                                    filenames.Add(filename);
                                }
                            }
                        }
                    }
                }
            }
            return (filenames);
        }
        /// <summary>
        /// check if the given file is a shortcut
        /// </summary>
        /// <param name="filename">file to check</param>
        /// <returns>true if the file is a shortcut, false otherwise</returns>
        private static bool IsShortcut(String filename)
        {
            try
            {
                BinaryReader fs = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
                byte[] read_id = new byte[4];
                byte[] read_guid = new byte[16];
                read_id = fs.ReadBytes(4);
                read_guid = fs.ReadBytes(16);
                fs.Close();
                return (isEqual(read_id, shortcutid) && isEqual(read_guid, shortcut_guid));
            }
            catch (IOException ex)
            {
                Logging.Logger.GetInstance().WARNING("IOException in MostRecentlyUsedFile.cs: " + ex.Message);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logging.Logger.GetInstance().WARNING("IOException in MostRecentlyUsedFile.cs: " + ex.Message);
                return false;
            }
        }

        // format from: http://www.stdlib.com/art6-Shortcut-File-Format-lnk.html
        private static byte[] shortcutid = { 0x4C, 0x00, 0x00, 0x00 };
        private static byte[] shortcut_guid = 
               {0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46};

        /// <summary>
        /// check if two set of byte array is the same
        /// </summary>
        /// <param name="x">first byte array</param>
        /// <param name="y">second byte array</param>
        /// <returns>true if the two array is same, false otherwise</returns>
        private static bool isEqual(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
                return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get the most recently used (MRU) file
        /// </summary>
        /// <param name="key">The key of the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="SystemException">Is thrown when incompatible version of windows is detected
        /// (i.e., too new (>6.2) or too old (<5.1)</exception>
        public static string Get(string key)
        {
            if (Environment.OSVersion.Version > new Version(6, 2) || Environment.OSVersion.Version < new Version(5, 1))
                throw new NotSupportedException("Imcompatible (Newer) Version of Windows Detected. Feature not supported");

            if (Environment.OSVersion.Version.Major >= 6)
                return GetPidl(key, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSavePidlMRU\\*");
            else
                return GetNonPidl(key, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSaveMRU\\*");
        }

        /// <summary>
        /// Get the most recently used (MRU) file as a sorted liist
        /// </summary>
        /// <param name="key">The key of the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="SystemException">Is thrown when incompatible version of windows is detected
        /// (i.e., too new (>6.2) or too old (<5.1)</exception>
        public static List<string> Get()
        {
            if (Environment.OSVersion.Version > new Version(6, 2) || Environment.OSVersion.Version < new Version(5, 1))
                throw new NotSupportedException("Incompatible (Newer) Version of Windows Detected. Feature not supported");

            if (Environment.OSVersion.Version.Major >= 6)
                return CleanUP(GetPidl("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSavePidlMRU\\*"));
            else
                return CleanUP(GetNonPidl("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSaveMRU\\*"));
        }

        /// <summary>
        /// Get a sorted list of most recently used (MRU) files for windows xp and below
        /// </summary>
        /// <param name="key">Registry key of the MRU</param>
        /// <returns>A sorted list of the MRU</returns>
        /// <exception cref="System.NullReferenceException">The given registry key is not valid</exception>
        private static List<string> GetNonPidl(string key)
        {
            List<string> mrus = new List<string>();
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);
            if (regKey == null)
                throw new NullReferenceException();
            foreach (string index in regKey.GetValueNames())
            {
                if (!index.Equals("MRUListEx"))
                {
                    mrus.Add((string)regKey.GetValue(index));
                }
            }

            return mrus;
        }

        /// <summary>
        /// Get a sorted list of most recently used (MRU) files for windows vista and above
        /// </summary>
        /// <param name="key">Registry key of the MRU</param>
        /// <returns>A sorted list of the MRU</returns>
        /// <exception cref="System.NullReferenceException">The given registry key is not valid</exception>
        private static List<string> GetPidl(string key)
        {
            List<string> mrus = new List<string>();
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);
            if (regKey == null)
                throw new NullReferenceException();
            foreach (string index in regKey.GetValueNames())
            {
                if (!index.Equals("MRUListEx"))
                {
                    mrus.Add(GetPidl(index, key));
                }
            }
            return mrus;
        }

        /// <summary>
        /// Get the most recently used (MRU) file for windows xp and below
        /// </summary>
        /// <param name="index">The index of the MRU</param>
        /// <param name="key">The registry key of the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="System.NullReferenceException">The given registry key or the index is not found.</exception>
        private static string GetNonPidl(string index, string key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);
            if (regKey == null)
                throw new NullReferenceException();
            string value = (string)regKey.GetValue(index);
            if (value == null)
                throw new NullReferenceException();
            return value;
        }

        /// <summary>
        /// Get the most recently used (MRU) file for windows vista and above
        /// </summary>
        /// <param name="index">The index of the MRU</param>
        /// <param name="key">The registry key to the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="System.NullReferenceException">The given registry key or the index is not found.</exception>
        private static string GetPidl(string index, string key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);

            if (regKey == null)
                throw new NullReferenceException();

            object value = regKey.GetValue(index);

            if (value == null)
                throw new NullReferenceException();

            byte[] data = (byte[])(value);

            IntPtr p = Marshal.AllocHGlobal(data.Length);

            Marshal.Copy(data, 0, p, data.Length);

            // get number of data;
            UInt32 cidl = (UInt32)Marshal.ReadInt16(p);

            // get parent folder
            UIntPtr parentpidl = (UIntPtr)((UInt32)p);

            StringBuilder path = new StringBuilder(256);

            SHGetPathFromIDListW(parentpidl, path);

            Marshal.Release(p);

            return path.ToString();
        }
    }
}
