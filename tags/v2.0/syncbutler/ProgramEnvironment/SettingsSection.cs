// Developer to contact: Lee Chee Full
/*****************************************************************************/
// Copyright 2010 Sync Butler and its original developers.
// This file is part of Sync Butler (http://www.syncbutler.org).
// 
// Sync Butler is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sync Butler is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sync Butler.  If not, see <http://www.gnu.org/licenses/>.
//
/*****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace SyncButler.ProgramEnvironment
{
    /// <summary>
    /// This sectional attributes contains any and all states related to the entire program.
    /// </summary>
    public class SettingsSection : ConfigurationSection
    {
        /// <summary>
        /// This sectional attributes contains any and all states related
        /// to the entire program.
        /// </summary>
        [ConfigurationProperty("systemSettings")]
        public SettingsConfigElement SystemSettings
        {
            get
            {
                return ((SettingsConfigElement)this["systemSettings"]);
            }
            set
            {
                this["systemSettings"] = value;
            }
        }
    }
}
