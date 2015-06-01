using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using log4net;

namespace BO.Implementation.Caching.Provider
{
    public class HttpCache : CacheProvider<Cache>
    {
        readonly ILog _logger = LogManager.GetLogger(typeof(HttpCache));

        protected override Cache InitCache()
        {
            return HttpRuntime.Cache;
        }
        
        public override bool Get<T>(string key, out T value)
        {
            try
            {
                if (_cache[key] == null)
                {
                    value = default(T);
                    return false;
                }

                value = (T)_cache[key];
            }
            catch
            {

                value = default(T);
                return false;
            }
            return true;
        }
        
        public override void Set<T>(string key, T value)
        {
            Set<T>(key, value, CacheDuration);
        }

        public override void Set<T>(string key, T value, int duration)
        {
            _cache.Insert(key, value, null, DateTime.Now.AddMinutes(duration), Cache.NoSlidingExpiration);
        }

        public override void Set<T>(string key, T value, int duration, CacheItemPriority cacheItemPriority)
        {
            _cache.Insert(key, value, null, DateTime.Now.AddMinutes(duration), Cache.NoSlidingExpiration, cacheItemPriority, OnRemoveCallback);
        }

       
        private void OnRemoveCallback(string key, object value, CacheItemRemovedReason reason)
        {
            _logger.Info(String.Format(" OnRemoveCallback called  key : {0} ", key));
        }

        public override void Clear(string key)
        {
            _cache.Remove(key);
        }

        public override IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            foreach (DictionaryEntry item in _cache)
            {
                yield return new KeyValuePair<string, object>(item.Key as string, item.Value);

            }
        }
    }
}
