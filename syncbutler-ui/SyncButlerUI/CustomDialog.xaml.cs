// Developer to contact: Poh Wei Sheng
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

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for CustomDialog.xaml
	/// </summary>
	public partial class CustomDialog : Window
	{
        public enum MessageResponse
        {
            Yes,
            No,
            Ok,
            Retry,
            Skip,
            Cancel,
            NotUsed
        }

		public enum MessageType
		{
			Error,
			Success,
			Message,
			Warning,
			Question
		}

        public enum MessageTemplate
        {
            OkOnly,
            YesNo,
            SkipRetryCancel,
            RetryCancel,
            SkipCancel
        }

        public struct MessageBoxInfo
        {
            public DependencyObject parent;
            public MessageTemplate messageTemplate;
            public MessageType messageType;
            public MessageResponse defaultResponse;
            public string message;
        }

        private MessageResponse userResponse;
        private MessageResponse button1Response, button2Response, button3Response;

        protected CustomDialog()
        {
            InitializeComponent();
        }

        protected static string ResponseString(MessageResponse response)
        {
            switch (response)
            {
                case MessageResponse.Cancel: return "Cancel";
                case MessageResponse.No: return "No";
                case MessageResponse.NotUsed: return "";
                case MessageResponse.Ok: return "Ok";
                case MessageResponse.Retry: return "Retry";
                case MessageResponse.Skip: return "Skip";
                case MessageResponse.Yes: return "Yes";
            }

            return "";
        }

        /// <summary>
        /// Method exposed to allow for showing of a dialog
        /// </summary>
        /// <param name="info">MessageBoxInfo struct containing several parameters</param>
        /// <returns>the MessageResponse to the message dialog</returns>
        public static MessageResponse Show(MessageBoxInfo info)
        {
            return Show(info.parent, info.messageTemplate, info.messageType, info.defaultResponse, info.message);
        }

        /// <summary>
        /// Display a message box based on several templates
        /// </summary>
        /// <param name="parent">The window which owns the message box</param>
        /// <param name="msgBoxStyle">The template to use</param>
        /// <param name="dialogClose">The value to return if the dialog is simply closed.</param>
        /// <param name="msg">The message to display</param>
        /// <returns>The result based on the button pressed. If the dialog was simply closed.</returns>
        public static MessageResponse Show(DependencyObject parent, MessageTemplate msgBoxStyle, MessageResponse dialogClose, string msg)
        {
            MessageType msgType;

            switch (msgBoxStyle)
            {
                case MessageTemplate.OkOnly:
                    msgType = MessageType.Message;
                    break;
                case MessageTemplate.SkipRetryCancel:
                case MessageTemplate.RetryCancel:
                case MessageTemplate.SkipCancel:
                    msgType = MessageType.Error;
                    break;
                case MessageTemplate.YesNo:
                    msgType = MessageType.Question;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Show(parent, msgBoxStyle, msgType, dialogClose, msg);
        }

        /// <summary>
        /// Display a message box based on several templates
        /// </summary>
        /// <param name="parent">The window which owns the message box</param>
        /// <param name="msgBoxStyle">The template to use</param>
        /// <param name="msgType">The type of message box this should be. Affects the title and icon.</param>
        /// <param name="dialogClose">The value to return if the dialog is simply closed.</param>
        /// <param name="msg">The message to display</param>
        /// <returns>The result based on the button pressed. If the dialog was simply closed.</returns>
        public static MessageResponse Show(DependencyObject parent, MessageTemplate msgBoxStyle, MessageType msgType, MessageResponse dialogClose, string msg)
        {
            MessageResponse btn1 = MessageResponse.NotUsed, btn2 = MessageResponse.NotUsed, btn3 = MessageResponse.NotUsed;

            switch (msgBoxStyle)
            {
                case MessageTemplate.OkOnly:
                    btn3 = MessageResponse.Ok;
                    break;
                case MessageTemplate.SkipRetryCancel:
                    btn3 = MessageResponse.Cancel;
                    btn2 = MessageResponse.Retry;
                    btn1 = MessageResponse.Skip;
                    break;
                case MessageTemplate.RetryCancel:
                    btn3 = MessageResponse.Cancel;
                    btn2 = MessageResponse.Retry;
                    break;
                case MessageTemplate.SkipCancel:
                    btn3 = MessageResponse.Cancel;
                    btn2 = MessageResponse.Skip;
                    break;
                case MessageTemplate.YesNo:
                    btn3 = MessageResponse.No;
                    btn2 = MessageResponse.Yes;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Show(parent, msgType, btn1, btn2, btn3, dialogClose, msg);
        }

        /// <summary>
        /// Displays a message box with up to 3 buttons
        /// </summary>
        /// <param name="parent">The window which owns this message box</param>
        /// <param name="msgType">The icon and title to display</param>
        /// <param name="button1">The value of button 1. Set to NotUsed if not needed.</param>
        /// <param name="button2">The value of button 2. Set to NotUsed if not needed.</param>
        /// <param name="button3">The value of button 3. Set to NotUsed if not needed.</param>
        /// <param name="dialogClose">The value to return if the dialog is simply closed.</param>
        /// <param name="msg">The message to display</param>
        /// <returns>The result based on the button pressed. If the dialog was simply closed.</returns>
        public static MessageResponse Show(DependencyObject parent, MessageType msgType, MessageResponse button1, MessageResponse button2, MessageResponse button3, MessageResponse dialogClose, string msg)
        {
            CustomDialog instance = new CustomDialog();

            instance.userResponse = dialogClose;

            if (button1 == MessageResponse.NotUsed) instance.DialogButton1.Visibility = Visibility.Hidden;
            else
            {
                instance.DialogButton1.Content = ResponseString(button1);
                instance.button1Response = button1;
            }

            if (button2 == MessageResponse.NotUsed) instance.DialogButton2.Visibility = Visibility.Hidden;
            else
            {
                instance.DialogButton2.Content = ResponseString(button2);
                instance.button2Response = button2;
            }
            
            if (button3 == MessageResponse.NotUsed) instance.DialogButton3.Visibility = Visibility.Hidden;
            else
            {
                instance.DialogButton3.Content = ResponseString(button3);
                instance.button3Response = button3;
            }

            string imageUri = "Images/logowTransparency.png";
            
            string dialogTitle = "";
            switch (msgType)
            {
                case MessageType.Error:
                    dialogTitle = "Sync Butler: Error";
                    imageUri = "Images/error.png";
                    break;

                case MessageType.Success:
                    dialogTitle = "Sync Butler: Success";
                    break;

                case MessageType.Message:
                    dialogTitle = "Sync Butler";
                    break;

                case MessageType.Warning:
                    dialogTitle = "Sync Butler: Warning";
                    break;

                case MessageType.Question:
                    dialogTitle = "Sync Butler: Question";
                    break;

                default:
                    break;
            }

            Uri src = new Uri(@imageUri, UriKind.Relative);

            instance.syncButlerMessage.Text = msg;
            instance.messageImage.Source = new BitmapImage(src); ;
            instance.Title = dialogTitle;
            
            if (parent != null) 
            {
                var parentWindow = Window.GetWindow(parent);
                if (parentWindow != null) instance.Owner = parentWindow;
            }

            instance.Topmost = false;
            instance.ShowDialog();

            return instance.userResponse;
        }

        private void DialogButton1_Click(object sender, RoutedEventArgs e)
        {
            userResponse = button1Response;
            Hide();
        }

        private void DialogButton2_Click(object sender, RoutedEventArgs e)
        {
            userResponse = button2Response;
            Hide();
        }

        private void DialogButton3_Click(object sender, RoutedEventArgs e)
        {
            userResponse = button3Response;
            Hide();
        }

		private void KeyDownHandler(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape) Hide();
		}
	}
}