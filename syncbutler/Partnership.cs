﻿// Developer to contact: Tan Chee Eng
/*****************************************************************************/
// Copyright 2010 Sync Butler and its original developers.
// This file is part of Sync Butler (http://www.syncbutler.org).
// 
// Sync Butler is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sync Butler is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sync Butler.  If not, see <http://www.gnu.org/licenses/>.
//
/*****************************************************************************/

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
        public SyncableStatusMonitor statusMonitor;

        /// <summary>
        /// This allows the GUI to report errors and attempt to continue from there
        /// </summary>
        public SyncableErrorHandler errorHandler;

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

            while ((xmlData.NodeType != XmlNodeType.Element) && (xmlData.Name != "Partnership"))
            {
                if (!(xmlData.Read())) throw new InvalidDataException();
            }
            
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

                        if ((xmlData.NodeType == XmlNodeType.EndElement) &&
                            (xmlData.Name == "Pair"))
                        {
                            if (curPair != 2) throw new InvalidDataException("Missing Node under Pair");
                            this.left = pair[0];
                            this.right = pair[1];
                            break;
                        }
                    }
                }
                else if (xmlData.Name == "Checksums")
                {
                    while (xmlData.Read())
                    {
                        if ((xmlData.NodeType == XmlNodeType.Element) &&
                            (xmlData.Name == "Entry"))
                        {
                            string key = xmlData.GetAttribute("key");
                            long value;

                            try
                            {
                                value = long.Parse(xmlData.GetAttribute("value"));
                            }
                            catch (FormatException)
                            {
                                throw new InvalidDataException("Invalid value in the checksum dictionary");
                            }

                            hashDictionary.Add(key, value);
                        }

                        if ((xmlData.NodeType == XmlNodeType.EndElement) &&
                            (xmlData.Name == "Checksums")) break;
                    }
                }
                else if ((xmlData.NodeType == XmlNodeType.EndElement) &&
                            (xmlData.Name == "Partnership")) break;
                else xmlData.Skip();
            }
        }

        /// <summary>
        /// initialize the partnership
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
            string key = syncable.EntityPath();
            if (!hashDictionary.ContainsKey(key)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[key];
        }

        /// <summary>
        /// Gets the descriptive element of the partnership given a root directory.
        /// e.g. If the root directory is the left element of this partnership, then 'Folder 1' is returned.
        /// </summary>
        /// <param name="rootdir">The root directory to check.</param>
        /// <returns>A string describing the element matched, or empty string if not found.</returns>
        public string GetPartnershipElem(string rootdir)
        {
            string elem = "";

            if (rootdir.TrimEnd('\\').Equals(this.LeftFullPath.TrimEnd('\\')))
                elem = "Folder 1";
            else if (rootdir.TrimEnd('\\').Equals(this.RightFullPath.TrimEnd('\\')))
                elem = "Folder 2";

            return elem;
        }

        /// <summary>
        /// (Overloaded) Retrieves the last known checksum from the dictionary
        /// </summary>
        /// <param name="entityPath">An "entityPath" (see SyncEnvironment) representation that needs its record (hash) retrieved</param>
        /// <returns>The last known checksum of the ISyncable</returns>
        /// <exception cref="SyncableNotExistsException">The dictionary does not have the last checksum of this Syncable</exception>
        public long GetLastChecksum(string entityPath)
        {
            if (!hashDictionary.ContainsKey(entityPath)) throw new Exceptions.SyncableNotExistsException();

            return hashDictionary[entityPath];
        }

        /// <summary>
        /// Checks whether the given ISyncable has an entry in the checksum dictionary
        /// </summary>
        /// <param name="syncable">ISyncable object that needs to check if its record (hash) is in the dictionary</param>
        /// <returns>True if found, False otherwise</returns>
        public bool ChecksumExists(ISyncable syncable)
        {
            return hashDictionary.ContainsKey(syncable.EntityPath());
        }

        /// <summary>
        /// (Overloaded) Checks whether the given ISyncable has an entry in the checksum dictionary
        /// </summary>
        /// <param name="entityPath">An "entityPath" (see SyncEnvironment) representation that needs to check if its
        /// record (hash) is in the dictionary</param>
        /// <returns>True if found, False otherwise</returns>
        public bool ChecksumExists(string entityPath)
        {
            return hashDictionary.ContainsKey(entityPath);
        }

        /// <summary>
        /// Adds/Updates the checksum dictionary for the given ISyncable
        /// </summary>
        /// <param name="syncable">ISyncable object that needs to update if its record (hash) is in the dictionary</param>
        public void UpdateLastChecksum(ISyncable syncable)
        {
            string key = syncable.EntityPath();

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
            hashDictionary.Remove(syncable.EntityPath());
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
            left.SetErrorHandler(errorHandler);
            right.SetErrorHandler(errorHandler);

            left.PrepareSync();
            right.PrepareSync();
            return left.Sync(right);
        }

        /// <summary>
        /// Removes orphaned checksums from the dictionary. This happens when the file
        /// is deleted on both side and the sync do not encounter the file during checking.
        /// </summary>
        public void CleanOrphanedChecksums()
        {
            List<string> toDelete = new List<string>();

            foreach (string skey in hashDictionary.Keys)
            {
                ISyncable leftChild = left.CreateChild(skey);
                ISyncable rightChild = right.CreateChild(skey);

                if (!(leftChild.Exists() || rightChild.Exists())) toDelete.Add(skey);
            }

            foreach (string skey in toDelete) hashDictionary.Remove(skey);
        }
        
        /// <summary>
        /// Returns the name of this partnership
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return Name;
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
            xmlData.WriteStartElement("Checksums");
            foreach (KeyValuePair<string, long> element in hashDictionary)
            {
                xmlData.WriteStartElement("Entry");
                xmlData.WriteAttributeString("key", element.Key);
                xmlData.WriteAttributeString("value", element.Value.ToString());
                xmlData.WriteEndElement();
            }
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