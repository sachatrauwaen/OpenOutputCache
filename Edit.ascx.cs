/*
' Copyright (c) 2013 Satrabel
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
using DotNetNuke.Entities.Portals;
using DotNetNuke.Common;

namespace Satrabel.OpenOutputCache
{

   
    public partial class Edit : OpenOutputCacheModuleBase
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
            lbSave.Click += new System.EventHandler(this.lbSave_Click);
        }

      
        private void Page_Load(object sender, System.EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    string CacheMode = PortalController.GetPortalSetting("OOC_CacheMode", PortalId, "ServerCache");
                    rblMode.SelectedValue = CacheMode;
                    string DefaultIncludeVaryBy = PortalController.GetPortalSetting("OOC_IncludeVaryBy", PortalId, "");
                    tbDefaultInclude.Text = DefaultIncludeVaryBy;
                    string DefaultExcludeVaryBy = PortalController.GetPortalSetting("OOC_ExcludeVaryBy", PortalId, "returnurl");
                    tbDefaultExclude.Text = DefaultExcludeVaryBy;
                    int DefaultCacheDuration = PortalController.GetPortalSettingAsInteger("OOC_CacheDuration", PortalId, 60);
                    tbDuration.Text = DefaultCacheDuration.ToString();

                    int DefaultMaxVaryByCount = PortalController.GetPortalSettingAsInteger("OOC_MaxVaryByCount", PortalId, 0);
                    tbMaxVaryBy.Text = DefaultMaxVaryByCount.ToString();


                    bool VaryByBrowser = PortalController.GetPortalSettingAsBoolean("OOC_VaryByBrowser", PortalId, false);
                    cbVaryByBrowser.Checked = VaryByBrowser;

                    int ExpireDelay = PortalController.GetPortalSettingAsInteger("OOC_ExpireDelay", PortalId, 60);
                    tbExpire.Text = ExpireDelay.ToString();

                    hlCancel.NavigateUrl = Globals.NavigateURL();
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

        protected void lbSave_Click(object sender, EventArgs e)
        {
            PortalController.UpdatePortalSetting(PortalId, "OOC_CacheMode", rblMode.SelectedValue);
            PortalController.UpdatePortalSetting(PortalId, "OOC_IncludeVaryBy", tbDefaultInclude.Text);
            PortalController.UpdatePortalSetting(PortalId, "OOC_ExcludeVaryBy", tbDefaultExclude.Text);
            PortalController.UpdatePortalSetting(PortalId, "OOC_CacheDuration", tbDuration.Text);
            PortalController.UpdatePortalSetting(PortalId, "OOC_MaxVaryByCount", tbMaxVaryBy.Text);
            PortalController.UpdatePortalSetting(PortalId, "OOC_VaryByBrowser", cbVaryByBrowser.Checked.ToString());
            PortalController.UpdatePortalSetting(PortalId, "OOC_ExpireDelay", tbExpire.Text);
            Response.Redirect(Globals.NavigateURL(), true);
        }

    }

}