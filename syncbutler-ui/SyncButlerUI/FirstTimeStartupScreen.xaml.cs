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
using SyncButler;
using System.Windows.Forms;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for FirstTimeStartupScreen.xaml
	/// </summary>
	public partial class FirstTimeStartupScreen : Window
	{
		
		public FirstTimeStartupScreen()
		{
			this.InitializeComponent();
			
		}
		
		
<<<<<<< .mine
=======
		private void FirstTimeComputerNameText_Enter(object sender, System.Windows.Input.KeyEventArgs e)
		{
            if (e.Key == Key.Return)
            {
                NameIt_Click(sender, e);
            }
		}
		

>>>>>>> .r352
	}
}