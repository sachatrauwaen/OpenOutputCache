using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.OutputCache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Instrumentation;
using Satrabel.Providers.OutputCachingProviders;
using System.Text;
using Satrabel.OpenOutputCache.Components;
using DotNetNuke.Common;

namespace Satrabel.OpenOutputCache.HttpModules.OutputCache
{
    public class OpenOutputCacheModule : IHttpModule
    {
        private static readonly DnnLogger Logger = DnnLogger.GetClassLogger(typeof(OpenOutputCacheModule));
        public OpenOutputCacheModule()
        {
        }
        public void Dispose()
        {
        }
        public void Init(HttpApplication app)
        {
            //app.BeginRequest += OnBeginRequest;
            app.ResolveRequestCache += OnResolveRequestCache;
            app.UpdateRequestCache += OnUpdateRequestCache;
        }
        /*
                public void OnBeginRequest(object s, EventArgs e)
                {
                    var app = (HttpApplication)s;
                    var server = app.Server;
                    var request = app.Request;
                    var response = app.Response;

            
                    //if (RewriterUtils.OmitFromRewriteProcessing(request.Url.LocalPath))
                    //{
                    //    return;
                    //}
            

                    //'Carry out first time initialization tasks
                    //Initialize.Init(app);

                    if (request.Url.LocalPath.ToLower().EndsWith("/install/install.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("/install/upgradewizard.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("/install/installwizard.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("captcha.aspx")
                        || request.Url.LocalPath.ToLower().EndsWith("scriptresource.axd")
                        || request.Url.LocalPath.ToLower().EndsWith("webresource.axd")
                        || request.Url.LocalPath.ToLower().EndsWith(".ashx")
                        )
                    {
                        return;
                    }                      

                    var AbsoluteUri = app.Request.Url.AbsoluteUri;
                    string portalAlias;
                    PortalAliasInfo portal = GetPortalAlias(app, out portalAlias);
                    if (portal != null)
                    {
                        CacheController CacheCtrl = new CacheController(portal.PortalID);
                        var CacheItem = CacheCtrl.GetItem(AbsoluteUri);

                        if (CacheItem != null)
                        {
                            if (IsCachable(app)){
                                app.Context.Items["OpenOutputCache:CacheKey"] = CacheItem.CacheKey;
                            }
                    
                    
                            //string sendToUrl = "~/Portals/0/Cache/Output/55_65E28BECAA964E5BE8F2284FF6380489.data.html";
                            //Satrabel.HttpModules.RewriterUtils.RewriteUrl(app.Context, sendToUrl);


                            //var queryString = string.Empty;
                            //string sendToUrlLessQString = sendToUrl;
                            //if ((sendToUrl.IndexOf("?") > 0))
                            //{
                            //    sendToUrlLessQString = sendToUrl.Substring(0, sendToUrl.IndexOf("?"));
                            //    queryString = sendToUrl.Substring(sendToUrl.IndexOf("?") + 1);
                            //}

                            //grab the file's physical path
                            //string filePath = string.Empty;
                            //filePath = app.Context.Server.MapPath(sendToUrlLessQString);

                            //rewrite the path..
                            //app.Context.RewritePath(sendToUrlLessQString, String.Empty, queryString);
                    
                        }
                    }
                }
        */
        private void OnResolveRequestCache(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            if (!IsCachable(app))
            {
                return;
            }
            HttpContext context = app.Context;
            try
            {
                //string CacheKey = app.Context.Items["OpenOutputCache:CacheKey"];
                PortalSettings ps = PortalController.GetCurrentPortalSettings();
                if (ps == null || ps.ActiveTab == null || ps.ActiveTab.TabID == Null.NullInteger)
                {
                    return;
                }
                int TabId = ps.ActiveTab.TabID;
                var tc = new TabController();
                Hashtable tabSettings = tc.GetTabSettings(TabId);
                string CacheProvider = GetTabSettingAsString(tabSettings, "CacheProvider");
                if (string.IsNullOrEmpty(CacheProvider))
                {
                    return;
                }
                OutputCachingProvider Provider = OutputCachingProvider.Instance(CacheProvider);
                //string CurrentCulture = Localization.GetPageLocale(ps).Name;
                StringCollection includeVaryByKeys;
                StringCollection excludeVaryByKeys;
                GetVarBy(tabSettings, out includeVaryByKeys, out excludeVaryByKeys, ps.PortalId);
                SortedDictionary<string, string> varyBy = new SortedDictionary<string, string>();
                bool VaryAll = includeVaryByKeys.Count == 0;
                foreach (string key in app.Context.Request.QueryString.Keys)
                {
                    varyBy.Add(key.ToLower(), app.Context.Request.QueryString[key]);
                    if (VaryAll)
                    {
                        includeVaryByKeys.Add(key.ToLower());
                    }
                    else
                    {
                        if (!includeVaryByKeys.Contains(key) && !excludeVaryByKeys.Contains(key))
                        {
                            return;
                        }
                    }
                }
                if (PortalController.GetPortalSettingAsBoolean("OOC_VaryByBrowser", ps.PortalId, false))
                {
                    varyBy.Add("Browser", app.Context.Request.Browser.Browser);
                    includeVaryByKeys.Add("browser");
                }
                //excludeVaryByKeys.Add("returnurl");
                string CacheKey = Provider.GenerateCacheKey(TabId, includeVaryByKeys, excludeVaryByKeys, varyBy);
                string RawCacheKey = GetRawCacheKey(includeVaryByKeys, excludeVaryByKeys, varyBy);
                app.Context.Items["OpenOutputCache:RawCacheKey"] = RawCacheKey;
                app.Context.Items["OpenOutputCache:AbsoluteUri"] = app.Request.Url.AbsoluteUri;
                string CacheMode = PortalController.GetPortalSetting("OOC_CacheMode", ps.PortalId, "ServerCache");
                int ExpireDelay = PortalController.GetPortalSettingAsInteger("OOC_ExpireDelay", ps.PortalId, 60);
                if (Provider.StreamOutput(TabId, CacheKey, app.Context))
                {
                    app.Context.Response.AddHeader("Content-Type", "text/html; charset=utf-8");
                    SetResponseCache(app.Context.Response, CacheKey, ExpireDelay, CacheMode);
                    app.CompleteRequest();
                }
                else
                {
                    int DefaultCacheDuration = PortalController.GetPortalSettingAsInteger("OOC_CacheDuration", ps.PortalId, 60);
                    int CacheDuration = GetTabSettingAsInteger(tabSettings, "CacheDuration", DefaultCacheDuration);
                    if (CacheDuration > 0)
                    {
                        int DefaultMaxVaryByCount = PortalController.GetPortalSettingAsInteger("OOC_MaxVaryByCount", ps.PortalId, 0);
                        int MaxVaryByCount = GetTabSettingAsInteger(tabSettings, "MaxVaryByCount", DefaultMaxVaryByCount);
                        int ItemCount = 0;
                        if (MaxVaryByCount > 0)
                        {
                            ItemCount = OutputCachingProvider.Instance(CacheProvider).GetItemCount(TabId);
                        }
                        if (MaxVaryByCount <= 0 || ItemCount < MaxVaryByCount)
                        {
                            OutputCacheResponseFilter responseFilter = OutputCachingProvider.Instance(CacheProvider).GetResponseFilter(TabId, MaxVaryByCount, app.Response.Filter, CacheKey, TimeSpan.FromSeconds(CacheDuration));
                            app.Context.Response.Filter = responseFilter;
                            app.Context.Items["OpenOutputCache:Filter"] = responseFilter;
                            app.Context.Items["OpenOutputCache:CacheMode"] = CacheMode;
                            app.Context.Items["OpenOutputCache:ExpireDelay"] = ExpireDelay;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        private void GetVarBy(Hashtable tabSettings, out StringCollection includeVaryByKeys, out StringCollection excludeVaryByKeys, int PortalId)
        {
            includeVaryByKeys = new StringCollection();
            excludeVaryByKeys = new StringCollection();
            bool Exclude = GetTabSettingAsInteger(tabSettings, "CacheIncludeExclude", 0) == 0;
            string DefaultIncludeVaryBy = PortalController.GetPortalSetting("IncludeVaryBy", PortalId, "");
            string DefaultExcludeVaryBy = PortalController.GetPortalSetting("ExcludeVaryBy", PortalId, "returnurl");
            if (!string.IsNullOrEmpty(DefaultIncludeVaryBy))
            {
                includeVaryByKeys.AddRange(DefaultIncludeVaryBy.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            if (!string.IsNullOrEmpty(DefaultExcludeVaryBy))
            {
                excludeVaryByKeys.AddRange(DefaultExcludeVaryBy.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            string IncludeVaryBy = GetTabSettingAsString(tabSettings, "IncludeVaryBy");
            string ExcludeVaryBy = GetTabSettingAsString(tabSettings, "ExcludeVaryBy");
            if (Exclude)
            {
                if (!string.IsNullOrEmpty(IncludeVaryBy))
                {
                    includeVaryByKeys.AddRange(IncludeVaryBy.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ExcludeVaryBy))
                {
                    excludeVaryByKeys.AddRange(ExcludeVaryBy.ToLower().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }
        private void OnUpdateRequestCache(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpContext context = app.Context;
            try
            {
                var HttpContextFilter = context.Items["OpenOutputCache:Filter"];
                //OpenOutputCacheFilter filter = context.Response.Filter as OpenOutputCacheFilter;
                if (HttpContextFilter != null)
                {
                    //PortalSettings ps = PortalController.GetCurrentPortalSettings();
                    //int TabId = ps.ActiveTab.TabID;
                    OpenOutputCacheFilter filter = (OpenOutputCacheFilter)HttpContext.Current.Items["OpenOutputCache:Filter"];
                    filter.StopFiltering();
                    var Response = context.Response;
                    Response.Cache.SetLastModified(DateTime.UtcNow);
                    string CacheMode = (string)app.Context.Items["OpenOutputCache:CacheMode"];
                    int ExpireDelay = (int)app.Context.Items["OpenOutputCache:ExpireDelay"];
                    SetResponseCache(Response, filter.CacheKey, ExpireDelay, CacheMode);
                    Logger.InfoFormat("OnUpdateRequestCache {0} / {1}", filter.TabId, filter.CacheKey);
                }
            }
            catch (Exception ex)
            {
                DnnLog.Error(ex);
            }
        }

        private static void SetResponseCache(HttpResponse Response, string CacheKey, int ExpireDelay, string CacheMode)
        {
            TimeSpan freshness = new TimeSpan(0, 0, 0, 0);
            //Response.Cache.SetCacheability(HttpCacheability.NoCache);
            if (CacheMode == "ServerCache")
            {
                Response.Cache.SetCacheability(HttpCacheability.Server);
            }
            if (CacheMode == "ExpireDelay")
            {
                freshness = new TimeSpan(0, 0, 0, ExpireDelay);
                Response.Cache.VaryByHeaders["Cookie"] = true; // authentificated users
            }
            if (CacheMode == "IfModified" || CacheMode == "ExpireDelay")
            {
                Response.Cache.SetCacheability(HttpCacheability.Public);
                Response.Cache.SetETag('"' + CacheKey + '"');
                Response.Cache.SetMaxAge(freshness);
            }
            //Response.Cache.SetExpires(DateTime.Now.Add(freshness));                            
        }

        private bool IsCachable(HttpApplication app)
        {
            Logger.InfoFormat("IsCachable {0} ", app.Context.Request.Url.LocalPath);
            if (app == null)
            {
                return false;
            }

            if (app.Context == null || app.Context.Items == null)
            {
                return false;
            }

            if (app.Response.ContentType.ToLower() != "text/html")
            {
                return false;
            }
            if (app.Context.Request.IsAuthenticated)
            {
                return false;
            }
            if (app.Context.Request.RequestType == "POST")
            {
                return false;
            }
            if (!app.Context.Request.Url.LocalPath.EndsWith("Default.aspx", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (app.Context.Request.QueryString["error"] != null)
            {
                return false;
            }
            Logger.InfoFormat("IsCachable {0} ", true);
            return true;
        }

        public static int GetTabSettingAsInteger(Hashtable TabSettings, string key, int defaultValue)
        {
            int retValue = defaultValue;
            try
            {
                if (TabSettings[key] == null || string.IsNullOrEmpty(TabSettings[key].ToString()))
                {
                    retValue = defaultValue;
                }
                else
                {
                    retValue = Convert.ToInt32(TabSettings[key]);
                }
            }
            catch (Exception exc)
            {
                DnnLog.Error(exc);
            }
            return retValue;
        }

        public static String GetTabSettingAsString(Hashtable TabSettings, string key)
        {
            String retValue = "";
            try
            {
                if (TabSettings[key] != null)
                {
                    retValue = TabSettings[key].ToString();
                }
            }
            catch (Exception exc)
            {
                DnnLog.Error(exc);
            }
            return retValue;
        }

        public string GetRawCacheKey(StringCollection includeVaryByKeys, StringCollection excludeVaryByKeys, SortedDictionary<string, string> varyBy)
        {
            var cacheKey = new StringBuilder();
            if (varyBy != null)
            {
                SortedDictionary<string, string>.Enumerator varyByParms = varyBy.GetEnumerator();
                while ((varyByParms.MoveNext()))
                {
                    string key = varyByParms.Current.Key.ToLower();
                    if (includeVaryByKeys.Contains(key) && !excludeVaryByKeys.Contains(key))
                    {
                        cacheKey.Append(string.Concat(key, "=", varyByParms.Current.Value, "|"));
                    }
                }
            }
            return cacheKey.ToString();
        }
        /*
        private static PortalAliasInfo GetPortalAlias(HttpApplication app, out string portalAlias)
        {
            PortalAliasInfo objPortalAlias = null;
            string myAlias = Globals.GetDomainName(app.Request, true);
            portalAlias = "";
            do
            {
                objPortalAlias = PortalAliasController.GetPortalAliasInfo(myAlias);

                if (objPortalAlias != null)
                {
                    portalAlias = myAlias;
                    break;
                }

                int slashIndex = myAlias.LastIndexOf('/');
                if (slashIndex > 1)
                {
                    myAlias = myAlias.Substring(0, slashIndex);
                }
                else
                {
                    myAlias = "";
                }
            } while (myAlias.Length > 0);

            return objPortalAlias;
        }
        */

    }
}