using System;
using System.Collections.Generic;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    /// <summary>
    /// This class is a XML descriptor for a Partnership object. Only the key is stored
    /// directly, the rest of the data is serialised by the Partnership class itself to
    /// reduce coupling
    /// </summary>
    public class PartnershipElement : ConfigurationElement
    {
        //protected Partnership partnershipObject = null;

        /* public Partnership obj
        {
            get
            {
                return partnershipObject;
            }
        } */

        public PartnershipElement(Partnership elem)
        {
            //partnershipObject = elem;
            obj = elem;
            friendlyName = elem.Name;
        }

        public PartnershipElement()
        {
            friendlyName = "";
            obj = null;
        }

        [ConfigurationProperty("friendlyName")]
        public string friendlyName
        {
            get
            {
                return (string)this["friendlyName"];
            }
            set
            {
                this["friendlyName"] = value;
            }
        }

        [ConfigurationProperty("obj")]
        public Partnership obj
        {
            get
            {
                return (Partnership)this["obj"];
            }
            set
            {
                this["obj"] = value;
            }
        }

        /// <summary>
        /// Serializes the partnership using Partnership.SerializeXML instead of the default
        /// ConfigurationElement.SerializaElement()
        /// </summary>
        /// <param name="writer">Required by XML Configurations to read XML Data</param>
        /// <param name="serializeCollectionKey">Required format descriptor by XML Configurations
        /// to read XML Data</param>
        /// <returns>value indicating whether there is data to serialize</returns>
        protected override bool SerializeElement(System.Xml.XmlWriter writer, bool serializeCollectionKey)
        {
            if (writer != null)
            {
                obj.SerializeXML(writer);
                return true;
            }
            else return true;
        }

        /// <summary>
        /// Instead of the using Deserialisze provided for by the XML Configuration,
        /// this method will directly deserialize the XML and immediately construct the Partnership
        /// object
        /// </summary>
        /// <param name="reader">Required by XML Configurations to read XML Data</param>
        /// <param name="serializeCollectionKey">Required format descriptor by XML Configurations
        /// to read XML Data</param>
        protected override void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey)
        {
            if (reader.Name != "add") return;
            
            obj = (Partnership)SyncEnvironment.ReflectiveUnserialize(reader.ReadInnerXml());
            friendlyName = obj.Name;

            //while (!((reader.NodeType == System.Xml.XmlNodeType.EndElement)
            //    && (reader.Name == "add"))) reader.Read();
        }
    }
}
