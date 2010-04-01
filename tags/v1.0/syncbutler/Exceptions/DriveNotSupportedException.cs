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
