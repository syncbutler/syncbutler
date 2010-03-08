using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

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
        /// Retrieves the last known checksum from the dictionary
        /// </summary>
        /// <param name="syncable"></param>
        /// <returns>The last known checksum of the ISyncable</returns>
        /// <exception cref="SyncableNotExistsException">The dictionary does not have the last checksum of this Syncable</exception>
        public long GetLastChecksum(ISyncable syncable)
        {
            string key = this.name + ":" + syncable.EntityPath();
            if (!hashDictionary.ContainsKey(key)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[key];
        }

        public long GetLastChecksum(string entityPath)
        {
            string key = this.name + ":" + entityPath;
            if (!hashDictionary.ContainsKey(key)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[key];
        }

        /// <summary>
        /// Checks whether the given syncable has an entry in the checksum dictionary
        /// </summary>
        /// <param name="syncable"></param>
        /// <returns></returns>
        public bool ChecksumExists(ISyncable syncable)
        {
            return hashDictionary.ContainsKey(this.name + ":" + syncable.EntityPath());
        }

        public bool ChecksumExists(string entityPath)
        {
            return hashDictionary.ContainsKey(this.name + ":" + entityPath);
        }

        /// <summary>
        /// Adds/Updates the checksum dictionary
        /// </summary>
        /// <param name="syncable"></param>
        public void UpdateLastChecksum(ISyncable syncable)
        {
            string key = this.name + ":" + syncable.EntityPath();

            if (hashDictionary.ContainsKey(key)) hashDictionary[key] = syncable.Checksum();
            else hashDictionary.Add(key, syncable.Checksum());
        }

        public void RemoveChecksum(ISyncable syncable)
        {
            hashDictionary.Remove(this.name + ":" + syncable.EntityPath());
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

        /// <summary>
        /// Removes orphaned checksums from the dictionary
        /// </summary>
        public void CleanOrphanedChecksums()
        {
            ChecksumKey key;
            List<string> toDelete = new List<string>();

            foreach (string skey in hashDictionary.Keys)
            {
                key = SyncEnvironment.DecodeChecksumKey(skey);
                if (key.partnershipName != this.name) continue;

                ISyncable leftChild = left.CreateChild(key.entityPath);
                ISyncable rightChild = right.CreateChild(key.entityPath);

                if (!(leftChild.Exists() || rightChild.Exists())) toDelete.Add(skey);
            }

            foreach (string skey in toDelete) hashDictionary.Remove(skey);

        }
        
        public override String ToString()
        {
            return left.ToString() + " <-> " + right.ToString();
        }
    }
}
