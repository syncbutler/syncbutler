using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler
{
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
