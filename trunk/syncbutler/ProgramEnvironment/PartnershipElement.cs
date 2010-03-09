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
        protected Partnership partnershipObject = null;

        public Partnership obj
        {
            get
            {
                return partnershipObject;
            }
        }

        public PartnershipElement(Partnership elem)
        {
            partnershipObject = elem;
            friendlyName = elem.Name;
            data = partnershipObject.Serialize();
        }

        public PartnershipElement()
        {
            friendlyName = "";
            data = "";
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

        [ConfigurationProperty("data")]
        public string data
        {
            get
            {
                return (string)this["data"];
            }
            set
            {
                this["data"] = value;
            }
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
            base.DeserializeElement(reader, serializeCollectionKey);
            partnershipObject = (Partnership)SyncEnvironment.ReflectiveUnserialize(data);
        }
    }
}
