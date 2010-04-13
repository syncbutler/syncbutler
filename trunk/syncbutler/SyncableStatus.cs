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

namespace SyncButler
{
    /// <summary>
    /// This class is used to report the status of the sync to the GUI. Largely for progress
    /// for a particular sync.
    /// </summary>
    public class SyncableStatus
    {
        public enum ActionType {
            Copy,
            Delete,
            Merge,
            Checksum,
            Sync
        };

        protected string _entityPath;
        protected int _percentComplete;
        protected int _curTaskPercentComplete;
        protected ActionType _actionType;

        /// <summary>
        /// Creates the SyncableStatus with the required information
        /// </summary>
        /// <param name="entityPath">The object/node currently being worked on</param>
        /// <param name="percentComplete">The overall percentage of the task which is complete</param>
        /// <param name="curTaskPercentComplete">The percentage of the current task which is complete</param>
        /// <param name="actionType">The current action being performed</param>
        public SyncableStatus(string entityPath, int percentComplete, int curTaskPercentComplete, ActionType actionType)
        {
            this._entityPath = entityPath;
            this._percentComplete = percentComplete;
            this._curTaskPercentComplete = curTaskPercentComplete;
            this._actionType = actionType;
        }

        /// <summary>
        /// A string that represents the object currently being processed
        /// (ie. relative path file/folder)
        /// </summary>
        public string EntityPath
        {
            get
            {
                return _entityPath;
            }
        }

        /// <summary>
        /// Gets a human-readable form of the entity path.
        /// </summary>
        public string FriendlyEntityPath
        {
            get
            {
                return GetFriendlyEntityPath();
            }
        }

        /// <summary>
        /// Gets a human-readable form of the entity path.
        /// </summary>
        /// <returns>A string with the human-readable form of the entity path.</returns>
        public string GetFriendlyEntityPath()
        {
            string path = _entityPath;

            if (path.ToLower().StartsWith(@"folder:\\"))
            {
                path = path.Replace(@"folder:\\", "Folder: ");
            }
            else if (path.ToLower().StartsWith(@"file:\\"))
            {
                path = path.Replace(@"file:\\", "File: ");
            }

            return path;
        }

        /// <summary>
        /// Percentage of work completed.
        /// NOTE: Not implemented in the ISyncables yet!
        /// </summary>
        public int percentComplete
        {
            get
            {
                return _percentComplete;
            }
        }

        /// <summary>
        /// Percentage of the work on the current subtask (as given in
        /// curObject) is complete.
        /// </summary>
        public int curTaskPercentComplete
        {
            get
            {
                return _curTaskPercentComplete;
            }
        }

        /// <summary>
        /// The type of action currently being performed
        /// </summary>
        public ActionType actionType
        {
            get
            {
                return _actionType;
            }
        }
    }
}
