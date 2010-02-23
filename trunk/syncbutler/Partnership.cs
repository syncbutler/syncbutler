using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace SyncButler
{
    /// <summary>
    /// A representation of a partnership between two syncable, left & right
    /// </summary>
    public class Partnership
    {
        /// <summary>
        /// left side of the syncable
        /// </summary>
        private ISyncable left;
        /// <summary>
        /// right side of the syncable
        /// </summary>
        private ISyncable right;

        /// <summary>
        /// right full path
        /// </summary>
        private string rightFullPath;
        /// <summary>
        /// left full path
        /// </summary>
        private string leftFullPath;

        /// <summary>
        /// initialize the partership
        /// </summary>
        /// <param name="left">left side of the syncable</param>
        /// <param name="right">right side of the syncable</param>
        public Partnership(ISyncable left, ISyncable right)
        {
            this.left = left;
            this.right = right;
        }

        /// <summary>
        /// Attempts to sync this partnership
        /// </summary>
        /// <returns>Null on no conflicts, else a list of conflicts.</returns>
        /// <summary>
        /// Attempts to sync this partnership
        /// </summary>
        /// <returns>Null on no conflicts, else a list of conflicts.</returns>
        public List<Conflict> Sync()
        {
            Trace.Assert(false, "not fully implemented");

            List<Conflict> conflictList = new List<Conflict>();

            // Check starting with the left ISyncable

            // Check for ISyncables missed from the right ISyncable

            return conflictList;
        }

        protected List<Conflict> Sync(ISyncable left, ISyncable right)
        {
            return null;
        }

    }
}
