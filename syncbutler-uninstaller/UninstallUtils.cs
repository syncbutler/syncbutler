using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace SyncButler.Uninstaller
{
    /// <summary>
    /// UninstallUtils provides the basic framework for cleaning up stuff left by Sync Butler.
    /// </summary>
    public class UninstallUtils
    {
        /* Registry keys that should be deleted (in proper order) */
        public static string[] DELETE_REG_LIST = {
            @"Software\Classes\AllFilesystemObjects\shell\Sync Butler, Sync!",
            @"Software\Classes\AllFilesystemObjects\shell\Mini-Sync This!",
        };

        /* Files that should remain (file names only, no full paths) */
        public static string[] DELETE_FILE_LIST = {
            @"log.xml",
            @"logstyle.css",
            @"*.butler"
        };

        /* Files that should always remain even if they match an entry in the DELETE_FILE_LIST */
        public static string[] KEEP_FILE_LIST = {
            @"Microsoft.Expression.Interactions.dll",
            @"SyncButler.dll",
            @"SyncButler.exe",
            @"System.Windows.Interactivity.dll",
            @"WPFToolkit.dll"
        };

        /// <summary>
        /// Performs an uninstall.
        /// </summary>
        /// <returns>An array containing messages/issues encountered during the sync - typically any exceptions that were caught.</returns>
        public static string[] Uninstall()
        {
            List<string> issues = new List<string>();

            foreach (string key in DELETE_REG_LIST)
            {
                try
                {
                    RemoveRegistryKey(key);
                }
                catch (Exception e)
                {
                    issues.Add("Exception occurred while removing registry key (" + key + "): " + e.GetType().Name + ": " + e.Message);
                }
            }

            foreach (string file in DELETE_FILE_LIST)
            {

                if (file.Contains("*"))
                {
                    string[] results = Directory.GetFiles(GetRunningDirectory(), file);

                    foreach (string result in results)
                    {
                        try
                        {
                            if (!IsFileAllowed(result))
                                File.Delete(result);
                        }
                        catch (Exception e)
                        {
                            issues.Add("Exception occurred while deleting file (" + file + "): " + e.GetType().Name + ": " + e.Message);
                        }
                    }
                }
                else
                {
                    try
                    {
                        if (!IsFileAllowed(file))
                            RemoveFile(file);
                    }
                    catch (Exception e)
                    {
                        issues.Add("Exception occurred while deleting file (" + file + "): " + e.GetType().Name + ": " + e.Message);
                    }
                }
            }

            return issues.ToArray();
        }

        /// <summary>
        /// Gets the directory that the uninstaller is running from.
        /// </summary>
        /// <returns>A string of the directory that this uninstaller is running from.</returns>
        public static string GetRunningDirectory()
        {
            string currDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int lastSlashIndex = currDir.LastIndexOf('\\');
            currDir = currDir.Substring(0, lastSlashIndex);

            return currDir;
        }

        /// <summary>
        /// Gets the name of the current executable.
        /// </summary>
        /// <returns>The string of the name of this executable.</returns>
        public static string GetCurrentExeName()
        {
            string currExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int lastSlashIndex = currExe.LastIndexOf('\\');
            currExe = currExe.Substring(lastSlashIndex + 1);

            return currExe;
        }

        /// <summary>
        /// Checks whether a file is in the keep list.
        /// </summary>
        /// <param name="file">Path to the file you wish to check.</param>
        /// <returns>True if file is to be kept, false otherwise.</returns>
        public static bool IsFileAllowed(string file)
        {
            bool allowed = false;

            foreach (string entry in KEEP_FILE_LIST)
            {
                if (entry.EndsWith(file) || entry.Equals(file))
                {
                    allowed = true;
                    break;
                }
            }

            return allowed;
        }

        /// <summary>
        /// Gets the list of immediate subkeys within a given key of the registry.
        /// </summary>
        /// <param name="path">The parent key.</param>
        /// <returns>Array containing the names of the subkeys under the parent key provided.</returns>
        public static string[] GetRegistrySubKeys(string path)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(path);
            return key.GetSubKeyNames();
        }

        /// <summary>
        /// Deletes a registry key and all its subkeys in a recursive manner.
        /// </summary>
        /// <param name="path">The key to delete.</param>
        public static void RemoveRegistryKey(string path)
        {
            Registry.CurrentUser.DeleteSubKeyTree(path);
        }

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        /// <param name="path">Path of the file to delete.</param>
        public static void RemoveFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
