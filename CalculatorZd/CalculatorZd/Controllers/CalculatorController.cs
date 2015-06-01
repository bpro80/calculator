using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BO;
using BO.Implementation;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using CalculatorZd.Models;
using CalculatorZd.Services;
using Common.Api;
using DA.Filters;
using Utils;

namespace CalculatorZd.Controllers
{
    [Authorize]
    [UserIpAccessDenied]
    public class CalculatorController : Controller
    {
        private const int LengthListShow = 20;

        public ActionResult Index2()
        {
            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetWagonTypes()
        {
            List<FilterWagonTypeViewModel> model = FilterService.GetWagonTypes();
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCountriesSending(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.CountriesSendingFilterName, LengthListShow,
                FilterNameHelper.CountriesSendingFilterName);
        }

        public JsonResult GetFirms(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.CompanySendingEGRPOFilterName, 15);
        }

        public JsonResult GetTransportationType()
        {
            return GetFilterListAll(FilterNameHelper.TransportationTypeFilterName, Constants.SELECT_VALUES);
        }

        public JsonResult GetOwnerVagon(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.OwnerVagonEGRPOFilterName, 15);
        }

        public JsonResult GetRenterVagon(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.RenterVagonGVCFilterName, 15);
        }

        public JsonResult GetPayer(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.PayerFilterName, 15);
        }

        public JsonResult GetCompanySending(string term)
        {
            return GetCompanySendingListByTerm(1, term, FilterNameHelper.CompanySendingFilterName, 15);
        }


        public JsonResult GetCompanyRecipient(string term)
        {
            return GetCompanySendingListByTerm(1, term, FilterNameHelper.CompanyRecipientFilterName, 15);
        }

        public JsonResult GetContainerVolume()
        {
            return GetFilterListAll(FilterNameHelper.ContainerVolumeFilterName, Constants.SELECT_VALUES);
        }

        public JsonResult GetSubjectsSending(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.SubjectsSendingFilterName, 15);
        }


        public JsonResult GetCargoNames(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.CargoNameFilterName, 15);
        }

        public CalculatorFiltersSearchResult GetCalcFilterList()
        {
            if (SessionManager.FirmInfo == null)
                return null;

            var model = new CalculatorFiltersSearchResult();
            var filterItems = new List<CalcFilterItem>();
            Guid firmid = SessionManager.FirmInfo.ID;
            List<CalcFirmSettings> dbfl = FiltersAdapter.GetCalculatorFirmFilterList(firmid);
            foreach (CalcFirmSettings filterItem in dbfl)
            {
                filterItems.Add(new CalcFilterItem {FilterID = filterItem.ID, FilterName = filterItem.FilterName});
            }
            model.FilterItems = filterItems;
            return model;
        }


        //public JsonResult GetCargoClassName(string term)
        //{
        //    return GetFilterListAll(CombineColumns(ColumnsMapping.CargoCode.GetStringValue(), ColumnsMapping.CargoClassName.GetStringValue()));
        //}
        public JsonResult GetCargoGroupNames()
        {
            return GetFilterListAll(FilterNameHelper.CargoGroupNamesFilterName, Constants.SELECT_VALUES);
        }

        public JsonResult GetWaysSending(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.WaySendingFilterName, LengthListShow,
                FilterNameHelper.WaySendingFilterName);
        }

        public JsonResult GetWaysDelivering(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.WayDeliveringFilterName, LengthListShow,
                FilterNameHelper.WaySendingFilterName);
        }

     
        public JsonResult GetCountriesDelivering(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.CountriesDeliveringFilterName, LengthListShow,
                FilterNameHelper.CountriesDeliveringFilterName);
        }

        public JsonResult GetSubjectsDelivering(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.SubjectsDeliveringFilterName, LengthListShow,
                FilterNameHelper.SubjectsDeliveringFilterName);
        }


        public JsonResult GetCargoNameEarlyTransportation(string term)
        {
            return GetFilterListByTerm(1, term, FilterNameHelper.CargoNameEarlyTransportationFilterName, LengthListShow,
                FilterNameHelper.CargoNameEarlyTransportationFilterName);
        }

        public JsonResult GetStationsSending(string term)
        {
          IEnumerable<string> cache = GetCacheValueByKey(FilterNameHelper.StationsSendingFilterKey) as IEnumerable<string>;
            if (cache == null)
            {
                DictionariesLoader.LoadStationSending();
            }
            IEnumerable<string> itemsResult = FindStringByTerm(cache, term).Take(LengthListShow);
            return Json(itemsResult);
        }

        public JsonResult GetStationsDelivering(string term, string filter)
        {
            IEnumerable<string> cache = GetCacheValueByKey(FilterNameHelper.StationsDeliveringFilterKey) as IEnumerable<string>;
            if (cache == null)
            {
                DictionariesLoader.LoadDeliveringSending();
            }
            IEnumerable<string> itemsResult = FindStringByTerm(cache, term).Take(LengthListShow);
            return Json(itemsResult);
        }

        private object GetCacheValueByKey(string key)
        {
            var http = new HttpCache();
            IEnumerable<string> cache = null;
            http.Get(key, out cache);
            return cache;
        }

        private List<string> FindStringByTerm(IEnumerable<string> cache, string term)
        {
            var filteredItems = new List<string>();
            foreach (string item in cache)
            {
                if(item==null)
                    continue;
                if (item.ToLower().IndexOf(term.ToLower(), StringComparison.Ordinal) >= 0)
                    filteredItems.Add(item);
            }
            return filteredItems;
        }
        private JsonResult GetFilterListByTerm(int termLength, string term, string filterName, int lengthListShow,
            string ordeBy = "")
        {
            IEnumerable<string> list = FilterService.GetFilterListByTerm(termLength, term, filterName, lengthListShow);

            if (list != null && list.Any())
            {
                if (!String.IsNullOrEmpty(ordeBy))
                {
                    list.OrderBy(s => s);
                }
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            return Json(new object(), JsonRequestBehavior.AllowGet);
        }

        private JsonResult GetCompanySendingListByTerm(int termLength, string term, string filterName,
            int lengthListShow, string ordeBy = "")
        {
            IEnumerable<string> list = FilterService.GetFilterListByTerm(termLength, term, filterName, lengthListShow);

           // list = GetFormatedList(list);

            if (list != null && list.Any())
            {
                if (!String.IsNullOrEmpty(ordeBy))
                {
                    list.OrderBy(s => s);
                }
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            return Json(new object(), JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<string> GetFormatedList(IEnumerable<string> list)
        {
            var retList = new List<string>();
            foreach (string item in list)
            {
                string[] arr = item.Split('|');
                string code = arr[0];
                retList.Add(String.Format("{0} | {1}", code.Substring(code.Length - 9, 9), arr[1]));
            }
            return retList;
        }

        private JsonResult GetFilterListAll(string filterName, string defaultValue = "", string orderBy = "")
        {
            IEnumerable<FilterTypeViewModel> types = FilterService.GetFilterListAll(filterName, defaultValue, orderBy);
            return Json(types, JsonRequestBehavior.AllowGet);
        }
    }
}