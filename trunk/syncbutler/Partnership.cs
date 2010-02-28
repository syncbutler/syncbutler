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
        protected internal string rightFullPath;
        /// <summary>
        /// left full path
        /// </summary>
        protected internal string leftFullPath;

        /// <summary>
        /// A dictionary of the hash values from the last sync.
        /// May be empty.
        /// </summary>
        protected internal Dictionary<string, long> hashDictionary;

        /// <summary>
        /// initialize the partership
        /// </summary>
        /// <param name="left">left side of the syncable</param>
        /// <param name="right">right side of the syncable</param>
        public Partnership( string leftFullPath, ISyncable left, 
                            string rightFullPath, ISyncable right,
                            Dictionary<string, long> hashDictionary)
        {
            this.left = left;
            this.right = right;
            this.leftFullPath = leftFullPath;
            this.rightFullPath = rightFullPath;
            this.hashDictionary = hashDictionary;
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
            left.SetParentPartnership(this);
            right.SetParentPartnership(this);
            return left.Sync(right);
        }

        public override String ToString()
        {
            return leftFullPath + " <-> " + rightFullPath;
        }
    }
}
