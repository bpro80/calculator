using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BO;
using BO.Implementation;
using Common.Api;
using DA.Entities;
using Microsoft.AspNet.Identity;
using Utils;

namespace CalculatorZd.Controllers
{
    public class FeedBackViewModel
    {
        public FeedBackViewModel()
        {
            Messages = new List<FeedBackMessage>();
        }
        public bool FeedBackStatus { get; set; }
        public string MessageText { get; set; }
        public int LastMessageID { get; set; }

        public List<FeedBackMessage> Messages;

    }
    public class FeedBackController : Controller
    {
        public ActionResult Index(FeedBackViewModel model)
        {
            model.Messages = EntityAdapter.GetFeedBackChart(SessionManager.FirmInfo.ID);
            if (model.Messages.Count > 0)
                 model.LastMessageID = model.Messages.Last().ID;
            return View(model);
        }

        [HttpPost]
        public ActionResult SubmitFeedBack(FeedBackViewModel model)
        {
            if (ModelState.IsValid)
            {
             //   MailService.SendMessageToEmail(model.Message,  ServerProperties.Instance.FeedBackEmail);
                EntityAdapter.FeedBackChartInsert(model.MessageText, model.LastMessageID, User.IsInRole("god"), SessionManager.FirmInfo.ID);
                model.FeedBackStatus = true;

            }
            return RedirectToAction("Index", model);
        }
    }
}