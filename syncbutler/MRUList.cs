using System;
using System.Collections.Generic;
using System.IO;

namespace SyncButler
{
    /// <summary>
    /// A repesentation a list of MRUs
    /// </summary>
    public class MRUList
    {
        private List<string> MRUs;

        /// <summary>
        /// Initialize an instance of MRU.
        /// </summary>
        public MRUList()
        {
            MRUs = new List<string>();

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
            String SyncTo = LetterDrive + ":\\SyncButler\\" + ComputerName + "\\";
            List<Conflict> Conflicts = new List<Conflict>();
            if (!Directory.Exists(SyncTo))
            {
                Directory.CreateDirectory(SyncTo);
            }
            foreach (string mru in MRUs)
            {
                if(File.Exists(mru))
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
                        else if (!WindowsFile.HaveEqualChecksums(MRUFile,Target))
                        {
                            Conflicts.Add(new Conflict(MRUFile, Target, Conflict.Action.CopyToRight));
                        }
                    }

                }
            }
           return Conflicts;
        }
    }
}
