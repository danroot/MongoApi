namespace MongoApi.Web.Controllers
{
    public class SimpleUntypedDemoApiController : MongoApiController
    {
        public SimpleUntypedDemoApiController()
        {
            this.Configure("demo", "todos")
                .WithConnectionString("mongoapidemo")
                .Allow("*")
                .WithUserFilter(() => User.Identity.Name);
        }
    }
}