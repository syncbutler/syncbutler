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
                case Conflict.Action.Error: return "An error hsa occured";
                default: return "Bad Action?";
            }
        }
    }
}
