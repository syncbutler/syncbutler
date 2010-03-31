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
        public ResolveActionSet userActions { get; set; }

        /// <summary>
        /// Possible actions for conflict resolution.
        /// </summary>
        public enum Action { CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Ignore, Unknown };

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
            }
            else if (!right.Exists())
            {
                userActions.AddAction(Action.CopyToRight);
                userActions.AddAction(Action.DeleteLeft);
            }
            else
            {
                userActions.AddAction(Action.CopyToLeft);
                userActions.AddAction(Action.CopyToRight);
             }

            userActions.AddAction(Action.Ignore);

            userActions.SetSelectedAction(suggestedAction);
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
            userActions.SetSelectedAction(suggestedAction);
        }

        public string OffendingPath
        {
            get
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
                throw new NullReferenceException("Non Existance EntityPath");
            }
        }

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
        /// Gets/Sets the suggested action for this conflict.
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
        /// Gets/Sets the action to be performed when this conflict can be resolved automatically.
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
                    //right.UpdateStoredChecksum();
                    return new Resolved(left, right, Resolved.ActionDone.CopyFromRight);
                case Action.DeleteLeft : 
                    left.Delete(true);
                    //left.RemoveStoredChecksum();
                    return new Resolved(left, right, Resolved.ActionDone.DeleteLeft);
                case Action.Merge:
                    left.Merge(right);
                    //left.UpdateStoredChecksum();
                    return new Resolved(left, right, Resolved.ActionDone.Merged);
                case Action.CopyToRight:
                    left.CopyTo(right);
                    //left.UpdateStoredChecksum();
                    return new Resolved(left, right, Resolved.ActionDone.CopyFromLeft);
                case Action.DeleteRight:
                    right.Delete(true);
                    //right.RemoveStoredChecksum();
                    return new Resolved(left, right, Resolved.ActionDone.DeleteRight);
                case Action.Ignore:
                    return new Resolved(left, right, Resolved.ActionDone.Ignored);
                default:
                    throw new System.ArgumentException("Invalid User Action");

            }
        }

        public override String ToString()
        {
            return left.EntityPath() + "\n" + this.autoResolveAction + "";
        }
    }
}