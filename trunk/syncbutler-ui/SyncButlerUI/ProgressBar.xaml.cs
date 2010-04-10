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
using System.Threading;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for ProgressBar.xaml
	/// </summary>
	public partial class ProgressBar : Window
	{
        public struct ProgressBarInfo
        {
            public int SubTaskPercent;
            public int TotalTaskPercent;
            public string taskDescription;
        }

        //private bool Cancelling;
        private BackgroundWorker taskWorker;
        private Semaphore waitForMessageResponse;
        private CustomDialog.MessageResponse messageResponse;
        private bool isClosing;

        public bool IsIndeterminate
        {
            set
            {
                if (TotalProgress.Visibility == Visibility.Hidden)
                {
                    SubProgress.IsIndeterminate = value;
                }
                else
                {
                    TotalProgress.IsIndeterminate = value;
                }
            }

            get
            {
                if (TotalProgress.Visibility == Visibility.Hidden)
                {
                    return SubProgress.IsIndeterminate;
                }
                else
                {
                    return TotalProgress.IsIndeterminate;
                }
            }
        }

        /// <summary>
        /// Constructor to set up all the basics. Made protected because it
        /// should only be called by other constructors.
        /// </summary>
		protected ProgressBar()
		{
			this.InitializeComponent();

            SubProgress.Minimum = 0;
            SubProgress.Maximum = 100;
            TotalProgress.Minimum = 0;
            TotalProgress.Maximum = 100;

            SubProgress.Value = 0;
            TotalProgress.Value = 0;
         //   Cancelling = false;
            isClosing = false;
            waitForMessageResponse = new Semaphore(0, 1);
            messageResponse = CustomDialog.MessageResponse.NotUsed;

		}

        /// <summary>
        /// Assiciates the BackgroundWorker to be used with this progress bar window.
        /// </summary>
        /// <param name="worker"></param>
        public ProgressBar(BackgroundWorker worker, string title)
            : this()
        {
            this.Title = title;
            taskWorker = worker;
            taskWorker.WorkerReportsProgress = true;
            if (!taskWorker.WorkerSupportsCancellation) HideCancelButton();
            taskWorker.ProgressChanged += ProgressListener;
        }

        public ProgressBar(BackgroundWorker worker, string title, string description)
            : this(worker, title)
        {
            ProgressText.Content = description;
        }

        /// <summary>
        /// Hides the total progress progress bar
        /// </summary>
        public void HideTotalProgress()
        {
            TotalProgress.Visibility = Visibility.Hidden;
            SubProgress.Margin = TotalProgress.Margin;
        }

        /// <summary>
        /// Hides the cancel button
        /// </summary>
        public void HideCancelButton()
        {
            CancelButton.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Shows the progress window and starts the background worker
        /// </summary>
        public void Start()
        {
            taskWorker.RunWorkerAsync();
            this.ShowDialog();
        }

        /// <summary>
        /// This method may be called by the BackgroundWorker thread in order to display a
        /// message box and get the user's response.
        /// </summary>
        /// <param name="worker">The BackgroundWorker object this thread belongs to</param>
        /// <param name="msgInfo">Information on the Message Box to show</param>
        /// <returns>The user's response</returns>
        public CustomDialog.MessageResponse RequestMessageDialog(BackgroundWorker worker, CustomDialog.MessageBoxInfo msgInfo)
        {
            worker.ReportProgress(0, msgInfo);
            waitForMessageResponse.WaitOne();
            return messageResponse;
        }

        /// <summary>
        /// This is the ProgressChanged listener. Provides the functionality require for RequestMessageDialog
        /// to work as well as to update the progress bars. Otherwise it passes the information on to the 
        /// progress changed delegate specified by the caller.
        /// </summary>
        /// <param name="workerObj">The BackgroundWorker object this thread belongs to</param>
        /// <param name="args"></param>
        private void ProgressListener(Object workerObj, ProgressChangedEventArgs args)
        {
            if (args.UserState is CustomDialog.MessageBoxInfo)
            {
                messageResponse = CustomDialog.Show((CustomDialog.MessageBoxInfo)args.UserState);
                waitForMessageResponse.Release();
            }
            else if (args.UserState is ProgressBarInfo)
            {
                ProgressBarInfo progress = (ProgressBarInfo)args.UserState;
                TotalProgress.Value = progress.TotalTaskPercent;
                SubProgress.Value = progress.SubTaskPercent;
                ProgressText.Content = progress.taskDescription;
            }
        }

        /// <summary>
        /// Event handler for the Cancel Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void CancelBtn(object sender, RoutedEventArgs e)
		{
            taskWorker.CancelAsync();
		}

        /// <summary>
        /// Closes this progress bar window
        /// </summary>
        public void TaskComplete()
        {
            isClosing = true;
            this.Close();
        }

        /// <summary>
        /// Window close event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (!isClosing) e.Cancel = true;
        }
	}
}