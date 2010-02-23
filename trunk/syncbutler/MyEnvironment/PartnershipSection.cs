using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    public class PartnershipSection : ConfigurationSection
    {
        [ConfigurationProperty("partnership")]
        public PartnershipConfigElement Partnership
        {
            get
            {
                return ( (PartnershipConfigElement)this["partnership"] );
            }
            set
            {
                this["partnership"] = value;
            }
        }
    }
}
