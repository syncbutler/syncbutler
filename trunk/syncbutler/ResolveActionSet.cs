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
