using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoApi.Web.Controllers
{

    public class Todo
    {
        [BsonId]
        public string Id { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public int Priority { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedDateUtc { get; set; }
    }

    public class SimpleTypedDemoApiController : MongoApiController
    {
        public SimpleTypedDemoApiController()
        {
            this.Configure<Todo>("typed", "todos")
                .WithConnectionString("mongoapidemo")
                .Allow("*")
                .WithUserFilter(() => User.Identity.Name)
                .BeforeAdd(x=>
                    x.CreatedDateUtc = DateTime.UtcNow);

            
        }
    }
}