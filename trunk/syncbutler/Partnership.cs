using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace SyncButler
{
    /// <summary>
    /// A representation of a partnership between two ISyncable, left & right
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
        /// Friendly name of the sync partnership
        /// </summary>
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

        /// <summary>
        /// This allows the GUI to call back to the Partnership
        /// </summary>
        public SyncableStatusMonitor statusMonitor = null;

        /// <summary>
        /// A dictionary of the hash values from the last sync.
        /// May be empty.
        /// </summary>
        protected internal Dictionary<string, long> hashDictionary;

        /// <summary>
        /// Unserialize this class
        /// </summary>
        /// <param name="xmlData">Constructs a Partnership object from serialised XML Data</param>
        public Partnership(XmlReader xmlData)
        {
            hashDictionary = new Dictionary<string, long>();

            xmlData.Read();
            if (xmlData.Name != "Partnership") throw new InvalidDataException("This is not Data for a Partnership");
            
            this.name = xmlData.GetAttribute("name");
            if (this.name == null) throw new InvalidDataException("Partnership is Missing the (Friendly) Name");

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
                            if (curPair > 1) throw new InvalidDataException("Too Many Children Node Under Pair");
                            try
                            {
                                pair[curPair] = (ISyncable)SyncEnvironment.ReflectiveUnserialize(xmlData.ReadOuterXml());
                                curPair++;
                            }
                            catch (InvalidCastException e)
                            {
                                throw new InvalidDataException("The Nodes Under Pair was not an ISyncable", e);
                            }
                        }
                    }

                    break;
                }
                else xmlData.Skip();
            }

            if (curPair != 2) throw new InvalidDataException("Missing Node under Pair");

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
        /// <param name="syncable">ISyncable object that needs its record (hash) retrieved</param>
        /// <returns>The last known checksum of the ISyncable</returns>
        /// <exception cref="SyncableNotExistsException">The dictionary does not have the last checksum of this Syncable</exception>
        public long GetLastChecksum(ISyncable syncable)
        {
            string key = this.name + ":" + syncable.EntityPath();
            if (!hashDictionary.ContainsKey(key)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[key];
        }

        /// <summary>
        /// (Overloaded) Retrieves the last known checksum from the dictionary
        /// </summary>
        /// <param name="entityPath">An "entityPath" (see SyncEnvironment) representation that needs its record (hash) retrieved</param>
        /// <returns>The last known checksum of the ISyncable</returns>
        /// <exception cref="SyncableNotExistsException">The dictionary does not have the last checksum of this Syncable</exception>
        public long GetLastChecksum(string entityPath)
        {
            string key = this.name + ":" + entityPath;
            if (!hashDictionary.ContainsKey(key)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[key];
        }

        /// <summary>
        /// Checks whether the given ISyncable has an entry in the checksum dictionary
        /// </summary>
        /// <param name="syncable">ISyncable object that needs to check if its record (hash) is in the dictionary</param>
        /// <returns>True if found, False otherwise</returns>
        public bool ChecksumExists(ISyncable syncable)
        {
            return hashDictionary.ContainsKey(this.name + ":" + syncable.EntityPath());
        }

        /// <summary>
        /// (Overloaded) Checks whether the given ISyncable has an entry in the checksum dictionary
        /// </summary>
        /// <param name="entityPath">An "entityPath" (see SyncEnvironment) representation that needs to check if its
        /// record (hash) is in the dictionary</param>
        /// <returns>True if found, False otherwise</returns>
        public bool ChecksumExists(string entityPath)
        {
            return hashDictionary.ContainsKey(this.name + ":" + entityPath);
        }

        /// <summary>
        /// Adds/Updates the checksum dictionary for the given ISyncable
        /// </summary>
        /// <param name="syncable">ISyncable object that needs to update if its record (hash) is in the dictionary</param>
        public void UpdateLastChecksum(ISyncable syncable)
        {
            string key = this.name + ":" + syncable.EntityPath();

            if (hashDictionary.ContainsKey(key)) hashDictionary[key] = syncable.Checksum();
            else hashDictionary.Add(key, syncable.Checksum());
        }

        /// <summary>
        /// Removes the checksum dictionary for the given ISyncable. Used when the file is deleted
        /// permanently on both sides.
        /// </summary>
        /// <param name="syncable">The ISyncable object that gave raise to this checksum in the first place</param>
        public void RemoveChecksum(ISyncable syncable)
        {
            hashDictionary.Remove(this.name + ":" + syncable.EntityPath());
        }

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
        /// Removes orphaned checksums from the dictionary. This happens when the file
        /// is deleted on both side and the sync do not encounter the file during checking.
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
        
        /// <summary>
        /// Calls the toString method of the left and right ISyncables
        /// </summary>
        /// <returns>Gives a string representation of the left and right ISyncables</returns>
        public override String ToString()
        {
            return left.ToString() + " <-> " + right.ToString();
        }

        /// <summary>
        /// This method serialises attributes of the class into a XML format
        /// </summary>
        /// <param name="xmlData">Writes directly to the XML Container</param>
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

        /// <summary>
        /// This method describes the XML format to serialise itself
        /// </summary>
        /// <returns>The format that can be understand by XML configuration class</returns>
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