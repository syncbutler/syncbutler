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
using System.Text;
using System.Xml;

namespace SyncButler
{
    /// <summary>
    /// A callback to monitor the progress of the Syncing.
    /// </summary>
    /// <param name="status">Status of the current Sync operation</param>
    /// <returns>False to abort</returns>
    public delegate bool SyncableStatusMonitor(SyncableStatus status);

    /// <summary>
    /// A callback to handle an error which occured while Syncing
    /// </summary>
    /// <param name="exp"></param>
    /// <returns>False to abort, true to continue with next</returns>
    public delegate bool SyncableErrorHandler(Exception exp);

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
        /// Used to define a callback which is used to report errors encountered while scanning.
        /// </summary>
        /// <param name="exp">true indicates the user wants the program to try and continue with the next file. false indicates the user wants to cancel.</param>
        void SetErrorHandler(SyncableErrorHandler handler);

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
        /// Gets the path to the directory containing this ISyncable.
        /// </summary>
        /// <returns></returns>
        string GetContainingFolder();

        /// <summary>
        /// This method is called on the root ISyncables just
        /// before left.Sync(right) is invoked.
        /// </summary>
        void PrepareSync();

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
        void CopyTo(ISyncable item);

        /// <summary>
        /// Not implemented. Performs a delete action.
        /// </summary>
        /// <param name="recoverable">if the action performed allow the user to do a roll back</param>
        /// <returns>True if delete succeeded, false otherwise.</returns>
        void Delete(bool recoverable);

        /// <summary>
        /// Not implemented. Performs a merge based on the item provided.
        /// </summary>
        /// <param name="item">The other ISyncable item in question.</param>
        /// <returns>True if merge succeeded, false otherwise.</returns>
        void Merge(ISyncable item);

        /// <summary>
        /// Determines if the two ISyncables has changed
        /// </summary>
        /// <returns>true if the isyncable is changed, false otherwise</returns>
        bool HasChanged();

        /// <summary>
        /// Determines the equality of two ISyncables
        /// </summary>
        /// <param name="item">The other ISyncable in question</param>
        /// <returns>True if equal, false otherwise</returns>
        bool Equals(ISyncable item);

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
        /// Compares an object with another ISyncable and returns the reason for its differences.
        /// </summary>
        /// <param name="obj">The other ISyncable object to compare with.</param>
        /// <returns>A string of the reason.</returns>
        string GetDifferenceReason(ISyncable obj);

        /// <summary>
        /// Adds/Updates the checksum dictionary in the parentPartnership object
        /// </summary>
        void UpdateStoredChecksum();

        /// <summary>
        /// Removes itself from the checksum dictionary
        /// </summary>
        void RemoveStoredChecksum();

        /// <summary>
        /// Returns a string that represents this file/folder in the context of the
        /// containing partnership
        /// </summary>
        /// <returns></returns>
        string EntityPath();

        /// <summary>
        /// Returns a string that represents this file/folder in the context of the
        /// containing partnership.
        /// </summary>
        /// <param name="addAttributes">Additional attributes to add to the string</param>
        /// <returns></returns>
        string EntityPath(string addAttributes);

        /// <summary>
        /// Does this Suncable actually exist on the storage device/disk?
        /// </summary>
        /// <returns></returns>
        bool Exists();

        /// <summary>
        /// Indicates whether this ISyncable should be ignored
        /// </summary>
        /// <returns></returns>
        bool Ignored();

        /// <summary>
        /// Sets whether this ISyncable should be ignored
        /// </summary>
        /// <param name="value"></param>
        void Ignored(bool value);

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
