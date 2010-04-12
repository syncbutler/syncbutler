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
using System.Collections.ObjectModel;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a list of actions that can be taken and the selected action to take.
    /// </summary>
    public class ResolveActionSet : ObservableCollection<ResolveAction>
    {
        private ResolveAction selectedAction;

        /// <summary>
        /// Gets or sets the action selected by the user.
        /// </summary>
        public ResolveAction SelectedAction
        {
            get
            {
                return this.selectedAction;
            }
			set{
				this.selectedAction = value;	
			}
        }

        /// <summary>
        /// Add an action to this set.
        /// </summary>
        /// <param name="toAdd">The action to add.</param>
        public void AddAction(Conflict.Action toAdd)
        {
            this.Add(new ResolveAction(toAdd, ResolveAction.ActionDescription(toAdd)));
        }

        /// <summary>
        /// Sets the action selected by the user.
        /// </summary>
        /// <param name="toSet">The selected action.</param>
        public void SetSelectedAction(Conflict.Action toSet)
        {
            this.selectedAction = null;
            foreach (ResolveAction action in this)
            {
                if (action.ResolutionAction == toSet)
                {
                    this.selectedAction = action;
                    break;
                }
            }
        }
    }
}
