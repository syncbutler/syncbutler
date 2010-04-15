// Developer to contact: Tan Chee Eng
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
using System.Linq;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Represents a key used in the checksum dictionary
    /// </summary>
    public class ChecksumKey
    {
        private string entityPath;
        private string relativePath;

        /// <summary>
        /// Default constructor that initialises the entity and relative paths to an empty string.
        /// </summary>
        public ChecksumKey()
        {
            entityPath = "";
            relativePath = "";
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ep">The entity path.</param>
        /// <param name="rp">The relative path.</param>
        public ChecksumKey(string ep, string rp)
        {
            entityPath = ep;
            relativePath = rp;
        }

        /// <summary>
        /// Gets or sets the entity path.
        /// The entity path is the relative path with a special prefix to indicate the type of the object it is referring to.
        /// </summary>
        public string EntityPath
        {
            get { return this.entityPath; }
            set { this.entityPath = value; }
        }

        /// <summary>
        /// Gets or sets the relative path of the object it is referring to.
        /// </summary>
        public string RelativePath
        {
            get { return this.relativePath; }
            set { this.relativePath = value; }
        }
    }
}
