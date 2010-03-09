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
	/// Interaction logic for CustomDialog.xaml
	/// </summary>
	public partial class CustomDialog : Window
	{
		public enum MessageType
		{
			Error,
			Success,
			Message,
			Warning,
			Question
		}
        //default msg box
		public CustomDialog(string msg){
			InitializeComponent();
			syncButlerMessage.Text=msg;
		}
		
		public CustomDialog(MessageType msgType, string msg){
			InitializeComponent();
			messageTypetoImage(msgType,msg);

		}
		
		private void messageTypetoImage(MessageType msgType,string msg){
			string imageUri="Images/logowTransparency.png";
			Visibility yesButtonVisible=System.Windows.Visibility.Hidden;
			Visibility noButtonVisible=System.Windows.Visibility.Hidden;
			Visibility okButtonVisible=System.Windows.Visibility.Hidden;
			string dialogTitle="";
			switch(msgType){
				case MessageType.Error:
					okButtonVisible=System.Windows.Visibility.Visible;
					dialogTitle="Error";
					imageUri="Images/error.png";
					break;
				case MessageType.Success:
					okButtonVisible=System.Windows.Visibility.Visible;
					dialogTitle="Success";
					break;
				case MessageType.Message:
					imageUri="Images/logowTransparency.png";
					okButtonVisible=System.Windows.Visibility.Visible;
					dialogTitle="Message";
					break;
				case MessageType.Warning:
					okButtonVisible=System.Windows.Visibility.Visible;
					dialogTitle="Warning";
					break;
				case MessageType.Question:
					yesButtonVisible=System.Windows.Visibility.Visible;
					noButtonVisible=System.Windows.Visibility.Visible;
					break;	
				default:	
				    break;	
			}
			Uri src = new Uri(@imageUri, UriKind.Relative);
 			
			BitmapImage img = new BitmapImage(src);
 			syncButlerMessage.Text=msg;
			messageImage.Source=img;
			yesButton.Visibility=yesButtonVisible;
			noButton.Visibility=noButtonVisible;
			okButton.Visibility=okButtonVisible;
			this.Title=dialogTitle;
		
		}
		
		
		private void yesClick(object sender, RoutedEventArgs e)		
		{
			this.DialogResult = true;		
		}
		private void noClick(object sender, RoutedEventArgs e)		
		{
			this.DialogResult = false;		
		}

	}
}