// Developer to contact: Lee Chee Full
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
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    /// <summary>
    /// This stores the list of partnerships, in XML descriptor form
    /// </summary>
    public class PartnershipSection : ConfigurationSection
    {
        /// <summary>
        /// This sectional attributes contains all the states related to
        /// Partnerships (particularly holding on to a list of them)
        /// </summary>
        [ConfigurationProperty("partnershipList")]
        public PartnershipCollection PartnershipList
        {
            get
            {
                return ((PartnershipCollection)this["partnershipList"]);
            }
            set
            {
                this["partnershipList"] = value;
            }
        }

        /// <summary>
        /// This sectional attributes contains all the states related to Mini
        /// Partnerships (particularly holding on to a list of them)
        /// </summary>
        [ConfigurationProperty("miniPartnershipList")]
        public PartnershipCollection MiniPartnershipList
        {
            get
            {
                return ((PartnershipCollection)this["miniPartnershipList"]);
            }
            set
            {
                this["miniPartnershipList"] = value;
            }
        }
    }
}
