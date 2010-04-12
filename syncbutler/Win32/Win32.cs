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
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using Microsoft.Win32;

namespace SyncButler.Win32
{
    /// <summary>
    /// A wrapper class to allow unmanaged called from WIN32 apis.
    /// </summary>
    class Win32Wrapper
    {
        #region MRU
        [DllImport("shell32.dll")]
        public static extern Int32 SHGetPathFromIDListW(
            UIntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);

        /// <summary>
        /// Get the most recently used (MRU) file for windows vista and above
        /// </summary>
        /// <param name="index">The index of the MRU</param>
        /// <param name="key">The registry key to the MRU</param>
        /// <returns>return path of the file</returns>
        /// <exception cref="System.NullReferenceException">The given registry key or the index is not found.</exception>
        public static string GetPidl(string index, string key)
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

        #endregion

        #region GetIconFromFile
        // Reference: http://support.microsoft.com/?kbid=319350
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        private const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        public enum IconSize { Big, Small};
        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        /// <summary>
        /// Get the icon of the file that is represented by the explorer
        /// </summary>
        /// <param name="filename">target filename</param>
        /// <param name="iconsize">the size of the icon</param>
        /// <returns>the icon of the file</returns>
        public static Icon GetIcon(String filename, IconSize iconsize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            // small
            IntPtr image;
            switch (iconsize)
            {
                case IconSize.Big:
                    image = SHGetFileInfo(filename, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
                    break;
                case IconSize.Small:
                    image = SHGetFileInfo(filename, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);
                    break;
            }
            return Icon.FromHandle(shinfo.hIcon);
        }
        #endregion
    }
}
