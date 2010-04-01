using System;
using System.Collections.Generic;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when the user provides an invalid input.
    /// </summary>
    public class UserInputException : Exception
    {
        public UserInputException(string msg) : base(msg) { }
    }
}
