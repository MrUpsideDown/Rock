﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Reporting.DataSelect.Person
{
    /// <summary>
    ///     A Report Field that shows the list of Groups in which a Person is participating from a set of candidates defined by
    ///     a Group Data View.
    /// </summary>
    [Description( "Shows a summary of Groups in which a Person participates from a filtered subset of Groups defined by a Data View" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Group Participation" )]
    public class GroupParticipationSelect : DataSelectComponent
    {
        #region Properties

        public override string AppliesToEntityType
        {
            get { return typeof( Model.Person ).FullName; }
        }

        public override string Section
        {
            get { return "Summaries"; }
        }

        public override string ColumnPropertyName
        {
            get { return "Group Participation"; }
        }

        public override Type ColumnFieldType
        {
            get { return typeof( IEnumerable<string> ); }
        }

        public override DataControlField GetGridField( Type entityType, string selection )
        {
            return new ListDelimitedField();
        }

        public override string ColumnHeaderText
        {
            get { return "Group Participation"; }
        }

        #endregion

        #region Methods

        public override string GetTitle( Type entityType )
        {
            return "Group Participation";
        }

        public override Expression GetExpression( RockContext context, MemberExpression entityIdProperty, string selection )
        {
            var settings = new GroupParticipationSelectSettings( selection );

            //
            // Define Candidate Groups
            //
            bool useDefaultGroupsFilter = true;

            // Get the Group Data View that defines the set of candidates from which matching Groups can be selected.
            DataView dataView = null;

            if ( settings.DataViewGuid.HasValue )
            {
                var dsService = new DataViewService( context );

                dataView = dsService.Get( settings.DataViewGuid.Value );

                if ( dataView != null )
                {
                    if ( dataView.DataViewFilter == null )
                    {
                        dataView = null;
                    }
                    else
                    {
                        // Verify that the Group Data View does not contain any references to this Data View in any of its components.
                        if ( dsService.IsViewInFilter( dataView.Id, dataView.DataViewFilter ) )
                        {
                            throw new Exception( "Filter issue(s): One of the filters contains a circular reference to the Data View itself." );
                        }
                    }
                }
            }

            // Evaluate the Data View that defines the candidate Groups.
            var groupService = new GroupService( context );

            var groupQuery = groupService.Queryable();

            if ( dataView != null )
            {
                var paramExpression = groupService.ParameterExpression;

                List<string> errorMessages;

                var whereExpression = dataView.GetExpression( groupService, paramExpression, out errorMessages );

                if ( errorMessages.Any() )
                {
                    throw new Exception( "Filter issue(s): " + errorMessages.AsDelimited( "; " ) );
                }

                groupQuery = groupQuery.Where( paramExpression, whereExpression, null );

                useDefaultGroupsFilter = false;
            }

            if ( useDefaultGroupsFilter )
            {
                groupQuery = groupQuery.Where( x => x.GroupType.ShowInGroupList );
            }

            var groupKeys = groupQuery.Select( x => x.Id );

            //
            // Construct the Query to return the list of Group Members matching the filter conditions.
            //
            var groupMemberQuery = new GroupMemberService( context ).Queryable();

            // Filter By Group.
            groupMemberQuery = groupMemberQuery.Where( x => groupKeys.Contains( x.GroupId ) );

            // Filter By Group Role Type.
            switch ( settings.RoleType )
            {
                case RoleTypeSpecifier.Member:
                    groupMemberQuery = groupMemberQuery.Where( x => !x.GroupRole.IsLeader );
                    break;

                case RoleTypeSpecifier.Leader:
                    groupMemberQuery = groupMemberQuery.Where( x => x.GroupRole.IsLeader );
                    break;
            }

            // Filter by Group Member Status.
            if (settings.MemberStatus.HasValue)
            {
                groupMemberQuery = groupMemberQuery.Where(x => x.GroupMemberStatus == settings.MemberStatus.Value);
            }

            // Set the Output Format of the field.
            Expression<Func<GroupMember, string>> outputExpression;

            switch ( settings.ListFormat )
            {
                case ListFormatSpecifier.GroupOnly:
                    outputExpression = ( ( m => m.Group.Name ) );
                    break;
                case ListFormatSpecifier.GroupAndRole:
                default:
                    outputExpression = ( ( m => m.Group.Name + " [" + m.GroupRole.Name + "]" ) );
                    break;
            }

            // Define the Select Expression containing the field output.
            var personGroupsQuery = new PersonService( context ).Queryable()
                                                                .Select( p => groupMemberQuery.Where( s => s.PersonId == p.Id )
                                                                                              .OrderBy( x => x.Group.Name )
                                                                                              .ThenBy( x => x.GroupRole.Name )
                                                                                              .Select( outputExpression ).AsEnumerable() );

            var selectExpression = SelectExpressionExtractor.Extract<Model.Person>( personGroupsQuery, entityIdProperty, "p" );

            return selectExpression;
        }

        private const string _CtlFormat = "ddlFormat";
        private const string _CtlDataView = "ddlDataView";
        private const string _CtlRoleType = "ddlRoleType";
        private const string _CtlGroupStatus = "ddlGroupStatus";

        public override Control[] CreateChildControls( Control parentControl )
        {
            // Define Control: Output Format DropDown List
            var ddlFormat = new RockDropDownList();
            ddlFormat.ID = GetControlInstanceName(parentControl, _CtlFormat);
            ddlFormat.Label = "Output Format";
            ddlFormat.Help = "Specifies the content and format of the items in this field.";
            ddlFormat.Items.Add( new ListItem( "Group Name And Role", ListFormatSpecifier.GroupAndRole.ToString() ) );
            ddlFormat.Items.Add( new ListItem( "Group Name", ListFormatSpecifier.GroupOnly.ToString() ) );
            parentControl.Controls.Add( ddlFormat );

            // Define Control: Group Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = GetControlInstanceName( parentControl, _CtlDataView );
            ddlDataView.Label = "Participates in Groups";
            ddlDataView.Help = "A Data View that filters the Groups included in the result. If no value is selected, any Groups that would be visible in a Group List will be included.";

            parentControl.Controls.Add( ddlDataView );

            // Define Control: Role Type DropDown List
            var ddlRoleType = new RockDropDownList();
            ddlRoleType.ID = GetControlInstanceName( parentControl, _CtlRoleType );
            ddlRoleType.Label = "with Group Role Type";
            ddlRoleType.Help = "Specifies the type of Group Role the Member must have to be included in the result. If no value is selected, Members in every Role will be shown.";
            ddlRoleType.Items.Add( new ListItem( string.Empty, RoleTypeSpecifier.Any.ToString() ) );
            ddlRoleType.Items.Add( new ListItem( "Leader", RoleTypeSpecifier.Leader.ToString() ) );
            ddlRoleType.Items.Add( new ListItem( "Member", RoleTypeSpecifier.Member.ToString() ) );
            parentControl.Controls.Add( ddlRoleType );

            // Define Control: Group Member Status DropDown List
            var ddlGroupMemberStatus = new RockDropDownList();
            ddlGroupMemberStatus.CssClass = "js-group-member-status";
            ddlGroupMemberStatus.ID = GetControlInstanceName( parentControl, _CtlGroupStatus );
            ddlGroupMemberStatus.Label = "with Group Member Status";
            ddlGroupMemberStatus.Help = "Specifies the Status the Member must have to be included in the result. If no value is selected, Members of every Group Status will be shown.";
            ddlGroupMemberStatus.BindToEnum<GroupMemberStatus>( true );
            ddlGroupMemberStatus.SetValue( GroupMemberStatus.Active.ConvertToInt() );
            parentControl.Controls.Add( ddlGroupMemberStatus );

            // Populate the Data View Picker
            int entityTypeId = EntityTypeCache.Read( typeof( Model.Group ) ).Id;
            ddlDataView.EntityTypeId = entityTypeId;

            return new Control[] { ddlDataView, ddlRoleType, ddlFormat, ddlGroupMemberStatus };
        }

        private string GetControlInstanceName(Control parentControl, string controlBaseName)
        {
            return string.Format("{0}_{1}", parentControl.ID, controlBaseName);
        }

        private TControl GetControlByName<TControl>(Control[] controls, string controlName)
            where TControl : class
        {
            object control = controls.FirstOrDefault(x => x.ID.EndsWith("_" + controlName));

            if (control == null)
            {
                throw new Exception(string.Format("Control \"{0}\" could not be found.", controlName));
            }

            control = control as TControl;

            if ( control == null )
            {
                throw new Exception( "Control \"{0}\" could not be found." );
            }

            return (TControl)control;
        }

        public override string GetSelection( Control[] controls )
        {           
            var ddlDataView = GetControlByName<DataViewPicker>( controls, _CtlDataView );
            var ddlRoleType = GetControlByName<RockDropDownList>( controls, _CtlRoleType );
            var ddlFormat = GetControlByName<RockDropDownList>( controls, _CtlFormat );
            var ddlGroupMemberStatus = GetControlByName<RockDropDownList>( controls, _CtlGroupStatus );

            var settings = new GroupParticipationSelectSettings();

            settings.ParseMemberStatus( ddlGroupMemberStatus.SelectedValue );
            settings.ParseRoleType( ddlRoleType.SelectedValue );
            settings.ParseDataViewId(ddlDataView.SelectedValue );
            settings.ParseListFormatType( ddlFormat.SelectedValue );
                        
            return settings.ToSelectionString();
        }

        public override void SetSelection( Control[] controls, string selection )
        {
            var ddlDataView = GetControlByName<DataViewPicker>( controls, _CtlDataView );
            var ddlRoleType = GetControlByName<RockDropDownList>( controls, _CtlRoleType );
            var ddlFormat = GetControlByName<RockDropDownList>( controls, _CtlFormat );
            var ddlGroupMemberStatus = GetControlByName<RockDropDownList>( controls, _CtlGroupStatus );

            var settings = new GroupParticipationSelectSettings( selection );

            if ( !settings.IsValid() )
            {
                return;
            }

            ddlFormat.SelectedValue = settings.ListFormat.ToString();

            if ( settings.DataViewGuid.HasValue )
            {
                var dsService = new DataViewService( new RockContext() );

                var dataView = dsService.Get( settings.DataViewGuid.Value );

                if ( dataView != null )
                {
                    ddlDataView.SelectedValue = dataView.Id.ToString();
                }
            }

            ddlRoleType.SelectedValue = settings.RoleType.ToStringSafe();
            ddlGroupMemberStatus.SelectedValue = settings.MemberStatus.ToStringSafe();            
        }

        #endregion

        #region Settings

        private enum RoleTypeSpecifier
        {
            Any = 0,
            Leader = 1,
            Member = 2
        }

        private enum ListFormatSpecifier
        {
            GroupAndRole = 0,
            GroupOnly = 1
        }

        /// <summary>
        ///     Settings for the Data Select Component "Group Participation".
        /// </summary>
        private class GroupParticipationSelectSettings
        {
            public const string SettingsVersion = "1";
            public Guid? DataViewGuid;
            public ListFormatSpecifier ListFormat = ListFormatSpecifier.GroupAndRole;
            public GroupMemberStatus? MemberStatus = GroupMemberStatus.Active;
            public RoleTypeSpecifier? RoleType;

            public GroupParticipationSelectSettings()
            {
                //
            }

            public GroupParticipationSelectSettings( string settingsString )
            {
                FromSelectionString( settingsString );
            }

            public bool IsValid()
            {
                return true;
            }

            /// <summary>
            ///     Set values from a string representation of the settings.
            /// </summary>
            /// <param name="selectionString"></param>
            public void FromSelectionString( string selectionString )
            {
                var selectionValues = selectionString.Split( '|' );

                // Read the settings string version from the first parameter.
                // This allows us to cater for any future upgrades to the content and format of the settings string.
                string version = selectionValues.ElementAtOrDefault( 0 );

                if ( version == SettingsVersion )
                {
                    // Parameter 1: Output Format
                    ParseListFormatType( selectionValues.ElementAtOrDefault( 1 ) );

                    // Parameter 2: Data View
                    DataViewGuid = selectionValues.ElementAtOrDefault( 2 ).AsGuidOrNull();

                    // Parameter 3: Role Type
                    ParseRoleType( selectionValues.ElementAtOrDefault( 3 ) );

                    // Parameter 4: Group Member Status
                    ParseMemberStatus( selectionValues.ElementAtOrDefault( 4 ) );
                }
            }

            public void ParseListFormatType( string listFormatName )
            {
                ListFormat = ParseEnum( listFormatName, ListFormatSpecifier.GroupAndRole );
            }

            public void ParseRoleType( string roleTypeName )
            {
                RoleType = ParseEnum<RoleTypeSpecifier>( roleTypeName );
            }

            public void ParseMemberStatus( string memberStatusName )
            {
                MemberStatus = ParseEnum<GroupMemberStatus>( memberStatusName );
            }

            public void ParseDataViewId( string dataViewId )
            {
                var id = dataViewId.AsIntegerOrNull();

                if ( id != null )
                {
                    var dsService = new DataViewService( new RockContext() );

                    var dataView = dsService.Get( id.Value );

                    DataViewGuid = dataView.Guid;
                }
                else
                {
                    DataViewGuid = null;
                }
            }

            public string ToSelectionString()
            {
                var settings = new List<string>();

                settings.Add( SettingsVersion );
                settings.Add( ( (int)ListFormat ).ToString() );
                settings.Add( DataViewGuid.ToStringSafe() );
                settings.Add( RoleType == null ? string.Empty : ( (int)RoleType ).ToString() );
                settings.Add( MemberStatus == null ? string.Empty : ( (int)MemberStatus ).ToString() );

                return settings.AsDelimited( "|" );
            }

            private TEnum ParseEnum<TEnum>( string value, TEnum defaultValue, bool ignoreCase = true )
                where TEnum : struct, IComparable, IFormattable, IConvertible
            {
                TEnum? parsedValue = ParseEnum<TEnum>( value, ignoreCase );

                return parsedValue.GetValueOrDefault( defaultValue );
            }

            private TEnum? ParseEnum<TEnum>( string value, bool ignoreCase = true )
                    where TEnum : struct, IComparable, IFormattable, IConvertible
            {
                if ( !typeof( TEnum ).IsEnum )
                {
                    throw new ArgumentException( "Target is not an Enumerated Type" );
                }

                if ( string.IsNullOrEmpty( value ) )
                {
                    return null;
                }

                TEnum lResult;

                if ( Enum.TryParse( value, ignoreCase, out lResult ) )
                {
                    return lResult;
                }

                return null;
            }
        }

        #endregion
    }
}