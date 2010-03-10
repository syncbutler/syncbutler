using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

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
                for (int i = 0; i < mrus.Length; i++)
                {
                    mrus[i] = new SyncedMRU(MRUs[i], SyncTo + Path.GetFileName(MRUs[i]));
                }
                return mrus;
            }
            set
            {
                if (value == null)
                    return;
                SyncedMRU[] mru = (SyncedMRU[])value;
                if (MRUs == null)
                    MRUs = new List<string>();
                MRUs.Clear();
                foreach (SyncedMRU s in mru)
                {
                    MRUs.Add(s.OriginalPath);
                }
            }
        }

        private List<string> MRUs;

        private string SyncTo;

        /// <summary>
        /// Initialize an instance of MRU.
        /// </summary>
        public MRUList()
        {
            MRUs = new List<string>();

            //
        }

        public void Load()
        {
            MRUs.AddRange(MostRecentlyUsedFile.Get().Values);
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
            foreach (string mru in MRUs)
            {
                if (File.Exists(mru))
                {
                    string Filename = Path.GetFileName(mru);

                    if (!File.Exists(SyncTo + Filename))
                    {
                        File.Copy(mru, SyncTo + Filename);
                    }
                    else
                    {
                        WindowsFile MRUFile = new WindowsFile(mru);
                        WindowsFile Target = new WindowsFile(SyncTo + Filename);
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


        /// TODO: find a way to embed xslt file to exe, and to extract it.
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
