using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;
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
        /// Unserialize this class
        /// </summary>
        /// <param name="xmlData"></param>
        public Partnership(XmlReader xmlData)
        {
            hashDictionary = new Dictionary<string, long>();

            xmlData.Read();
            if (xmlData.Name != "Partnership") throw new InvalidDataException("This is not data for a Partnership");
            
            this.name = xmlData.GetAttribute("name");
            if (this.name == null) throw new InvalidDataException("Partnership is missing the name");

            ISyncable[] pair = new ISyncable[2];
            int curPair = 0;
            
            while (xmlData.Read())
            {
                if (xmlData.NodeType != XmlNodeType.Element) continue;

                if (xmlData.Name == "Pair")
                {
                    while (xmlData.Read())
                    {
                        if (xmlData.NodeType == XmlNodeType.Element)
                        {
                            if (curPair > 1) throw new InvalidDataException("Too many children under Pair");
                            try
                            {
                                pair[curPair] = (ISyncable)SyncEnvironment.ReflectiveUnserialize(xmlData.ReadOuterXml());
                                curPair++;
                            }
                            catch (InvalidCastException e)
                            {
                                throw new InvalidDataException("The nodes under Pair was not an ISyncable");
                            }
                        }
                    }

                    break;
                }
                else xmlData.Skip();
            }

            if (curPair != 2) throw new InvalidDataException("Missing node under Pair");

            this.left = pair[0];
            this.right = pair[1];
        }

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

        public void SerializeXML(XmlWriter xmlData)
        {
            xmlData.WriteStartElement("Partnership");
            xmlData.WriteAttributeString("name", this.name);
            // ---
            xmlData.WriteStartElement("Pair");
            left.SerializeXML(xmlData);
            right.SerializeXML(xmlData);
            xmlData.WriteEndElement();
            // ---
            xmlData.WriteEndElement();
        }

        public string Serialize()
        {
            StringWriter output = new StringWriter();
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.OmitXmlDeclaration = true;
            xmlSettings.Indent = true;
            XmlWriter xmlData = XmlWriter.Create(output, xmlSettings);

            xmlData.WriteStartDocument();
            SerializeXML(xmlData);
            xmlData.WriteEndDocument();
            xmlData.Close();

            return output.ToString();
        }
    }
}
