using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alexitech.Controllers
{
    public class HelpController : Controller
    {
        public ActionResult Index()
        {
            return Redirect("~/");
        }

        public ActionResult Privacy()
        {
            return View();
        }
    }
}