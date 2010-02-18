using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler.Checksums
{
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
