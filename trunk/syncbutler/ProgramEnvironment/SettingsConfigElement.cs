using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    public class SettingsConfigElement : ConfigurationElement
    {
        /// <summary>
        /// At the moment, this will list all the possible options editable
        /// and/or used by the program.
        /// </summary>
        public enum Options { AllowAutoSyncForConflictFreeTasks, FirstRunComplete, FileReadBufferSize };

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
    }
}
