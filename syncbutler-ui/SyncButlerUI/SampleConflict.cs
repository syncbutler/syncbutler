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
	public class SamplePartnershipConflict
	{
		private static List<SamplePartnershipConflict> samplePartnershipConflictCollection = default(List<SamplePartnershipConflict>);
		public string partnershipName{get;set;}
		public List<SampleConflict> listSampleConflict { get; set; }
		//this is a sample conflict list Please delete when binded with the real data
		public SamplePartnershipConflict(string a_partnershipName)
		{
			partnershipName=a_partnershipName;
			listSampleConflict=SampleConflict.getSampleConflictCollection();
		}
		
		public static List<SamplePartnershipConflict> getSamplePartnershipConflictCollection()

		{
		if (samplePartnershipConflictCollection == null)

		samplePartnershipConflictCollection = createCollection();

		return samplePartnershipConflictCollection;

		}

		private static List<SamplePartnershipConflict> createCollection()
		
		{
		
		return new List<SamplePartnershipConflict>
		
		{
		new SamplePartnershipConflict("Test1"),		
		new SamplePartnershipConflict("Test2"),

				};
		
		}
		
	}
	public class SampleConflict
	{
		private static List<SampleConflict> sampleConflictCollection = default(List<SampleConflict>);
		public string filename{get;set;}
		public bool folder1{get;set;}
		public bool folder2{get;set;}
		public int sizeFolder1{get;set;}
		public int sizeFolder2{get;set;}
		public string dateWriteAccessed1{get;set;}
		public string dateWriteAccessed2{get;set;}
		//this is a sample conflict list Please delete when binded with the real data
		public SampleConflict(string a_filename,bool a_folder1,bool a_folder2, int a_sizeFolder1,int a_sizeFolder2,string a_dateWriteAccessed1,string a_dateWriteAccessed2)
		{
			filename=a_filename;
			folder1=a_folder1;
			folder2=a_folder2;
			sizeFolder1=a_sizeFolder1;
			sizeFolder2=a_sizeFolder2;
			dateWriteAccessed1=a_dateWriteAccessed1;
			dateWriteAccessed2=a_dateWriteAccessed2;			
		}
		
		public static List<SampleConflict> getSampleConflictCollection()

{		if (sampleConflictCollection == null)

		sampleConflictCollection = createCollection();

		return sampleConflictCollection;

		}

		private static List<SampleConflict> createCollection()
		
		{
		
		return new List<SampleConflict>
		
		{
		new SampleConflict("/test/test.doc", true , false, 20,21,"12/31/2009","12/31/2009"),		
		new SampleConflict("/test/test1.doc", true , false, 20,21,"12/31/2009","12/31/2009"),
		new SampleConflict("/test/test2.doc", true , false, 20,21,"12/31/2009","12/31/2009"),
		new SampleConflict("/test/test3.doc", true , false, 20,21,"12/31/2009","12/31/2009"),
		new SampleConflict("/test/test4.doc", true , false, 20,21,"12/31/2009","12/31/2009")
				};
		
		}
		
	}
}