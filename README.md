MongoApi
========

A RESTful api over MongoDb written in ASP.NET.

Note this is pre-beta, expirimental code and should not be used for production! 

The idea behind this project is to provide a simple RESTful proxy to MongoDB for use in ASP.NET applications.  Where possible, JSON is submitted directly to MongoDB.  Examples are provided for using the API with a variety of common javascript frameworks.  The appeal of this approach is that very little setup is necessary to build persistence for an application.  This allows the developer to focus largely on the UI and to refactor that more easily.

Simple Example
--------------

Server:

>        public class MyApiController : MongoApiController{
>           public MyApiController(){
>                this.Configure("demodb","todos")
>                    .Allow("*");
>           }	
>        }

RouteConfig.cs:

>        routes.MapMongoApiRoute("db", "demo", "MyApi");

Client:

>        $.getJSON('/db/todos',function(data){ ... });
>        $.post('/db/todos', JSON.stringify({ Title: 'example'}));



There are some challenges to this sort of proxy:

* Risk of uninteded data exposure.  Since queries are passed directly to Mongo, there is a risk of allowing access to data the developer doesnt intend the current user to have.  This is handled by applying a filter which is ANDed with the query.  In addition, updates, deletes, inserts can be handled to ensure users only work with "their" data. This seems to work, but may have unexpected holes that allow for "NoSQL injection"
* Incompatibility between JSON and BSON.  JSON does not allow for some types MongoDB requires.  For example, ObjectId and ISODate.  ObjectId is currently handled with some creative text parsing, but other types need a better approach.  Strong typing may help this issue.
