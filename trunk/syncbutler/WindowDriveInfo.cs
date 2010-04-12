using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace SyncButler
{
    /// <summary>
    /// A logical repesentation of drive information
    /// </summary>
    public class WindowDriveInfo
    {
        private char DriveLetter;
        private String Label;

        /// <summary>
        /// The constructor to initilize the class
        /// </summary>
        /// <param name="DriveLetter">Create a drive info base on the drive letter</param>
        /// <exception cref="System.Exception">If the given drive letter is not a valid drive</exception>
        public WindowDriveInfo(String DriveLetter)
        {
            if (DriveLetter.Length == 0)
            {
                throw new Exception("invalid drive");
            }
            this.DriveLetter = DriveLetter[0];

            if (!Directory.Exists(DriveLetter[0] + ":\\"))
            {
                throw new Exception("invalid drive");
            }

            DriveInfo di = new DriveInfo(""+ DriveLetter[0]);
            Label = di.VolumeLabel;
        }

        public char GetDriveLetter()
        {
            return this.DriveLetter;
        }
        /// <summary>
        /// The constructor to initilize the class
        /// </summary>
        /// <param name="DriveLetter">Create a drive info base on the drive letter</param>
        /// <exception cref="System.Exception">If the given drive letter is not a valid drive</exception>
        public WindowDriveInfo(char driveLetter)
        {
            this.DriveLetter = driveLetter;

            if (!Directory.Exists(driveLetter + ":\\"))
            {
                throw new Exception("invalid drive");
            }
            else
            {
                DriveInfo di = new DriveInfo("" + driveLetter);
                Label = di.VolumeLabel;
            }
        }

        /// <summary>
        /// Get a list of drive information base on a list of drive letters
        /// </summary>
        /// <param name="DriveLetters">A list of drive letters</param>
        /// <returns>A list of drive information</returns>
        public static List<WindowDriveInfo> GetDriveInfo(List<String> DriveLetters)
        {
            List<WindowDriveInfo> ToRtn = new List<WindowDriveInfo>(); 
            foreach (String s in DriveLetters)
            {
                ToRtn.Add(new WindowDriveInfo(s));
            }
            return ToRtn;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is WindowDriveInfo))
            {
                return false;
            }
            else
            {
                WindowDriveInfo wdi = (WindowDriveInfo) obj;
                return (wdi.GetDriveLetter() == this.GetDriveLetter() && wdi.Label.Equals(this.Label));
            }
        }

        public override string ToString()
        {
            if (Label != null && Label.Length != 0)
                return Label + " - " + "[" + DriveLetter + ":]";
            else
                return "[" + this.DriveLetter + ":]";
        }
    }
}
