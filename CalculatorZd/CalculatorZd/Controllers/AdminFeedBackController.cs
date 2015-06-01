using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BO;
using DA.Entities;

namespace CalculatorZd.Controllers
{
    public class AdminFeedBackViewModel
    {
        public AdminFeedBackViewModel ()
        {
            Messages = new List<FeedBackMessage>();
        }

        public List<FeedBackMessage> Messages;
    }
    public class AdminFeedBackController : Controller
    {
        public ActionResult Index()
        {
            if (!(User.IsInRole("admin") || User.IsInRole("god")))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new AdminFeedBackViewModel();
            model.Messages = EntityAdapter.GetFeedBackMessagesAll();
            return View(model);
        }
	}
}