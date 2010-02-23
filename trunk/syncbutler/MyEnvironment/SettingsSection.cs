using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.MyEnvironment
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
