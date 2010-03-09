using System;
using SyncButler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace SyncButlerTest
{
    /// <summary>
    ///This is a test class for ControllerTest and is intended
    ///to contain all ControllerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ControllerTest
    {
        Partnership actual;
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(ArgumentException), "A folder was allowed to partner with a non-folder.")]
        public void LeftFolderRightFileCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1";
            string rightPath = @"C:\Folder2\test.txt";
            //Partnership actual;
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(ArgumentException), "A folder was allowed to partner with a non-folder.")]
        public void RightFolderLeftFileCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1";
            string rightPath = @"C:\Folder2\test.txt";
            //Partnership actual;
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.DirectoryNotFoundException), "A folder was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentFoldersCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\FolderNOTFOUND";
            string rightPath = @"C:\FolderNOTFOUND2";
            //Partnership actual;
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.DirectoryNotFoundException), "A folder was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentLeftFolderCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\FolderNOTFOUND";
            string rightPath = @"C:\Folder2";
            //Partnership actual;
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.DirectoryNotFoundException), "A folder was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentRightFolderCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1";
            string rightPath = @"C:\FolderNOTFOUND";
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.FileNotFoundException), "A file was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentFilesCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1\NOTFOUND.txt";
            string rightPath = @"C:\Folder2\NOTFOUND.txt";
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.FileNotFoundException), "A file was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentLeftFileCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1\NOTFOUND.txt";
            string rightPath = @"C:\Folder2";
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        [ExpectedException(typeof(System.IO.FileNotFoundException), "A file was not found and an exception was expected.")]
        //MODIFY: Type of exception to be expected
        public void NonExistentRightFileCreatePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1";
            string rightPath = @"C:\Folder2\NOTFOUND.txt";
            target.AddPartnership(name, leftPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CreateFolderPartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1\";
            string rightPath = @"C:\Folder2\";
            target.AddPartnership(name, leftPath, rightPath);
            Assert.AreEqual(actual.Name, name);
            Assert.AreEqual(actual.LeftFullPath, leftPath);
            Assert.AreEqual(actual.RightFullPath, rightPath);
        }

        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CreateFilePartnershipTest()
        {
            Controller_Accessor target = new Controller_Accessor();
            string name = "Test Partnership";
            string leftPath = @"C:\Folder1\test.txt";
            string rightPath = @"C:\Folder2\test.txt";
            target.AddPartnership(name, leftPath, rightPath);
            Assert.AreEqual(actual.Name, name);
            Assert.AreEqual(actual.LeftFullPath, leftPath);
            Assert.AreEqual(actual.RightFullPath, rightPath);
        }
    }
}
