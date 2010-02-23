using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    class SettingsConfigElement : ConfigurationElement
    {
        /// <summary>
        /// At the moment, this will list all the possible options editable
        /// and/or used by the program.
        /// </summary>
        public enum Options { AllowAutoSyncForConflictFreeTasks };

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
    }
}
