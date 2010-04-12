using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace SyncButler.Logging
{
    /// <summary>
    /// This is in charge of carrying out logging operations
    /// </summary>
    public sealed class Logger
    {
        private static LogLevel LOGLEVEL_DEFAULT = LogLevel.DEBUG;
        private static string LOG_PREF = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static string LOG_FILE = LOG_PREF + @"\log.xml";
        private static string LOG_STYLE = LOG_PREF + @"\logstyle.css";
        private static Logger logger = null;

        private LogLevel logLevel;
        private XmlDocument xmlLog = null;
        private XmlNode rootElem = null;

        /// <summary>
        /// Pre-defined log levels
        /// </summary>
        public enum LogLevel {
            DEBUG = 4,
            INFO = 3,
            WARNING = 2,
            FATAL = 1,
            NONE = 0
        }

        /// <summary>
        /// Private constructor - do not use this.
        /// Use the factory method GetInstance() to obtain an instance of Logger.
        /// </summary>
        private Logger(LogLevel level)
        {
            this.logLevel = level;
            this.xmlLog = new XmlDocument();

            if (File.Exists(LOG_FILE))
                this.xmlLog.Load(LOG_FILE);
            else
                this.InitLogFile();

            this.rootElem = this.xmlLog.SelectSingleNode("SyncButlerLog");
            if (this.rootElem == null)
            {
                this.rootElem = this.xmlLog.SelectSingleNode("SyncButlerLog");

                if (this.rootElem == null)
                    throw new Exception("An unexpection error occured while attempt to log an event -- could not create/repair the log file");
            }
        }

        /// <summary>
        /// Factory method to get an instance of Logger.
        /// </summary>
        /// <returns></returns>
        public static Logger GetInstance()
        {
            if (logger == null)
                logger = new Logger(LOGLEVEL_DEFAULT);

            return logger;
        }

        /// <summary>
        /// Gets/Sets the log level.
        /// </summary>
        public LogLevel Level
        {
            get
            {
                return this.logLevel;
            }
            set
            {
                this.logLevel = value;
            }
        }

        /// <summary>
        /// Initializes the log file.
        /// </summary>
        private void InitLogFile()
        {
            xmlLog.AppendChild(xmlLog.CreateXmlDeclaration("1.0", "UTF-8", null));
            xmlLog.AppendChild(xmlLog.CreateProcessingInstruction("xml-stylesheet", "type=\"text/css\" href=\"" + LOG_STYLE + "\""));
            xmlLog.AppendChild(xmlLog.CreateElement("SyncButlerLog"));

            xmlLog.Save(LOG_FILE);

            // Produce stylesheet
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Stream css = assembly.GetManifestResourceStream("SyncButler.logstyle.css");
            byte[] cssData = new byte[css.Length];
            css.Read(cssData, 0, (int)css.Length);
            css.Close();

            if (File.Exists(LOG_STYLE)) File.Delete(LOG_STYLE);
            FileStream cssFile = new FileStream(LOG_STYLE, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            cssFile.Write(cssData, 0, cssData.Length);
            cssFile.Close();
        }

        /// <summary>
        /// Use when a fatal exception that is unrecoverable occurs.
        /// </summary>
        /// <param name="message">Message to write to log file</param>
        public void FATAL(string message)
        {
            if (this.logLevel > LogLevel.WARNING)
                LogMessage(message, LogLevel.FATAL);
        }

        /// <summary>
        /// Use for information purposes. Similar to level DEBUG.
        /// DEBUG should be used for developer's logging purposes.
        /// </summary>
        /// <param name="message">Message to write to log file</param>
        public void INFO(string message)
        {
            if (this.logLevel > LogLevel.DEBUG)
                LogMessage(message, LogLevel.INFO);
        }

        /// <summary>
        /// Use when an error occurs but the error is recoverable (non-fatal).
        /// </summary>
        /// <param name="message">Message to write to log file</param>
        public void WARNING(string message)
        {
            if (this.logLevel > LogLevel.INFO)
                LogMessage(message, LogLevel.WARNING);
        }

        /// <summary>
        /// To be used by developers
        /// </summary>
        /// <param name="message">Message to write to log file</param>
        public void DEBUG(string message)
        {
            if (this.logLevel > LogLevel.NONE)
                LogMessage(message, LogLevel.DEBUG);
        }

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="message">The message to save</param>
        /// <param name="level">The LogLevel of the message</param>
        private void LogMessage(string message, LogLevel level)
        {
            if (this.logLevel == LogLevel.NONE) return;

            XmlNode logElem = this.rootElem.AppendChild(xmlLog.CreateElement("log"));

            XmlNode logType = logElem.AppendChild(xmlLog.CreateElement("type"));
            logType.AppendChild(xmlLog.CreateTextNode(level.ToString()));

            XmlNode timestamp = logElem.AppendChild(xmlLog.CreateElement("timestamp"));
            timestamp.AppendChild(xmlLog.CreateTextNode(DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss")));

            XmlNode msg = logElem.AppendChild(this.xmlLog.CreateElement("message"));
            msg.AppendChild(this.xmlLog.CreateTextNode(message));

            //this.xmlLog.Save(LOG_FILE);
        }
    }
}
