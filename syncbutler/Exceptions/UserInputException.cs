using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when the user provides an invalid input.
    /// </summary>
    public class UserInputException:Exception
    {
        public string message { get; set; }
        public UserInputException(string a_message)
        {
            message = a_message;
        }
    }
}
