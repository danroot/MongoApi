using System;

namespace MongoApi.Web.Controllers
{

    public class Todo
    {
        public string Title { get; set; }
        public string Username { get; set; }
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
                .BeforeAdd(x=>x.CreatedDateUtc = DateTime.UtcNow);
        }
    }
}