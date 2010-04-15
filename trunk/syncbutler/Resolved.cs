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
    public class Resolved
    {
        private ISyncable left;
        private ISyncable right;
        private ActionDone action;

        /// <summary>
        /// Returns a string representation of the left ISyncable
        /// </summary>
        public string Left
        {
            get
            {
                if (action == ActionDone.DeleteRight)
                    return "-";
                return this.left.ToString();
            }
            set
            {

            }
        }

        /// <summary>
        /// Returns a string representation of the right ISyncable
        /// </summary>
        public string Right
        {
            get
            {
                if (action == ActionDone.DeleteLeft)
                    return "-";
                return this.right.ToString();
            }
            set
            {

            }
        }

        /// <summary>
        /// Returns a string representation of the action of the resolution
        /// </summary>
        public string Action
        {
            get
            {
                switch (action)
                {
                    case ActionDone.CopyFromLeft:
                        return "copied to";
                    case ActionDone.CopyFromRight:
                        return "copied from";
                    case ActionDone.DeleteBoth:
                        return "deleted with";
                    case ActionDone.DeleteLeft:
                    case ActionDone.DeleteRight:
                        return "deleted";
                    case ActionDone.Merged:
                        return "merged with";
                }
                return "no action done";

            }
            set
            {

            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="left">ISyncable</param>
        /// <param name="right">ISyncable</param>
        /// <param name="action">Action taken to resolve the conflict</param>
        public Resolved(ISyncable left, ISyncable right, ActionDone action)
        {
            this.left = left;
            this.right = right;
            this.action = action;
        }

        /// <summary>
        /// Enumeration of possible actions
        /// </summary>
        public enum ActionDone
        { DeleteLeft, DeleteRight, DeleteBoth, CopyFromLeft, CopyFromRight, Merged, Ignored }

        /// <summary>
        /// Converts this Resolved object into a readable string
        /// </summary>
        /// <returns>a string representation of this object</returns>
        public override string ToString()
        {
            switch (action)
            {
                case ActionDone.CopyFromLeft:
                    return Left + " copied to " + Right;
                case ActionDone.CopyFromRight:
                    return Right + " copied to " + Left;
                case ActionDone.DeleteBoth:
                    return Left + " and " + Right + " have been deleted";
                case ActionDone.DeleteLeft:
                    return Left + " has been deleted";
                case ActionDone.DeleteRight:
                    return Right + " has been deleted";
                case ActionDone.Merged:
                    return Right + " and " + Left + " have been merged";
            }
            return "No Action has been done for " + Left + " and " + Right; 
        }
    }
}
