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
