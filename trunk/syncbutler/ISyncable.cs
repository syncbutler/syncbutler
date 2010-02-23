using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Interface containing the methods necessary for items to be syncable.
    /// All items that can be synchronised must implement this interface.
    /// </summary>
    public interface ISyncable
    {
        /// <summary>
        /// Sets a reference to the parent Partnership object.
        /// </summary>
        /// <param name="?">The containing Partnership object</param>
        void SetParentPartnership(Partnership parentPartnership);

        /// <summary>
        /// Checks if it's in Sync with the other pair
        /// </summary>
        /// <param name="otherPair"></param>
        /// <returns>List of conflicts detected</returns>
        List<Conflict> Sync(ISyncable otherPair);

        /// <summary>
        /// Not implemented. Performs a copy action based on the item provided.
        /// </summary>
        /// <param name="item">The other ISyncable item in question.</param>
        /// <returns>True if copy succeeded, false otherwise.</returns>
        object CopyTo(ISyncable item);

        /// <summary>
        /// Not implemented. Performs a delete action.
        /// </summary>
        /// <returns>True if delete succeeded, false otherwise.</returns>
        object Delete();

        /// <summary>
        /// Not implemented. Performs a merge based on the item provided.
        /// </summary>
        /// <param name="item">The other ISyncable item in question.</param>
        /// <returns>True if merge succeeded, false otherwise.</returns>
        object Merge(ISyncable item);

        /// <summary>
        /// Determines if the two isyncable has changed
        /// </summary>
        /// <returns>true if the isyncable is changed, false otherwise</returns>
        Boolean HasChanged();

        /// <summary>
        /// Determines the equality of two ISyncables
        /// </summary>
        /// <param name="item">The other ISyncable in quesstion</param>
        /// <returns>True if equal, false otherwise</returns>
        Boolean Equals(ISyncable item);

        /// <summary>
        /// Returns a checksum that represents the current state of the syncable object.
        /// </summary>
        /// <returns>the checksum</returns>
        long Checksum();

        /// <summary>
        /// Returns a "path" to the node. (eg. In the case of Files,
        /// it's relative path + file name)
        /// </summary>
        /// <returns>Path to the data</returns>
        string EntityPath();
    }
}
