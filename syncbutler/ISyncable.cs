using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SyncButler
{
    public enum Error { NoError, NoPermission, PathTooLong, DirectoryDoesNotExist, InvalidPath, NotImplemented, IsWorkingFolder };

    /// <summary>
    /// A callback to monitor the progress of the Syncing.
    /// 
    /// </summary>
    /// <param name="status">Status of the current Sync operation</param>
    /// <returns>False to abort</returns>
    public delegate bool SyncableStatusMonitor(SyncableStatus status);

    /// <summary>
    /// Interface containing the methods necessary for items to be syncable.
    /// All items that can be synchronised must implement this interface.
    /// </summary>
    public interface ISyncable
    {
        /// <summary>
        /// Used to defines a callback which may be used to monitor the progress of a Sync
        /// </summary>
        /// <param name="monitor"></param>
        void SetStatusMonitor(SyncableStatusMonitor monitor);

        /// <summary>
        /// Sets a reference to the parent Partnership object.
        /// </summary>
        /// <param name="?">The containing Partnership object</param>
        void SetParentPartnership(Partnership parentPartnership);

        /// <summary>
        /// Gets a reference to the parent partnership object
        /// </summary>
        /// <returns></returns>
        Partnership GetParentPartnership();

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
        Error CopyTo(ISyncable item);

        /// <summary>
        /// Not implemented. Performs a delete action.
        /// </summary>
        /// <returns>True if delete succeeded, false otherwise.</returns>
        Error Delete();

        /// <summary>
        /// Not implemented. Performs a merge based on the item provided.
        /// </summary>
        /// <param name="item">The other ISyncable item in question.</param>
        /// <returns>True if merge succeeded, false otherwise.</returns>
        Error Merge(ISyncable item);

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
        /// Retrieves the checksum from the checksum dictionary
        /// </summary>
        /// <returns></returns>
        long GetStoredChecksum();

        /// <summary>
        /// Adds/Updates the checksum dictionary in the parentPartnership object
        /// </summary>
        void UpdateStoredChecksum();

        /// <summary>
        /// Removes itself from the checksum dictionary
        /// </summary>
        void RemoveStoredChecksum();

        /// <summary>
        /// Returns a "path" to the node. (eg. In the case of Files,
        /// it's relative path + file name)
        /// </summary>
        /// <returns>Path to the data</returns>
        string EntityPath();

        /// <summary>
        /// Does this Suncable actually exist on the storage device/disk?
        /// </summary>
        /// <returns></returns>
        bool Exists();

        /// <summary>
        /// Creates a child from an EntityPath. (ie. provides the absolute path then creates the ISyncable)
        /// </summary>
        /// <param name="EntityPath"></param>
        /// <returns></returns>
        /// <exception cref="ArguementException">The entityPath cannot be a child of the current syncable.</exception>
        ISyncable CreateChild(string entityPath);

        /// <summary>
        /// Serializes the object as a string
        /// </summary>
        /// <returns>The serialized output of the syncable</returns>
        string Serialize();

        /// <summary>
        /// Serializes the object to an XmlTextWriter
        /// </summary>
        /// <param name="xmlData"></param>
        void SerializeXML(XmlWriter xmlData);
    }
}
