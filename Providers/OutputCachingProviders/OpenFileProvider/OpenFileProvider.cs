#region Copyright
/*
 *
 * Satabel
 * 
 */
#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DotNetNuke.Services.OutputCache;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Collections.Internal;
using DotNetNuke.Common;
using System.Globalization;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Exceptions;
using System.Web;

#endregion

namespace Satrabel.Providers.OutputCachingProviders.OpenFileProvider
{
    public class OpenFileProvider : OpenOutputCachingProvider
    {
        #region "Private Members"
        public const string DataFileExtension = ".data.resources";
        public const string AttribFileExtension = ".attrib.resources";
        private static readonly SharedDictionary<int, string> CacheFolderPath = new SharedDictionary<int, string>(LockingStrategy.ReaderWriter);
        #endregion

        public OpenFileProvider()
        {
        }
        public static string GetAttribFileName(int tabModuleId, string cacheKey)
        {
            return string.Concat(GetCacheFolder(), cacheKey, AttribFileExtension);
        }
        public static int GetCachedItemCount(int tabId)
        {
            string folder = GetCacheFolder();
            if (Directory.Exists(folder))
                return Directory.GetFiles(folder, $"{tabId}_*{DataFileExtension}").Length;
            else
            {
                return 0;
            }
        }
        public static string GetCachedOutputFileName(int tabId, string cacheKey)
        {
            return string.Concat(GetCacheFolder(), cacheKey, DataFileExtension);
        }
        private static string GetCacheFolder(int portalId)
        {
            string cacheFolder;

            using (var readerLock = CacheFolderPath.GetReadLock())
            {
                if (CacheFolderPath.TryGetValue(portalId, out cacheFolder))
                {
                    return cacheFolder;
                }
            }
            var portalController = new PortalController();
            PortalInfo portalInfo = portalController.GetPortal(portalId);
            string homeDirectoryMapPath = portalInfo.HomeDirectoryMapPath;
            if (!(string.IsNullOrEmpty(homeDirectoryMapPath)))
            {
                cacheFolder = string.Concat(homeDirectoryMapPath, "Cache\\Output\\");
                if (!(Directory.Exists(cacheFolder)))
                {
                    Directory.CreateDirectory(cacheFolder);
                }
            }
            using (var writerLock = CacheFolderPath.GetWriteLock())
            {
                if (!CacheFolderPath.ContainsKey(portalId))
                    CacheFolderPath.Add(portalId, cacheFolder);
            }
            return cacheFolder;
        }
        private static string GetCacheFolder()
        {
            int portalId = PortalController.GetCurrentPortalSettings().PortalId;
            return GetCacheFolder(portalId);
        }
        public override int GetItemCount(int tabId)
        {
            return OpenFileProvider.GetCachedItemCount(tabId);
        }
        public override byte[] GetOutput(int tabId, string cacheKey)
        {
            string cachedOutputFileName = GetCachedOutputFileName(tabId, cacheKey);
            if (File.Exists(cachedOutputFileName))
            {
                return File.ReadAllBytes(cachedOutputFileName);
            }
            else
            {
                return null;
            }
        }
        private bool IsFileExpired(string file)
        {
            StreamReader oRead = null;
            try
            {
                var Lines = File.ReadAllLines(file);
                DateTime expires = DateTime.Parse(Lines[0], CultureInfo.InvariantCulture);
                if (expires < DateTime.UtcNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                //if check expire time failed, then force to expire the cache.
                DnnLog.Error(ex);
                return true;
            }
            finally
            {
                if (oRead != null)
                {
                    oRead.Close();
                }
            }
        }
        public override bool IsExpired(int tabId, string cacheKey, out DateTime LastModified)
        {
            LastModified = Null.NullDate;

            string attribFileName = OpenFileProvider.GetAttribFileName(tabId, cacheKey);
            string cachedOutputFileName = OpenFileProvider.GetCachedOutputFileName(tabId, cacheKey);
            if (File.Exists(attribFileName) && File.Exists(cachedOutputFileName) && !IsFileExpired(attribFileName))
            {
                LastModified = File.GetLastWriteTimeUtc(cachedOutputFileName);
                return false;
            }
            return true;
        }
        private void PurgeCache(string folder)
        {
            var filesNotDeleted = new StringBuilder();
            int i = 0;
            foreach (string File in Directory.GetFiles(folder, "*.resources"))
            {
                if (!FileSystemUtils.DeleteFileWithWait(File, 100, 200))
                {
                    filesNotDeleted.Append($"{File};");
                }
                else
                {
                    i += 1;
                }
            }
            if (filesNotDeleted.Length > 0)
            {
                throw new IOException($"Deleted {i} files, however, some files are locked.  Could not delete the following files: {filesNotDeleted}");
            }
        }
        public override void PurgeCache(int portalId)
        {
            string cacheFolder = GetCacheFolder(portalId);
            if (!string.IsNullOrEmpty(cacheFolder))
            {
                this.PurgeCache(cacheFolder);
            }
        }
        private static bool IsPathInApplication(string cacheFolder)
        {
            return cacheFolder.Contains(Globals.ApplicationMapPath);
        }
        public override void PurgeExpiredItems(int portalId)
        {
            var filesNotDeleted = new StringBuilder();
            int i = 0;
            string cacheFolder = GetCacheFolder(portalId);
            if (Directory.Exists(cacheFolder) && IsPathInApplication(cacheFolder))
            {
                foreach (string file in Directory.GetFiles(cacheFolder, $"*{AttribFileExtension}"))
                {
                    if (IsFileExpired(file))
                    {
                        string fileToDelete = file.Replace(AttribFileExtension, DataFileExtension);
                        if (!FileSystemUtils.DeleteFileWithWait(fileToDelete, 100, 200))
                        {
                            filesNotDeleted.Append($"{fileToDelete};");
                        }
                        else
                        {
                            i += 1;
                        }
                    }
                }
            }
            if (filesNotDeleted.Length > 0)
            {
                throw new IOException($"Deleted {i} files, however, some files are locked.  Could not delete the following files: {filesNotDeleted}");
            }
        }

        public override void Remove(int tabId)
        {
            Dictionary<int, int> portalDictionary = PortalController.GetPortalDictionary();
            if (portalDictionary.ContainsKey(tabId) && portalDictionary[tabId] > Null.NullInteger)
            {
                StringBuilder stringBuilder = new StringBuilder();
                int num = 0;
                string cacheFolder = OpenFileProvider.GetCacheFolder(portalDictionary[tabId]);
                if (!string.IsNullOrEmpty(cacheFolder))
                {
                    string[] files = Directory.GetFiles(cacheFolder, string.Concat(tabId, "_*.*"));
                    for (int i = 0; i < checked((int)files.Length); i++)
                    {
                        string str = files[i];
                        if (FileSystemUtils.DeleteFileWithWait(str, 100, 200))
                        {
                            num++;
                        }
                        else
                        {
                            stringBuilder.Append(string.Concat(str, ";"));
                        }
                    }
                    if (stringBuilder.Length > 0)
                    {
                        throw new IOException($"Deleted {num} files, however, some files are locked.  Could not delete the following files: {stringBuilder}");
                    }
                }
            }
        }

        public override void SetOutput(int tabId, string cacheKey, TimeSpan duration, byte[] output)
        {
            try
            {
                string attribFileName = GetAttribFileName(tabId, cacheKey);
                string cachedOutputFileName = GetCachedOutputFileName(tabId, cacheKey);
                string AbsoluteUri = HttpContext.Current.Items["OpenOutputCache:AbsoluteUri"].ToString();


                if (File.Exists(cachedOutputFileName))
                {
                    FileSystemUtils.DeleteFileWithWait(cachedOutputFileName, 100, 200);
                }
                if (File.Exists(attribFileName))
                {
                    FileSystemUtils.DeleteFileWithWait(attribFileName, 100, 200);
                }
                File.WriteAllBytes(cachedOutputFileName, output);
                File.WriteAllLines(attribFileName, new string[] {
                    DateTime.UtcNow.Add(duration).ToString(CultureInfo.InvariantCulture),
                    DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
                    HttpContext.Current.Request.RawUrl,
                    HttpContext.Current.Request.Url.PathAndQuery,
                    HttpContext.Current.Request.Browser.Browser,
                    HttpContext.Current.Items["OpenOutputCache:RawCacheKey"].ToString(),
                    tabId.ToString(),
                    AbsoluteUri
                });
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }
        }

        public override bool StreamOutput(int tabId, string cacheKey, HttpContext context)
        {
            bool Succes = false;
            try
            {
                string attribFileName = OpenFileProvider.GetAttribFileName(tabId, cacheKey);
                string cachedOutputFileName = OpenFileProvider.GetCachedOutputFileName(tabId, cacheKey);
                if (File.Exists(attribFileName) && File.Exists(cachedOutputFileName) && !IsFileExpired(attribFileName))
                {
                    var Response = context.Response;
                    DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(cachedOutputFileName);
                    Response.Cache.SetLastModified(lastWriteTimeUtc);
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
                            if (lastWriteTimeUtc <= utcIfModifiedSince && etag == cacheKey)
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

                    if (!send304)
                    {
                        context.Response.TransmitFile(cachedOutputFileName);
                    }

                    Succes = true;
                }
                else
                {
                    FileSystemUtils.DeleteFileWithWait(attribFileName, 100, 200);
                    FileSystemUtils.DeleteFileWithWait(cachedOutputFileName, 100, 200);
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

        public override List<OpenOutputCacheItem> GetCacheItems(int TabId)
        {
            List<OpenOutputCacheItem> items = new List<OpenOutputCacheItem>();

            var Files = Directory.GetFiles(GetCacheFolder(), $"{TabId}_*{DataFileExtension}");
            foreach (var dateFileName in Files)
            {
                //LastModified = Null.NullDate;

                string attribFileName = dateFileName.Replace(DataFileExtension, AttribFileExtension);

                if (File.Exists(attribFileName) && File.Exists(dateFileName) /*&& !IsFileExpired(attribFileName)*/ )
                {
                    DateTime LastModified = File.GetLastWriteTimeUtc(dateFileName);

                    string[] lines = File.ReadAllLines(attribFileName);
                    DateTime expire = DateTime.Parse(lines[0], CultureInfo.InvariantCulture);
                    string RawUrl = lines[2];


                    items.Add(new OpenOutputCacheItem()
                    {
                        Tabid = TabId,
                        CacheKey = Path.GetFileName(dateFileName).Replace(DataFileExtension, ""),
                        Expire = expire,
                        Modified = LastModified,
                        Url = RawUrl,
                        RawCacheKey = lines[5],
                        AbsoluteUri = lines[7]
                    });
                }
            }
            return items;
        }
        public override List<OpenOutputCacheItem> GetCacheItems()
        {
            List<OpenOutputCacheItem> items = new List<OpenOutputCacheItem>();
            var Files = Directory.GetFiles(GetCacheFolder(), String.Format("*_*{1}", DataFileExtension));
            foreach (var dateFileName in Files)
            {
                //LastModified = Null.NullDate;
                string attribFileName = dateFileName.Replace(DataFileExtension, AttribFileExtension);
                if (File.Exists(attribFileName) && File.Exists(dateFileName) /*&& !IsFileExpired(attribFileName)*/ )
                {
                    DateTime LastModified = File.GetLastWriteTimeUtc(dateFileName);
                    string[] lines = File.ReadAllLines(attribFileName);
                    DateTime expire = DateTime.Parse(lines[0], CultureInfo.InvariantCulture);
                    string RawUrl = lines[2];
                    items.Add(new OpenOutputCacheItem()
                    {
                        CacheKey = Path.GetFileName(dateFileName).Replace(DataFileExtension, ""),
                        Expire = expire,
                        Modified = LastModified,
                        Url = RawUrl,
                        RawCacheKey = lines[5],
                        Tabid = int.Parse(lines[6]),
                        AbsoluteUri = lines[7]
                    });
                }
            }
            return items;
        }
    }
}
