// Developer to contact: Bryan Chen Shenglong
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
using System.Text;
using System.IO;
using System.Management;

namespace SyncButler.SystemEnvironment
{
    /// <summary>
    /// Class to query information from various storage devices.
    /// </summary>
    public class StorageDevices
    {
        /// <summary>
        /// Possible formats of file systems.
        /// </summary>
        public enum Format
        {
            FAT16,
            FAT32,
            NTFS
        }

        /// <summary>
        /// Device types that are supported by SyncButler.
        /// </summary>
        public enum DeviceType
        {
            FixedStorage,
            RemovableStorage,
            NetworkDrive,
            CDRom,
            Unknown
        }

        /// <summary>
        /// Gets the type of the device given the drive letter.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the form of C: or C:\</param>
        /// <returns>The type of the device, or unknown if not known.</returns>
        /// <exception cref="ArgumentNullException">driveLetter was null</exception>
        /// <exception cref="ArgumentException">The first letter of driveLetter is not an uppercase or lowercase letter from 'a' to 'z' or driveLetter does not refer to a valid drive.</exception>
        public static DeviceType GetDeviceType(string driveLetter)
        {
            DriveInfo d = new DriveInfo(driveLetter);
            DeviceType type = DeviceType.Unknown;

            switch (d.DriveType)
            {
                case DriveType.CDRom:
                    type = DeviceType.CDRom; break;
                case DriveType.Fixed:
                    type = DeviceType.FixedStorage; break;
                case DriveType.Network:
                    type = DeviceType.NetworkDrive; break;
                case DriveType.Removable:
                    type = DeviceType.RemovableStorage; break;
            }

            return type;
        }

        /// <summary>
        /// Maximum file size for FAT32 filesystem.
        /// </summary>
        public static long MAX_SIZE_FAT32 = 4294967296;

        /// <summary>
        /// Gets the drive letter, in the form of X:, based on the PNPDeviceID
        /// </summary>
        /// <param name="driveID">The PNPDeviceID</param>
        /// <returns>String of drive letter</returns>
        /// <exception cref="Exceptions.DriveNotSupportedException">Thrown if the low-level details of the drive could not be accessed.</exception>
        public static string GetDriveLetter(string driveID)
        {
            if (driveID.Length == 0)
                throw new Exceptions.DriveNotSupportedException("The drive type could not be determined.");

            string letter = "";
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                if (DDObj["PNPDeviceID"].ToString().Equals(driveID))
                {
                    foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_LogicalDisk"))
                        {
                            letter = LDObj["DeviceID"].ToString();
                        }
                    }
                }
            }

            return letter;
        }

        /// <summary>
        /// Gets the drive letter, in the form of X:, based on the PNPDeviceID and the partition index.
        /// This is preferable over GetDriveLetter(string driveID) because it is more accurate as it takes the partition index into account.
        /// </summary>
        /// <param name="driveID">The PNPDeviceID</param>
        /// <param name="partitionIndex">The partition index of the drive</param>
        /// <returns>A string with the drive letter in the form of C:</returns>
        /// <exception cref="Exceptions.DriveNotSupportedException">Thrown if the low-level details of the drive could not be accessed.</exception>
        public static string GetDriveLetter(string driveID, int partitionIndex)
        {
            if (driveID == null || (driveID.Length == 0) || (partitionIndex == -1))
                throw new Exceptions.DriveNotSupportedException("The drive type could not be determined.");

            string letter = "";
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                if (DDObj["PNPDeviceID"].ToString().Equals(driveID))
                {
                    foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                    {
                        if (int.Parse(DPObj["Index"].ToString()) == partitionIndex)
                        {
                            foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_LogicalDisk"))
                            {
                                letter = LDObj["DeviceID"].ToString();
                            }
                        }
                    }
                }
            }

            return letter;
        }

        /// <summary>
        /// Gets the partition index of a drive partition, based on the drive letter provided.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the format of C:</param>
        /// <returns>An integer containing the partition index. Returns -1 if an error had occurred.</returns>
        /// <exception cref="Exceptions.DriveNotSupportedException">Thrown if the low-level details of the drive could not be accessed.</exception>
        public static int GetDrivePartitionIndex(string driveLetter)
        {
            int id = -1;
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DeviceID='" + driveLetter.TrimEnd('\\') + "'");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                {
                    id = int.Parse(DPObj["Index"].ToString());
                }
            }

            if (id == -1) throw new Exceptions.DriveNotSupportedException("The drive type of '" + driveLetter + "' is not supported.");
            return id;
        }
        
        /// <summary>
        /// Gets the drive letter of a USB drive, in the form of X:. If a drive letter is not associated to a USB device, then it will not find anything.
        /// </summary>
        /// <param name="driveID">PNPDeviceID of the device</param>
        /// <returns>String drive letter</returns>
        public static string GetUSBDriveLetter(string driveID)
        {
            string letter = "";
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                if (DDObj["PNPDeviceID"].ToString().Equals(driveID))
                {
                    foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_LogicalDisk"))
                        {
                            letter = LDObj["DeviceID"].ToString();
                        }
                    }
                }
            }

            return letter;
        }

        /// <summary>
        /// Returns whether the drive with the drive letter is a USB storage device.
        /// </summary>
        /// <param name="driveLetter">The drive letter in the form of C:</param>
        /// <returns>True if drive letter belongs to a USB storage device. False otherwise.</returns>
        public static bool IsUSBDrive(string driveLetter)
        {
            driveLetter = driveLetter.TrimEnd('\\');
            List<string> usbDriveList = GetRemovableDeviceDriveLetters();
            return (usbDriveList.Contains(driveLetter));
        }

        /// <summary>
        /// Returns a List of drive letters of all removable storage devices attached to the computer.
        /// Drive letter format is of the format X:
        /// </summary>
        /// <returns>List of USB Drive letters</returns>
        public static List<string> GetRemovableDeviceDriveLetters()
        {
            List<string> list = new List<string>();
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType='Removable Media' OR InterfaceType = 'USB'");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_LogicalDisk"))
                    {
                        list.Add(LDObj["DeviceID"].ToString());
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Returns a List of drive letters of Non USB storage devices attached to the computer.
        /// Drive letter format is of the format X:
        /// </summary>
        /// <returns>List of non USB Drive letters</returns>
        public static List<string> GetNonUSBDriveLetters()
        {
            List<string> list = new List<string>();
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType<>'USB'");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_LogicalDisk"))
                    {
                        list.Add(LDObj["DeviceID"].ToString());
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the unique PNPDeviceID based on a drive letter.
        /// Drive letter can be of the form X:\ or X:
        /// </summary>
        /// <param name="driveLetter">The drive letter</param>
        /// <returns>String containing the unique PNPDeviceID</returns>
        /// <exception cref="Exceptions.DriveNotSupportedException">Thrown if the low-level details of the drive could not be accessed.</exception>
        public static string GetDriveID(string driveLetter)
        {
            string id = "";
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DeviceID='" + driveLetter.TrimEnd('\\') + "'");

            foreach (ManagementObject DDObj in DDMgmtObjSearcher.Get())
            {
                foreach (ManagementObject DPObj in DDObj.GetRelated("Win32_DiskPartition"))
                {
                    foreach (ManagementObject LDObj in DPObj.GetRelated("Win32_DiskDrive"))
                    {
                        id = LDObj["PNPDeviceID"].ToString();
                    }
                }
            }

            if (id.Length == 0) throw new Exceptions.DriveNotSupportedException("The drive type of '" + driveLetter + "' is not supported.");
            else return id;
        }

        /// <summary>
        /// Returns the available space of a drive.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the form of 'C' or 'C:' or 'C:\'. Must never be null.</param>
        /// <returns>The available space (taking into consideration the user context) in bytes.</returns>
        /// <exception cref="ArgumentNullException">If null was passed.</exception>
        /// <exception cref="ArgumentException">If the drive letter provided is not valid.</exception>
        /// <exception cref="UnauthorizedAccessException">If the user is not allowed to access the drive.</exception>
        /// <exception cref="IOException">An I/O error occurred (for example, a disk error or a drive was not ready).</exception>
        public static long GetAvailableSpace(string driveLetter)
        {
            DriveInfo drive = new DriveInfo(driveLetter);
            return drive.AvailableFreeSpace;
        }

        /// <summary>
        /// Returns the drive format.
        /// </summary>
        /// <param name="driveLetter">Drive letter in the form of 'C' or 'C:' or 'C:\'. Must never be null.</param>
        /// <returns>The format of the drive (i.e. Format.NTFS or Format.FAT32).</returns>
        /// <exception cref="ArgumentNullException">If null was passed.</exception>
        /// <exception cref="ArgumentException">If the drive letter provided is not valid.</exception>
        /// <exception cref="UnauthorizedAccessException">If the user is not allowed to access the drive.</exception>
        /// <exception cref="IOException">An I/O error occurred (for example, a disk error or a drive was not ready).</exception>
        /// <exception cref="Exceptions.UnknownStorageFormatException">If the underlying system call returned something that was not recognised.</exception>
        public static Format GetDriveFormat(string driveLetter)
        {
            DriveInfo drive = new DriveInfo(driveLetter);
            Format driveFormat;

            if (drive.DriveFormat.ToUpper().Equals("NTFS"))
                driveFormat = Format.NTFS;
            else if (drive.DriveFormat.ToUpper().Equals("FAT32"))
                driveFormat = Format.FAT32;
            else if (drive.DriveFormat.ToUpper().Equals("FAT"))
                driveFormat = Format.FAT16;
            else
                throw new Exceptions.UnknownStorageFormatException("The drive '" + driveLetter + "' has an unsupported format '" + drive.DriveFormat + "'.");

            return driveFormat;
        }

        /// <summary>
        /// Gets a string array containing all drive letters.
        /// Drive letters are in the format C:\
        /// </summary>
        /// <returns>All drive letters currently detected on the computer.</returns>
        public static List<string> GetAllDrives()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            List<string> driveList = new List<string>();

            foreach (DriveInfo d in drives)
            {
                driveList.Add(d.Name);
            }

            return driveList;
        }
    }
}
