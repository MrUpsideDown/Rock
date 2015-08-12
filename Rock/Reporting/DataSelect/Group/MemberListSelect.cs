// <copyright>
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
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;

namespace Rock.Reporting.DataSelect.Group
{
    /// <summary>
    ///     A Report Field that shows the list of People participating in a Group from a set of candidates defined by
    ///     a Person Data View.
    /// </summary>
    [Description( "Shows a summary of People participating in a Group from a filtered subset of People defined by a Data View" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "People Participating" )]
    public class MemberListSelect : DataSelectComponent
    {
        #region Properties

        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof( Model.Group ).FullName; }
        }

        /// <summary>
        /// Gets the section that this will appear in in the Field Selector
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Members"; }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get { return "Member List"; }
        }

        /// <summary>
        /// Gets the type of the column field.
        /// </summary>
        /// <value>
        /// The type of the column field.
        /// </value>
        public override Type ColumnFieldType
        {
            get { return typeof( IEnumerable<string> ); }
        }

        /// <summary>
        /// Gets the grid field.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override DataControlField GetGridField( Type entityType, string selection )
        {
            return new ListDelimitedField();
        }

        /// <summary>
        /// Gets the default column header text.
        /// </summary>
        /// <value>
        /// The default column header text.
        /// </value>
        public override string ColumnHeaderText
        {
            get { return "Members"; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Member List";
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( RockContext context, MemberExpression entityIdProperty, string selection )
        {
            var settings = new SelectSettings( selection );

            // Get the Person Data View that defines the set of candidates from which matching Group Members can be selected.
            IQueryable<int> personKeys = null;

            var dataView = DataComponentSettingsHelper.GetDataViewForFilterComponent( settings.DataViewGuid, context );

            var personService = new PersonService( context );

            var personQuery = personService.Queryable();

            if (dataView != null)
            {
                personQuery = DataComponentSettingsHelper.FilterByDataView( personQuery, dataView, personService );

                personKeys = personQuery.Select( x => x.Id );
            }

            //
            // Construct the Query to return the list of Group Members matching the filter conditions.
            //
            var groupMemberQuery = new GroupMemberService( context ).Queryable();

            // Filter By People.
            if (personKeys != null)
            {
                groupMemberQuery = groupMemberQuery.Where( x => personKeys.Contains( x.PersonId ) );
            }

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
            if ( settings.MemberStatus.HasValue )
            {
                groupMemberQuery = groupMemberQuery.Where( x => x.GroupMemberStatus == settings.MemberStatus.Value );
            }

            //
            // Create a Select Expression to return the requested values.
            //

            // Set the Output Format of the field.
            Expression<Func<GroupMember, string>> outputExpression;

            switch ( settings.ListFormat )
            {
                case ListFormatSpecifier.FirstNameLastName:
                    outputExpression = ( ( m => m.Person.FirstName + " " + m.Person.LastName ) );
                    break;
                default: // ListFormatSpecifier.GroupAndRole:                    
                    outputExpression = ( ( m => m.Person.FirstName + " " + m.Person.LastName + " [" + m.GroupRole.Name + "]" ) );
                    break;
            }

            // Define a Query to return the collection of filtered People for each Group.
            var groupPeopleQuery = new GroupService( context ).Queryable()
                                                                .Select( g => groupMemberQuery.Where( s => s.GroupId == g.Id )
                                                                                              .OrderBy( x => x.Person.LastName )
                                                                                              .ThenBy( x => x.Person.FirstName )
                                                                                              .ThenBy( x => x.GroupRole.Name )
                                                                                              .Select( outputExpression ).AsEnumerable() );

            var selectExpression = SelectExpressionExtractor.Extract<Model.Person>( groupPeopleQuery, entityIdProperty, "g" );

            return selectExpression;
        }

        private const string _CtlFormat = "ddlFormat";
        private const string _CtlDataView = "ddlDataView";
        private const string _CtlRoleType = "ddlRoleType";
        private const string _CtlGroupStatus = "ddlGroupStatus";

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <param name="parentControl"></param>
        /// <returns></returns>
        public override Control[] CreateChildControls( Control parentControl )
        {
            // Define Control: Output Format DropDown List
            var ddlFormat = new RockDropDownList();
            ddlFormat.ID = parentControl.GetChildControlInstanceName( _CtlFormat );
            ddlFormat.Label = "Output Format";
            ddlFormat.Help = "Specifies the content and format of the items in this field.";
            ddlFormat.Items.Add( new ListItem( "First Name/Last Name/Role", ListFormatSpecifier.FirstNameLastNameRole.ToString() ) );
            ddlFormat.Items.Add( new ListItem( "First Name/Last Name", ListFormatSpecifier.FirstNameLastName.ToString() ) );           

            parentControl.Controls.Add( ddlFormat );

            // Define Control: Group Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = parentControl.GetChildControlInstanceName( _CtlDataView );
            ddlDataView.Label = "Exists in this Person Data View";
            ddlDataView.Help = "A Data View that filters the people included in the result. If no value is selected, all Group Members will be included.";
            parentControl.Controls.Add( ddlDataView );

            // Define Control: Role Type DropDown List
            var ddlRoleType = new RockDropDownList();
            ddlRoleType.ID = parentControl.GetChildControlInstanceName( _CtlRoleType );
            ddlRoleType.Label = "with Group Role Type";
            ddlRoleType.Help = "Specifies the type of Group Role the Member must have to be included in the result. If no value is selected, Members in every Role will be shown.";
            ddlRoleType.Items.Add( new ListItem( string.Empty, RoleTypeSpecifier.Any.ToString() ) );
            ddlRoleType.Items.Add( new ListItem( "Leader", RoleTypeSpecifier.Leader.ToString() ) );
            ddlRoleType.Items.Add( new ListItem( "Member", RoleTypeSpecifier.Member.ToString() ) );
            parentControl.Controls.Add( ddlRoleType );

            // Define Control: Group Member Status DropDown List
            var ddlGroupMemberStatus = new RockDropDownList();
            ddlGroupMemberStatus.CssClass = "js-group-member-status";
            ddlGroupMemberStatus.ID = parentControl.GetChildControlInstanceName( _CtlGroupStatus );
            ddlGroupMemberStatus.Label = "with Group Member Status";
            ddlGroupMemberStatus.Help = "Specifies the Status the Member must have to be included in the result. If no value is selected, Members of every Group Status will be shown.";
            ddlGroupMemberStatus.BindToEnum<GroupMemberStatus>( true );
            ddlGroupMemberStatus.SetValue( GroupMemberStatus.Active.ConvertToInt() );
            parentControl.Controls.Add( ddlGroupMemberStatus );

            // Populate the Data View Picker
            int entityTypeId = EntityTypeCache.Read( typeof( Model.Person ) ).Id;
            ddlDataView.EntityTypeId = entityTypeId;

            return new Control[] { ddlDataView, ddlRoleType, ddlFormat, ddlGroupMemberStatus };
        }

        /// <summary>
        /// Gets the selection.
        /// This is typically a string that contains the values selected with the Controls
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlRoleType = controls.GetByName<RockDropDownList>( _CtlRoleType );
            var ddlFormat = controls.GetByName<RockDropDownList>( _CtlFormat );
            var ddlGroupMemberStatus = controls.GetByName<RockDropDownList>( _CtlGroupStatus );

            var settings = new SelectSettings();

            settings.MemberStatus = ddlGroupMemberStatus.SelectedValue.ConvertToEnumOrNull<GroupMemberStatus>();
            settings.RoleType = ddlRoleType.SelectedValue.ConvertToEnumOrNull<RoleTypeSpecifier>();
            settings.DataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( ddlDataView.SelectedValue );
            settings.ListFormat = ddlFormat.SelectedValue.ConvertToEnum<ListFormatSpecifier>( ListFormatSpecifier.FirstNameLastNameRole );

            return settings.ToSelectionString();
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Control[] controls, string selection )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlRoleType = controls.GetByName<RockDropDownList>( _CtlRoleType );
            var ddlFormat = controls.GetByName<RockDropDownList>( _CtlFormat );
            var ddlGroupMemberStatus = controls.GetByName<RockDropDownList>( _CtlGroupStatus );

            var settings = new SelectSettings( selection );

            if ( !settings.IsValid )
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
            FirstNameLastNameRole = 0,
            FirstNameLastName = 1,
        }

        /// <summary>
        ///     Settings for the Data Select Component "Group/Member List".
        /// </summary>
        private class SelectSettings : SettingsStringBase
        {
            public Guid? DataViewGuid;
            public ListFormatSpecifier ListFormat = ListFormatSpecifier.FirstNameLastNameRole;
            public GroupMemberStatus? MemberStatus = GroupMemberStatus.Active;
            public RoleTypeSpecifier? RoleType;

            public SelectSettings()
            {
                //
            }

            public SelectSettings( string settingsString )
            {
                FromSelectionString( settingsString );
            }

            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                ListFormat = DataComponentSettingsHelper.GetParameterAsEnum( parameters, 0, ListFormatSpecifier.FirstNameLastNameRole );
                DataViewGuid = DataComponentSettingsHelper.GetParameterOrDefault( parameters, 1, string.Empty ).AsGuidOrNull();
                RoleType = DataComponentSettingsHelper.GetParameterAsEnum<RoleTypeSpecifier>( parameters, 2 );
                MemberStatus = DataComponentSettingsHelper.GetParameterAsEnum<GroupMemberStatus>( parameters, 3 );
            }

            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( ( (int)ListFormat ).ToString() );
                settings.Add( DataViewGuid.ToStringSafe() );
                settings.Add( RoleType == null ? string.Empty : ( (int)RoleType ).ToString() );
                settings.Add( MemberStatus == null ? string.Empty : ( (int)MemberStatus ).ToString() );

                return settings;
            }
        }

        #endregion
    }
}