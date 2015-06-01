using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using BO.Implementation.Calculator;
using BO;
using BO.Implementation;
using CalculatorZd.Models;
using CalculatorZd.Services;
using Common.Api;
using Common.Api.Extensions;
using Common.Api.Types;
using DA.Entities;
using DA.Filters;
using DA.Report;
using log4net;
using Utils;
using WebGrease.Css.Ast.Selectors;
using DataTable = System.Data.DataTable;

namespace CalculatorZd.Controllers.api
{
    public class EntitiesController : ApiController
    {
        public IEnumerable<CoefficientItemViewModel> Get()
        {
            var list = new List<CoefficientItemViewModel>();
            for (int i = 1; i < 11; i++)
            {
                list.Add(new CoefficientItemViewModel
                {
                    Id = i,
                    Value = i
                });
            }
            return list;
        }


        [System.Web.Http.HttpGet]
        public FilterTypeExistsModel CheckExistsFilterItem(int filterTypeId, string value)
        {
            ColumnsMapping columnName;
            IEnumerable<FilterTypeViewModel> filterList = null;
            if (Enum.TryParse(filterTypeId.ToString(CultureInfo.InvariantCulture), true, out columnName))
            {
                string key = String.Format(CacheKeys.FILTER_TYPE_FORMAT_KEY, columnName);

                if (HttpRuntime.Cache[key] == null)
                {
                    if (columnName == ColumnsMapping.CODE_NAME_STATION_SENDING_RUS_SNG)
                    {
                        string[] columns =
                        {
                            CombineColumns(ColumnsMapping.StationSendingCodeRUS.GetStringValue(),
                                ColumnsMapping.StationSendingRUS.GetStringValue()),
                            CombineColumns(ColumnsMapping.StationSendingCodeSNG.GetStringValue(),
                                ColumnsMapping.StationSendingSNG.GetStringValue())
                        };

                        filterList = GetFilterListByFilter(columns, "", "");
                        AddToCache(key, filterList);
                    }
                    else if (columnName == ColumnsMapping.CODE_NAME_STATION_DELIVERING_RUS_SNG)
                    {
                        var columns = new[]
                        {
                            CombineColumns(ColumnsMapping.StationDeliveringCodeRUS.GetStringValue(),
                                ColumnsMapping.StationDeliveringRUS.GetStringValue()),
                            CombineColumns(ColumnsMapping.StationDeliveringCodeSNG.GetStringValue(),
                                ColumnsMapping.StationDeliveringSNG.GetStringValue())
                        };

                        filterList = GetFilterListByFilter(columns, "", "");
                        AddToCache(key, filterList);
                    }
                    else
                    {
                        filterList = FilterService.GetFilterListAll(columnName.GetStringValue());
                        AddToCache(key, filterList);
                    }
                }
                else
                {
                    filterList = (IEnumerable<FilterTypeViewModel>)HttpRuntime.Cache[key];
                }
            }

            bool isFound = false;
            if (filterList != null)
                foreach (var item in filterList)
                {
                    if (String.Equals(item.name.Trim(), value.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        isFound = true;
                        break;
                    }
                }

            return new FilterTypeExistsModel() { isExists = isFound };
        }

        private void AddToCache(string key, IEnumerable<FilterTypeViewModel> filterList)
        {
            HttpRuntime.Cache.Add(key, filterList, null, DateTime.Now.AddDays(1), TimeSpan.Zero,
                       CacheItemPriority.Normal, null);
        }
        private string CombineColumns(string s1, string s2)
        {
            return String.Format("{0} + ' | '+ {1}", s1, s2);
        }

        private IEnumerable<FilterTypeViewModel> GetFilterListByFilter(string[] filterName, string sWhere, string orderBy)
        {
            var items = CalculatorManager.GetEntities(filterName[0], filterName.Length > 1 ? filterName[1] : "", sWhere, orderBy);
            var types = new List<FilterTypeViewModel>();

            if (items == null)
                return null;

            foreach (var item in items)
            {
                if (item != null && !String.IsNullOrEmpty(item.Trim()))
                {
                    types.Add(new FilterTypeViewModel
                    {
                        name = item.Split('|')[1].Trim()
                    });
                }
            }

            return types;
        }


        public IEnumerable<CoefficientItemViewModel> Save(string items, int typeId)
        {
            var serializer = new JavaScriptSerializer();
            var list = serializer.Deserialize<List<CoefficientItemViewModel>>(items);
            var coeffs = new List<FilterCoefficient>();
            foreach (CoefficientItemViewModel l in list)
            {
                coeffs.Add(new FilterCoefficient
                {
                    FilterTypeID = typeId,
                    CoefficientValue = l.Value,
                    CountItems = l.Id
                });
            }

            FilterManager.InsertCoefficients(coeffs, typeId);
            return list;
        }

        public FilterCalculatorResult GetCalculatorResult(string filters)
        {
            var serializer = new JavaScriptSerializer();

            var filtersItems = serializer.Deserialize<List<FilterParamstItemViewModel>>(filters);
            List<FilterCoefficient> listCoeffs = FilterManager.GetCoefficientsAll();
            var calcCoeffsDetailReport = new StringBuilder();
            decimal sumCoeffs = 0;
            bool periodAdded = false;
            for (int i = 0; i < filtersItems.Count; i++)
            {
                int countItemsInFilter = filtersItems[i].cv;
                if (countItemsInFilter == 0)
                    continue;

                int filterId = TypeConverter.ToInt32(filtersItems[i].filterId);
                if (filterId == (int)ColumnsMapping.CargoCode)
                {
                    filterId = (int)ColumnsMapping.CargoName;
                }

                ColumnsMapping columnName;
                if (Enum.TryParse(filterId.ToString(CultureInfo.InvariantCulture), true, out columnName))
                {
                    foreach (FilterCoefficient coeff in listCoeffs)
                    {
                        if (filterId == (int)ColumnsMapping.DateSending)
                        {
                            if (periodAdded)
                                continue;

                            DateTime dateBegin;
                            DateTime dateEnd;
                            DateTime.TryParse(filtersItems[i].sv[0].name, out dateBegin);
                            DateTime.TryParse(filtersItems[i].sv[1].name, out dateEnd);
                            TimeSpan dateDifferent = dateEnd - dateBegin;
                            int months = dateDifferent.GetMonths();
                            if (months > 0)
                            {
                                decimal coeffValue = GetCoeffBySelectedCount(months, listCoeffs, filterId);
                                sumCoeffs += coeffValue;
                                calcCoeffsDetailReport.Append(String.Format("{0}:{1} = {2}, ",
                                    columnName.GetStringValue(), months, coeffValue));
                                periodAdded = true;
                            }
                        }
                        else if (coeff.FilterTypeID == filterId)
                        {
                            decimal coeffValue = GetCoeffBySelectedCount(countItemsInFilter, listCoeffs, filterId);
                            sumCoeffs += coeffValue;
                            calcCoeffsDetailReport.Append(String.Format("{0}:{1} = {2}, ", columnName.GetStringValue(),
                                countItemsInFilter, coeffValue));
                            break;
                        }
                    }
                }
            }

            calcCoeffsDetailReport.Append(String.Format(" Итого:{0}", sumCoeffs));
            return new FilterCalculatorResult
            {
                Result =
                    String.Format("{0:f2}",
                        ServerProperties.Instance.SearchResultMainCoeff * (double)sumCoeffs),
                CalcCoeffsDetailReport = calcCoeffsDetailReport.ToString()
            };
        }

        private decimal GetCoeffBySelectedCount(int selectedItemsCount, List<FilterCoefficient> listCoeffs, int typeId)
        {
            decimal coeff = 0;
            FilterCoefficient highest = listCoeffs.Where(rs => rs.FilterTypeID == typeId)
                .OrderByDescending(rs => rs.CoefficientValue)
                .FirstOrDefault();

            if (highest != null && (selectedItemsCount == -1 || highest.CountItems < selectedItemsCount))
            {
                coeff = highest.CoefficientValue;
            }
            else
            {
                FilterCoefficient filterCoefficient = listCoeffs.FirstOrDefault(rs => rs.FilterTypeID == typeId && rs.CountItems == selectedItemsCount);

                if (filterCoefficient != null) coeff = filterCoefficient.CoefficientValue;
            }
            return coeff;
        }

        private int pageSize = 30;

        public enum SearchStatus
        {
            None,
            DataAbsent,
            DataExist,
            DataTooMuch,
            Error
        }

        public SearchItemsResultViewModel GetSearchResultBySessionId(string sessionId)
        {
            //   Thread.Sleep(100000);
            SearchStatus searchStatus;
            int totalRowsCount = 0;

            GetSearchResult(CacheKeys.CACHE_CALC_SEARCH_STATUS_KEY, sessionId, out searchStatus);

            if (searchStatus == SearchStatus.None)//еще нет информации о ответе на запрос
            {
                return new SearchItemsResultViewModel() {StatusSearch = (int) SearchStatus.None};
            }
            else
            {
                if (searchStatus == SearchStatus.DataTooMuch)
                {
                    return new SearchItemsResultViewModel() {StatusSearch = (int) SearchStatus.DataTooMuch};
                }
                else
                {
                    SearchItemsResultViewModel searchItemsResultViewModel = null;
                    GetSearchResult(CacheKeys.CACHE_CALC_REPORT_MODEL_KEY, sessionId, out searchItemsResultViewModel);
                    GetSearchResult(CacheKeys.CACHE_CALC_REPORT_TOTAL_ROWS_COUNT_KEY, sessionId, out totalRowsCount);

                    if (searchStatus == SearchStatus.DataExist || searchItemsResultViewModel != null)
                    {
                        searchItemsResultViewModel.TotalRecords = totalRowsCount;
                        searchItemsResultViewModel.TotalPages = totalRowsCount/pageSize == 0
                            ? 1
                            : (totalRowsCount/pageSize) +
                              (totalRowsCount%pageSize != totalRowsCount
                                  ? 1
                                  : 0);
                        searchItemsResultViewModel.StatusSearch = (int) SearchStatus.DataExist;
                        return searchItemsResultViewModel;
                    }
                    return new SearchItemsResultViewModel() {StatusSearch = (int) searchStatus};
                }
            }
        }

        public SessionViewModel GetSessionId()
        {
            return new SessionViewModel() { sessionId = SessionId };
        }

        private void PutSearchResult(string key, string sessionId, object value, int cacheDuration = 3600)
        {
            var http = new HttpCache();
            http.CacheDuration = cacheDuration;
            http.Set(CachingHelper.GetReportCachKey(key, sessionId), value);
        }

        private void Clear(string key, string sessionId)
        {
            var http = new HttpCache();
            http.Clear(CachingHelper.GetReportCachKey(key, sessionId));
        }

        public void GetSearchResult<T>(string key, string sessionId, out T value)
        {
            var http = new HttpCache();
            http.Get(CachingHelper.GetReportCachKey(key, sessionId), out value);
        }
     
        public enum ReportType
        {
            Standart,
            Analize
        }
        public async Task<DownloadReportModel> GetDownloadReport(string sessionId, string nameReport, ReportType reportType = ReportType.Standart)
        {

            Guid firmId = SessionManager.FirmInfo.ID;
            string path =
                System.Web.HttpContext.Current.Server.MapPath(String.Format("{0}", ServerProperties.Instance.ReportStoragePath));

            var taskResult = Task<DownloadReportResult>.Factory.StartNew(() =>
            {
                var status = DownloadReportProcess(path, sessionId, nameReport, firmId, reportType);
                var downloadResult = new DownloadReportResult(sessionId, firmId, path, OperationStatus.Failure);

                downloadResult._StatusReport = status;
                return null;
            });
            var model = new DownloadReportModel { Status = OperationStatus.Success };
            return model;
        }

        public bool GetSaveFilter(string filterName, string period, string transportationType, string wagonType,
                                  string volumeType, string cargoName, string cargoGroup, string companySending, string companyRecipient,
                                  string countrySending, string countryDelivering, string waySending, string wayDelivering, string stationSending, string stationDelivering,
                                  string subjectSending, string subjectDelivering, string ownerWagon, string payerWagon, string renter, string columns, string earlyTransportationCargo, string vagonType)
        {
            if (SessionManager.FirmInfo == null)
                return false;

            Guid firmid = SessionManager.FirmInfo.ID;
            var f = new CalculatorFirmFilter
            {
                FirmID = firmid,
                FilterName = filterName,
                PeriodTransportation = period,
                TransportationType = transportationType,
                WagonType = wagonType,
                VolumeType = volumeType,
                CargoName = cargoName,
                CargoGroup = cargoGroup,
                CompanySending = companySending,
                CompanyRecipient = companyRecipient,
                CountrySending = countrySending,
                CountryDelivering = countryDelivering,
                WaySending = waySending,
                WayDelivering = wayDelivering,
                StationSending = stationSending,
                StationDelivering = stationDelivering,
                SubjectSending = subjectSending,
                SubjectDelivering = subjectDelivering,
                OwnerWagon = ownerWagon,
                RenterWagon = renter,
                PayerWagon = payerWagon,
                Columns = columns,
                EarlyTransportationCargo = earlyTransportationCargo,
                VagonType = vagonType
            };
            bool success = FiltersAdapter.InsertCalculatorFirmFilter(f);

            return success;
        }

        public CalculatorFilterSettingsSearchResult GetCalcFilterSettings(int filterId)
        {
            if (SessionManager.FirmInfo == null)
                return null;

            var model = new CalculatorFilterSettingsSearchResult();

            Guid firmid = SessionManager.FirmInfo.ID;
            var f = FiltersAdapter.GetCalculatorFirmFilter(firmid, filterId);

            var filterItems = new CalcFilterSettingsItem()
            {
                FilterID = f.ID,
                periodTransportation = f.PeriodTransportation,
                TransportationType = f.TransportationType,
                VolumeType = f.VolumeType,
                WagonType = f.WagonType,
                CargoName = f.CargoName,
                CargoGroup = f.CargoGroup,
                CompanyRecipient = f.CompanyRecipient,
                CompanySending = f.CompanySending,
                CountryDelivering = f.CountryDelivering,
                CountrySending = f.CountrySending,
                WaySending = f.WaySending,
                WayDelivering = f.WayDelivering,
                SubjectSending = f.SubjectSending,
                SubjectDelivering = f.CountryDelivering,
                StationSending = f.StationSending,
                StationDelivering = f.StationDelivering,
                OwnerWagon = f.OwnerWagon,
                PayerWagon = f.PayerWagon,
                RenterWagon = f.RenterWagon,
                Columns = f.Columns,
                EarlyTransportationCargo = f.EarlyTransportationCargo,
                VagonType = String.IsNullOrEmpty(f.VagonType) ? "0" : f.VagonType
            };

            model.FilterItems = filterItems;
            return model;
        }

        public bool GetDeleteFilter(int filterId)
        {
            return FiltersAdapter.DeleteFilter(filterId);
        }
        public CalculatorFiltersSearchResult GetCalcFilterList(int pageId)
        {
            if (SessionManager.FirmInfo == null)
                return null;

            var model = new CalculatorFiltersSearchResult();
            var filterItems = new List<CalcFilterItem>();
            Guid firmid = SessionManager.FirmInfo.ID;
            var dbfl = FiltersAdapter.GetCalculatorFirmFilterList(firmid);
            var q = from score in dbfl
                    where score.NumRow > (pageId - 1) * 10 && score.NumRow <= (pageId) * 10
                    select score;
            int index = 1;
            foreach (var filterItem in q.ToList())
            {
                filterItems.Add(new CalcFilterItem() { FilterID = filterItem.ID, FilterName = filterItem.FilterName, IndexId = index });
                index++;
            }
            model.FilterItems = filterItems;
            model.TotalPages = 3;
            return model;
        }

        public class SearchParams
        {
            public int pageId { get; set; }
            public string filters { get; set; }
            public string selectedColumnsFilter { get; set; }
            public string sessionId { get; set; }
            public string vagonSourceTypeParam { get; set; }
        }

        readonly ILog _logger = LogManager.GetLogger(typeof(EntitiesController));

        [System.Web.Http.HttpPost]
        public SearchItemsResultViewModel GetCalculatorSearchResult(SearchParams searchParams)
        {
            var serializer = new JavaScriptSerializer();
            List<SelectedColumnsViewModel> selectedColumns = null;

            try
            {
                selectedColumns =
                    serializer.Deserialize<List<SelectedColumnsViewModel>>(searchParams.selectedColumnsFilter);
            }
            catch (Exception e)
            {
                _logger.Info(e.StackTrace);
                _logger.Error(e.Message);
                throw;
            }

            if (selectedColumns.Count == 0)
                return null;

            VagonSourceTypeParamEnum dbType = VagonSourceTypeParamEnum.Gruzhon;
            if (searchParams.vagonSourceTypeParam == "1")
            {
                dbType = VagonSourceTypeParamEnum.Porozhn;
            }

            int totalRowsCount = 0;
            int pageId = searchParams.pageId;

            if (pageId > 1)
            {
                GetSearchResult(CacheKeys.CACHE_CALC_REPORT_TOTAL_ROWS_COUNT_KEY, SessionId, out totalRowsCount);
            }
            else
            {
                Clear(CacheKeys.CACHE_CALC_REPORT_QUERY_FULL_KEY, SessionId);
                Clear(CacheKeys.CACHE_CALC_REPORT_DATATABLE_KEY, SessionId);
                Clear(CacheKeys.CACHE_CALC_REPORT_MODEL_KEY, SessionId);
                Clear(CacheKeys.CACHE_CALC_SEARCH_STATUS_KEY, SessionId);
            }

            Guid firmId = SessionManager.FirmInfo.ID;//(HttpContext.Current.Session[SessionManager.SESSION_FIRM_INFO_KEY] as Firm).ID;
            string ip = SessionManager.CurrentIP;
            SearchResult searchResult = GetData(firmId, ip, searchParams, serializer, pageId, selectedColumns, dbType);
           // Task<SearchResult> taskSearch =
             //   Task<SearchResult>.Factory.StartNew(() => GetData(firmId, ip, searchParams, serializer, pageId, selectedColumns, dbType));

            PutSearchResult(CacheKeys.CACHE_CALC_SEARCH_STATUS_KEY, SessionId,
                 (int)searchResult.SearchStatus);
            
            if (searchResult.SearchStatus != SearchStatus.DataTooMuch)
            {
                if (searchResult.SearchStatus != SearchStatus.Error)
                    searchResult.SearchItemsModel.Query = searchResult.Query;

                PutSearchResult(CacheKeys.CACHE_CALC_SEARCH_STATUS_KEY, SessionId,
                    (int)searchResult.SearchStatus);

                if (searchResult.TotalRowsCount > 0 && searchResult.SearchResultDataTable != null)
                {
                    //full select
                    PutSearchResult(CacheKeys.CACHE_CALC_REPORT_QUERY_FULL_KEY, SessionId,
                        searchResult.Query);
                    //filtered datatable
                    PutSearchResult(CacheKeys.CACHE_CALC_REPORT_DATATABLE_KEY, SessionId,
                        searchResult.SearchResultDataTable);
                    //filtered model
                    PutSearchResult(CacheKeys.CACHE_CALC_REPORT_MODEL_KEY, SessionId, searchResult.SearchItemsModel);

                    if (pageId == 1)
                        PutSearchResult(CacheKeys.CACHE_CALC_REPORT_TOTAL_ROWS_COUNT_KEY, SessionId,
                            searchResult.TotalRowsCount);
                }
            }
            return null;
        }

        public SearchResult GetData(Guid firmId, string ip, SearchParams searchParams, JavaScriptSerializer serializer, int pageId, List<SelectedColumnsViewModel> selectedColumns, VagonSourceTypeParamEnum dbType)
        {
            int totalRowsCount = 0;
            const int pageSize = 30;
            var searchResult = new SearchResult(null, "", "", 0);
            bool searchError = true;
            string allSelect = null;

            try
            {
                var filterParamstItems = serializer.Deserialize<List<FilterParamstItemViewModel>>(searchParams.filters);

                if (filterParamstItems.Count == 0)
                    return null;
                string allQuerySummary = "";
                //select 
                int pageIdFrom = ((pageId == 1 ? 0 : pageId - 1) * pageSize) + 1;
                int pageIdTo = pageId * pageSize;

                ViewTypeReport viewTypeReport = ViewTypeReport.None;
                string queryForExelReport = null;
                DataTable dt = null;

                allSelect = CalculatorService.GetQueryReport(selectedColumns, filterParamstItems, pageIdFrom,
                    pageIdTo, out allQuerySummary, out viewTypeReport, out queryForExelReport, dbType);

                dt = EntityAdapter.GetCalculatorResultByFilter(firmId, ip, allSelect,
                    pageId == 1 ? allQuerySummary : "", out totalRowsCount, true);
              
                searchResult = new SearchResult(dt, allSelect, queryForExelReport, totalRowsCount)
                {
                    SearchItemsModel = GetSearchItemsViewModel(dt, pageSize,
                        pageId, totalRowsCount, viewTypeReport, dbType)
                };

            }
            catch (Exception e)
            {
                _logger.Error(e.StackTrace);
                _logger.Error(e.Message);
                _logger.Info(String.Format("User json filter:{0}", searchParams.filters));
                _logger.Info(String.Format("User select:{0}", allSelect));
                searchResult.SearchStatus = SearchStatus.Error;
            }
          
            return searchResult;
        }

        private string SessionId
        {
            get { return HttpContext.Current.Session.SessionID; }
        }

        public class SearchResult
        {
            public readonly DataTable SearchResultDataTable;
            public SearchItemsResultViewModel SearchItemsModel;
            public string Query;
            public string QueryForExelReport;
            public int TotalRowsCount;
            public SearchStatus SearchStatus;

            public SearchResult(DataTable searchResultDataTable, string query, string queryForExelReport, int totalRowsCount)
            {
                if (totalRowsCount <= ServerProperties.Instance.MaxCountRowsRenderGridReport)
                {
                    SearchResultDataTable = searchResultDataTable;
                    SearchStatus = totalRowsCount > 0 ? SearchStatus.DataExist :SearchStatus.DataAbsent;
                }
                else
                {
                   SearchStatus = SearchStatus.DataTooMuch;
                }

                Query = query;
                QueryForExelReport = queryForExelReport;
                TotalRowsCount = totalRowsCount;
            }
        }

        class DownloadReportResult
        {
            public string _SessionId;
            public Guid _FirmId;
            public string _PathStorageReport;
            public OperationStatus _StatusReport;
            public DownloadReportResult(string sessionId, Guid firmId, string path, OperationStatus statusReport)
            {
                _SessionId = sessionId;
                _FirmId = firmId;
                _PathStorageReport = path;
                _StatusReport = statusReport;
            }
        }

        public OperationStatus DownloadReportProcess(string pathStorageReport, string sessionId, string nameReport, Guid firmId, ReportType reportType)
        {
            string allSelect = "";
            DataTable dt;
            SearchItemsResultViewModel model;
            GetSearchResult(CacheKeys.CACHE_CALC_REPORT_QUERY_FULL_KEY, sessionId, out allSelect);
            GetSearchResult(CacheKeys.CACHE_CALC_REPORT_DATATABLE_KEY, sessionId, out dt);
            GetSearchResult(CacheKeys.CACHE_CALC_REPORT_MODEL_KEY, sessionId, out model);

            OperationStatus operationStatus = reportType == ReportType.Standart
                ? model.TotalRecords > ServerProperties.Instance.MaxCountRowsRenderGridReport
                          ? SaveReportToFileReadByRowMethod(dt, allSelect.Replace(QueryUtils.TOPCountQuery, ""), model, firmId, pathStorageReport, nameReport)
                          : SaveReportToXls(dt, allSelect.Replace(QueryUtils.TOPCountQuery, ""), model, firmId, pathStorageReport, nameReport)
                : SaveAnalize(dt, allSelect, firmId, pathStorageReport, nameReport);
            return operationStatus;
        }

        private OperationStatus SaveReportToXls(DataTable dtTable, string allSelect, SearchItemsResultViewModel model, Guid firmId, string pathStorageReport, string nameReport)
        {
            string fileName = String.Format("{0}.xls", nameReport);
            string path = pathStorageReport;
            Guid reportId = new Guid();

            if (File.Exists(path + "/" + fileName))
            {
                fileName = String.Format("{0}_{1}.xls", nameReport, DateTime.Now.ToShortDateString());
            }
            int totalRowsCount;
            if (dtTable == null)
            {
                if (String.IsNullOrEmpty(allSelect))
                    return OperationStatus.Failure;

                dtTable = EntityAdapter.GetCalculatorResultByFilter(firmId, "", allSelect, "", out totalRowsCount);
                if (dtTable.Rows.Count == 0)
                {
                    return OperationStatus.Failure;
                }
            }

            reportId = ReportAdapter.AddReportFileNameByFirm(firmId, fileName, (int)StatusProcess.Process);

            long fileLength = 0;
            try
            {
                FileStream file = System.IO.File.Create(path + "/" + fileName);

                GridView dgGrid = new GridView();
                dgGrid.DataSource = dtTable;
                dgGrid.DataBind();

                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                var dnl = new DownloadFileActionResult(dgGrid, "Report.xls");

                dnl.ExcelGridView.RenderControl(htw);

                //Open a memory stream that you can use to write back to the response
                byte[] byteArray = Encoding.UTF8.GetBytes(sw.ToString());
                file.Write(byteArray, 0, byteArray.Length);

                fileLength = file.Length;

                file.Close();
                ReportAdapter.UpdateReportStatusById(reportId, (int)StatusProcess.Ready, fileLength);
                return OperationStatus.Success;
            }
            catch (Exception e)
            {
                _logger.Info(e.StackTrace);
                _logger.Error(e.Message);

                ReportAdapter.UpdateReportStatusById(reportId, (int)StatusProcess.Error, 0);
                return OperationStatus.Failure;
            }
        }


        private OperationStatus SaveReportToFileReadByRowMethod(DataTable dt, string allSelect, SearchItemsResultViewModel model, Guid firmId, string pathStorageReport, string fileName)
        {
            OperationStatus operationStatus = OperationStatus.Failure;

            string path = pathStorageReport;
            fileName = String.Format("{0}.xlsx", fileName);
            Guid reportId;

            if (dt != null && dt.Rows.Count > 0)
            {
                reportId = ReportAdapter.AddReportFileNameByFirm(firmId, fileName, (int)StatusProcess.Process);
            }
            else
            {
                return operationStatus;
            }
            long size = 0;

            if (String.IsNullOrEmpty(allSelect))
                return operationStatus;
            try
            {
                string fullPathReport = String.Format(@"{0}{1}", path, fileName);
                if (File.Exists(fullPathReport))
                {
                    File.Delete(fullPathReport);
                }

                int indexOrderBy = allSelect.IndexOf("Order By", System.StringComparison.Ordinal);
                bool existOrderBy = indexOrderBy > 0;
                string query = "";
                string orderBy = "";

                if (existOrderBy)
                {
                    query = allSelect.Substring(0, indexOrderBy);
                    orderBy = allSelect.Substring(indexOrderBy, allSelect.Length - indexOrderBy);
                }
                else
                {
                    query = allSelect;
                }

                int totalRowsCount;
                string allQuery = String.Format("{0} {1} {2}", query, "", orderBy);

                if (model.ReportType == ViewTypeReport.Svodnaya)
                    allQuery += QueryUtils.DropTempTableQuery;
                string templateFileName = ServerProperties.Instance.TemplateMainReportFileName;
                operationStatus = EntityAdapter.SaveCalculatorResultForReportByFilter(path, fileName, templateFileName, allQuery, "", out totalRowsCount);

                FileInfo fi;
                if (operationStatus == OperationStatus.Success)
                {
                    fi = new FileInfo(fullPathReport);
                    size = fi.Length;
                }
            }
            catch (Exception e)
            {
                _logger.Info(e.StackTrace);
                _logger.Error(e.Message);
            }
            ReportAdapter.UpdateReportStatusById(reportId, operationStatus == OperationStatus.Success ? (int)StatusProcess.Ready : (int)StatusProcess.Error, size);

            return operationStatus;
        }

        private OperationStatus SaveAnalize(DataTable dt, string allSelect, Guid firmId, string path, string fileName)
        {
            Guid reportId;
            if (dt != null && dt.Rows.Count > 0)
            {
                reportId = ReportAdapter.AddReportFileNameByFirm(firmId, String.Format("{0}{1}", fileName, Constants.FileTypeExcelReport), (int)StatusProcess.Process);
            }
            else
            {
                return OperationStatus.Failure;
            }
            if (String.IsNullOrEmpty(allSelect))
                return OperationStatus.Failure;

            var operationStatus = OperationStatus.Failure;
            try
            {
                string fullPathReport;
              
                string templateFileName = ServerProperties.Instance.TemplateAnalizeFileName;
                operationStatus = EntityAdapter.SaveCalculatorResultAnalizeReport(dt, path, fileName, templateFileName, out fullPathReport);

                FileInfo fi;
                long size = 0;
                if (operationStatus == OperationStatus.Success)
                {
                    fi = new FileInfo(fullPathReport);
                    size = fi.Length;
                }

                ReportAdapter.UpdateReportStatusById(reportId, operationStatus == OperationStatus.Success ? (int)StatusProcess.Ready : (int)StatusProcess.Error, size);

                return OperationStatus.Success;

            }
            catch (Exception e)
            {
                _logger.Info(e.StackTrace);
                _logger.Error(e.Message);
                if (e.InnerException != null)
                {
                    _logger.Error(e.InnerException.Message);
                    _logger.Error(e.InnerException.StackTrace);

                }
            }

            return operationStatus;
        }

        private SearchItemsResultViewModel GetSearchItemsViewModel(DataTable dt, int pageSize, int pageId, int totalRowsCount, ViewTypeReport viewTypeReport, VagonSourceTypeParamEnum dbType)
        {
            var searchItemsResultViewModel = new SearchItemsResultViewModel();
            foreach (DataRow dr in dt.Rows)
            {
                var itemsViewModel = new ItemsViewModel();

                foreach (DataColumn column in dt.Columns)
                {
                    itemsViewModel.ValuesItemViewModel.Add(new ValueItemViewModel
                    {
                        Value = dr[column.Caption].ToString()
                    });
                }

                searchItemsResultViewModel.SearchItems.Add(itemsViewModel);
            }

            IEnumerable<HeaderItemViewModel> columns = from DataColumn c in dt.Columns
                                                       select new HeaderItemViewModel { Name = c.ColumnName };
            searchItemsResultViewModel.Headers = columns.ToList();

            searchItemsResultViewModel.TotalPages = totalRowsCount / pageSize == 0 ? 1 : (totalRowsCount / pageSize) +
                                                    (totalRowsCount % pageSize != totalRowsCount
                                                        ? 1
                                                        : 0);

            var items = new SearchItemsResultViewModel();
            //IEnumerable<ItemsViewModel> query =
            //    searchItemsResultViewModel.SearchItems.Skip((pageId - 1) * pageSize).Take(pageSize);
            IEnumerable<ItemsViewModel> query = searchItemsResultViewModel.SearchItems.AsQueryable();

            
            items.SearchItems = query.ToList();
            items.TotalPages = searchItemsResultViewModel.TotalPages;
            items.TotalRecords = totalRowsCount;
            items.Headers = searchItemsResultViewModel.Headers;
            items.CurrentPageId = pageId;
            items.ReportType = viewTypeReport;
            items.DbType = dbType;

            return items;
        }

        public IEnumerable<ColumnsSearchResultViewModel> GetColumnsSearchResultUrl()
        {
            var items = new List<ColumnsSearchResultViewModel>();

            List<FiltersTypes> list = FilterManager.GetFiltersTypes();

            foreach (FiltersTypes l in list)
            {
                items.Add(new ColumnsSearchResultViewModel
                {
                    id = l.FilterTypeID,
                    Name = l.FilterTypeName
                });
            }

            return items;
        }

        public static IEnumerable<bool> GetColumnNameById(int id)
        {
            return
                Enum.GetNames(typeof(ColumnsMapping))
                    .Select(r => r == ColumnsMapping.TransportationType.GetStringValue());
        }


    }

    public class DownloadFileActionResult : ActionResult
    {

        public GridView ExcelGridView { get; set; }
        public string fileName { get; set; }


        public DownloadFileActionResult(GridView gv, string pFileName)
        {
            ExcelGridView = gv;
            fileName = pFileName;
        }


        public override void ExecuteResult(ControllerContext context)
        {

            //Create a response stream to create and write the Excel file
            HttpContext curContext = HttpContext.Current;
            curContext.Response.Clear();
            curContext.Response.AddHeader("content-disposition", "attachment;filename=" + fileName);
            curContext.Response.Charset = "";
            curContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            curContext.Response.ContentType = "application/vnd.ms-excel";

            //Convert the rendering of the gridview to a string representation 
            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);
            ExcelGridView.RenderControl(htw);

            //Open a memory stream that you can use to write back to the response
            byte[] byteArray = Encoding.ASCII.GetBytes(sw.ToString());
            MemoryStream s = new MemoryStream(byteArray);
            StreamReader sr = new StreamReader(s, Encoding.ASCII);

            //Write the stream back to the response
            curContext.Response.Write(sr.ReadToEnd());
            curContext.Response.End();

        }

        //public class ValuesController : ApiController
        //{
        //    private static HttpSessionState session = HttpContext.Current.Session;
        //    public static Firm FirmInfo
        //    {
        //        get { return session[SessionManager.SESSION_FIRM_INFO_KEY] as Firm; }
        //        set { session[SessionManager.SESSION_FIRM_INFO_KEY] = value; }
        //    }
        //}

    }
}