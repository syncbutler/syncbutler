using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncButler
{
    public class ComputerNameChecker
    {
        private static string[] reserved = { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8" };
        private static string pattern = "^[A-Za-z_0-9]+$";

        public static bool IsComputerNameValid(String ComputerName)
        {
            Regex regex = new Regex(pattern);
            if (ComputerName.Length != 0)
            {
                if (regex.IsMatch(ComputerName))
                {
                    if (Array.IndexOf(reserved, ComputerName) == -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
