MongoApi
========

A RESTful api over MongoDb written in ASP.NET MVC.

Note this is pre-beta, expirimental code and should not be used for production! 

The idea behind this project is to provide a simple RESTful proxy to MongoDB for use in ASP.NET applications.  Where possible, JSON is submitted directly to MongoDB.  Examples are provided for using the API with a variety of common javascript frameworks.  The appeal of this approach is that very little setup is necessary to build persistence for an application.  This allows the developer to focus largely on the UI and to refactor that more easily. 

I am not that familiar with it, but from what I've read this is similar to Sleepy Mongoose in python.

Simplest Thing That Works
-------------------------
By creating a simple api and adding routing, you can expose Mongo for use in javascript.  Note you probably _shouldn't_ do this in production code, since it would allow end-users to do any query, add, update, or delete against the collection.

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


Simplest Thing That is Useful
-----------------------------
The simple example above is handy for banging out a UI with some persistance, but probably a bad idea. A little more code can restrict the API to data the user is 'allowed' to work against.  In this case we assume the data has a 'Username' field and users may only see items for their username.  This may not always be the case, but we'll explore other use cases in future examples.


Server:

>       public class SimpleUntypedDemoApiController : MongoApiController
>       {
>           public SimpleUntypedDemoApiController()
>           {
>               this.Configure("demo", "todos")
>               .Allow("*")
>               .WithUserFilter(() => User.Identity.Name);
>           }
>       }

RouteConfig.cs:

>        routes.MapMongoApiRoute("db", "demo", "SimpleUntypedDemoApi");

Client:

>        $.getJSON('/db/todos',function(data){ ... });
>        $.post('/db/todos', JSON.stringify({ Title: 'example'}));

Bridging Static vs Dynamic Typing
---------------------------------
Javascript is a dynamic language. MongoDB and .NET have dynamic features, but are generally staticly typed.  In the case of MongoDB, indexing and query need to know _something_ about the data types (Date, numeric, etc) to be efficient.  In the case of .NET, it can be easier to work with typed classes in some scenarios.  In other words, this: 

>       data.CreatedDateUtc = DateTime.UtcNow;

can be better than

>       data["CreatedDateUtc"] = DateTime.UtcNow;

because you can get compile-time guarantees that CreatedDateUtc exists and avoid "magic strings" that can have typos, etc.  It also happens that .NET types are a pretty good way to
specify the data typing that MongoDB needs.  In other words, this:

>       data.CreatedDateUtc = DateTime.UtcNow;

can be better than

>       data["CreatedDateUtc"] = new BSonDateTime(DateTime.UtcNow);

To enable these scenarios, MongoApi allows working with both untyped data or typed classes:

Server:
>          public class Todo
>          {
>              [BsonId]
>              public string Id { get; set; }
>              public string Title { get; set; }
>              public string Username { get; set; }
>              public int Priority { get; set; }
>              public DateTime DueDate { get; set; }
>              public DateTime CreatedDateUtc { get; set; }
>          }
>               
>         public class SimpleTypedDemoApiController : MongoApiController
>          {
>              public SimpleTypedDemoApiController()
>              {
>                  this.Configure<Todo>("typed", "todos")
>                      .Allow("*")
>                      .WithUserFilter(() => User.Identity.Name)
>                      .BeforeAdd(x=>
>                          x.CreatedDateUtc = DateTime.UtcNow);
>               }
>          }

RouteConfig.cs:

>        routes.MapMongoApiRoute("typed", "typed", "SimpleTypedDemoApi");

Client:

>        $.getJSON('/typed/todos',function(data){ ... });
>        $.post('/typed/todos', JSON.stringify({ Title: 'example', 
>                      Priority:1, DueDate: new Date()}));



What's Next
-----------

There are some challenges to this sort of proxy:

* Risk of uninteded data exposure.  Since queries are passed directly to Mongo, there is a risk of allowing access to data the developer doesnt intend the current user to have.  This is handled by applying a filter which is ANDed with the query.  In addition, updates, deletes, inserts can be handled to ensure users only work with "their" data. This seems to work, but may have unexpected holes that allow for "NoSQL injection"
* Incompatibility between JSON and BSON.  JSON does not allow for some types MongoDB requires.  For example, ObjectId and ISODate.  This is currently handled by outputing 'Strict' JSON, but this causes a "leaky abstraction" into the UI:  the UI now has to "know" about some Mongo JSON quirks.  Future enhancements to typing may fix some of this.
