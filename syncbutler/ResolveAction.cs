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

        public static string ActionDescription(Conflict.Action a)
        {
            switch (a)
            {
                case Conflict.Action.CopyToLeft: return "Copy to Folder 1";
                case Conflict.Action.CopyToRight: return "Copy to Folder 2";
                case Conflict.Action.DeleteLeft: return "Delete from Folder 1";
                case Conflict.Action.DeleteRight: return "Delete from Folder 2";
                case Conflict.Action.Ignore: return "Do not do anything";
                case Conflict.Action.Merge: return "Merge differences";
                case Conflict.Action.Unknown: return "You should not see this";
                default: return "Bad Action?";
            }
        }
    }
}
