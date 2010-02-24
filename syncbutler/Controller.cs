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

        public bool CreatePartnership(String leftPath, String rightPath)
        {
            FileInfo leftInfo = new FileInfo(leftPath);
            FileInfo rightInfo = new FileInfo(rightPath);
            bool isFolderLeft = leftInfo.Attributes.ToString().Equals("Directory");
            bool isFolderRight = rightInfo.Attributes.ToString().Equals("Directory");
            if (isFolderLeft && isFolderRight)
            {
                ISyncable left = new WindowsFolder(leftPath);
                ISyncable right = new WindowsFolder(rightPath);
                Partnership partner = new Partnership(leftPath, left, rightPath, right, null);
                syncEnvironment.AddPartnership(partner);
            }
            else if (isFolderLeft || isFolderRight) 
            {
                throw new ArgumentException("Folder cannot sync with a non-folder");
            }
            else 
            {
                ISyncable left = new WindowsFile(leftPath);
                ISyncable right = new WindowsFile(rightPath);
                Partnership partner = new Partnership(leftPath, left, rightPath, right, null);
                syncEnvironment.AddPartnership(partner);
            }

        }

        /*
        public bool AddPartnership(ISyncable left, ISyncable right)
        {
            throw new NotImplementedException();
        }
        */

        public bool AddPartnership(ISyncable left, ISyncable right)
        {
            throw new NotImplementedException();
        }

        public bool DeletePartnership(int idx)
        {
            throw new NotImplementedException();
        }

        /* What are the updates?
        public bool UpdatePartnership(ISyncable left, ISyncable right)
        {
            throw new NotImplementedException();
        }
         */

        public List<Partnership> GetPartnershipList()
        {
            throw new NotImplementedException();
        }

        public bool ToggleMonitor()
        {
            throw new NotImplementedException();
        }


    }
}
