using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a conflict detected in the sync.
    /// </summary>
    class Conflict
    {
        protected ISyncable left;
        protected ISyncable right;
        private enum StatusOptions {Resolved, Unresolved, Resolving}
        protected StatusOptions status;

        public enum Action { CopyToLeft, DeleteLeft, Merge, CopyToRight, DeleteRight };
        /// <summary>
        /// Not implemented. Attempts to resolve a conflict based on a specified user action.
        /// </summary>
        /// <returns>true if the conflict was successfully resolved, false otherwise.</returns>
        /// <exception cref="ArgumentException">This exception is generated when an invalid user action is passed into the method.</exception>
        public Boolean Resolve(Action user)
        {
            switch (user) {
                case Action.CopyToLeft : 
                    {
                        return left.Copy(right);
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
                        return right.Copy(left);
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
