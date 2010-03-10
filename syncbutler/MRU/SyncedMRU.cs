using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SyncButler.MRU
{
    /// <summary>
    /// This is a data container to faciliate the usage of XMLSerializer 
    /// </summary>
    public class SyncedMRU
    {

        public string OriginalPath { get; set; }

        public string SyncedTo { get; set; }

        public SyncedMRU(string OriginalPath, string SyncedTo)
        {
            this.OriginalPath = OriginalPath;
            this.SyncedTo = SyncedTo;
        }

        public SyncedMRU()
        {

        }
    }
}
