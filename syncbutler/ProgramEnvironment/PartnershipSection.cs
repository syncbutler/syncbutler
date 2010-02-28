using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    public class PartnershipSection : ConfigurationSection
    {
        /// <summary>
        /// This sectional attributes contains all the states related to
        /// Partnerships (particularly holding on to a list of them)
        /// </summary>
        [ConfigurationProperty("partnershipList")]
        public PartnershipListConfigCollection Partnership
        {
            get
            {
                return ((PartnershipListConfigCollection)this["partnershipList"]);
            }
            set
            {
                this["partnershipList"] = value;
            }
        }
    }
}
