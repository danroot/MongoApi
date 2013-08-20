using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MongoApi.Web.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SimpleUserTodo()
        {

            return View();
        }

        public ActionResult SimpleUserTodoKo()
        {
            return View();
        }

        public ActionResult SimpleUserTodoNg()
        {
            return View();
        }


        public ActionResult TypedUserTodo()
        {
            return View();
        }

        public ActionResult TypedUserTodoKo()
        {
            return View();
        }

        public ActionResult TypedUserTodoNg()
        {
            return View();
        }
    }
}
