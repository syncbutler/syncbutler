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

        public string curObject
        {
            get
            {
                return _curObject;
            }
        }

        public int percentComplete
        {
            get
            {
                return _percentComplete;
            }
        }
    }
}
