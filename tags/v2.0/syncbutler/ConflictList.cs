// Developer to contact: Tan Chee Eng
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
using System.Linq;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a list of conflicts
    /// </summary>
    public class ConflictList
    {
        private List<Conflict> conflicts;
        private string partnershipName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="conflicts">List of Conflict objects.</param>
        /// <param name="partnershipName">The name of the partnership which this conflict list refers to.</param>
        public ConflictList(List<Conflict> conflicts, string partnershipName)
        {
            this.conflicts = conflicts;
            this.partnershipName = partnershipName;
        }

        /// <summary>
        /// Gets or sets the list of Conflict objects.
        /// </summary>
        public List<Conflict> Conflicts
        {
            get
            {
                return this.conflicts;
            }
            set
            {
                this.conflicts = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the partnership which this conflict list refers to.
        /// </summary>
        public string PartnershipName {
            get
            {
                return this.partnershipName;
            }
            set
            {
                this.partnershipName = value;
            }
        }
    }
}
