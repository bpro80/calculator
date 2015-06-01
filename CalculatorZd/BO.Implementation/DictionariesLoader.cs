using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Caching;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using BO.Implementation.Calculator;
using DA.Entities;
using log4net;

namespace BO.Implementation
{
    public class DictionariesLoader
    {
        static int DefaultDictionaryDuration = 10080;//week

        private static bool _isFirstStart = true;
        private static Timer _timer;
        static readonly ILog _logger = LogManager.GetLogger(typeof(DictionariesLoader));
        static readonly HttpCache _http = new HttpCache();
        public static void Start()
        {
            if (_timer == null)
            {
                var autoEvent = new AutoResetEvent(false);
                TimerCallback checkDictionaries = LoadData;
                _timer = new Timer(checkDictionaries, autoEvent, 1000, 3600000);//1 hours iteration cycle
            }
        }

        public void Stop()
        {
            _timer.Dispose();
        }

        public static void LoadData(object state)
        {
            TimeSpan timeOfDay = DateTime.Now.TimeOfDay;
            int hour = timeOfDay.Hours;

            if (_isFirstStart || hour == 5)
            {
                _isFirstStart = false;
                
                _logger.Info("Load dictionaries");
                
                LoadDFilterData(FilterNameHelper.CargoNameEarlyTransportationFilterName, "",
                          ServerProperties.Instance.DBPorozhn);

                LoadDFilterData(FilterNameHelper.CargoNameFilterName);
                LoadDFilterData(FilterNameHelper.ContainerVolumeFilterName);
                LoadDFilterData(FilterNameHelper.CargoGroupNamesFilterName);
                LoadDFilterData(FilterNameHelper.TransportationTypeFilterName);
               // LoadDFilterData(FilterNameHelper.CompanyRecipientFilterName);
                
                //Грузоотправитель
                LoadCompanySending();
                
                //Грузополучатель
                LoadCompanyRecipient();

                //Станция отправления
                LoadStationSending();

                //Станция назначения
                LoadDeliveringSending();

                LoadDFilterData(FilterNameHelper.CountriesSendingFilterName);
                LoadDFilterData(FilterNameHelper.CountriesDeliveringFilterName);
                LoadDFilterData(FilterNameHelper.CountriesDeliveringFilterName);
                LoadDFilterData(FilterNameHelper.WaySendingFilterName);
                LoadDFilterData(FilterNameHelper.WayDeliveringFilterName);
                LoadDFilterData(FilterNameHelper.SubjectsSendingFilterName);
                LoadDFilterData(FilterNameHelper.SubjectsDeliveringFilterName);
                LoadDFilterData(FilterNameHelper.PayerFilterName);
                LoadDFilterData(FilterNameHelper.OwnerVagonEGRPOFilterName);
                LoadDFilterData(FilterNameHelper.RenterVagonGVCFilterName);
                FilterManager.GetWagonTypesHierarhy();
               
                _logger.Info("Load dictionaries finished");
            }
        }

        public static void LoadDFilterData(string filterNameHelper, string orderBy = "", string dbname = null)
        {
            try
            {
                List<string> list = CalculatorManager.GetEntities(filterNameHelper, "", "", orderBy, dbname);
                if (list != null) 
                    AddToCache(filterNameHelper, list);
                _logger.Info(String.Format("load dictionary {0}, time: {1}, count:{2}", filterNameHelper, DateTime.Now.ToString(), list.Count));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
            }
        }

         public static void LoadCompanySending()
        {
            try
            {
                List<string> list = EntityAdapter.GetCompanySendingDictionary();
                if (list != null)
                    AddToCache(FilterNameHelper.CompanySendingFilterName, list);
                _logger.Info(String.Format("load dictionary {0}, time: {1}, count:{2}", FilterNameHelper.CompanySendingFilterName, DateTime.Now.ToString(), list.Count));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
            }
        }

         public static void LoadStationSending()
         {
             try
             {
                 List<string> list = EntityAdapter.GetStationSendingDictionary();
                 if (list != null)
                     AddToCache(FilterNameHelper.StationsSendingFilterKey, list);
                 _logger.Info(String.Format("load dictionary {0}, time: {1}, count:{2}", FilterNameHelper.StationsSendingFilterKey, DateTime.Now.ToString(), list.Count));
             }
             catch (Exception ex)
             {
                 _logger.Error(ex.StackTrace);
             }
         }

         public static void LoadDeliveringSending()
         {
             try
             {
                 List<string> list = EntityAdapter.GetStationDeliveringDictionary();
                 if (list != null)
                     AddToCache(FilterNameHelper.StationsDeliveringFilterKey, list);
                 _logger.Info(String.Format("load dictionary {0}, time: {1}, count:{2}", FilterNameHelper.StationsDeliveringFilterKey, DateTime.Now.ToString(), list.Count));
             }
             catch (Exception ex)
             {
                 _logger.Error(ex.StackTrace);
             }
         }
          public static void LoadCompanyRecipient()
        {
            try
            {
                List<string> list = EntityAdapter.GetCompanyRecipientDictionary();
                if (list != null)
                    AddToCache(FilterNameHelper.CompanyRecipientFilterName, list);
                _logger.Info(String.Format("load dictionary {0}, time: {1}, count:{2}", FilterNameHelper.CompanyRecipientFilterName, DateTime.Now.ToString(), list.Count));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.StackTrace);
            }
        }
        
        private static void AddToCache(string key, object obj)
        {
            _http.Set(key, obj, DefaultDictionaryDuration, CacheItemPriority.NotRemovable);
        }
    }
}