using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MongoDB.Bson;

namespace MongoApi
{
    public static class MongoHelper
    {

        private static readonly Regex RemoveObjectId = new Regex(@"(ObjectId\()(.*)(\))");

        public static void MapMongoApiRoute(this RouteCollection routes, string prefix, string database, string controller)
        {
            routes.MapRoute(
              name: "MongoAPI_" + prefix + "_" +database,
              url: prefix  + "/{collection}/{query}",
              defaults: new { controller, database, action = "Endpoint", query = UrlParameter.Optional }
          );
        }

        public static string ToClientJson(this IEnumerable<BsonDocument> document)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            var all = document.Select(x => RemoveObjectId.Replace(BsonExtensionMethods.ToJson<BsonDocument>(x), "$2"));
            sb.Append(string.Join(",", all));
            sb.Append("]");
            return sb.ToString();
        }

        public static string ToClientJson(this BsonDocument document)
        {
            return RemoveObjectId.Replace(document.ToJson(), "$2");

        }

        public static BsonDocument ToBsonDocumentFromClientJson(this string value)
        {
            var bson = BsonDocument.Parse(value);
            //if (bson.Contains("_id"))
            //{
            //    bson["_id"] = new ObjectId(bson["_id"].ToString());
            //}
            return bson;
        }

        public static BsonDocument GetSubmittedBsonDocument(this HttpRequestBase request)
        {
            return request.GetAllRequestContent().ToBsonDocumentFromClientJson();
        }
    }
}