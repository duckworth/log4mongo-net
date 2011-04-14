using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace log4net.Appender
{
    /// <summary>
    /// Class to serialize a representation of an Exception 
    /// </summary>
    public class Log4MongoException
    {
        #region public properties
        [BsonIgnoreIfNull]
        public string Message { get; set; }
        [BsonIgnoreIfNull]
        public string Source { get; set; }
        [BsonIgnoreIfNull]
        public string StackTrace { get; set; }
        [BsonIgnoreIfNull]
        public Log4MongoException InnerException { get; set; }
        #endregion
    }  
}
