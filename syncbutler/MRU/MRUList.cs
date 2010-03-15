using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;

namespace SyncButler.MRU
{
    /// <summary>
    /// A repesentation a list of MRUs
    /// </summary>
    /// 
    [XmlRoot("MRUList", IsNullable = false)]
    public class MRUList
    {
        [XmlElement(Type = typeof(string))]
        public string ComputerName { get; set; }

        [XmlArray("MRUs"), XmlArrayItem("MRU", typeof(SyncedMRU))]
        public SyncedMRU[] MRUListing
        {
            get
            {
                SyncedMRU[] mrus = new SyncedMRU[MRUs.Count];
                int i = 0;
                foreach(string mru in MRUs.Values)
                {
                    mrus[i] = new SyncedMRU(mru, SyncTo + Path.GetFileName(mru));
                    i++;
                }
                return mrus;
            }
            set
            {
                if (value == null)
                    return;
                SyncedMRU[] mru = (SyncedMRU[])value;
                List<string> mrus = new List<string>();
                //if (MRUs == null)
                //    MRUs = new SortedList<string,string>();
                //MRUs.Clear();
                foreach (SyncedMRU s in mru)
                {
                    mrus.Add(s.OriginalPath);
                }
                MRUs = MostRecentlyUsedFile.ConvertToSortedList(mrus);
            }
        }

        private SortedList<string, string> MRUs;

        private string SyncTo;

        /// <summary>
        /// Initialize an instance of MRU.
        /// </summary>
        public MRUList()
        {
            MRUs = new SortedList<string,string>();
            //Load();
        }

        public void Load(SortedList<string,string> mrus)
        {
            MRUs = mrus;
        }


        /// <summary>
        /// Sync the MRU to SyncButler folder. Syncs in a manner similar to
        /// the classic synchorinisation used for files and folders. It will
        /// not sync files not found but listed in MRU
        /// </summary>
        /// <param name="ComputerName">Name of the computer</param>
        /// <param name="LetterDrive">Letter drive of the device to the target folder</param>
        /// <returns>The list of conflics</returns>
        public List<Conflict> Sync(string ComputerName, string LetterDrive)
        {
            SyncTo = LetterDrive + ":\\SyncButler\\" + ComputerName + "\\";
            this.ComputerName = ComputerName;
            List<Conflict> Conflicts = new List<Conflict>();
            if (!Directory.Exists(SyncTo))
            {
                Directory.CreateDirectory(SyncTo);
            }
            //foreach (string mru in MRUs)
            foreach(string mru in MRUs.Keys)
            {
                if (File.Exists(MRUs[mru]))
                {
                    //string Filename = Path.GetFileName(mru);

                    if (!File.Exists(SyncTo + mru))
                    {
                        File.Copy(MRUs[mru], SyncTo + mru);
                    }
                    else
                    {
                        WindowsFile MRUFile = new WindowsFile(MRUs[mru]);
                        WindowsFile Target = new WindowsFile(SyncTo + mru);
                        if (MRUFile.Length != Target.Length)
                        {
                            Conflicts.Add(new Conflict(MRUFile, Target, Conflict.Action.CopyToRight));
                        }
                        else if (!WindowsFile.HaveEqualChecksums(MRUFile, Target))
                        {
                            Conflicts.Add(new Conflict(MRUFile, Target, Conflict.Action.CopyToRight));
                        }
                    }

                }
            }
            return Conflicts;
        }


        /// <summary>
        /// Save information of a synced MRU into a xml format
        /// This xml file comes with a xslt file which can use to format the xml to a readable format
        /// </summary>
        /// <param name="filename">File of the xml file</param>
        /// <param name="mrus">MRU that is to be saved</param>
        public static void SaveInfoTo(String filename, MRUList mrus)
        {
            System.Xml.Serialization.XmlSerializer xmlS = new System.Xml.Serialization.XmlSerializer(typeof(MRUList));
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineChars = "\r\n";
            XmlWriter xtw = XmlWriter.Create(filename, settings);
            xtw.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"SyncedFile.xslt\"");
            xmlS.Serialize(xtw, mrus);
            xtw.Close();

            // extract the xslt file
            Assembly assemby;
            assemby = Assembly.GetExecutingAssembly();
            Stream s = assemby.GetManifestResourceStream("SyncButler.MRU.SyncedFile.xslt");
            StreamReader sr = new StreamReader(s);
            string xsltFilename = Path.GetDirectoryName(filename) + "\\SyncedFile.xslt";
            StreamWriter sw = new StreamWriter(xsltFilename);
            sw.Write(sr.ReadToEnd());
            sw.Close();
            sr.Close();
        }

        /// <summary>
        /// Load Information of synced MRUs from a xml file
        /// </summary>
        /// <param name="filename">File where the information of synced MRUs are stroed</param>
        /// <returns>a MRUList that is stored in the file</returns>
        public static MRUList LoadInfoFrom(String filename)
        {
            System.Xml.Serialization.XmlSerializer xmlS = new System.Xml.Serialization.XmlSerializer(typeof(MRUList));

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            XmlReader xr = XmlReader.Create(filename, settings);

            MRUList mrus;
            mrus = (MRUList)xmlS.Deserialize(xr);

            xr.Close();
            return mrus;
        }
    }
}
