using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler.Exceptions
{
    public class UserInputException:Exception
    {
        public string message { get; set; }
        public UserInputException(string a_message)
        {
            message = a_message;
        }
    }
}
