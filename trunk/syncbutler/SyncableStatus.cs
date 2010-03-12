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

        protected string _curObject;
        protected int _percentComplete;
        protected int _curTaskPercentComplete;
        protected ActionType _actionType;

        /// <summary>
        /// Creates the SyncableStatus with the required information
        /// </summary>
        /// <param name="curObject">The object/node currently being worked on</param>
        /// <param name="percentComplete">The overall percentage of the task which is complete</param>
        /// <param name="curTaskPercentComplete">The percentage of the current task which is complete</param>
        /// <param name="actionType">The current action being performed</param>
        public SyncableStatus(string curObject, int percentComplete, int curTaskPercentComplete, ActionType actionType)
        {
            this._curObject = curObject;
            this._percentComplete = percentComplete;
            this._curTaskPercentComplete = curTaskPercentComplete;
        }

        /// <summary>
        /// A string that represents the object currently being processed
        /// (ie. relative path file/folder)
        /// </summary>
        public string curObject
        {
            get
            {
                return _curObject;
            }
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
