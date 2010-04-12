using System;
using System.Collections.Generic;
using SyncButler.Exceptions;
using System.Collections.ObjectModel;

namespace SyncButler
{
    /// <summary>
    /// Represents a conflict detected in the sync. The state can be
    /// CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Unknown.
    /// </summary>
    public class Conflict
    {
        internal ISyncable left;
        internal ISyncable right;
        protected Action autoResolveAction;
        protected Action suggestedAction;
        public ResolveActionSet userActions {get; set;}

        /// <summary>
        /// Possible actions for conflict resolution.
        /// </summary>
        public enum Action { CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Ignore, Unknown, Error };

        /// <summary>
        /// Constructor used to instantiate a Conflict object.
        /// </summary>
        /// <param name="left">The left ISyncable</param>
        /// <param name="right">The other (right) ISyncable</param>
        /// <param name="autoResolveAction">The Action to be performed, or Unknown if this conflict cannot be automatically resolved.</param>
        public Conflict(ISyncable left, ISyncable right, Action autoResolveAction)
        {
            this.left = left;
            this.right = right;
            this.autoResolveAction = autoResolveAction;
            this.suggestedAction = Action.Unknown;

            userActions = new ResolveActionSet();
            if (!left.Exists())
            {
                userActions.AddAction(Action.CopyToLeft);
                userActions.AddAction(Action.DeleteRight);
                if (suggestedAction == Action.Unknown) userActions.SetSelectedAction(Action.CopyToLeft);
            }
            else if (!right.Exists())
            {
                userActions.AddAction(Action.CopyToRight);
                userActions.AddAction(Action.DeleteLeft);
                if (suggestedAction == Action.Unknown) userActions.SetSelectedAction(Action.CopyToRight);
            }
            else
            {
                userActions.AddAction(Action.CopyToLeft);
                userActions.AddAction(Action.CopyToRight);
                if (suggestedAction == Action.Unknown) userActions.SetSelectedAction(Action.CopyToLeft);
             }

            userActions.AddAction(Action.Ignore);
            if (suggestedAction != Action.Unknown) userActions.SetSelectedAction(suggestedAction);
        }

        /// <summary>
        /// Gets the folder containing the left ISyncable.
        /// </summary>
        public string LeftFolder
        {
            get
            {
                return this.left.GetContainingFolder();
            }
        }

        /// <summary>
        /// Gets the folder containing the right ISyncable.
        /// </summary>
        public string RightFolder
        {
            get
            {
                return this.right.GetContainingFolder();
            }
        }
		
        /// <summary>
        /// Constructor used to instantiate a Conflict object.
        /// </summary>
        /// <param name="left">The left ISyncable</param>
        /// <param name="right">The other (right) ISyncable</param>
        /// <param name="autoResolveAction">The Action to be performed, or Unknown if this conflict cannot be automatically resolved.</param>
        /// <param name="suggestedAction">If the conflict cannot be automatically resolved, then this should contain a suggested action.</param>
        public Conflict(ISyncable left, ISyncable right, Action autoResolveAction, Action suggestedAction) : this(left, right, autoResolveAction)
        {
            this.suggestedAction = suggestedAction;
            if (suggestedAction != Action.Unknown) userActions.SetSelectedAction(suggestedAction);
        }

        /// <summary>
        /// Internal method used to generate the offending path from the left or right object, depending on which is null.
        /// </summary>
        public string GetOffendingPath()
        {
            if (left != null)
            {
                if (left.EntityPath().Length != 0)
                {
                    return left.EntityPath();
                }
            }
            else if (right != null)
            {
                if (right.EntityPath().Length != 0)
                {
                    return right.EntityPath();
                }
            }

            throw new NullReferenceException("A non-existent entity path was detected.");
        }

        /// <summary>
        /// Gets or sets whether this conflict should be ignored.
        /// </summary>
        public bool IgnoreConflict
        {
            get
            {
                return left.Ignored();
            }
            set
            {
                left.Ignored(value);
            }
        }
		
		/// <summary>
		/// Gets the relative  for conflicts
		/// </summary>
		/// <returns></returns>
		public string OffendingPath{
			get{
			 return GetOffendingPath();	
			}
		}
        /// <summary>
        /// Gets or sets the suggested action for this conflict.
        /// This property will contain a suggested action when the system fails to automatically resolve a conflict.
        /// It acts as a hint for the UI to prompt the user with a default action.
        /// </summary>
        public Action SuggestedAction
        {
            get
            {
                return this.suggestedAction;
            }
            set
            {
                this.suggestedAction = value;
            }
        }

        /// <summary>
        /// Gets the partnership that is associated with this conflict.
        /// </summary>
        /// <returns>A Partnership object that this conflict refers to.</returns>
        public Partnership GetPartnership()
        {
            return left.GetParentPartnership();
        }

        /// <summary>
        /// Sets the status monitor on the syncables
        /// </summary>
        /// <param name="monitor"></param>
        public void SetStatusMonitor(SyncableStatusMonitor monitor)
        {
            left.SetStatusMonitor(monitor);
            right.SetStatusMonitor(monitor);
        }

        /// <summary>
        /// Gets or sets the action to be performed when this conflict can be resolved automatically.
        /// It will return unknown if the system could not find a solution automatically.
        /// </summary>
        public Action AutoResolveAction
        {
            get
            {
                return autoResolveAction;
            }
            set
            {
                this.autoResolveAction = value;
            }
        }

        /// <summary>
        /// Attempts to resolve a conflict based on the recommended action.
        /// </summary>
        /// <returns>A Resolved object containing the details of the resolution.</returns>
        public Resolved Resolve()
        {
            if (autoResolveAction == Action.Unknown) return Resolve(userActions.SelectedAction.ResolutionAction);
            else return Resolve(autoResolveAction);
        }

        /// <summary>
        /// Attempts to resolve a conflict based on a specified user action.
        /// </summary>
        /// <returns>true if the conflict was successfully resolved, false otherwise.</returns>
        /// <exception cref="ArgumentException">This exception is generated when an invalid user action is passed into the method.</exception>
        public Resolved Resolve(Action user)
        {
            switch (user) {
                case Action.CopyToLeft : 
                    right.CopyTo(left);
                    return new Resolved(left, right, Resolved.ActionDone.CopyFromRight);
                case Action.DeleteLeft : 
                    left.Delete(true);
                    return new Resolved(left, right, Resolved.ActionDone.DeleteLeft);
                case Action.Merge:
                    left.Merge(right);
                    return new Resolved(left, right, Resolved.ActionDone.Merged);
                case Action.CopyToRight:
                    left.CopyTo(right);
                    return new Resolved(left, right, Resolved.ActionDone.CopyFromLeft);
                case Action.DeleteRight:
                    right.Delete(true);
                    return new Resolved(left, right, Resolved.ActionDone.DeleteRight);
                case Action.Ignore:
                    return new Resolved(left, right, Resolved.ActionDone.Ignored);
                default:
                    throw new System.ArgumentException("Invalid User Action");

            }
        }
    }
}