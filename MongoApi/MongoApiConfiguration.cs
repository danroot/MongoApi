using System;
using System.Collections.Generic;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoApi
{
    public abstract class MongoApiConfigurationBase
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string Collection { get; set; }
        public IEnumerable<string> AllowedOperations { get; set; }

        public abstract BsonDocument ApplyInterceptsFor(string operation, BsonDocument document);


        public abstract IMongoQuery ApplyDataFilters(IMongoQuery query);

    }

    public class MongoApiConfiguration<T> : MongoApiConfigurationBase where T : class
    {
        public MongoApiConfiguration()
        {
            this.Intercepts = new Dictionary<string, Action<T>>();
            this.BsonIntercepts = new Dictionary<string, Action<BsonDocument>>();
            this.DataFilter = x => x;
        }
        //TODO: allow strongly typed option
        //TODO: allow multiple intercepts per operation
        //TODO: allow change tracking
        //Done: dont require database name in route url
        //TODO: allow config of connection string
        //TODO: OnAnyRequest intercept to do custom security, etc.



        public Func<IMongoQuery, IMongoQuery> DataFilter { get; set; }
        public Dictionary<string, Action<T>> Intercepts { get; set; }
        public Dictionary<string, Action<BsonDocument>> BsonIntercepts { get; set; }

        public MongoApiConfiguration<T> WithConnectionString(string connection)
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
        public MongoApiConfiguration<T> WithUserFilter(Func<string> getUsername)
        {



            this.WithDataFilter(x => Query.And(x, Query.EQ("Username", getUsername())))
                .BeforeAddBson(x => x["Username"] = getUsername())
                .BeforeUpdateBson(x => x["Username"] = getUsername())
                .BeforeDeleteBson(x =>
                {
                    //TODO: already limited by data filter.  also throw here?
                });
            return this;
        }

        /// <summary>
        /// Applies a filter to all queries, including update and deletes, so that only data for the allowed context is exposed.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public MongoApiConfiguration<T> WithDataFilter(Func<IMongoQuery, IMongoQuery> filter)
        {

            this.DataFilter = filter;
            return this;
        }



        /// <summary>
        /// Allows read, add, update, delete or * operations for this collection.
        /// </summary>
        /// <param name="operations"></param>
        /// <returns></returns>
        public MongoApiConfiguration<T> Allow(params string[] operations)
        {
            this.AllowedOperations = operations;
            return this;
        }

        /// <summary>
        /// Fires before adding data to the database, to allow modifying all inserted data or applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration<T> BeforeAdd(Action<T> intercept)
        {
            this.Intercepts.Add("add", intercept);
            return this;
        }
        public MongoApiConfiguration<T> BeforeAddBson(Action<BsonDocument> intercept)
        {
            this.BsonIntercepts.Add("add", intercept);
            return this;
        }
        /// <summary>
        /// Fires before updates, to allow modifying all updated data or applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration<T> BeforeUpdate(Action<T> intercept)
        {
            this.Intercepts.Add("update", intercept);
            return this;
        }
        public MongoApiConfiguration<T> BeforeUpdateBson(Action<BsonDocument> intercept)
        {
            this.BsonIntercepts.Add("update", intercept);
            return this;
        }

        /// <summary>
        /// Fires before deletes, to allow applying logic and security constraints.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public MongoApiConfiguration<T> BeforeDelete(Action<T> intercept)
        {
            this.Intercepts.Add("delete", intercept);
            return this;
        }
        public MongoApiConfiguration<T> BeforeDeleteBson(Action<BsonDocument> intercept)
        {
            this.BsonIntercepts.Add("delete", intercept);
            return this;
        }

        public override BsonDocument ApplyInterceptsFor(string operation, BsonDocument document)
        {
            //TODO: mod to allow multiple intercepts.
            var bsonIntercepts = BsonIntercepts.ContainsKey(operation) ? BsonIntercepts[operation] : null;
            var typedIntercepts = Intercepts.ContainsKey(operation) ? Intercepts[operation] : null;
            if (typeof(T) == typeof(BsonDocument))
            {
                if (bsonIntercepts != null) bsonIntercepts.Invoke(document);
                if (typedIntercepts != null) typedIntercepts.Invoke(document as T);
            }
            else
            {

                if (bsonIntercepts != null) bsonIntercepts.Invoke(document);
                if (typedIntercepts != null)
                {
                    var instance = BsonSerializer.Deserialize<T>(document);

                    typedIntercepts.Invoke(instance);
                    document = instance.ToBsonDocument();
                }
            }
            return document;
        }

        public override IMongoQuery ApplyDataFilters(IMongoQuery query)
        {
            if (this.DataFilter == null) return query;
            //TODO: strong-type query...
            return this.DataFilter.Invoke(query);
        }
    }
}