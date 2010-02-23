using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    class SettingsSection : ConfigurationSection
    {
        [ConfigurationProperty("systemSettings")]
        public SettingsConfigElement SystemSettings
        {
            get
            {
                return ((SettingsConfigElement)this["systemSettings"]);
            }
            set
            {
                this["systemSettings"] = value;
            }
        }
    }
}
