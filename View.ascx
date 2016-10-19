<%@ Control Language="C#" Inherits="Satrabel.OpenOutputCache.View" AutoEventWireup="true" CodeBehind="View.ascx.cs" %>
<%@ Register TagPrefix="dnn" Assembly="DotNetNuke" Namespace="DotNetNuke.UI.WebControls" %>


<asp:Label ID="lDefault" runat="server"></asp:Label>&nbsp;<asp:LinkButton ID="lbEdit"
    runat="server">Default Cache Settings</asp:LinkButton>&nbsp;-&nbsp;<asp:LinkButton ID="lbClearCache"
        runat="server" OnClick="lbClearCache_Click">Clear all output cache items</asp:LinkButton>

<hr />
<table style="width: 100%;">
    <tr>
        <td>
            <asp:Label ID="Label1" runat="server" Text="Provider"></asp:Label>
        </td>
        <td>
            <asp:Label ID="Label2" runat="server" Text="Duration (sec)"></asp:Label>
        </td>
        <td>
            <asp:Label ID="Label3" runat="server" Text="Default"></asp:Label>
        </td>
        <td>
            <asp:Label ID="Label4" runat="server" Text="Except"></asp:Label>
        </td>
        <td>
            <asp:Label ID="Label5" runat="server" Text="Max count"></asp:Label>
        </td>
        <td>
            <asp:Label ID="Label6" runat="server" Text="(0 to remove)"></asp:Label>
        </td>
    </tr>
    <tr>
        <td>
            <asp:DropDownList ID="ddlProvider" runat="server" DataValueField="Key"
                DataTextField="Key">
                <asp:ListItem>&lt;None&gt;</asp:ListItem>
            </asp:DropDownList>
        </td>
        <td>
            <asp:TextBox ID="tbDuration" runat="server" Columns="5"></asp:TextBox>
        </td>
        <td>
            <asp:DropDownList ID="ddlDefault" runat="server">

                <asp:ListItem Value="0">Exclude</asp:ListItem>
                <asp:ListItem Value="1">Include</asp:ListItem>
            </asp:DropDownList>
        </td>
        <td>
            <asp:TextBox ID="tbExcept" runat="server"></asp:TextBox>
        </td>
        <td>
            <asp:TextBox ID="tbMaxCount" runat="server" Columns="5"></asp:TextBox>
        </td>
        <td>
            <asp:Button ID="bUpdate" runat="server" Text="Update" OnClick="bUpdate_Click" />
        </td>
    </tr>

</table>
<hr />

<div class="dnnForm OpenOutputCacheForm" style="width: auto;">
    <div style="height: 200px; overflow: auto; margin-bottom: 20px;">
        <asp:GridView ID="gvPages" runat="server" BorderStyle="None" GridLines="None"
            CssClass="dnnGrid" Width="98%"
            AutoGenerateColumns="False" DataKeyNames="TabId"
            OnSelectedIndexChanged="gvPages_SelectedIndexChanged">
            <HeaderStyle HorizontalAlign="Left" />
            <SelectedRowStyle BackColor="#eeeeee" />
            <Columns>
                <asp:TemplateField>
                    <HeaderTemplate>
                        <asp:LinkButton ID="lbAll" runat="server" OnClick="lbAll_Click">All</asp:LinkButton>
                        / 
                    <asp:LinkButton ID="lbNone" runat="server">None</asp:LinkButton>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:CheckBox ID="cbSelected" runat="server" Visible='<%# Eval("Public") %>' ToolTip='<%# Eval("TabId") %>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Page">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="False"
                            CommandName="Select" Text='<%# Eval("TabName") %>'></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>

                <asp:BoundField DataField="Provider" HeaderText="Provider" />
                <asp:BoundField DataField="Duration" HeaderText="Duration" />
                <asp:BoundField DataField="Exclude" HeaderText="Default" />
                <asp:BoundField DataField="VaryBy" HeaderText="Except" />
                <asp:BoundField DataField="MaxVaryByCount" HeaderText="Max Count" />
                <asp:BoundField DataField="CachedItemCount" HeaderText="Count" />



            </Columns>
            <EmptyDataTemplate>
                No tabs
            </EmptyDataTemplate>
        </asp:GridView>
    </div>
    <div id="tabs-demo">

        <ul class="dnnAdminTabNav">
            <li><a href="#Cache">Cache Items</a></li>
            <li><a href="#Modules">Modules</a></li>
        </ul>

        <div id="Cache" class="dnnClear">
            <div style="max-height: 300px; overflow: auto;">
                <asp:GridView ID="gvCache" runat="server" BorderStyle="None" GridLines="None"
                    CssClass="dnnGrid" EnableViewState="False" Width="98%" AutoGenerateColumns="false">
                    <HeaderStyle HorizontalAlign="Left" />
                    <Columns>
                        <asp:BoundField DataField="TabId" HeaderText="TabId" Visible="false" />
                        <asp:BoundField DataField="CacheKey" HeaderText="CacheKey" />
                        <asp:BoundField DataField="RawCacheKey" HeaderText="RawCacheKey" />
                        <asp:BoundField DataField="Expire" HeaderText="Expire" />
                        <asp:BoundField DataField="Modified" HeaderText="Last Modified" />
                        <asp:BoundField DataField="Url" HeaderText="Raw Url" />


                    </Columns>
                    <EmptyDataTemplate>
                        No cache items
                    </EmptyDataTemplate>
                </asp:GridView>
            </div>
        </div>

        <div id="Modules" class="dnnClear">
            <asp:GridView ID="gvModules" runat="server" BorderStyle="None" GridLines="None"
                CssClass="dnnGrid" EnableViewState="False" AutoGenerateColumns="false">
                <HeaderStyle HorizontalAlign="Left" />
                <Columns>
                    <asp:BoundField DataField="ModuleTitle" HeaderText="Title" />
                    <asp:BoundField DataField="FriendlyName" HeaderText="Module" />
                    <asp:BoundField DataField="CacheMethod" HeaderText="Cache Provider" />
                    <asp:BoundField DataField="CacheTime" HeaderText="Cache Time" />
                </Columns>

                <EmptyDataTemplate>
                    No errors & Warnings
                </EmptyDataTemplate>
            </asp:GridView>
        </div>
    </div>

</div>

<div class="clear"></div>






<script type="text/javascript">
    jQuery(function ($) {

        //refreshTab();

        $('#tabs-demo').dnnTabs();


    });

    function refreshTab() {

        var gridheight = 0;
        $("#OpenUrlRewriter .grids").each(function () {
            gridheight = Math.max(gridheight, $(this).height());
        });
        if (gridheight > 0) {
            $('#OpenUrlRewriter .grids').height(gridheight);
        }

        var gridheight = 0;
        $("#WebsiteAuditor .grids").each(function () {
            gridheight = Math.max(gridheight, $(this).height());
        });
        if (gridheight > 0) {
            $('#WebsiteAuditor .grids').height(gridheight);
        }
    }


</script>
