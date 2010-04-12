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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for ErrorList.xaml
	/// </summary>
	public partial class ErrorList : Window
	{
		public ErrorList()
		{
			this.InitializeComponent();
			// Insert code required on object creation below this point.
		}

        private void closeWindow_Click(object sender, RoutedEventArgs e)
        {
            errorTable.RowGroups[1].Rows.Clear();
            this.Hide();
        }

        void ErrorList_Closing(object sender, CancelEventArgs e)
        {
            closeWindow_Click(sender, null);
            e.Cancel = true;
        }
	}
}