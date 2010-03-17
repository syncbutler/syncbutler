using System;
using System.Collections.Generic;
using System.Text;

namespace SyncButler
{
    /// <summary>
    /// Interface to be implemented by the GUI in order for the controller to access the GUI
    /// </summary>
    public interface IGUI
    {
        void GrabFocus();
    }
}
