using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Contains methods to load and store settings as well as handling access to the list of partnerships
    /// </summary>
    class SyncEnvironment
    {
        private List<Partnership> partnershipList;

        /// <summary>
        /// Returns the partnership at the specified index.
        /// </summary>
        /// <returns>A Partnership object</returns>
        public Partnership LoadPartnership(int idx)
        {
            return partnershipList[idx];
        }

        /// <summary>
        /// Not implemented. Gets the settings for the program.
        /// </summary>
        /// <returns>A Dictionary object with strings as keys and value (subject to change).</returns>
        public Dictionary<String, String> ReadSettings()
        {
            return null;
        }

        /// <summary>
        /// Not implemented. Stores the settings for the program to persistent storage.
        /// </summary>
        /// <returns>True if the store operation was successful false otherwise.</returns>
        public bool StoreSettings()
        {
            return false;
        }
    }
}
/*
Environment

Attributes:

partnershipList:List<partnership>

Methods:

loadPartnership(input:int):Partnership
readSettings():Dictionary
storeSettings():boolean
*/