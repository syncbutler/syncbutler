using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
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
