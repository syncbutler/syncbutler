// Developer to contact: Bryan Chen Shenglong
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

namespace SyncButler.Checksums
{
    /// <summary>
    /// Interface which hides the implementation of the rolling hash from the application. This allows for ease of switching to a more robust or faster implementation.
    /// </summary>
    public interface IRollingHash
    {
        /// <summary>
        /// Gets the (long) value of the current checksum.
        /// </summary>
        long Value { get; }

        /// <summary>
        /// Resets the checksum. Useful for generating multiple checksums from the same instance.
        /// </summary>
        void Reset();

        /// <summary>
        /// Update the checksum calculation with an integer (or single byte) value.
        /// </summary>
        /// <param name="bval">Integer of the byte to update the checksum with.</param>
        void Update(int bval);

        /// <summary>
        /// Update the checksum with a byte array.
        /// This is especially useful for calculating checksums from files.
        /// </summary>
        /// <param name="buffer">byte[] containing the bytes to update the checksum with.</param>
        void Update(byte[] buffer);

        /// <summary>
        /// Update the checksum with a byte array.
        /// </summary>
        /// <param name="buf">byte[] containing the bytes to update the checksum with.</param>
        /// <param name="off">int offset to start the reading from.</param>
        /// <param name="len">int length of bytes to read, starting from the offset.</param>
        void Update(byte[] buf, int off, int len);
    }
}
