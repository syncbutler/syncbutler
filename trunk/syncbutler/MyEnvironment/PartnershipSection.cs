using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.MyEnvironment
{
    public class PartnershipSection : ConfigurationSection
    {
        [ConfigurationProperty("Partnership")]
        public PartnershipConfigElement partnership
        {
            get
            {
                return ( (PartnershipConfigElement)this["Partnership"] );
            }
            set
            {
                this["Partnership"] = value;
            }
        }
    }
}
