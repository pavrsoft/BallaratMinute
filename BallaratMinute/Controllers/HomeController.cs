using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BallaratMinute.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return File("index.html", "text/html");
            //return new RedirectResult("index.html", true);
            ViewBag.Title = "Ballarat Minute";

            return View();
        }
    }
}
