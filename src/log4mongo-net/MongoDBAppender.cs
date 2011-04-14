﻿#region Licence

/*
 *   Copyright (c) 2010, Jozef Sevcik <sevcik@styxys.com>
 *   All rights reserved.
 *
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *   * Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *   * Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *   * Neither the name of the <organization> nor the
 *     names of its contributors may be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 *   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 *   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 *   DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 *   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 *   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 *   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 *   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 *   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System;
using System.Security;
using MongoDB.Driver;
using MongoDB.Bson;
using log4net.Core;
using System.Text;

namespace log4net.Appender
{
    /// <summary>
    /// log4net Appender into MongoDB database
    /// This appender does not use layout option
    /// Format of log event (for exception):
    /// <code>
    /// { 
    ///     "timestamp" : "Wed Apr 28 2010 00:01:41 GMT+0200 (Central Europe Daylight Time)",
    ///     "level": "ERROR", 
    ///     "thread": "7", 
    ///     "userName": "jsk", 
    ///     "message": "I'm sorry", 
    ///     "loggerName": "log4net_MongoDB.Tests.MongoDBAppenderTests",
    ///     "fileName": "C:\jsk\work\opensource\log4mongo-net\src\log4mongo-net.Tests\MongoDBAppenderTests.cs", 
    ///     "method": "TestException", 
    ///     "lineNumber": "102", 
    ///     "className": "log4mongo_net.Tests.MongoDBAppenderTests", 
    ///     "exception": { 
    ///                     "message": "Something wrong happened", 
    ///                     "source": null, 
    ///                     "stackTrace": null, 
    ///                     "innerException": { 
    ///                                         "message": "I'm the inner", 
    ///                                         "source": null, 
    ///                                         "stackTrace": null 
    ///                                       } 
    ///                  } 
    /// }
    /// </code>
    /// </summary>
    public class MongoDBAppender : AppenderSkeleton
    {
        private DateTime lastError = DateTime.MaxValue;
        private bool inErrorState = false;
        private double errorDelaySeconds = 30;

        protected const string DEFAULT_MONGO_HOST = "localhost";
        protected const int DEFAULT_MONGO_PORT = 27017;
        protected const string DEFAULT_DB_NAME = "log4net_mongodb";
        protected const string DEFAULT_COLLECTION_NAME = "logs";

        private string hostname = DEFAULT_MONGO_HOST;
        private int port = DEFAULT_MONGO_PORT;
        private string dbName = DEFAULT_DB_NAME;
        private string collectionName = DEFAULT_COLLECTION_NAME;
  
        protected MongoServer server;
        protected MongoCollection collection;


        protected override bool RequiresLayout
        {
            get { return false; }
        }

        /// <summary>
        /// Mongo collection used for logs
        /// The main reason of exposing this is to have same log collection available for unit tests
        /// </summary>
        public MongoCollection LogCollection
        {
            get { return collection; }
        }
        
        #region Appender configuration properties

        /// <summary>
        /// Hostname of MongoDB server
        /// Defaults to DEFAULT_MONGO_HOST
        /// </summary>
        public string Host
        {
            get { return hostname; }
            set { hostname = value; }
        }

        /// <summary>
        /// Port of MongoDB server
        /// Defaults to DEFAULT_MONGO_PORT
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        /// Name of the database on MongoDB
        /// Defaults to DEFAULT_DB_NAME
        /// </summary>
        public string DatabaseName
        {
            get { return dbName; }
            set { dbName = value; }
        }

        /// <summary>
        /// Name of the collection in database
        /// Defaults to DEFAULT_COLLECTION_NAME
        /// </summary>
        public string CollectionName
        {
            get { return collectionName; }
            set { collectionName = value; }
        }

        /// <summary>
        /// MongoDB database user name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// MongoDB database password
        /// </summary>
        public string Password { get; set; }



        #endregion

        public override void ActivateOptions()
        {
            try
            {
                var mongoConnectionString = new StringBuilder("mongodb://");
               
                if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                {
                    // use MongoDB authentication
                    mongoConnectionString.AppendFormat("{0}:{1}@", UserName, Password);
                }
                mongoConnectionString.AppendFormat("{0}:{1}", Host, Port);

                server = MongoServer.Create(mongoConnectionString.ToString());
                server.Connect();
                var db = server.GetDatabase(DatabaseName);
                collection = db.GetCollection<Log4MongoEvent>(CollectionName);
            }
            catch (Exception e)
            {
                ErrorHandler.Error("Exception while initializing MongoDB Appender", e, ErrorCode.GenericFailure);
            }
        }

        protected override void OnClose()
        {
            collection = null;
            server.Disconnect();
            base.OnClose();
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if(!inErrorState)
                {
                    collection.Insert(new Log4MongoEvent(loggingEvent));
                    inErrorState = false;
                }
                else
                {
                    if(DateTime.Now > lastError.AddSeconds(errorDelaySeconds))
                    {
                        collection.Insert(new Log4MongoEvent(loggingEvent));
                        inErrorState = false;
                    }
                }

            }
            catch(Exception ex)
            {
                lastError = DateTime.Now;
                inErrorState = true;
                ErrorHandler.Error("An error occurred while inserting to Mongodb.", ex);
            }
        }
    }
}
