using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    class SettingsSection : ConfigurationSection
    {
        /// <summary>
        /// This sectional attributes contains any and all states related
        /// to the entire program.
        /// </summary>
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
