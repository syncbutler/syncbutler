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

        private string name;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public string LeftFullPath
        {
            get
            {
                return this.left.ToString();
            }
        }

        public string RightFullPath
        {
            get
            {
                return this.right.ToString();
            }
        }

        public SyncableStatusMonitor statusMonitor = null;

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
        public Partnership(String name, ISyncable left, ISyncable right,
                            Dictionary<string, long> hashDictionary)
        {
            this.name = name;
            this.left = left;
            this.right = right;
            if (hashDictionary == null)
            {
                this.hashDictionary = new Dictionary<string, long>();
            }
            else
            {
                this.hashDictionary = hashDictionary;
            }
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
            left.SetStatusMonitor(statusMonitor);
            right.SetStatusMonitor(statusMonitor);
            return left.Sync(right);
        }
        
        public override String ToString()
        {
            return left.ToString() + " <-> " + right.ToString();
        }
    }
}
