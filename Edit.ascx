<%@ Control language="C#" Inherits="Satrabel.OpenOutputCache.Edit" AutoEventWireup="false"  Codebehind="Edit.ascx.cs" %>
<%@ Register TagName="label" TagPrefix="dnn" Src="~/controls/labelcontrol.ascx" %>
<div class="dnnForm" id="form-demo" style="padding-top:20px">
    
    <asp:Label ID="Label5" runat="server" CssClass="dnnFormMessage dnnFormInfo" ResourceKey="Intro" />
    <div class="dnnFormItem dnnFormHelp dnnClear">
        <p class="dnnFormRequired"><asp:Label ID="Label6" runat="server" ResourceKey="Required Indicator" /></p>
    </div>
    <fieldset>
    
         <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="rblMode" ResourceKey="Mode" />
             <asp:RadioButtonList ID="rblMode" runat="server">
                 <asp:ListItem Value="NoCache">No Cache</asp:ListItem>
                 <asp:ListItem Value="ServerCache" Selected="True">Server Cache</asp:ListItem>
                 <asp:ListItem Value="IfModified">Server Cache &amp; Client Cache with Server Check If Modified</asp:ListItem>
                 <asp:ListItem Value="ExpireDelay">Server Cache &amp; Client Cache Expire Delay</asp:ListItem>
             </asp:RadioButtonList>
        </div>
    
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbDefaultInclude" ResourceKey="DefaultInclude" />
            <asp:TextBox runat="server" ID="tbDefaultInclude"  />           
        </div>
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbDefaultExclude" ResourceKey="DefaultExclude" />
            <asp:TextBox runat="server" ID="tbDefaultExclude"  />           
        </div>
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbDuration" ResourceKey="Duration" />
            <asp:TextBox runat="server" ID="tbDuration"  />           
        </div>
        <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbMaxVaryBy" ResourceKey="MaxVaryBy" />
            <asp:TextBox runat="server" ID="tbMaxVaryBy"  />           
        </div>
        
         <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="cbVaryByBrowser" ResourceKey="VaryByBrowser" />
            <asp:CheckBox runat="server" ID="cbVaryByBrowser" />
        </div>
         <div class="dnnFormItem">
            <dnn:Label runat="server" ControlName="tbExpire" ResourceKey="ExpireDelay" />
            <asp:TextBox runat="server" ID="tbExpire"  />           
        </div>
    </fieldset>
    <ul class="dnnActions dnnClear">
        <li><asp:LinkButton ID="lbSave" runat="server" CssClass="dnnPrimaryAction" 
                ResourceKey="Save" onclick="lbSave_Click" /></li>
        <li><asp:HyperLink ID="hlCancel" runat="server" CssClass="dnnSecondaryAction" NavigateUrl="/" ResourceKey="Cancel" /></li>
    </ul>
</div>