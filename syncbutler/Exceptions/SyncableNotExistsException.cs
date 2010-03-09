using System;
using System.Collections.Generic;

namespace SyncButler.Exceptions
{
    /// <summary>
    /// This is a custom SyncButler exception. It is generated when either
    /// 1) the ISyncable cannot be located or when 2) The ISyncable exist but
    /// the checksum for it do not exist
    /// </summary>
    class SyncableNotExistsException : Exception
    {
    }
}
