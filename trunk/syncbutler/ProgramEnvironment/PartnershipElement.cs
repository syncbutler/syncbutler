using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
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

        protected override void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey)
        {
            base.DeserializeElement(reader, serializeCollectionKey);
            partnershipObject = (Partnership)SyncEnvironment.ReflectiveUnserialize(data);
        }
    }
}
