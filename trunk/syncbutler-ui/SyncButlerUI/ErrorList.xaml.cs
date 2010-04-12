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
	}
}