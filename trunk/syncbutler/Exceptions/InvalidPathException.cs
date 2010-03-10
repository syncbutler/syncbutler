using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler.Exceptions
{
    class InvalidPathException : Exception
    {
        public InvalidPathException(string msg) : base(msg) { }
    }
}
