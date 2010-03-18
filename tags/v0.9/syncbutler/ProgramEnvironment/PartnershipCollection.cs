using System;
using System.Collections.Generic;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    /// <summary>
    /// This class is used to wrap collection typed variables in XML descriptors.
    /// In this case, PartnershipList is represented by this entire list
    /// </summary>
    public class PartnershipCollection : ConfigurationElementCollection
    {
        public PartnershipElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as PartnershipElement;
            }
            set
            {
                if (base.BaseGet(index) != null) base.BaseRemoveAt(index);
                this.BaseAdd(index, value);
            }
        }

        public void Clear()
        {
            base.BaseClear();
        }

        public void Add(Partnership elem)
        {
            base.BaseAdd(new PartnershipElement(elem));
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PartnershipElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PartnershipElement)element).friendlyName;
        }
    }
}
