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
using SyncButler;
namespace SyncButlerUI
{
	public class PartnershipTempData
	{
		///Instantiate variables with empty strings to prevent null reference as Texbox will refer to it
		public PartnershipTempData()
		{
		    sourcePath="";
			destinationPath="";
			partnershipName="";
		}
		
		/// <summary>
		/// Instantiate partnership variable 
		/// </summary>
		/// <param name="p"></param>
        public PartnershipTempData(Partnership p)
        {
            sourcePath = p.LeftFullPath;
            destinationPath = p.RightFullPath;
            partnershipName = p.Name;
        }
		
		/// <summary>
		/// get set for folder1 path
		/// </summary>
		/// <returns></returns>
		public static string sourcePath{
		get;set;}
		
		/// <summary>
		/// get set for folder2 Path
		/// </summary>
		/// <returns></returns>
		public static string destinationPath{
		get;set;}
		
		/// <summary>
		/// get set for partnershipName
		/// </summary>
		/// <returns></returns>
		public static string partnershipName{
		get;set;}
		
		public static void clear(){
			sourcePath="";
			destinationPath="";
			partnershipName="";	
		}
	}
}