using SyncButler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace SyncButlerTest
{
    /// <summary>
    ///This is a test class for SyncEnvironmentTest and is intended
    ///to contain all SyncEnvironmentTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SyncEnvironmentTest
    {
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

        /// <summary>
        ///A test for writeMRUFile
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void writeMRUFileTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor();
            string drivePrefix = Path.GetTempPath() + "writeMRUFileTest";
            string friendlyName = "writeMRUFileTest";
            string content = "This is a test";
            target.writeMRUFile(drivePrefix, friendlyName, content);
            
            //Try to read the file
            string readContent = "";
            string pathToTestFile = Path.GetTempPath() + "writeMRUFileTest\\writeMRUFileTest.txt";
            TextReader tr = new StreamReader(pathToTestFile);
            readContent = tr.ReadToEnd();
            tr.Close();

            //Testing
            Assert.AreEqual(content, readContent);           
        }

        /// <summary>
        ///A test for UpdatePartnership
        ///</summary>
        [TestMethod()]
        public void UpdatePartnershipTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            string newname = string.Empty; // TODO: Initialize to an appropriate value
            string leftpath = string.Empty; // TODO: Initialize to an appropriate value
            string rightpath = string.Empty; // TODO: Initialize to an appropriate value
            target.UpdatePartnership(name, newname, leftpath, rightpath);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for StoreEnv
        ///</summary>
        [TestMethod()]
        public void StoreEnvTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.StoreEnv();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for RestoreEnv
        ///</summary>
        [TestMethod()]
        public void RestoreEnvTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.RestoreEnv();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for RemovePartnership
        ///</summary>
        [TestMethod()]
        public void RemovePartnershipTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            int idx = 0; // TODO: Initialize to an appropriate value
            target.RemovePartnership(idx);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for ReflectiveUnserialize
        ///</summary>
        [TestMethod()]
        public void ReflectiveUnserializeTest()
        {
            string xmlString = string.Empty; // TODO: Initialize to an appropriate value
            object expected = null; // TODO: Initialize to an appropriate value
            object actual;
            actual = SyncEnvironment.ReflectiveUnserialize(xmlString);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for readMRUFile
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void readMRUFileTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string drivePrefix = string.Empty; // TODO: Initialize to an appropriate value
            string friendlyName = string.Empty; // TODO: Initialize to an appropriate value
            string expected = string.Empty; // TODO: Initialize to an appropriate value
            string actual;
            actual = target.readMRUFile(drivePrefix, friendlyName);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for LoadPartnership
        ///</summary>
        [TestMethod()]
        public void LoadPartnershipTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            Partnership expected = null; // TODO: Initialize to an appropriate value
            Partnership actual;
            actual = target.LoadPartnership(name);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for IntialEnv
        ///</summary>
        [TestMethod()]
        public void IntialEnvTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.IntialEnv();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for InitSyncButlerAssembly
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void InitSyncButlerAssemblyTest()
        {
            SyncEnvironment_Accessor.InitSyncButlerAssembly();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for GetSettingsFileName
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void GetSettingsFileNameTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string expected = string.Empty; // TODO: Initialize to an appropriate value
            string actual;
            actual = target.GetSettingsFileName();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for GetPartnerships
        ///</summary>
        [TestMethod()]
        public void GetPartnershipsTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            SortedList<string, Partnership> expected = null; // TODO: Initialize to an appropriate value
            SortedList<string, Partnership> actual;
            actual = target.GetPartnerships();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for GetInstance
        ///</summary>
        [TestMethod()]
        public void GetInstanceTest()
        {
            SyncEnvironment expected = null; // TODO: Initialize to an appropriate value
            SyncEnvironment actual;
            actual = SyncEnvironment.GetInstance();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for DecodeChecksumKey
        ///</summary>
        [TestMethod()]
        public void DecodeChecksumKeyTest()
        {
            string key = string.Empty; // TODO: Initialize to an appropriate value
            ChecksumKey expected = new ChecksumKey(); // TODO: Initialize to an appropriate value
            ChecksumKey actual;
            actual = SyncEnvironment.DecodeChecksumKey(key);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CreatePartnership
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CreatePartnershipTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            string leftPath = string.Empty; // TODO: Initialize to an appropriate value
            string rightPath = string.Empty; // TODO: Initialize to an appropriate value
            Partnership expected = null; // TODO: Initialize to an appropriate value
            Partnership actual;
            actual = target.CreatePartnership(name, leftPath, rightPath);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CreateFolderPartner
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CreateFolderPartnerTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string leftpath = string.Empty; // TODO: Initialize to an appropriate value
            string rightpath = string.Empty; // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            Partnership expected = null; // TODO: Initialize to an appropriate value
            Partnership actual;
            actual = target.CreateFolderPartner(leftpath, rightpath, name);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CreateFilePartner
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CreateFilePartnerTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string leftdir = string.Empty; // TODO: Initialize to an appropriate value
            string rightdir = string.Empty; // TODO: Initialize to an appropriate value
            string leftpath = string.Empty; // TODO: Initialize to an appropriate value
            string rightpath = string.Empty; // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            Partnership expected = null; // TODO: Initialize to an appropriate value
            Partnership actual;
            actual = target.CreateFilePartner(leftdir, rightdir, leftpath, rightpath, name);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for CreateEnv
        ///</summary>
        [TestMethod()]
        public void CreateEnvTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.CreateEnv();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for ConvertXML2PartnershipList
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void ConvertXML2PartnershipListTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.ConvertXML2PartnershipList();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for ConvertPartnershipList2XML
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void ConvertPartnershipList2XMLTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            target.ConvertPartnershipList2XML();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for CheckFilePartnerAbility
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void CheckFilePartnerAbilityTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string leftpath = string.Empty; // TODO: Initialize to an appropriate value
            string rightpath = string.Empty; // TODO: Initialize to an appropriate value
            bool expected = false; // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.CheckFilePartnerAbility(leftpath, rightpath);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for AddPartnership
        ///</summary>
        [TestMethod()]
        public void AddPartnershipTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor(); // TODO: Initialize to an appropriate value
            string name = string.Empty; // TODO: Initialize to an appropriate value
            string leftPath = string.Empty; // TODO: Initialize to an appropriate value
            string rightPath = string.Empty; // TODO: Initialize to an appropriate value
            target.AddPartnership(name, leftPath, rightPath);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for SyncEnvironment Constructor
        ///</summary>
        [TestMethod()]
        [DeploymentItem("SyncButler.dll")]
        public void SyncEnvironmentConstructorTest()
        {
            SyncEnvironment_Accessor target = new SyncEnvironment_Accessor();
            Assert.Inconclusive("TODO: Implement code to verify target");
        }
    }
}
