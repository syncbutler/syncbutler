using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when the user cancels a sync which is in progress.
    /// </summary>
    public class UserCancelledException : Exception
    {
    }
}
