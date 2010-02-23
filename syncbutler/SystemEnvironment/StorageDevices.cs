using System;
using System.Collections.Generic;
using System.Text;
using System.Management;

namespace SyncButler.SystemEnvironment
{
    public class StorageDevices
    {
        public static String GetDriveLetter(String driveID)
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
