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
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using SyncButler.Exceptions;

namespace SyncButler.MRU
{
    /// <summary>
    /// A repesentation a list of MRUs
    /// </summary>
    /// 
    [XmlRoot("MRUList", IsNullable = false)]
    public class MRUList
    {
        // The delegate to report progress to. Can be null
        protected SyncableStatusMonitor statusMonitor = null;
        protected SyncableErrorHandler errorHandler = null;

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
        public List<Conflict> Sync(string ComputerName, char LetterDrive)
        {
            SyncTo = LetterDrive + ":\\SyncButler\\" + ComputerName + "\\";
            this.ComputerName = ComputerName;
            List<Conflict> Conflicts = new List<Conflict>();
            if (WindowsFolder.CheckIfUserHasRightsTo(SyncTo, System.Security.Principal.WindowsIdentity.GetCurrent().Name))
            {
                if (!Directory.Exists(SyncTo))
                {
                    Directory.CreateDirectory(SyncTo);
                }
            }
            else
            {
                if (errorHandler != null)
                {
                    if (!errorHandler(new Exception("Please Check if you have permission to SBS folder")))
                        return Conflicts;
                }
            }
            foreach(string mru in MRUs.Keys)
            {
                try
                {
                    if (File.Exists(MRUs[mru]))
                    {

                        if (!File.Exists(SyncTo + mru))
                        {
                            this.Copy(MRUs[mru], SyncTo + mru);
                        }
                        else
                        {
                            if (!statusMonitor(new SyncableStatus(MRUs[mru], 0, 0, SyncableStatus.ActionType.Sync)))
                                return Conflicts;

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
                catch (Exception e)
                {
                    if (errorHandler != null)
                    {
                        if (errorHandler(e)) continue;
                        else break;
                    }
                    else throw e;
                }
            }
            return Conflicts;
        }

        protected void Copy(string sourcePath, string destPath)
        {
            FileInfo sourceFile = new FileInfo(sourcePath);
            FileInfo destFile = new FileInfo(destPath);

            //// Make sure there's enough free space.
            //if ((sourceFile.Length + 4096) > SystemEnvironment.StorageDevices.GetAvailableSpace(DRIVE LETTER GOES HERE))
            //    throw new IOException("There is insufficient space to copy the file to " + destFile.nativeFileObj.FullName);

            int bufferSize = (int)SyncEnvironment.FileReadBufferSize;

            FileStream inputStream = sourceFile.OpenRead();
            FileStream outputStream = null;

            string tempName = null;
            for (int i = 0; i < 10000; i++)
            {
                tempName = destFile.FullName + "." + i + ".syncbutler_safecopy";
                if (File.Exists(tempName)) continue;
                outputStream = new FileStream(tempName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                break;
            }

            if (outputStream == null) throw new IOException("Could not create a temporary file to be used for safe copying");

            byte[] buf = new byte[bufferSize];
            long totalCopied = 0;
            int amountRead;
            float toPercent = 100f / sourceFile.Length;

            do
            {
                amountRead = inputStream.Read(buf, 0, bufferSize);
                if (amountRead > 0) outputStream.Write(buf, 0, amountRead);

                totalCopied += amountRead;

                if (statusMonitor != null)
                {
                    if (!statusMonitor(new SyncableStatus(sourceFile.FullName, 0, (int)(totalCopied * toPercent), SyncableStatus.ActionType.Copy)))
                    {
                        inputStream.Close();
                        outputStream.Close();
                        File.Delete(tempName);
                        throw new UserCancelledException();
                    }
                }

            } while (amountRead > 0);

            inputStream.Close();
            outputStream.Close();

            if (destFile.Exists) destFile.Delete();
            File.Move(tempName, destFile.FullName);

            destFile.LastWriteTime = sourceFile.LastWriteTime;
            destFile.CreationTime = sourceFile.CreationTime;
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
            xtw.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"Design.xslt\"");
            xmlS.Serialize(xtw, mrus);
            xtw.Close();

            // extract the xslt file
            Assembly assemby;
            assemby = Assembly.GetExecutingAssembly();
            Stream s = assemby.GetManifestResourceStream("SyncButler.MRU.Design.xslt");
            StreamReader sr = new StreamReader(s);
            string xsltFilename = Path.GetDirectoryName(filename) + "\\Design.xslt";
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

        /// <summary>
        /// Sets a delegate to be used to report progress
        /// </summary>
        /// <param name="statusMonitor"></param>
        public void SetStatusMonitor(SyncableStatusMonitor statusMonitor)
        {
            this.statusMonitor = statusMonitor;
        }

        /// <summary>
        /// Sets a delagate to handle errors while syncing.
        /// </summary>
        /// <param name="errorHandler"></param>
        public void SetErrorHandler(SyncableErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;
        }
    }
}
