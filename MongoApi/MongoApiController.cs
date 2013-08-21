using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Threading;
using System.Web.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoApi
{ //TODO: Allow configuring connection string once per api...
    public class MongoApiController : Controller
    {
         
        //TODO: add pre and post db events to allow controlling access, validation, etc.
        private readonly IList<MongoApiConfigurationBase> configurations = new List<MongoApiConfigurationBase>();

        private JsonWriterSettings jsonSettings = new JsonWriterSettings()
        {
            OutputMode = JsonOutputMode.Strict
        };

        public MongoCollection<BsonDocument> GetCollectionForRequest(string database, string collection)
        {
            var config = GetConfigurations(database, collection).FirstOrDefault();
            if(config == null) throw new InvalidOperationException("There is no configuration for this database and collection.");
            if(String.IsNullOrEmpty(config.ConnectionString)) throw new InvalidOperationException("The connection string for this request is not configured.");
            return new MongoClient(config.ConnectionString).GetServer().GetDatabase(database).GetCollection(collection);
        }

        public MongoApiConfiguration<BsonDocument> Configure(string database, string collection)
        {
            var result = new MongoApiConfiguration<BsonDocument>();
            result.Database = database;
            result.Collection = collection;
            result.WithConnectionString("mongoapi");
            configurations.Add(result);
            return result;
        }
        public MongoApiConfiguration<T> Configure<T>(string database, string collection) where T : class
        {
            var result = new MongoApiConfiguration<T>();
            result.Database = database;
            result.Collection = collection;
            result.WithConnectionString("mongoapi");
            configurations.Add(result);
            return result;
        }


        private bool IsAllowedOperation(string database, string collection, string operation)
        {

            return
                this.configurations.Any(
                    x =>
                        (x.Collection == "*" || x.Collection == collection) &&
                        (x.Database == "*" || x.Database == database) &&
                        (x.AllowedOperations.Contains("*") || x.AllowedOperations.Contains(operation, StringComparer.CurrentCultureIgnoreCase)));
        }

        private IEnumerable<MongoApiConfigurationBase> GetConfigurations(string database, string collection)
        {
            
            return
                this.configurations.Where(
                    x =>
                        (x.Collection == "*" || x.Collection == collection) &&
                        (x.Database == "*" || x.Database == database)
                    );
        }

        [HttpGet]
        [ActionName("Endpoint")]
        public ActionResult Find(string database, string collection, string query, string sort, string limit, string skip, bool? inlineCount)
        {
            if (!IsAllowedOperation(database, collection, "read")) throw new SecurityException("Read is not allowed for this database and collection.  A developer should explicity allow this by calling this.Allow in the constructor.");

            if (String.IsNullOrEmpty(query)) query = "{}";
            if (String.IsNullOrEmpty(sort)) sort = "{}";
            long count = 0;
            string result;

            var db = GetCollectionForRequest(database,collection);
            IMongoQuery queryDocument = new QueryDocument(BsonDocument.Parse(query));
            queryDocument = ApplyDataFilter(database, collection, queryDocument);

            var data = db
                .Find(queryDocument)
                .SetSortOrder(new SortByDocument(sort.ToBsonDocumentFromClientJson()));

            if (inlineCount.HasValue && inlineCount.Value == true)
            {
                count = db.Count(new QueryDocument(BsonDocument.Parse(query)));
            }
            if (!string.IsNullOrEmpty(skip) && skip.ToLower() != "null")
            {
                var intSkip = int.Parse(skip);
                if (intSkip > 0) data = data.SetSkip(int.Parse(skip));
            }

            if (!string.IsNullOrEmpty(limit) && limit.ToLower() != "null")
            {
                var intlimit = int.Parse(limit);
                if (intlimit > 0) data = data.SetLimit(int.Parse(limit));
            }

            //TODO: consider returning json based on type for typed api.  Otherwise there is disconnect between typical inserted JSON and typical JSON returned here.
            var dataAsJson = data.ToJson(jsonSettings);

            if (inlineCount.HasValue && inlineCount.Value == true)
            {
                result = "{count:" + count + ", data: " + dataAsJson + "}";
            }
            else
            {
                result = dataAsJson; //TODO: when typed, use typed ToJSON
            }

            return Content(result, "application/json");
        }

        private IMongoQuery ApplyDataFilter(string database, string collection, IMongoQuery queryDocument)
        {
            var configs = this.GetConfigurations(database, collection);
            foreach (var config in configs)
            {
                queryDocument = config.ApplyDataFilters(queryDocument);
            }
            return queryDocument;
        }

        private BsonDocument ApplyInterceptsFor(string database, string collection, string operation, BsonDocument document)
        {
            var configs = this.GetConfigurations(database, collection);
            foreach (var config in configs)
            {
                document = config.ApplyInterceptsFor(operation,document);
            }
            return document;
        }

        [HttpPost]
        [ActionName("Endpoint")]
        public ActionResult Add(string database, string collection)
        {
            if (!IsAllowedOperation(database, collection, "add")) throw new SecurityException("Add is not allowed for this database and collection.  A developer should explicity allow this by calling this.Allow in the constructor.");

            var document = Request.GetSubmittedBsonDocument();            
            document = ApplyInterceptsFor(database, collection, "add", document);
            document.Remove("_id");
            var db = GetCollectionForRequest(database, collection);

            var response = db.Save(document);
            //TODO: throw errors.
            return Content(document.ToJson(jsonSettings), "application/json");
        }

        [HttpPut]
        [ActionName("Endpoint")]
        public ActionResult Update(string database, string collection)
        {
            if (!IsAllowedOperation(database, collection, "update")) throw new SecurityException("Update is not allowed for this database and collection.  A developer should explicity allow this by calling this.Allow in the constructor.");

            var document = Request.GetSubmittedBsonDocument();
            document = ApplyInterceptsFor(database, collection, "update", document);
            //TODO: allow "update where" instead of just "update one"
            var updateQuery = Query.EQ("_id", document["_id"]);
            updateQuery = ApplyDataFilter(database, collection, updateQuery);
            var db = GetCollectionForRequest(database, collection);
            var response = db.Update(updateQuery, new UpdateDocument(document));
            //TODO: throw errors.
            return Content(document.ToJson(jsonSettings), "application/json");
        }

        [HttpDelete]
        [ActionName("Endpoint")]
        public ActionResult Remove(string database, string collection, string query)
        {
            if (!IsAllowedOperation(database, collection, "delete")) throw new SecurityException("Delete is not allowed for this database and collection.  A developer should explicity allow this by calling this.Allow in the constructor.");
            var document = query;
            if (String.IsNullOrEmpty(query)) document = Request.GetAllRequestContent();
            var bson = document.ToBsonDocumentFromClientJson();
            bson = ApplyInterceptsFor(database, collection, "delete", bson);
            IMongoQuery deleteQuery = new QueryDocument(bson);
            deleteQuery = ApplyDataFilter(database, collection, deleteQuery);
            var db = GetCollectionForRequest(database, collection);
            var response = db.Remove(deleteQuery);
            //TODO: throw errors.
            return Content(bson.ToJson(jsonSettings), "application/json");
        }

    }
}