using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace log4net.Appender
{
    /// <summary>
    /// Class to serialize a representation of a log4net LoggingEvent 
    /// </summary>
    public class Log4MongoEvent
    {
        private static string machine = null;

        static Log4MongoEvent()
        {
            machine = Environment.MachineName;
        }

        #region constructors
        public Log4MongoEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) return;
           
            Time = loggingEvent.TimeStamp;
            Level = loggingEvent.Level.ToString();
            Thread = loggingEvent.ThreadName;
            User = loggingEvent.UserName;
            Message = loggingEvent.RenderedMessage;
            Logger = loggingEvent.LoggerName;

            // location information, if available
            if (loggingEvent.LocationInformation != null)
            {
                File = loggingEvent.LocationInformation.FileName;
                Method = loggingEvent.LocationInformation.MethodName;
                Line = loggingEvent.LocationInformation.LineNumber;
                Class = loggingEvent.LocationInformation.ClassName;
            }

            // exception information
            if (loggingEvent.ExceptionObject != null)
            {
                Exception = ExceptionToLog4MongoException(loggingEvent.ExceptionObject);
            }
        }
        #endregion


        #region public properties
        [BsonId]
        public ObjectId Id { get; set; }
        public DateTime Time { get; set; }

        public string MachineName
        {
            get { return machine; }
            set { machine = value; }
        }

        public string Level { get; set; }
        public string Thread { get; set; }
        public string User { get; set; }
        public string Message { get; set; }
        public string Logger { get; set; }
        [BsonIgnoreIfNull]
        public string File { get; set; }
        [BsonIgnoreIfNull]
        public string Method { get; set; }
        [BsonIgnoreIfNull]
        public string Line { get; set; }
        [BsonIgnoreIfNull]
        public string Class { get; set; }
        [BsonIgnoreIfNull]
        public Log4MongoException Exception { get; set; }
        #endregion

        #region private methods
        /// <summary>
        /// Create representation of Exception
        /// Inner exceptions are handled recursively
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private Log4MongoException ExceptionToLog4MongoException(Exception ex)
        {
            var exception = new Log4MongoException();
            exception.Message = ex.Message;
            exception.Source = ex.Source;
            exception.StackTrace = ex.StackTrace;

            if (ex.InnerException != null)
            {
                exception.InnerException = ExceptionToLog4MongoException(ex.InnerException);
            }

            return exception;
        }
        #endregion
    }
}
