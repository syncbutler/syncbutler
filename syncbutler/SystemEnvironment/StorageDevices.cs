using System;
using System.Collections.Generic;
using System.Text;
using System.Management;

namespace SyncButler.SystemEnvironment
{
    /// <summary>
    /// Class to query information from various storage devices.
    /// </summary>
    public class StorageDevices
    {
        /// <summary>
        /// Gets the drive letter, in the form of X:, based on the PNPDeviceID
        /// </summary>
        /// <param name="driveID">The PNPDeviceID</param>
        /// <returns>String of drive letter</returns>
        public static string GetDriveLetter(string driveID)
        {
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
        public static string GetDriveLetter(string driveID, int partitionIndex)
        {
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
            List<string> usbDriveList = GetUSBDriveLetters();
            return (usbDriveList.Contains(driveLetter));
        }

        /// <summary>
        /// Returns a List of drive letters of USB storage devices attached to the computer.
        /// Drive letter format is of the format X:
        /// </summary>
        /// <returns>List of USB Drive letters</returns>
        public static List<string> GetUSBDriveLetters()
        {
            List<string> list = new List<string>();
            ManagementObjectSearcher DDMgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

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

            return id;
        }
    }
}
