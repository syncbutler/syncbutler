using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    public class PartnershipListConfigCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Returns the the partnership object that is stored at the particular index
        /// </summary>
        /// <param name="index">Index, 0 is inclusive</param>
        /// <returns>A Partnership Config Element that in it contains a partnership object</returns>
        public PartnershipConfigElement this[int index]
        {
            get
            {
                return (PartnershipConfigElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        /// <summary>
        /// Creates a Partnership object that is automatically encapsulated in Partnership
        /// Config element
        /// </summary>
        /// <param name="partnership">A Partnership Config Element that in it contains a partnership object</param>
        public void Add(string leftPath, string rightPath)
        {
            PartnershipConfigElement newElement = new PartnershipConfigElement(leftPath, rightPath);
            BaseAdd(newElement);
        }

        /// <summary>
        /// A generic method required by ConfigCollection to create a Configuration
        /// Element (specifically PartnershipConfigElement)
        /// </summary>
        /// <returns>A new declared Partnership Config Element</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new PartnershipConfigElement();
        }

        /// <summary>
        /// A generic method required by ConfigCollection to return a Configuration
        /// Element (specifically PartnershipConfigElement)
        /// </summary>
        /// <param name="element"></param>
        /// <returns>An object containing the information stored in the ConfigElement object,
        /// already boxed to be PartnershipConfigElement</returns>
        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((PartnershipConfigElement) element).LeftPath;
        }

        /// <summary>
        /// Clear out the list stored in the XML settings file
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }

    }
}
