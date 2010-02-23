using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.MyEnvironment
{
    class SettingsConfigElement : ConfigurationElement
    {
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
