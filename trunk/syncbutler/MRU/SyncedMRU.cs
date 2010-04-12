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