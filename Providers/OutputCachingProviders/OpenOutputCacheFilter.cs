using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using DotNetNuke.Services.OutputCache;
using System.Text;
using System.Collections.Specialized;

namespace Satrabel.Providers.OutputCachingProviders
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenOutputCacheFilter : OutputCacheResponseFilter
    {
        private OpenOutputCachingProvider Provider;
        private int _TabId;
        public int TabId
        {
            get
            {
                return _TabId;
            }
        }
        private string _RawCacheKey;
        public string RawCacheKey
        {
            get
            {
                return _RawCacheKey;
            }
        }
        internal OpenOutputCacheFilter(OpenOutputCachingProvider provider, int tabId,
                                        int maxVaryByCount, Stream responseFilter, string cacheKey,
                                        TimeSpan cacheDuration) :
            base(responseFilter, cacheKey, cacheDuration, maxVaryByCount)
        {
            Provider = provider;
            _TabId = tabId;
            CaptureStream = new MemoryStream();
        }
        protected override void AddItemToCache(int itemId, string output)
        {
            Provider.SetOutput(itemId, CacheKey, CacheDuration, Encoding.Default.GetBytes(output));
        }
        protected override void RemoveItemFromCache(int itemId)
        {
            Provider.Remove(itemId);
        }
        public void StopFiltering()
        {
            StopFiltering(TabId, false);
        }
    }
}