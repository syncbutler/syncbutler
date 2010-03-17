using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when the path is invalid
    /// </summary>
    class InvalidPathException : Exception
    {
        public InvalidPathException(string msg) : base(msg) { }
    }
}
