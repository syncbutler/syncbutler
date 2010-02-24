using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a conflict detected in the sync.
    /// </summary>
    public class Conflict
    {
        protected internal ISyncable left;
        protected internal ISyncable right;
        protected Action RecommendedAction;

        private enum StatusOptions {Resolved, Unresolved, Resolving}
        private StatusOptions status;

        public enum Action { CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Unknown };

        public Conflict(ISyncable left, ISyncable right, Action RecommendedAction)
        {
            this.left = left;
            this.right = right;
            this.RecommendedAction = RecommendedAction;
            this.status = StatusOptions.Unresolved;
        }

        public Action GetRecommendedAction() { return RecommendedAction; }

        /// <summary>
        /// Not implemented. Attempts to resolve a conflict based on a specified user action.
        /// </summary>
        /// <returns>true if the conflict was successfully resolved, false otherwise.</returns>
        /// <exception cref="ArgumentException">This exception is generated when an invalid user action is passed into the method.</exception>
        public Object Resolve(Action user)
        {
            switch (user) {
                case Action.CopyToLeft : 
                    {
                        return right.CopyTo(left);
                    }
                case Action.DeleteLeft : 
                    {
                        return left.Delete();           
                    }
                case Action.Merge:
                    {
                        return left.Merge(right);
                    }
                case Action.CopyToRight:
                    {
                        return left.CopyTo(right);
                    }
                case Action.DeleteRight:
                    {
                        return right.Delete();
                    }
                default:
                    {
                        throw new System.ArgumentException("Invalid User Action");
                        //return false;
                    }

            }

        }
    }
}
/*
 * Conflict

Attributes:

left:Syncables
right:Syncables
status:Enum
Methods:

Resolve(user:Action):boolean
 */
