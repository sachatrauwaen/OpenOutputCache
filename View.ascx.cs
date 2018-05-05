/*
' Copyright (c) 2012  DotNetNuke Corporation
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Services.Localization;
using DotNetNuke.Security;
using DotNetNuke.Entities.Tabs;
using System.Collections.Generic;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Common;
using DotNetNuke.Security.Permissions;
using System.Collections;
using System.Linq;
using DotNetNuke.Services.OutputCache;
using Satrabel.Providers.OutputCachingProviders;
using DotNetNuke.Entities.Portals;
using System.Web.UI.WebControls;
using System.Threading;

namespace Satrabel.OpenOutputCache
{

    public partial class View : DotNetNuke.Entities.Modules.PortalModuleBase, IActionable
    {

        #region Event Handlers

        override protected void OnInit(EventArgs e)
        {
            InitializeComponent();
            base.OnInit(e);
        }

        private void InitializeComponent()
        {
            this.Load += new System.EventHandler(this.Page_Load);
        }

        private void Page_Load(object sender, System.EventArgs e)
        {
            try
            {
                var editUrl = ModuleContext.EditUrl();
                if (ModuleContext.PortalSettings.EnablePopUps)
                {
                    editUrl = UrlUtils.PopUpUrl(editUrl, this, ModuleContext.PortalSettings, true, false);
                    lbEdit.Attributes.Add("onclick", "return " + editUrl);
                }
                else
                {
                    lbEdit.Click += delegate(object sen, EventArgs ev)
                    {
                        Response.Redirect(editUrl, true);
                    };

                }

                if (!Page.IsPostBack)
                {
                    ShowUrls();
                    //string CacheMode = PortalController.GetPortalSetting("OOC_CacheMode", PortalId, "ServerCache");
                    int DefaultCacheDuration = PortalController.GetPortalSettingAsInteger("OOC_CacheDuration", PortalId, 60);
                    string DefaultIncludeVaryBy = PortalController.GetPortalSetting("OOC_IncludeVaryBy", PortalId, "");
                    string DefaultExcludeVaryBy = PortalController.GetPortalSetting("OOC_ExcludeVaryBy", PortalId, "returnurl");
                    int DefaultMaxVaryByCount = PortalController.GetPortalSettingAsInteger("OOC_MaxVaryByCount", PortalId, 0);
                    bool VaryByBrowser = PortalController.GetPortalSettingAsBoolean("OOC_VaryByBrowser", PortalId, false);


                    lDefault.Text = "Default Cache Duration : " + DefaultCacheDuration + " - " +
                                    (string.IsNullOrEmpty(DefaultIncludeVaryBy) ? "" : "Default Include Parameters : " + DefaultIncludeVaryBy + " - ") +
                                    (VaryByBrowser ? " + Browser - " : "") +
                                    (string.IsNullOrEmpty(DefaultExcludeVaryBy) ? "" : "Default Exclude Parameters: " + DefaultExcludeVaryBy + " - ") +
                                    (DefaultMaxVaryByCount == 0 ? "" : "Default Max Vary By Count : " + DefaultMaxVaryByCount + " - ");



                    ddlProvider.DataSource = OutputCachingProvider.GetProviderList();
                    ddlProvider.DataBind();
                    ddlProvider.Items.Insert(0, new ListItem(Localization.GetString("Remove_Provider", LocalResourceFile), "Remove_Provider"));
                    ddlProvider.Items.Insert(0, new ListItem(Localization.GetString("None_Specified"), "None_Specified"));

                    if (ddlDefault.Items.Count == 2)
                        ddlDefault.Items.Insert(0, new ListItem(Localization.GetString("None_Specified"), ""));
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

        private void ShowUrls()
        {
            var TabLst = new List<PageInfo>();
            var tc = new TabController();
            foreach (TabInfo tab in TabController.GetTabsBySortOrder(PortalId, Thread.CurrentThread.CurrentCulture.Name, true))
            {
                if (!tab.IsDeleted
                    && tab.TabID != PortalSettings.AdminTabId && tab.ParentId != PortalSettings.AdminTabId
                    && tab.TabID != PortalSettings.SearchTabId
                    && tab.TabID != PortalSettings.UserTabId && tab.ParentId != PortalSettings.UserTabId
                    /* && 
                    */ )
                {
                    Hashtable tabSettings = tc.GetTabSettings(tab.TabID);
                    var pi = new PageInfo()
                    {
                        TabId = tab.TabID,
                        TabName = tab.IndentedTabName,
                        Provider = GetTabSettingAsString(tabSettings, "CacheProvider"),
                    };
                    if (!string.IsNullOrEmpty(pi.Provider))
                    {
                        pi.Duration = GetTabSettingAsString(tabSettings, "CacheDuration");

                        try
                        {
                            TimeSpan t = TimeSpan.FromSeconds(int.Parse(pi.Duration));
                            string dur = "";
                            if (t.Days > 0)
                                dur += t.Days + " days ";
                            if (t.Hours > 0)
                                dur += t.Hours + " hours ";

                            if (t.Minutes > 0)
                                dur += t.Minutes + " min ";

                            if (t.Seconds > 0)
                                dur += t.Seconds + " sec ";

                            pi.Duration = dur;
                        }
                        catch { }
                        pi.MaxVaryByCount = GetTabSettingAsString(tabSettings, "MaxVaryByCount");
                        pi.Exclude = GetTabSettingAsInteger(tabSettings, "CacheIncludeExclude", 0) == 0 ? "Exlude" : "Include";
                        pi.VaryBy = GetTabSettingAsInteger(tabSettings, "CacheIncludeExclude", 0) == 0 ? GetTabSettingAsString(tabSettings, "IncludeVaryBy") : GetTabSettingAsString(tabSettings, "ExcludeVaryBy");

                        var provider = OutputCachingProvider.Instance(pi.Provider);
                        if (provider != null)
                        {
                            pi.CachedItemCount = provider.GetItemCount(pi.TabId);
                        }
                    }
                    pi.Public = !tab.DisableLink && tab.TabType == TabType.Normal &&
                        /*(Null.IsNull(tab.StartDate) || tab.StartDate < DateTime.Now) &&
                        (Null.IsNull(tab.EndDate) || tab.EndDate > DateTime.Now) && */
                                IsTabPublic(tab.TabPermissions);


                    TabLst.Add(pi);
                    for (int i = 0; i < tab.Level; i++)
                    {
                        //pi.TabName = ".." + pi.TabName;
                    }
                }
            }


            gvPages.DataSource = TabLst;
            gvPages.DataBind();
        }

        public static bool IsTabPublic(TabPermissionCollection objTabPermissions)
        {
            string roles = objTabPermissions.ToString("VIEW");
            bool hasPublicRole = false;


            if ((roles != null))
            {
                // permissions strings are encoded with Deny permissions at the beginning and Grant permissions at the end for optimal performance
                foreach (string role in roles.Split(new[] { ';' }))
                {
                    if (!string.IsNullOrEmpty(role))
                    {
                        // Deny permission
                        if (role.StartsWith("!"))
                        {
                            string denyRole = role.Replace("!", "");
                            if ((denyRole == Globals.glbRoleUnauthUserName || denyRole == Globals.glbRoleAllUsersName))
                            {
                                hasPublicRole = false;
                                break;
                            }
                            // Grant permission
                        }
                        else
                        {
                            if ((role == Globals.glbRoleUnauthUserName || role == Globals.glbRoleAllUsersName))
                            {
                                hasPublicRole = true;
                                break;
                            }
                        }
                    }
                }
            }

            return hasPublicRole;
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
                //DnnLog.Error(exc);
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
                //DnnLog.Error(exc);
            }
            return retValue;
        }



        #region Optional Interfaces

        public ModuleActionCollection ModuleActions
        {
            get
            {
                ModuleActionCollection Actions = new ModuleActionCollection();
                Actions.Add(GetNextActionID(), Localization.GetString("EditModule", this.LocalResourceFile), "", "", "", EditUrl(), false, SecurityAccessLevel.Edit, true, false);
                return Actions;
            }
        }

        #endregion

        protected void gvPages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int SelectedTabId = (int)gvPages.SelectedDataKey.Value;
            var tc = new TabController();
            var moduleCtl = new ModuleController();

            //var tab = tc.GetTab(SelectedTabId, PortalId, false);
            gvModules.DataSource = moduleCtl.GetTabModules(SelectedTabId).Values;
            gvModules.DataBind();


            string ProviderName = gvPages.Rows[gvPages.SelectedIndex].Cells[2].Text;

            var Provider = OutputCachingProvider.Instance(ProviderName) as OpenOutputCachingProvider;
            if (Provider != null)
            {
                var items = Provider.GetCacheItems(SelectedTabId);
                gvCache.DataSource = items;
                gvCache.DataBind();
            }

        }

        protected void lbClearCache_Click(object sender, EventArgs e)
        {
            foreach (var prov in OutputCachingProvider.GetProviderList())
            {
                prov.Value.PurgeCache(PortalId);
            }
            ShowUrls();
        }

        protected void bUpdate_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gvPages.Rows.Count; i++)
            {
                var row = gvPages.Rows[i];
                CheckBox cbSelected = (CheckBox)row.FindControl("cbSelected");
                if (cbSelected != null && cbSelected.Visible && cbSelected.Checked)
                {
                    var tc = new TabController();
                    int TabId = int.Parse(cbSelected.ToolTip);
                    if (ddlProvider.SelectedIndex == 1)
                        tc.UpdateTabSetting(TabId, "CacheProvider", "");
                    else if (ddlProvider.SelectedIndex > 1)
                        tc.UpdateTabSetting(TabId, "CacheProvider", ddlProvider.SelectedValue);

                    if (tbDuration.Text != "")
                        tc.UpdateTabSetting(TabId, "CacheDuration", (tbDuration.Text == "0" ? "" : tbDuration.Text));

                    if (tbMaxCount.Text != "")
                        tc.UpdateTabSetting(TabId, "MaxVaryByCount", (tbMaxCount.Text == "0" ? "" : tbMaxCount.Text));

                    if (ddlDefault.SelectedIndex > 0)
                        tc.UpdateTabSetting(TabId, "CacheIncludeExclude", ddlDefault.SelectedValue);


                    if (tbExcept.Text != "")
                    {
                        Hashtable tabSettings = tc.GetTabSettings(TabId);

                        string Except = tbExcept.Text == "0" ? "" : tbExcept.Text;

                        if (GetTabSettingAsInteger(tabSettings, "CacheIncludeExclude", 0) == 0)
                        {
                            tc.UpdateTabSetting(TabId, "IncludeVaryBy", Except);
                        }
                        else
                        {
                            tc.UpdateTabSetting(TabId, "ExcludeVaryBy", Except);
                        }
                    }
                }
            }
            ShowUrls();
        }

        protected void lbAll_Click(object sender, EventArgs e)
        {
            foreach (GridViewRow row in gvPages.Rows)
            {
                CheckBox cbSelected = (CheckBox)row.FindControl("cbSelected");
                if (cbSelected != null)
                {
                    cbSelected.Checked = true;
                }
            }
        }




    }

    public class PageInfo
    {
        public int TabId { get; set; }
        public string TabName { get; set; }
        public string Provider { get; set; }
        public string Duration { get; set; }
        public string MaxVaryByCount { get; set; }
        public string Exclude { get; set; }
        public string VaryBy { get; set; }
        public int? CachedItemCount { get; set; }
        public bool Public { get; set; }

        public PageInfo()
        {

        }
    }

}
