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
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    /// <summary>
    /// XML descriptor for storing individual settings in the settings section
    /// </summary>
    public class SettingsConfigElement : ConfigurationElement
    {
        /// <summary>
        /// At the moment, this will list all the possible options editable
        /// and/or used by the program.
        /// </summary>
        public enum Options {AllowAutoSyncForConflictFreeTasks, FirstRunComplete,
        FileReadBufferSize, ComputerName};

        /// <summary>
        /// This is a flag to allow multithreading during user conflict
        /// to auto run those actions that can be resolved without the user's
        /// intervention.
        /// </summary>
        [ConfigurationProperty("allowAutoSyncForConflictFreeTasks")]
        public bool AllowAutoSyncForConflictFreeTasks
        {
            get
            {
                return (bool) this["allowAutoSyncForConflictFreeTasks"];
            }
            set
            {
                this["allowAutoSyncForConflictFreeTasks"] = value;
            }
        }

        /// <summary>
        /// When this flag is turned on, it will reduce the amount of load required
        /// to create the initial environment
        /// </summary>
        [ConfigurationProperty("firstRunComplete")]
        public bool FirstRunComplete
        {
            get
            {
                return (bool) this["firstRunComplete"];
            }
            set
            {
                this["firstRunComplete"] = value;
            }
        }

        /// <summary>
        /// When this flag is turned on, user will not see the help items in settings
        /// </summary>
        [ConfigurationProperty("firstSBSRun")]
        public bool FirstSBSRun
        {
            get
            {
                return (bool)this["firstSBSRun"];
            }
            set
            {
                this["firstSBSRun"] = value;
            }
        }

        /// <summary>
        /// The size of the buffer when reading from files.
        /// This should grow/shrink in proportion to the memory available.
        /// </summary>
        [ConfigurationProperty("fileReadBufferSize")]
        public long FileReadBufferSize
        {
            get
            {
                return (long) this["fileReadBufferSize"];
            }
            set
            {
                this["fileReadBufferSize"] = value;
            }
        }

        /// <summary>
        /// The serilised version of the friendly name of the computer
        /// </summary>
        [ConfigurationProperty("computerName")]
        public string ComputerName
        {
            get
            {
                return (string) this["computerName"];
            }
            set
            {
                this["computerName"] = value;
            }
        }

        /// <summary>
        /// The serilised version of the friendly name of the computer
        /// </summary>
        [ConfigurationProperty("computerNamed")]
        public bool ComputerNamed
        {
            get
            {
                return (bool)this["computerNamed"];
            }
            set
            {
                this["computerNamed"] = value;
            }
        }

        [ConfigurationProperty("SBSDriveLetter")]
        public char SBSDriveLetter
        {
            get
            {
                return (char) this["SBSDriveLetter"];
            }
            set
            {
                this["SBSDriveLetter"] = value;
            }
        }

        [ConfigurationProperty("SBSEnable")]
        public string SBSEnable
        {
            get
            {
                return (string)this["SBSEnable"];
            }
            set
            {
                this["SBSEnable"] = value;
            }
        }

        [ConfigurationProperty("EnableSyncAll")]
        public bool EnableSyncAll
        {
            get
            {
                return (bool)this["EnableSyncAll"];
            }
            set
            {
                this["EnableSyncAll"] = value;
            }
        }

        [ConfigurationProperty("SBSDrivePartition")]
        public int SBSDrivePartition
        {
            get
            {
                return (int)this["SBSDrivePartition"];
            }
            set
            {
                this["SBSDrivePartition"] = value;
            }
        }
        [ConfigurationProperty("SBSDriveId")]
        public string SBSDriveId
        {
            get
            {
                return (string)this["SBSDriveId"];
            }
            set
            {
                this["SBSDriveId"] = value;
            }
        }
        [ConfigurationProperty("Resolution")]
        public string Resolution
        {
            get
            {
                return (string)this["Resolution"];
            }
            set
            {
                this["Resolution"] = value;
            }
        }


        [ConfigurationProperty("FreeSpaceToUse")]
        public double FreeSpaceToUse
        {
            get
            {
                return (double)this["FreeSpaceToUse"];
            }
            set
            {
                this["FreeSpaceToUse"] = value;
            }
        }


        /// <summary>
        /// Determines if the shell integration menu should be kept on or off
        /// </summary>
        [ConfigurationProperty("enableShellContext")]
        public bool EnableShellContext
        {
            get
            {
                return (bool) this["enableShellContext"];
            }
            set
            {
                this["enableShellContext"] = value;
            }
        }
    }
}
