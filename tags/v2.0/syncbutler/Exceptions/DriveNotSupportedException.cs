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

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when we cannot access the low level details of the drive.
    /// </summary>
    public class DriveNotSupportedException : Exception
    {
        public DriveNotSupportedException(string msg) : base(msg) { }
    }
}
