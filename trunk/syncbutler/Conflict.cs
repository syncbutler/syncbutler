using System;
using System.Collections.Generic;
using SyncButler.Exceptions;

namespace SyncButler
{
    /// <summary>
    /// Represents a conflict detected in the sync. The state can be
    /// CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Unknown.
    /// </summary>
    public class Conflict
    {
        protected internal ISyncable left;
        protected internal ISyncable right;
        protected Action RecommendedAction;

        //private enum StatusOptions {Resolved, Unresolved, Resolving}

        public enum Action { CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight, Unknown };

        public Conflict(ISyncable left, ISyncable right, Action RecommendedAction)
        {
            this.left = left;
            this.right = right;
            this.RecommendedAction = RecommendedAction;
        }

        public Action GetRecommendedAction() { return RecommendedAction; }

        /// <summary>
        /// Attempts to rsolve a conflict based on th recommended action.
        /// </summary>
        /// <returns></returns>
        public Error Resolve()
        {
            if (this.RecommendedAction == Action.Unknown) throw new InvalidActionException();
            return this.Resolve(this.RecommendedAction);
        }

        /// <summary>
        /// Attempts to resolve a conflict based on a specified user action.
        /// </summary>
        /// <returns>true if the conflict was successfully resolved, false otherwise.</returns>
        /// <exception cref="ArgumentException">This exception is generated when an invalid user action is passed into the method.</exception>
        public Error Resolve(Action user)
        {
            Error ret;

            switch (user) {
                case Action.CopyToLeft : 
                    {
                        ret = right.CopyTo(left);
                        if (ret == Error.NoError) right.UpdateStoredChecksum();
                        break;
                    }
                case Action.DeleteLeft : 
                    {
                        ret = left.Delete();
                        if (ret == Error.NoError) left.RemoveStoredChecksum();
                        break;
                    }
                case Action.Merge:
                    {
                        ret = left.Merge(right);
                        if (ret == Error.NoError) left.UpdateStoredChecksum();
                        break;
                    }
                case Action.CopyToRight:
                    {
                        ret = left.CopyTo(right);
                        if (ret == Error.NoError) left.UpdateStoredChecksum();
                        break;
                    }
                case Action.DeleteRight:
                    {
                        ret = right.Delete();
                        if (ret == Error.NoError) right.RemoveStoredChecksum();
                        break;
                    }
                default:
                    {
                        throw new System.ArgumentException("Invalid User Action");
                        //return false;
                    }

            }

            return ret;
        }

        public override String ToString()
        {
            return left.EntityPath() + "\n" + this.RecommendedAction + "";
        }
    }
}