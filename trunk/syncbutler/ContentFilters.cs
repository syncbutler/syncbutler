﻿using System;
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
        //Censorship
        private static string englishSexualConnotation =
            "breast|\\scum\\s|cunt|blowjob|blow job|anal|booty|pussy\\s|tits|titty|titties|wank|" +
            "whore|dildo|suck my dick|cock suck|dick suck|penis suck|jack off|masturbation|jerk off|" +
            "ejaculat|jizz|porn|p0rn|\\shumping\\s";

        private static string japaneseSexualConnotation =
            "hentai|変態|パイ刷り|パイずり|paizuri|淫具|ingu|レイプ|reipo|強淫|ごういん|gouin" +
            "陰道|いんどう|indou|打っ掛け|ぶっか|";

        private static string vulgarities =
            "fuck|asshole|arse|bastard|bitch|faggot|fagg|slut|turd|effin|motherfucker|jackasss";

        private static string confidentiality =
            "top sercret|secret|confidential|restricted|classified|authorized|authorised|" +
            "need to know|official use|encrypt|eyes only";

        private static string personal =
            "personal|private|sensitive|initimate|embarrassing";

        private static string sensitiveContentPattern =
            englishSexualConnotation + "|" + japaneseSexualConnotation + "|" +
            vulgarities + "|" + confidentiality + "|" + personal;

        //Combined censorship filter
        private static Regex sensitivePattern = null;
                
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
            ".avi|.wmv|.mkv|.rmvb|.mp4|.m4v|.mov|.mpg|.flv";

        private static string audioFormats =
            ".wma|.mp3|.ogg|.flac|.m4a|.alac|.wav|.aac|.midi|.mid";

        private static string pictureFormats =
            ".jpg|.jpeg|.gif|.png|.raw|.bmp|.tiff";

        //PL can mean Perl or Prolog, HS is Haskell, F is Fortran
        //AS is Actionscript
        private static string sourceCodeFormats =
            ".c|.cpp|.cs|.java|.pl|.py|.php|.pas|.js|.jsp|.asp|.f|.hs|.as";

        //Microsoft Office, OpenOffice, LaTeX
        private static string productivityFormats =
            ".doc|.docx|.xls|.xlsx|.ppt|.pptx|.rtf|.odt|.ods|.odp|.pdf|" +
            ".accdb|.sdb|.txt|.csv|.tex";

        //AUP is Audacity project, CEL is Adobe Audition, CPR is Cubase,
        //NPR is Nuendo, CWP is Cakewalk, Premiere Pro, other Adobe Suite
        private static string mediaCreationFormats =
            ".aup|.cel|.cpr|.npr|.cwp|.prproj|.ai|.flp|.fla|.psd|.pdd|.drw";

        private static string archiveFormats =
            ".7z|.rar|.zip";

        private static string programSetupFormats =
            "setup|.msi";

        //AutoCAD, 3dStudioMax
        private static string engineeringFormats =
            ".dwg|.dwf|.3ds|.max";

        private static string gameFormats =
            ".sav";        

        //This sorted list will be iterated to determine which level of interest
        //the file type is in.
        private static SortedList<String, ValueLevel> interestingFormatValue= null;
        private static Regex interestingPattern = null;

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
        /// Determines if the file contents is interesting by inspecting its filename
        /// and returns the Value Level that can be matched to the number of files
        /// that should be copied for this particular format.
        /// </summary>
        /// <param name="text">The text to be checked (filename for example)</param>
        /// <returns>A ValueLevel that ranges from 0 to 15. 0 represent no value</returns>
        public static ValueLevel isInteresting(string text)
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