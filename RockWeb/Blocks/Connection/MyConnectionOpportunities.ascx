﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MyConnectionOpportunities.ascx.cs" Inherits="RockWeb.Blocks.Connection.MyConnectionOpportunities" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lTypeIcon" runat="server" />
                    My Connection Opportunities</h1>

                <div class="pull-right">
                    <asp:LinkButton ID="lbConnectionTypes" runat="server" CssClass=" pull-right" OnClick="lbConnectionTypes_Click" CausesValidation="false"><i class="fa fa-gear"></i></asp:LinkButton>
                </div>

            </div>
            <div class="panel-body">

                <div class="list-as-blocks margin-t-lg clearfix">
                    <ul class="list-unstyled">
                        <asp:Repeater ID="rptConnectionOpportunities" runat="server">
                            <ItemTemplate>
                                <li class='<%# Eval("Class") %>'>
                                    <asp:LinkButton ID="lbConnectionOpportunity" runat="server" CommandArgument='<%# Eval("ConnectionOpportunity.Id") %>' CommandName="Display">
                                        <i class='<%# Eval("ConnectionOpportunity.IconCssClass") %>'></i>
                                        <h3><%# Eval("ConnectionOpportunity.Name") %> </h3>
                                        <div class="notification">
                                            <span class="label label-danger"><%# ((int)Eval("Count")).ToString("#,###,###") %></span>
                                        </div>
                                    </asp:LinkButton>
                                </li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </div>

                <h4>
            </div>
        </div>
        <asp:Panel ID="pnlGrid" runat="server" CssClass="panel panel-block" Visible="false">
            <div class="panel-heading">
                <asp:Literal ID="lOpportunityIcon" runat="server" />

                <asp:Literal ID="lConnectionRequest" runat="server"></asp:Literal></h4>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="rFilter" runat="server" OnDisplayFilterValue="rFilter_DisplayFilterValue">
                        <Rock:PersonPicker ID="ppRequester" runat="server" Label="Requester" />
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                        <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                        <Rock:PersonPicker ID="ppConnector" runat="server" Label="Connector" />
                        <Rock:RockCheckBoxList ID="cblState" runat="server" Label="State" RepeatDirection="Horizontal" />
                        <Rock:RockCheckBoxList ID="cblStatus" runat="server" Label="Status" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                        <Rock:RockCheckBoxList ID="cblCampus" runat="server" Label="Campus" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                    </Rock:GridFilter>
                    <Rock:Grid ID="gConnectionRequests" runat="server" OnRowSelected="gConnectionRequests_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:RockBoundField DataField="Group" HeaderText="Group" SortExpression="Group" />
                            <Rock:RockBoundField DataField="Status" HeaderText="Status" />
                            <Rock:RockBoundField DataField="Connector" HeaderText="Connector" />
                            <Rock:RockBoundField DataField="Activities" HeaderText="Activities" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="State" HeaderText="State" HtmlEncode="false" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
        <script>
            $(".my-workflows .list-as-blocks li").on("click", function () {
                $(".my-workflows .list-as-blocks li").removeClass('active');
                $(this).addClass('active');
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
