using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using DotNetNuke.Common.Utilities;
using System.Web.Caching;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Cache;
using System.Collections.Generic;
using DotNetNuke.Services.OutputCache;
using Satrabel.Providers.OutputCachingProviders;

namespace Satrabel.OpenOutputCache.Components
{
    public class CacheController
    {
        private static readonly DnnLogger Logger = DnnLogger.GetClassLogger(typeof(CacheController));
        private int _portalId;
        private List<OpenOutputCacheItem> _rules;
        public CacheController(int PortalId) {
            _portalId = PortalId;
            _rules = GetUrlCacheItems();
        }
        public const string OutputCacheItemsCacheKey = "OutputCacheItems{0}";
        private List<OpenOutputCacheItem> GetUrlCacheItems()
        {
            string cacheKey = String.Format(OutputCacheItemsCacheKey, _portalId);
            return CBO.GetCachedObject<List<OpenOutputCacheItem>>(
                new CacheItemArgs(cacheKey, DataCache.TabCacheTimeOut, DataCache.TabCachePriority, _portalId),
                GetUrlCacheItemsCallBack);           
        }
        private object GetUrlCacheItemsCallBack(CacheItemArgs cacheItemArgs)
	    {
            int PortalId = (int)cacheItemArgs.ParamList[0];
            //UrlRuleConfiguration config = UrlRuleConfiguration.GenerateConfig(PortalId);
            List<OpenOutputCacheItem> items = new List<OpenOutputCacheItem>();
            var provider = OutputCachingProvider.Instance("FileOutputCachingProvider") as OpenOutputCachingProvider;
            if (provider != null) {
                foreach (var item in provider.GetCacheItems()) {
                    items.Add(item);    
                }            
            }
            int CacheTimeout = 20 * Convert.ToInt32(DotNetNuke.Entities.Host.Host.PerformanceSetting);
            cacheItemArgs.CacheTimeOut = CacheTimeout;
            cacheItemArgs.CacheDependency = new DNNCacheDependency(null, null);
            #if DEBUG
            cacheItemArgs.CacheCallback = new CacheItemRemovedCallback(this.RemovedCallBack);
            #endif
            return items;
	    }

        private void RemovedCallBack(string k, object v, CacheItemRemovedReason r)
        {            
            Logger.Info(k + " : " + r.ToString() + "/" + Environment.StackTrace);
        }

        #region Rewriter
        public IEnumerable<OpenOutputCacheItem> GetItems()
        {
            return _rules;
        }
        public OpenOutputCacheItem GetItem(string AbsoluteUrl)
        {
            return _rules.SingleOrDefault(u => u.AbsoluteUri == AbsoluteUrl);
        }
        #endregion
    }
}
