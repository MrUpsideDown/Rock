﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CalendarLava.ascx.cs" Inherits="RockWeb.Blocks.Event.CalendarLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbConfiguration" NotificationBoxType="Danger" runat="server" Visible="false" />
        <div class="row">
            <asp:Panel ID="pnlFilters" CssClass="col-md-3" runat="server">
                <asp:Panel ID="pnlCalendar" CssClass="calendar" runat="server">
                    <asp:Calendar ID="calEventCalendar" runat="server" DayNameFormat="FirstLetter" SelectionMode="Day" BorderStyle="None"
                        TitleStyle-BackColor="#ffffff" NextPrevStyle-ForeColor="#333333" FirstDayOfWeek="Sunday" Width="100%" CssClass="calendar-month" OnSelectionChanged="calEventCalendar_SelectionChanged" OnDayRender="calEventCalendar_DayRender" OnVisibleMonthChanged="calEventCalendar_VisibleMonthChanged">
                        <DayStyle CssClass="calendar-day" />
                        <TodayDayStyle CssClass="calendar-today" />
                        <SelectedDayStyle CssClass="calendar-selected" BackColor="Transparent" />
                        <OtherMonthDayStyle CssClass="calendar-last-month" />
                        <DayHeaderStyle CssClass="calendar-day-header" />
                        <NextPrevStyle CssClass="calendar-next-prev" />
                        <TitleStyle CssClass="calendar-title" />
                    </asp:Calendar>
                </asp:Panel>

                <Rock:RockCheckBoxList ID="cblCampus" RepeatDirection="Vertical" runat="server" Label="Filter by Campus" DataTextField="Name" DataValueField="Id" OnSelectedIndexChanged="cblCampus_SelectedIndexChanged" />

                <Rock:RockCheckBoxList ID="cblCategory" RepeatDirection="Vertical" runat="server" Label="Filter by Category" DataTextField="Name" DataValueField="Id" OnSelectedIndexChanged="cblCategory_SelectedIndexChanged" />

                <Rock:DateRangePicker ID="drpDateRange" runat="server" Label="Select Range" />
            </asp:Panel>
            <asp:Panel ID="pnlList" CssClass="col-md-9" runat="server">

                <div class="btn-group" role="group">
                    <Rock:BootstrapButton ID="btnDay" runat="server" CssClass="btn btn-default" Text="Day" OnClick="btnDay_Click" />
                    <Rock:BootstrapButton ID="btnWeek" runat="server" CssClass="btn btn-default" Text="Week" OnClick="btnWeek_Click" />
                    <Rock:BootstrapButton ID="btnMonth" runat="server" CssClass="btn btn-default" Text="Month" OnClick="btnMonth_Click" />
                </div>

                <asp:Literal ID="lOutput" runat="server"></asp:Literal>

                <asp:Literal ID="lDebug" Visible="false" runat="server"></asp:Literal>

            </asp:Panel>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
