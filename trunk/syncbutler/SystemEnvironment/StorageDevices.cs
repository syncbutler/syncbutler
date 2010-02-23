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
        public static String GetDriveLetter(String driveID)
        {
            String letter = "";
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
        /// Gets the drive letter of a USB drive, in the form of X:. If a drive letter is not associated to a USB device, then it will not find anything.
        /// </summary>
        /// <param name="driveID">PNPDeviceID of the device</param>
        /// <returns>String drive letter</returns>
        public static String GetUSBDriveLetter(String driveID)
        {
            String letter = "";
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
        /// Returns a List of drive letters of USB storage devices attached to the computer.
        /// Drive letter format is of the format X:
        /// </summary>
        /// <returns>List of USB Drive letters</returns>
        public static List<String> GetUSBDriveLetters()
        {
            List<String> list = new List<string>();
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
        public static String GetDriveID(String driveLetter)
        {
            String id = "";
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
