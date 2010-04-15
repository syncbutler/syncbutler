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

        /// <summary>
        /// The drive letter of this WindowDriveInfo object
        /// </summary>
        /// <returns>a char for the drive letter</returns>
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

        /// <summary>
        /// Use to test if 2 WindowDriveInfo object are content equivalent
        /// </summary>
        /// <param name="obj">The WindowDriveInfo to be tested againt the current one</param>
        /// <returns>true if they are equal, false otherwise</returns>
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

        /// <summary>
        /// Returns the string representation of this object
        /// </summary>
        /// <returns>a string representing this object</returns>
        public override string ToString()
        {
            if (Label != null && Label.Length != 0)
                return Label + " - " + "[" + DriveLetter + ":]";
            else
                return "[" + this.DriveLetter + ":]";
        }
    }
}
