using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SyncButler
{
    /// <summary>
    /// A service class to retrive most recently used file for windows
    /// </summary>
    public class MostRecentlyUsedFile
    {
        [DllImport("shell32.dll")]
        private static extern Int32 SHGetPathFromIDListW(
            UIntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);
        //        IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);

        [DllImport("shell32.dll")]
        private static extern IntPtr ILCombine(
            IntPtr pidl1, IntPtr pidl2);

        [DllImport("shell32.dll")]
        private static extern void ILFree(
            IntPtr pidl);

        /// <summary>
        /// Get the most recently used (MRU) file
        /// </summary>
        /// <param name="key">The key of the MRU</param>
        /// <returns>return path of the file</returns>
        public static string Get(string key)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                return GetPidl(key);
            else
                return GetNonPidl(key);
        }

        /// <summary>
        /// Get the most recently used (MRU) file for windows xp and below
        /// </summary>
        /// <param name="key">The key of the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="System.NullReferenceException">The given key is not found.</exception>
        private static string GetNonPidl(string key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSaveMRU\\*");
            string value = (string)regKey.GetValue(key);
            if (value == null)
                throw new NullReferenceException();
            return value;
        }

        /// <summary>
        /// Get the most recently used (MRU) file for windows vista and above
        /// </summary>
        /// <param name="key">The key of the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="System.NullReferenceException">The given key is not found.</exception>
        private static string GetPidl(string key)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSavePidlMRU\\*");
            object value = regKey.GetValue(key);

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
