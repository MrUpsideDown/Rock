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
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;
using DefinedType = Rock.SystemGuid.DefinedType;
using GroupType = Rock.SystemGuid.GroupType;

namespace Rock.Reporting.DataSelect.Person
{

    /// <summary>
    ///     A Report Field that shows a list of Locations within a specified proximity to a Person from a set of candidates defined by
    ///     a Location Data View.
    /// </summary>
    [Description( "Shows a filtered summary of Locations with a specified proximity to a Person's address." )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "FilteredLocations" )]
    public class LocationsSelect : DataSelectComponent
    {
        #region Shared (Move to DataSelectComponent base class)


        #endregion

        #region Properties

        public override string AppliesToEntityType
        {
            get { return typeof( Model.Person ).FullName; }
        }

        public override string Section
        {
            get { return "Location"; }
        }

        public override string ColumnPropertyName
        {
            get { return "Filtered Locations"; }
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
            get { return "Locations"; }
        }

        #endregion

        #region Methods

        public override string GetTitle( Type entityType )
        {
            return "Filtered Locations";
        }

        public override Expression GetExpression( RockContext context, MemberExpression entityIdProperty, string selection )
        {
            var settings = new FilterSettings( selection );

            //
            // 1. Define Candidate Locations
            //
            bool useDefaultLocationsFilter = true;

            // Get the Location Data View that defines the set of candidates from which proximate Locations can be selected.
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

            // Evaluate the Data View that defines the candidate Locations.
            var locationService = new LocationService( context );

            var locationQuery = locationService.Queryable();

            if ( dataView != null )
            {
                var paramExpression = locationService.ParameterExpression;

                List<string> errorMessages;

                var whereExpression = dataView.GetExpression( locationService, paramExpression, out errorMessages );

                if ( errorMessages.Any() )
                {
                    throw new Exception( "Filter issue(s): " + errorMessages.AsDelimited( "; " ) );
                }

                locationQuery = locationQuery.Where( paramExpression, whereExpression, null );

                useDefaultLocationsFilter = false;

                // Include child groups?
                //if (true)
                //{
                //    var searchGroupKeys = new HashSet<int>();

                //    var parentGroups = groupQuery.Select(x => x.Id);

                //    foreach (var parentGroupId in parentGroups)
                //    {
                //        var branchKeys = this.GetGroupBranchKeys(groupService, parentGroupId);

                //        searchGroupKeys.UnionWith(branchKeys);
                //    }

                //    groupQuery = groupService.Queryable().Where(x => searchGroupKeys.Contains(x.Id));
                //}                
            }

            if ( useDefaultLocationsFilter )
            {
                locationQuery = locationQuery.Where( x => !( x.Name == null || x.Name.Trim() == string.Empty ) );
            }

            // TODO: Remove this
            locationQuery = locationQuery.Where( x => x.Name == "3149-Mount Waverley" || x.Name == "3140-Lilydale" );
            //var locationKeys = locationQuery.Select( x => x.Id );

            //
            // 2. Find the Group Locations that are proximate to the candidate Locations and are associated with Family Groups.
            //
            var proximateGroupLocationsBaseQuery = new GroupLocationService( context ).Queryable();

            // Filter for Groups that are Families.
            var familyGroupTypeGuid = GroupType.GROUPTYPE_FAMILY.AsGuid();

            proximateGroupLocationsBaseQuery = proximateGroupLocationsBaseQuery.Where( x => x.Group.GroupType.Guid == familyGroupTypeGuid );

            // Filter By Location Type.
            if ( settings.LocationTypeGuid.HasValue )
            {
                proximateGroupLocationsBaseQuery = proximateGroupLocationsBaseQuery.Where( x => x.GroupLocationTypeValue.Guid == settings.LocationTypeGuid );
            }

            // Create Queries to find Family Group Locations that are proximate to each of the candidate Locations, then return a union of the result sets.
            // We do this to preserve the link between the candidate Location and the Families located near that candidate Location.
            double proximityInMeters = 0;

            var locations = locationQuery.ToList();

            IQueryable<PersonNearLocationResult> unionQuery = null;

            foreach ( var l in locations )
            {
                var groupLocationPredicate = LinqPredicateBuilder.Begin<GroupLocation>();

                if ( l.GeoPoint != null )
                {
                    var gp = l.GeoPoint;
                    groupLocationPredicate = groupLocationPredicate.Or( gl => gl.Location.GeoPoint.Distance( gp ) <= proximityInMeters );
                }

                if ( l.GeoFence != null )
                {
                    var gf = l.GeoFence;
                    groupLocationPredicate =
                        groupLocationPredicate.Or( gl => gl.Location.GeoPoint != null && gl.Location.GeoPoint.Intersects( gf ) || gl.Location.GeoPoint.Distance( gf ) <= proximityInMeters );
                    groupLocationPredicate =
                        groupLocationPredicate.Or( gl => gl.Location.GeoFence != null && gl.Location.GeoFence.Intersects( gf ) || gl.Location.GeoFence.Distance( gf ) <= proximityInMeters );
                }

                var proximateGroupLocationsQuery = proximateGroupLocationsBaseQuery.Where( groupLocationPredicate );

                // Return all of the People in the Groups identified in the Group Locations, and the set of candidate Locations their Family Group is associated with.            
                var groupMembersOfProximateLocations = new GroupMemberService( context ).Queryable()
                                                                                      .Where( gm => proximateGroupLocationsQuery.Select( gl => gl.GroupId ).Contains( gm.GroupId ) );


                //
                // ** This Query produces the correct results.
                //
                string locationName = l.ToString();

                var personLocationsQuery = new PersonService( context ).Queryable()
                                                                     .Where( p => groupMembersOfProximateLocations.Select( gm => gm.PersonId ).Contains( p.Id ) )
                                                                     .Select( x => new PersonNearLocationResult
                                                                                  {
                                                                                      Person = x,
                                                                                      LocationName = locationName
                                                                                  } );

                //var result5 = personLocationsQuery.ToList();

                if ( unionQuery == null )
                {
                    unionQuery = personLocationsQuery;
                }
                else
                {
                    unionQuery = unionQuery.Union( personLocationsQuery );
                }

            }

            //var finalQuery = unionQuery.Select(pnl => unionQuery.Where(uq => uq.Person.Id == pnl.Person.Id).Select(p => p.LocationName));

            //var resultUnion = unionQuery.ToList();

            var finalQuery = new PersonService( context ).Queryable().Select( p => unionQuery.Where( uq => uq.Person.Id == p.Id ).Select( x => x.LocationName ) );

            //var result6 = finalQuery.Where( x => x.Any() ).ToList();

            // Define the Select Expression containing the field output.
            var selectExpression = SelectExpressionExtractor.Extract<Model.Person>( finalQuery, entityIdProperty, "p" );

            return selectExpression;
        }

        private class PersonNearLocationResult
        {
            public string LocationName;
            public Model.Person Person;

            public override string ToString()
            {
                return Person.ToString() + ", " + LocationName;
            }
        }

        /// <summary>
        ///     Gets the set of Groups that are included in a Group Branch, either as the parent or a descendant.
        /// </summary>
        /// <param name="groupService"></param>
        /// <param name="parentGroup"></param>
        /// <param name="includedBranchItems"></param>
        /// <returns></returns>
        private HashSet<int> GetGroupBranchKeys( GroupService groupService, int parentGroupId ) //, IncludedGroupsSpecifier includedBranchItems )
        {
            var groupKeys = new HashSet<int>();

            //if ( parentGroupId == null )
            //{
            //    return groupKeys;
            //}

            // Include the Parent Group?
            //if ( includedBranchItems == IncludedGroupsSpecifier.EntireBranch )
            //{
            //    groupKeys.Add( parentGroup.Id );
            //}

            // Include descendants of the Parent Group.
            foreach ( int childGroupId in groupService.GetAllDescendents( parentGroupId ).Select( x => x.Id ) )
            {
                groupKeys.Add( childGroupId );
            }

            return groupKeys;
        }

        //private const string _CtlFormat = "ddlFormat";
        private const string _CtlDataView = "ddlDataView";
        private const string _CtlLocationType = "ddlLocationType";
        //private const string _CtlGroupStatus = "ddlGroupStatus";

        public override Control[] CreateChildControls( Control parentControl )
        {
            // Define Control: Output Format DropDown List
            //var ddlFormat = new RockDropDownList();
            //ddlFormat.ID = GetControlInstanceName(parentControl, _CtlFormat);
            //ddlFormat.Label = "Output Format";
            //ddlFormat.Help = "Specifies the content and format of the items in this field.";
            //ddlFormat.Items.Add( new ListItem( "Group Name And Role", ListFormatSpecifier.GroupAndRole.ToString() ) );
            //ddlFormat.Items.Add( new ListItem( "Group Name", ListFormatSpecifier.GroupOnly.ToString() ) );
            //parentControl.Controls.Add( ddlFormat );

            // Define Control: Location Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = parentControl.GetChildControlInstanceName( _CtlDataView );
            ddlDataView.Label = "Found in Locations";
            ddlDataView.Help = "A Data View that filters the Locations included in the result. If no value is selected, all Named Locations will be included.";

            parentControl.Controls.Add( ddlDataView );

            // Define Control: Location Type DropDown List
            var ddlLocationType = new RockDropDownList();
            ddlLocationType.ID = parentControl.GetChildControlInstanceName( _CtlLocationType );
            ddlLocationType.Label = "Address Type";
            ddlLocationType.Help = "Specifies the type of Address to use as the Person's Location. If no value is selected, all of a Person's Addresses will be considered.";

            foreach ( var value in DefinedTypeCache.Read( DefinedType.GROUP_LOCATION_TYPE.AsGuid() ).DefinedValues.OrderBy( a => a.Order ).ThenBy( a => a.Value ) )
            {
                ddlLocationType.Items.Add( new ListItem( value.Value, value.Guid.ToString() ) );
            }

            ddlLocationType.Items.Insert( 0, None.ListItem );

            parentControl.Controls.Add( ddlLocationType );

            // Populate the Data View Picker
            int entityTypeId = EntityTypeCache.Read( typeof( Location ) ).Id;
            ddlDataView.EntityTypeId = entityTypeId;

            return new Control[] { ddlDataView, ddlLocationType };
        }

        public override string GetSelection( Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlLocationType = controls.GetByName<RockDropDownList>( _CtlLocationType );

            var settings = new FilterSettings();

            settings.LocationTypeGuid = ddlLocationType.SelectedValue.AsGuidOrNull();
            settings.DataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( ddlDataView.SelectedValue );

            return settings.ToSelectionString();
        }

        public override void SetSelection( Control[] controls, string selection )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlLocationType = controls.GetByName<RockDropDownList>( _CtlLocationType );

            var settings = new FilterSettings( selection );

            if ( !settings.IsValid )
            {
                return;
            }

            if ( settings.DataViewGuid.HasValue )
            {
                var dsService = new DataViewService( new RockContext() );

                var dataView = dsService.Get( settings.DataViewGuid.Value );

                if ( dataView != null )
                {
                    ddlDataView.SelectedValue = dataView.Id.ToString();
                }
            }

            ddlLocationType.SelectedValue = settings.LocationTypeGuid.ToStringSafe();
        }

        #endregion

        #region Settings

        /// <summary>
        ///     Settings for the Data Select Component.
        /// </summary>
        private class FilterSettings : SettingsStringBase
        {
            public Guid? DataViewGuid;
            public Guid? LocationTypeGuid;

            public FilterSettings()
            {
                //
            }

            public FilterSettings( string settingsString )
            {
                FromSelectionString( settingsString );
            }

            public override bool IsValid
            {
                get
                {
                    if (!DataViewGuid.HasValue)
                        return false;

                    return true;
                }
            }

            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                // Parameter 1: Data View
                DataViewGuid = DataComponentSettingsHelper.GetParameterOrDefault( parameters, 0, string.Empty ).AsGuidOrNull();

                // Parameter 2: Location Type
                LocationTypeGuid = DataComponentSettingsHelper.GetParameterOrDefault( parameters, 1, string.Empty ).AsGuidOrNull();
            }

            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( DataViewGuid.ToStringSafe() );
                settings.Add( LocationTypeGuid.ToStringSafe() );

                return settings;
            }
        }

        #endregion
    }
}