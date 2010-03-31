using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents an action to take and its text description.
    /// </summary>
    public class ResolveAction
    {

        /// <summary>
        /// Gets or sets the action to use when resolving a conflict.
        /// </summary>
        public Conflict.Action ResolutionAction { get; set;}

        /// <summary>
        /// Gets or sets the description of the action to be taken.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resolutionAction">The action to use when resolving a conflict.</param>
        /// <param name="description">The description of the action to be taken.</param>
        public ResolveAction(Conflict.Action resolutionAction, string description)
        {
            this.ResolutionAction = resolutionAction;
            this.Description = description;
        }
    }
}
