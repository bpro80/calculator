using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Web.Mvc;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using BO.Implementation;
using CalculatorZd.Models;
using Common.Api;
using DA;
using DA.Entities;
using Utils;

namespace CalculatorZd.Controllers
{
    [AccessDeniedAuthorize(Roles = "admin, god")]
    public class AdminController : Controller
    {
        public ActionResult Index()
        {
            var http = new HttpCache();
            http.CacheDuration = 10440;
            List<string> obj;
            StringBuilder sb = new StringBuilder();
            string format = "{0}, {1}  ";
            http.Get(FilterNameHelper.CargoNameFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.CargoNameFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.StationsSendingFilterKey, out obj);
            sb.Append(string.Format(format, FilterNameHelper.StationsSendingFilterKey, obj != null ? obj.Count : 0));
            http.Get(FilterNameHelper.ContainerVolumeFilterName, out obj);
            sb.AppendLine("<br>");
            sb.Append(string.Format(format, FilterNameHelper.ContainerVolumeFilterName, obj != null ? obj.Count : 0));
            http.Get(FilterNameHelper.CargoGroupNamesFilterName, out obj);
            sb.AppendLine("<br>");
            sb.Append(string.Format(format, FilterNameHelper.CargoGroupNamesFilterName, obj != null ? obj.Count : 0));
            http.Get(FilterNameHelper.CargoGroupNamesFilterName, out obj);

            sb.Append(string.Format(format, FilterNameHelper.CargoGroupNamesFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.TransportationTypeFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.TransportationTypeFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.CompanySendingFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.CompanySendingFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.CompanyRecipientFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.CompanyRecipientFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.CompanySendingFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.CompanySendingFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.CountriesDeliveringFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.CountriesDeliveringFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.WaySendingFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.WaySendingFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.WayDeliveringFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.WayDeliveringFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.SubjectsSendingFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.SubjectsSendingFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.SubjectsDeliveringFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.SubjectsDeliveringFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.PayerFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.PayerFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.OwnerVagonEGRPOFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.PayerFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
            http.Get(FilterNameHelper.RenterVagonGVCFilterName, out obj);
            sb.Append(string.Format(format, FilterNameHelper.RenterVagonGVCFilterName, obj != null ? obj.Count : 0));
            sb.AppendLine("<br>");
              
            ViewBag.CacheReport = sb;
            var firmQueryStatistics = EntityAdapter.GetQueryStatisticAll();
            return View(new AdminViewModel() { FirmQueryStatistics = firmQueryStatistics });
        }

        [HttpPost]
        public ActionResult ClearAllCache()
        {
            HttpCache cache = new HttpCache();
            var keys = cache.GetAll();
            foreach (var key in keys)
            {
                cache.Clear(key.Key);
            }

            return View("Index");
        }

        [HttpPost]
        public ActionResult ReloadServerProps()
        {
            ServerProperties.Instance.Init();
            if (ServerProperties.Instance.EnableBlockUnKnowIP == 1)
            {
                if ((User.IsInRole("admin") || User.IsInRole("god")))
                {
                    SessionManager.AccessDenied = false;
                }
                else
                {
                    SessionManager.AccessDenied = AutorizeHelper.CheckIPAccess(SessionManager.CurrentIP);
                }
            }
            else
            {
                SessionManager.AccessDenied = false;
            }

            return RedirectToAction("Index");
        }

        
        public ActionResult FirmPayments()
        {
            var model = new FirmPaymentsViewModel {FirmPayments = FirmAdapter.GetFirmPayments(null, null, null,"")};

            return View(model);
        }
       
        public ActionResult AddFirmPayment()
        {
            return View(new AddFirmPaymentViewModel(){ActiveFirms = FirmAdapter.GetAllActiveFirms()});
        }

        [HttpPost]
        public ActionResult CreatePayment(AddFirmPaymentViewModel model)
        {
           if (ModelState.IsValid)
           {
               FirmAdapter.CreateFirmPayment(new Guid(model.FirmID), model.PayDate, model.PayTypeID, model.CurrencyID,
                   model.Summa, model.Comments);
           }
           else
           {
               model.ActiveFirms = FirmAdapter.GetAllActiveFirms();
               return View("AddFirmPayment",model);
           }
           return RedirectToAction("AddFirmPayment");
        }

        public ActionResult SearchFirmPayments(FirmPaymentsViewModel model)
        {
             model.FirmPayments = FirmAdapter.GetFirmPayments(model.FirmID, model.DatePayBegin, model.DatePayEnd,"");

            return View("FirmPayments", model);
        }
    }
}
