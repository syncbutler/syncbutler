using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncButler
{
    class Controller
    {
        SyncEnvironment syncEnvironment;

        public Controller()
        {
            syncEnvironment = new SyncEnvironment();
            syncEnvironment.IntialEnv();
        }

        /// <summary>
        /// Creates a new Partnership based on 2 full paths.
        /// </summary>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>        
        private Partnership CreatePartnership(String leftPath, String rightPath)
        {
            FileInfo leftInfo = new FileInfo(leftPath);
            FileInfo rightInfo = new FileInfo(rightPath);
            bool isFolderLeft = leftInfo.Attributes.ToString().Equals("Directory");
            bool isFolderRight = rightInfo.Attributes.ToString().Equals("Directory");
            if (isFolderLeft && isFolderRight)
            {
                ISyncable left = new WindowsFolder(leftPath, leftPath);
                ISyncable right = new WindowsFolder(rightPath, rightPath);
                Partnership partner = new Partnership(leftPath, left, rightPath, right, null);
                return partner;
            }
            else if (isFolderLeft || isFolderRight) 
            {
                throw new ArgumentException("Folder cannot sync with a non-folder");
            }
            else 
            {
                ISyncable left = new WindowsFile(leftInfo.DirectoryName, leftPath);
                ISyncable right = new WindowsFile(rightInfo.DirectoryName, rightPath);
                Partnership partner = new Partnership(leftPath, left, rightPath, right, null);
                return partner;
            }

        }

        /// <summary>
        /// Adds a partnership to the list of Partnerships based on 2 full paths
        /// </summary>
        /// <param name="leftPath">Full Path to the left of a partnership</param>
        /// <param name="rightPath">Full Path to the right of a partnership</param>
        public void AddPartnership(String leftPath, String rightPath) 
        {
            Partnership partner = CreatePartnership(leftPath, rightPath);
            syncEnvironment.AddPartnership(partner);
        }
        
        /// <summary>
        /// Delete a partnership from the list of partnerships based at an index.
        /// </summary>
        /// <param name="idx">Index of the partnership to be deleted.</param>
        public void DeletePartnership(int idx)
        {
            syncEnvironment.RemovePartnership(idx);
        }

        /// <summary>
        /// Updates the paths of a partnership in the partnership list.
        /// </summary>
        /// <param name="idx">Index of the partnership to be updated</param>
        /// <param name="leftPath">New Full Path to the left of the partnership</param>
        /// <param name="rightPath">New Full Path to the right of a partnership</param>
        public void UpdatePartnership(int idx, String leftPath, String rightPath)
        {
            Partnership partner = CreatePartnership(leftPath, rightPath);
            syncEnvironment.UpdatePartnership(idx, partner);
        }
        

        /// <summary>
        /// Retrieves the list of partnerships from the sync environment. Allows for the user interface to display the list.
        /// </summary>
        /// <returns>The list of all partnerships</returns>
        public List<Partnership> GetPartnershipList()
        {
            return syncEnvironment.GetPartnerships();
        }

        /// <summary>
        /// Starts syncing a partnership.
        /// </summary>
        /// <param name="idx">Index of the partnership to be synced.</param>
        /// <returns>A list of conflicts. Will be null if there are no conflicts.</returns>
        public List<Conflict> SyncPartnership(int idx) 
        {
            return syncEnvironment.GetPartnerships()[idx].Sync();
        }

        /// <summary>
        /// Synchronizes all partnerships.
        /// </summary>
        public void SyncAll()
        {
            int elements = GetPartnershipList().Count;
            for (int i = 0; i < elements; i++)
            {
                SyncPartnership(i);
            }
        }

        /// <summary>
        /// Not Implemented. Turns the recent file monitoring on/off.
        /// </summary>
        public void ToggleMonitor()
        {
            throw new NotImplementedException();
        }


    }
}
