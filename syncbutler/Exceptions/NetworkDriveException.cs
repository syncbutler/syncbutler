using System;
using System.Collections.Generic;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when the user tries to sync on a network drive and some errors occur.
    /// </summary>
    public class NetworkDriveException : Exception
    {
        public NetworkDriveException(string msg) : base(msg) { }
    }
}
