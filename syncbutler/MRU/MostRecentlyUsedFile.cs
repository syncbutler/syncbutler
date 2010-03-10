using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

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

        /// <summary>
        /// Do a cleanup of the given list of MRUs, to remove non existance files.
        /// </summary>
        /// <param name="MRUs">the list of MRUs</param>
        /// <returns>the cleaned list of MRU</returns>
        private static SortedList<string, string> CleanUP(SortedList<string, string> MRUs)
        {
            List<string> keys = new List<string>();
            foreach (string key in MRUs.Keys)
            {
                string filename = MRUs[key];
                if (!File.Exists(filename))
                {
                    keys.Add(key);
                }
            }
            foreach (string key in keys)
            {
                MRUs.Remove(key);
            }
            return MRUs;
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
                throw new SystemException("Imcompatible (Newer) Version of Windows Detected. Feature Disabled");

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
        public static SortedList<string, string> Get()
        {
            if (Environment.OSVersion.Version > new Version(6, 2) || Environment.OSVersion.Version < new Version(5, 1))
                throw new SystemException("Incompatible (Newer) Version of Windows Detected. Feature Disabled");

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
        private static SortedList<string, string> GetNonPidl(string key)
        {
            SortedList<string, string> mrus = new SortedList<string, string>();
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);
            if (regKey == null)
                throw new NullReferenceException();
            foreach (string index in regKey.GetValueNames())
            {
                if (!index.Equals("MRUListEx"))
                {
                    mrus.Add(index, (string)regKey.GetValue(index));
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
        private static SortedList<string, string> GetPidl(string key)
        {
            SortedList<string, string> mrus = new SortedList<string, string>();
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(key);
            if (regKey == null)
                throw new NullReferenceException();
            foreach (string index in regKey.GetValueNames())
            {
                if (!index.Equals("MRUListEx"))
                {
                    mrus.Add(index, GetPidl(index, key));
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
