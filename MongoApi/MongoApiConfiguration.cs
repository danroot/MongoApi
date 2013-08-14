using System;
using System.Collections.Generic;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoApi
{
    public class MongoApiConfiguration
    {
        public MongoApiConfiguration()
        {
            this.Intercepts = new Dictionary<string, Action<BsonDocument>>();
            this.DataFilter = x => x;
        }
        //TODO: allow strongly typed option
        //TODO: allow multiple intercepts per operation
        //TODO: allow change tracking
        //Done: dont require database name in route url
        //TODO: allow config of connection string
        //TODO: OnAnyRequest intercept to do custom security, etc.
       

        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string Collection { get; set; }
        public IEnumerable<string> AllowedOperations { get; set; }
        public Func<IMongoQuery, IMongoQuery> DataFilter { get; set; }
        public Dictionary<string, Action<BsonDocument>> Intercepts { get; set; }

        public MongoApiConfiguration WithConnectionString(string connection)
        {
            var config = ConfigurationManager.ConnectionStrings[connection];
            if (config == null) return this;
            this.ConnectionString = config.ConnectionString;
            return this;
        }

        /// <summary>
        /// Applies a data filter and "Before" handlers such that 'Username' is set, and all queries are filtered by username.  This ensures
        /// a user can only see, update, delete, and insert "their" data.
        /// </summary>
        /// <param name="getUsername"></param>
        /// <returns></returns>
        public MongoApiConfiguration WithUserFilter(Func<string> getUsername)
        {
           


            this.WithDataFilter(x => Query.And(x, Query.EQ("Username", getUsername())))
                .BeforeAdd(x => x["Username"] = getUsername())
                .BeforeUpdate(x => x["Username"] = getUsername())
                .BeforeDelete(x =>
                {
                    // if(x["Title"].ToString().StartsWith("T")) throw new InvalidOperationException("Can't delete things that start with T.");
                });
            return this;
        }

        /// <summary>
        /// Applies a filter to all queries, including update and deletes, so that only data for the allowed context is exposed.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public MongoApiConfiguration WithDataFilter(Func<IMongoQuery, IMongoQuery> filter)
        {
            this.DataFilter = filter;
            return this;
        }

        /// <summary>
        /// Allows read, add, update, delete or * operations for this collection.
        /// </summary>
        /// <param name="operations"></param>
        /// <returns></returns>
        public MongoApiConfiguration Allow(params string[] operations)
        {
            this.AllowedOperations = operations;
            return this;
        }

        /// <summary>
        /// Fires before adding data to the database, to allow modifying all inserted data or applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration BeforeAdd(Action<BsonDocument> intercept)
        {
            this.Intercepts.Add("add", intercept);
            return this;
        }

        /// <summary>
        /// Fires before updates, to allow modifying all updated data or applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration BeforeUpdate(Action<BsonDocument> intercept)
        {
            this.Intercepts.Add("update", intercept);
            return this;
        }

        /// <summary>
        /// Fires before deletes, to allow applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration BeforeDelete(Action<BsonDocument> intercept)
        {
            this.Intercepts.Add("delete", intercept);
            return this;
        }

        public Action<BsonDocument> GetInterceptFor(string operation)
        {
            return Intercepts.ContainsKey(operation) ? Intercepts[operation] : null;
        }


    }
}