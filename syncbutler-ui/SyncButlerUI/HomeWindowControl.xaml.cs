﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Data;
using System.Threading;
using System.ComponentModel;
using WPF_Explorer_Tree;
using SyncButler;

namespace SyncButlerUI
{
	/// <summary>
	/// Interaction logic for HomeWindowControl.xaml
	/// </summary>
	public partial class HomeWindowControl : UserControl
	{
    
	#region fields&Attributes 
    /// <summary>
    /// Defines the size of a single increment of the progress bar.
    /// </summary>
    //private int progressBarIncrement = 5;
		
    private object dummyNode = null;
    public string SelectedImagePath { get; set; }
	public SyncButler.Controller Controller{get;set;}
	#endregion
		
		
		//Controller controller;
		public HomeWindowControl()
		{
			this.InitializeComponent();
			//Temporary testing link to Controller
            //controller = new Controller();
            //partnershipList.ItemsSource = controller.GetPartnershipList();
		}
		
	#region UIcode
	/// <summary>
    /// Interaction logic for Creating Partnership
    /// </summary>
		
		/// <summary>
		/// Populate the tree view with storage devices that are ready
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void clearTreeView()
        {
			foldersItem.Items.Clear();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
				if(d.IsReady){
				string s = d.Name;
                TreeViewItem item = new TreeViewItem();
                item.Header = s;
             	item.Tag = s;
            	item.FontWeight = FontWeights.Normal;
             	item.Items.Add(dummyNode);
             	item.Expanded += new RoutedEventHandler(folder_Expanded);
			 	item.Collapsed += new RoutedEventHandler(folder_Collapsed);
						
                foldersItem.Items.Add(item);
				}
            }

        }
	
		/// <summary>
		/// remove subItems of the list and repopulate when collasped
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void folder_Collapsed(object sender, RoutedEventArgs e)
		{
		      try
               {
			   TreeViewItem item=(TreeViewItem)sender;
			   item.Items.Clear();
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
						subitem.Collapsed += new RoutedEventHandler(folder_Collapsed);
                        item.Items.Add(subitem);
                    }
                }catch (Exception) { }
		}
		/// <summary>
		/// populate the list when folder is expanded
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

        private void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
						subitem.Collapsed += new RoutedEventHandler(folder_Collapsed);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception) { }
            }
        }
		
		/// <summary>
		/// populate the textbox with current selected value when folder is expanded
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void foldersItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tree = (TreeView)sender;
            TreeViewItem temp = ((TreeViewItem)tree.SelectedItem);

            if (temp == null)
                return;
            SelectedImagePath = "";
            string temp1 = "";
            string temp2 = "";
            while (true)
            {
                temp1 = temp.Header.ToString();
                if (temp1.Contains(@"\"))
                {
                    temp2 = "";
                }
                SelectedImagePath = temp1 + temp2 + SelectedImagePath;
                if (temp.Parent.GetType().Equals(typeof(TreeView)))
                {
                    break;
                }
                temp = ((TreeViewItem)temp.Parent);
                temp2 = @"\";
            }
            //show user selected path
			//destinationTextBox.Text=SelectedImagePath;
          	sourceTextBox.Text=SelectedImagePath;
			//  MessageBox.Show(SelectedImagePath);
        }
		
		/// <summary>
		/// Goes the 2nd Page of Create Partnership to set Destination Values
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goToPartnershipDest(object sender, RoutedEventArgs e){
		    try{
			checkInput();
			PartnershipTempData.sourcePath=sourceTextBox.Text;
		    clearTreeView();	
		    sourceTextBox.Text=PartnershipTempData.destinationPath;
			VisualStateManager.GoToState(this,"CreatePartnershipState2",false);
		    }catch(Exception ex){
				showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}
		}
		
		/// <summary>
		/// Checks the sourceTextbox for values if its empty or if the directory exists
		/// </summary>
		private void checkInput(){
			if(sourceTextBox.Text.Length>266){
				throw new Exception("Folder Path is too long");
			}else if(sourceTextBox.Text.Equals("")){
				throw new Exception("Please select a Folder");
			}else if(!Directory.Exists(sourceTextBox.Text)){
				throw new Exception("No Such Folder");
		
			}
			
		}
		
		/// <summary>
		/// go to the 1st page of create partnership to set source Textbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		
		private void goToCreatePartnership_Click(object sender, RoutedEventArgs e){
		   clearTreeView();
		   VisualStateManager.GoToState(this,"CreatePartnershipState1",false);
		}
		
		/// <summary>
		/// goes back to the 1st page from the 2nd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goBackToCreatePartnershipSrc(object sender, RoutedEventArgs e){
		  	try{

			PartnershipTempData.destinationPath=sourceTextBox.Text;
		    sourceTextBox.Text=PartnershipTempData.sourcePath;
		    clearTreeView();	
			VisualStateManager.GoToState(this,"CreatePartnershipState1",false);
		    }catch(Exception ex){
				showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}
		}
		/// <summary>
		/// goes to the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goToCreatePartnershipName(object sender, RoutedEventArgs e){
			try{
			checkInput();
			PartnershipTempData.destinationPath=sourceTextBox.Text;
		    if(PartnershipTempData.destinationPath.Equals(PartnershipTempData.sourcePath)){
			throw new Exception("Same Folders selected: Please pick another Folder");	
			}else if ( PartnershipTempData.sourcePath.IndexOf(PartnershipTempData.destinationPath+"\\")==0 )	
			{
				throw new Exception("Error- 1st Folder is under the 2nd Folder  ");	
			}
			else if (PartnershipTempData.destinationPath.IndexOf(PartnershipTempData.sourcePath+"\\")==0){
				throw new Exception("Error- 2nd Folder is under the 1st Folder  ");	
			}
			sourceTextBox1.Text=PartnershipTempData.sourcePath;
			destinationTextBox1.Text=PartnershipTempData.destinationPath;
			partnershipNameTextBox.Text=PartnershipTempData.partnershipName;	
			VisualStateManager.GoToState(this,"CreatePartnershipState3",false);
		    }catch(Exception ex){
				showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}	
		}
		/// <summary>
		/// goes back to the 2nd page from the 3rd page of create partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goBackToCreatePartnershipDes(object sender, RoutedEventArgs e){
		   PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
		   destinationTextBox1.Text=PartnershipTempData.destinationPath;
		   clearTreeView();	
		   VisualStateManager.GoToState(this,"CreatePartnershipState2",false);
		}
		
		
		/// <summary>
		/// done to submit the create partnership to controller
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void createPartnership(object sender, RoutedEventArgs e){
		 try{
			if(partnershipNameTextBox.Text.Equals("")){
			    throw new Exception("Please input a partnership name");	
			
			}
			PartnershipTempData.partnershipName=partnershipNameTextBox.Text;
			sourceFolderPath.Text=PartnershipTempData.sourcePath;
			destinationFolderPath.Text=PartnershipTempData.destinationPath;
		    partnerShipName.Text=PartnershipTempData.partnershipName;
			this.Controller.AddPartnership(partnerShipName.Text,sourceFolderPath.Text,destinationFolderPath.Text);
			VisualStateManager.GoToState(this,"CreatePartnershipDone1",false);
			sourceTextBox1.Text="";
			destinationTextBox1.Text="";
			sourceTextBox.Text="";
		    PartnershipTempData.clear();
			partnershipList.Items.Refresh();
		   }catch(Exception ex){
				showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}	
		}
		
		/// <summary>
		/// goes back to Home state
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goHome(object sender, RoutedEventArgs e){
				VisualStateManager.GoToState(this,"Home",false);
		}
		
		/// <summary>
		/// goes to view
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void goToViewPartnerships(object sender, RoutedEventArgs e)
        {
			VisualStateManager.GoToState(this,"ViewPartnership1",false);
			SortedList<string,Partnership> partnershiplist = this.Controller.GetPartnershipList();
			this.partnershipList.ItemsSource = partnershiplist.Values;
            this.partnershipList.Items.Refresh();
			
        }
		
		
		/// <summary>
		/// Checks for the index selected and delete the partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void deletePartnership_Click(object sender, RoutedEventArgs e)
		{
			try{
		  	if(partnershipList.SelectedIndex<0){
				throw new Exception("Please select a partnership to delete.");
			}
			if (showMessageBox(CustomDialog.MessageType.Question,"Are you sure?")==true){
				this.Controller.DeletePartnership(partnershipList.SelectedIndex);
				partnershipList.Items.Refresh();
			}
			}catch(Exception ex){
					showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}
		}
		
		/// <summary>
		/// Executes upon clicking resolve partnership
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void resolvePartnership_Click(object sender, RoutedEventArgs e){
			// resolve it here?
			List<SamplePartnershipConflict> conflictList=(List<SamplePartnershipConflict>)this.ConflictList.ItemsSource;
		}
		
		/// <summary>
		/// Executes when clicking on the explore features button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void goToExploreFeatures_Click(object sender, RoutedEventArgs e)
		{
			showMessageBox(CustomDialog.MessageType.Message,"Exploring New Features is still under construction!");
		}
		
		/// <summary>
		/// Display Custom Dialog Box. 
		/// </summary>
		/// <param name="messagetype">MessageType Enumerator, to tell what kind of message it is: Error, Question, Warning, Message</param>
		/// <param name="msg">String msg to tell what message the error is</param>
		private bool showMessageBox(CustomDialog.MessageType messagetype,string msg){
			CustomDialog dialog=new CustomDialog(messagetype,msg);
			var parent = Window.GetWindow(this);
			if(parent!=null){
				dialog.Owner=parent;
			}
			dialog.ShowDialog();
			return (bool)dialog.DialogResult;
		}
		
		
		/// <summary>
		/// Executes when SyncAll button is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Sync(object sender, RoutedEventArgs e)
		{
			try{
			if(this.Controller.GetPartnershipList().Count<1 ){
				throw new Exception("No Partnerships created yet");	
			}
			if (showMessageBox(CustomDialog.MessageType.Question,"Are you sure?")==true){
			VisualStateManager.GoToState(this,"ConflictState1",false);
			
				
			//Instantiates background worker 
			BackgroundWorker worker = new BackgroundWorker();
				worker.WorkerReportsProgress=true;
				worker.WorkerSupportsCancellation=true;
				
			this.Controller.SyncAll();
			createAndBindSamples();
			showMessageBox(CustomDialog.MessageType.Message,"Sync-ed.\r\nPlease check.");
			}
			}catch(Exception ex){
			 	
				showMessageBox(CustomDialog.MessageType.Error,ex.Message);
			}
		}
		#endregion	
		
		/// <summary>
		/// Syncs MRUs
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void MRUSync(object sender, RoutedEventArgs e)
        {
            this.Controller.SyncMRUs("c");
        }
		
		/// <summary>
		/// this is a test methods to bimd the sample list into the datagrid
		/// </summary>
		private void createAndBindSamples(){
			List<SamplePartnershipConflict> conflictList=SamplePartnershipConflict.getSamplePartnershipConflictCollection();
			this.ConflictList.ItemsSource=conflictList;
			this.ConflictList.Items.Refresh();
			
		
		}
		
		private void SaveSetting(object sender, RoutedEventArgs e)
		{
			string ComputerName = this.ComputerNameTextBox.Text;
			bool SBSEnable = this.SBSSettingComboBox.SelectedItem.Equals("Enable");
			char DriveLetter = (char)this.SBSWorkingDriveComboBox.SelectedItem;
			
			this.Controller.SaveSetting(ComputerName,SBSEnable,DriveLetter);
		}
		
		private void SBSSettingChanged(object sender, RoutedEventArgs e)
		{
            if (this.SBSSettingComboBox.SelectedItem != null)
			    this.SBSWorkingDriveComboBox.IsEnabled = this.SBSSettingComboBox.SelectedItem.Equals("Enable");
		}
		
		private void DefaultSetting(object sender, RoutedEventArgs e)
		{
			
		}

	}
}