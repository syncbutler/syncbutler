using System;

namespace SyncButler
{
    /// <summary>
    /// This class is used to report the status of the sync to the GUI. Largely for progress
    /// for a particular sync.
    /// </summary>
    public class SyncableStatus
    {
        protected string _curObject;
        protected int _percentComplete;

        public SyncableStatus(string curObject, int percentComplete)
        {
            this._curObject = curObject;
            this._percentComplete = percentComplete;
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
    }
}
