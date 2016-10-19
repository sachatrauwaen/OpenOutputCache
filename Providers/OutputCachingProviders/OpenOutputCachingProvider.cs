using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetNuke.Services.OutputCache;
using System.IO;
using DotNetNuke.Instrumentation;
using System.Text;

namespace Satrabel.Providers.OutputCachingProviders
{

    /// <summary>
    /// Description résumée de OpenOutputCachingProvider
    /// </summary>
    public abstract class OpenOutputCachingProvider : OutputCachingProvider
    {
        public OpenOutputCachingProvider()
        {
        }
        public override OutputCacheResponseFilter GetResponseFilter(int tabId, int maxVaryByCount, Stream responseFilter, string cacheKey, TimeSpan cacheDuration)
        {
            return new OpenOutputCacheFilter(this, tabId, maxVaryByCount, responseFilter, cacheKey, cacheDuration);
        }
        public abstract bool IsExpired(int tabId, string cacheKey, out DateTime LastModified);
        public override bool StreamOutput(int tabId, string cacheKey, HttpContext context)
        {
            bool Succes = false;
            try
            {
                DateTime LastModified;
                bool Expired = IsExpired(tabId, cacheKey, out LastModified);
                if (!Expired)
                {
                    var Response = context.Response;
                    Response.Cache.SetLastModified(LastModified);
                    Response.Cache.SetETag('"' + cacheKey + '"');
                    TimeSpan freshness = new TimeSpan(0, 0, 0, 0);
                    //Response.Cache.SetExpires(DateTime.UtcNow.Add(freshness));
                    Response.Cache.SetMaxAge(freshness);
                    Response.Cache.SetCacheability(HttpCacheability.Public);
                    //Response.Cache.SetValidUntilExpires(true);
                    //Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                    //Response.AddHeader("Vary", "Accept-Encoding");
                    //Response.Cache.VaryByHeaders["Accept-Encoding"] = true;
                    bool send304 = false;
                    HttpRequest request = context.Request;
                    string ifModifiedSinceHeader = request.Headers["If-Modified-Since"];
                    string etag = request.Headers["If-None-Match"];
                    if (ifModifiedSinceHeader != null && etag != null)
                    {
                        etag = etag.Trim('"');
                        try
                        {
                            DateTime utcIfModifiedSince = DateTime.Parse(ifModifiedSinceHeader);
                            if (LastModified <= utcIfModifiedSince && etag == cacheKey)
                            {
                                Response.StatusCode = 304;
                                Response.StatusDescription = "Not Modified";
                                Response.SuppressContent = true;
                                //Response.ClearContent();
                                Response.AddHeader("Content-Length", "0");
                                send304 = true;
                            }
                        }
                        catch
                        {
                            DnnLog.Error("Ignore If-Modified-Since header, invalid format: " + ifModifiedSinceHeader);
                        }
                    }
                    else
                    {

                    }
                    if (!send304)
                    {
                        context.Response.Write(Encoding.Default.GetString(GetOutput(tabId, cacheKey)));
                        //context.Response.TransmitFile(cachedOutputFileName);
                    }
                    Succes = true;
                }
                else
                {
                    Remove(tabId);
                    Succes = false;
                }
            }
            catch (Exception ex)
            {
                Succes = false;
                DnnLog.Error(ex);
            }
            return Succes;
        }
        public abstract List<OpenOutputCacheItem> GetCacheItems(int TabId);
        public abstract List<OpenOutputCacheItem> GetCacheItems();
    }
    public class OpenOutputCacheItem
    {
        public int Tabid { get; set; }
        public string CacheKey { get; set; }
        public string RawCacheKey { get; set; }
        public DateTime Expire { get; set; }
        public DateTime Modified { get; set; }
        public string Url { get; set; }
        public string AbsoluteUri { get; set; }

    }
}