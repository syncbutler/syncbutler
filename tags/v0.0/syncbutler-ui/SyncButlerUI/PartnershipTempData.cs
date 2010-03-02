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

		public PartnershipTempData()
		{
			// Insert code required on object creation below this point.
		    sourcePath="";
			destinationPath="";
			partnershipName="";
		}
        public PartnershipTempData(Partnership p)
        {
            sourcePath = p.LeftFullPath;
            destinationPath = p.RightFullPath;
            partnershipName = p.Name;
        }
		
		public static string sourcePath{
		get;set;}
		public static string destinationPath{
		get;set;}
		public static string partnershipName{
		get;set;}
		
		public static void clear(){
			sourcePath="";
			destinationPath="";
			partnershipName="";	
		}
	}
}