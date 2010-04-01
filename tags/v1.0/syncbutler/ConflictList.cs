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
