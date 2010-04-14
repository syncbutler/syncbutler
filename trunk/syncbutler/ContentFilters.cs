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
using System.Text.RegularExpressions;

namespace SyncButler
{
    /// <summary>
    /// This class recongises a large list of file formats and their relative importance
    /// to the user. Its knowledge is used to select useful files for the user.
    /// The secondary role of the class is to filter out private or personal folders and
    /// preventing them from being synced (or taken out of the system). This filter is limited
    /// to SyncButlerSync only! It does not affect normal syncing.
    /// </summary>
    class ContentFilters
    {

        #region Censoredword
        //Censorship
        private static string englishSexualConnotation =
            "(breast)|(\\scum\\s)|(cunt)|(blowjob)|(blow job)|(anal)|(booty)|(pussy\\s)|(tits)|(titty)|(titties)|(wank)|" +
            "(whore)|(dildo)|(suck my dick)|(cock suck)|(dick suck)|(penis suck)|(jack off)|(masturbation)|(jerk off)|" +
            "(ejaculat)|(jizz)|(porn)|(p0rn)|(\\shumping\\s)";

        private static string japaneseSexualConnotation =
            "(hentai)|(変態)|(パイ刷り)|(パイずり)|(paizuri)|(淫具)|(ingu)|(レイプ)|(reipo)|(強淫)|(ごういん)|(gouin)|" +
            "(陰道)|(いんどう)|(indou)|(打っ掛け)|(ぶっか)|(18 禁)";

        private static string vulgarities =
            "(fuck)|(asshole)|(arse)|(bastard)|(bitch)|(faggot)|(fagg)|(slut)|(turd)|(effin)|(motherfucker)|(jackasss)";

        private static string confidentiality =
            "(top secret)|(secret)|(confidential)|(restricted)|(classified)|(authorized)|(authorised)|" +
            "(need to know)|(official use)|(encrypt)|(eyes only)";

        private static string personal =
            "(personal)|(private)|(sensitive)|(initimate)|(embarrassing)";

         

        private static string sensitiveContentPattern =
            englishSexualConnotation + "|" + japaneseSexualConnotation + "|" +
            vulgarities + "|" + confidentiality + "|" + personal;
        #endregion

        #region Extension
        //Combined censorship filter
        private static Regex sensitivePattern;
                
        //Interesting file types
        //
        //Research was done on user data and types of jobs out there and the types of
        //file that they generate
        //
        //Version 2 of this design will allow dynamic selection of value and format
        //types
        //
        //Catergories:
        //videoFormats
        //audioFormats
        //pictureFormats
        //sourceCodeFormats
        //productivityFormats
        //mediaCreationFormats
        //archiveFormats
        //programSetupFormats
        //engineeringFormats
        //gameFormats

        private static string videoFormats =
            "(\\.avi)|(\\.wmv)|(\\.mkv)|(\\.rmvb)|(\\.mp4)|(\\.m4v)|(\\.mov)|(\\.mpg)|(\\.flv)";

        private static string audioFormats =
            "(\\.wma)|(\\.mp3)|(\\.ogg)|(\\.flac)|(\\.m4a)|(\\.alac)|(\\.wav)|(\\.aac)|(\\.midi)|(\\.mid)";

        private static string pictureFormats =
            "(\\.jpg)|(\\.jpeg)|(\\.gif)|(\\.png)|(\\.raw)|(\\.bmp)|(\\.tiff)";

        //PL can mean Perl or Prolog, HS is Haskell, F is Fortran
        //AS is Actionscript
        private static string sourceCodeFormats =
            "(\\.c)|(\\.cpp)|(\\.cs)|(\\.java)|(\\.pl)|(\\.py)|(\\.php)|(\\.pas)|(\\.js)|(\\.jsp)|(\\.asp)|(\\.f)|(\\.hs)|(\\.as)";

        //Microsoft Office, OpenOffice, LaTeX
        private static string productivityFormats =
            "(\\.doc)|(\\.docx)|(\\.xls)|(\\.xlsx)|(\\.ppt)|(\\.pptx)|(\\.rtf)|(\\.odt)|(\\.ods)|(\\.odp)|(\\.pdf)|" +
            "(\\.accdb)|(\\.sdb)|(\\.txt)|(\\.csv)|(\\.tex)";

        //AUP is Audacity project, CEL is Adobe Audition, CPR is Cubase,
        //NPR is Nuendo, CWP is Cakewalk, Premiere Pro, other Adobe Suite
        private static string mediaCreationFormats =
            "(\\.aup)|(\\.cel)|(\\.cpr)|(\\.npr)|(\\.cwp)|(\\.prproj)|(\\.ai)|(\\.flp)|(\\.fla)|(\\.psd)|(\\.pdd)|(\\.drw)";

        private static string archiveFormats =
            "(\\.7z)|(\\.rar)|(\\.zip)";

        private static string programSetupFormats =
            "(setup)|(\\.msi)";

        //AutoCAD, 3dStudioMax
        private static string engineeringFormats =
            "(\\.dwg)|(\\.dwf)|(\\.3ds)|(\\.max)";

        private static string gameFormats =
            "(\\.sav)";
        #endregion

        //This sorted list will be iterated to determine which level of interest
        //the file type is in.
        private static SortedList<String, ValueLevel> interestingFormatValue;
        private static Regex interestingPattern;

        private static string interestingFormatPattern =
            videoFormats + "|" + audioFormats + "|" + productivityFormats + "|" +
            pictureFormats + "|" + sourceCodeFormats + "|" + mediaCreationFormats + "|" + archiveFormats + "|" +
            programSetupFormats + "|" + engineeringFormats + "|" + gameFormats;

        //Some conversion of interest level to the number of files to copy
        public enum ValueLevel {None = 0, UltraLow = 2, Low = 3, LowMed = 5, Med = 8, MedHigh = 10, High = 15 };

        /// <summary>
        /// Determines if the file contents is sensitive by inspecting its filename
        /// </summary>
        /// <param name="text">The text to be checked</param>
        /// <returns>True if it may be sensitve content</returns>
        public static bool isSensitive(string text)
        {
            if(sensitivePattern == null)
                sensitivePattern = new Regex(sensitiveContentPattern);

            Match match = Regex.Match(text.ToLower(), sensitiveContentPattern);

            if (match.Success)
                return true;
            
            return false;
        }

        /// <summary>
        /// Retrieves the value of the enumeration as an integer.
        /// </summary>
        /// <param name="d">A ValueLevel</param>
        /// <returns>An integer representing the value of the parameter.</returns>
        private static int ConvertEnumToInt(ValueLevel d)
        {
            return (int)Enum.Parse(typeof(ValueLevel), Enum.GetName(typeof(ValueLevel), d));
        }

        /// <summary>
        /// Splits a whole list of most recently used files into several bands based on level of interest.
        /// </summary>
        /// <param name="mrus">A sorted list of MRUs</param>
        /// <returns>A sorted list containing 6 different keys with 6 lists of MRUs banded together based on the level of interest.</returns>
        public static SortedList<string, SortedList<string, string>> Spilt(SortedList<string, string> mrus)
        {
            SortedList<string, string> interestingUltraLow = new SortedList<string, string>();
            SortedList<string, string> interestingLow = new SortedList<string, string>();
            SortedList<string, string> interestingLowMed = new SortedList<string, string>();
            SortedList<string, string> interestingMed = new SortedList<string, string>();
            SortedList<string, string> interestingMedHigh = new SortedList<string, string>();
            SortedList<string, string> interestingHigh = new SortedList<string, string>();

            SortedList<string, string> sensitive = new SortedList<string, string>();
            int InterestingLevel = 0;
            foreach (string filename in mrus.Keys)
            {
                InterestingLevel = ConvertEnumToInt(IsInteresting(filename));
                if (isSensitive(filename))
                {
                    sensitive.Add(filename, mrus[filename]);
                }
                else if (InterestingLevel != 0)
                {
                    switch (InterestingLevel)
                    {
                        case 2:
                            interestingUltraLow.Add(filename, mrus[filename]);
                            break;
                        case 3:
                            interestingLow.Add(filename, mrus[filename]);
                            break;
                        case 5:
                            interestingLowMed.Add(filename, mrus[filename]);
                            break;
                        case 8:
                            interestingMed.Add(filename, mrus[filename]);
                            break;
                        case 10:
                            interestingMedHigh.Add(filename, mrus[filename]);
                            break;
                        case 15:
                            interestingHigh.Add(filename, mrus[filename]);
                            break;
                        
                    }
                }
            }
            SortedList<string, SortedList<string, string>> spilt = new SortedList<string, SortedList<string, string>>();
            spilt.Add("interestingUltraLow", interestingUltraLow);
            spilt.Add("interestingLow", interestingLow);
            spilt.Add("interestingLowMed", interestingLowMed);
            spilt.Add("interestingMed", interestingMed);
            spilt.Add("interestingMedHigh", interestingMedHigh);
            spilt.Add("interestingHigh", interestingHigh);
            spilt.Add("sensitive",sensitive);
            return spilt;
               
        }

        /// <summary>
        /// Determines if the file contents is interesting by inspecting its filename
        /// and returns the Value Level that can be matched to the number of files
        /// that should be copied for this particular format.
        /// </summary>
        /// <param name="text">The text to be checked (filename for example)</param>
        /// <returns>A ValueLevel that ranges from 0 to 15. 0 represent no value</returns>
        public static ValueLevel IsInteresting(string text)
        {
            string lowerCapsText = text.ToLower();

            if (interestingPattern == null)
                interestingPattern = new Regex(interestingFormatPattern);

            Match match = interestingPattern.Match(lowerCapsText);

            //If it matches at least one of the formats, then iterate through
            //the format list to find which one it correspond to and obtain the
            //value worth of the format
            if (match.Success)
            {
                if(interestingFormatValue == null)
                    prepareInterestingFormatValue();

                foreach (string format in interestingFormatValue.Keys)
                {
                    Match formatMatch = Regex.Match(lowerCapsText, format);

                    if (formatMatch.Success)
                    {
                        return interestingFormatValue[format];
                    }
                }
            }

            return ValueLevel.None;
        }

        /// <summary>
        /// This is a class only method that prepares the format and their respective
        /// value based on a fixed ratio. It can however be scaled to fit appropriately
        /// to obtain the most useful list of files with the leat amount of space
        /// </summary>
        private static void prepareInterestingFormatValue()
        {
            interestingFormatValue = new SortedList<string,ValueLevel>();

            //In general, it will be to cap the total data transffered to be
            //< 20MB per catergory (except for video which it may hit 1GB)
            //In the future, but having a scale, this ratio can be scaled
            //according to the size of the storage
            interestingFormatValue.Add(videoFormats, ValueLevel.UltraLow);
            interestingFormatValue.Add(audioFormats, ValueLevel.Med);
            interestingFormatValue.Add(pictureFormats, ValueLevel.MedHigh);
            interestingFormatValue.Add(sourceCodeFormats, ValueLevel.MedHigh);
            interestingFormatValue.Add(productivityFormats, ValueLevel.High);
            interestingFormatValue.Add(mediaCreationFormats, ValueLevel.Low);
            interestingFormatValue.Add(programSetupFormats, ValueLevel.Low);
            interestingFormatValue.Add(engineeringFormats, ValueLevel.Low);
            interestingFormatValue.Add(archiveFormats, ValueLevel.Low);
            interestingFormatValue.Add(gameFormats, ValueLevel.LowMed);      
        }
    }
}

/*
private static string customFormats =
    "";
*/