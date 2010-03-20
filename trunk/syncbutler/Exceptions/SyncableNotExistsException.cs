using System;
using System.Collections.Generic;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is thrown when either
    /// 1) the ISyncable cannot be located or 2) The ISyncable exists but
    /// the checksum for it does not exist.
    /// </summary>
    public class SyncableNotExistsException : Exception
    {
    }
}
