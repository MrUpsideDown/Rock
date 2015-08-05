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
using Rock.Reporting.DataSelect;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;

namespace Rock.Reporting.DataFilter.Person
{
    /// <summary>
    ///     A DataFilter that filters People by home address within a specified distance from a location.
    /// </summary>
    [Description( "Filter people by address using a set of locations identified by a Location Data View" )]
    [Export( typeof(DataFilterComponent) )]
    [ExportMetadata( "ComponentName", "Location Data View Filter" )]
    public class LocationDataViewFilter : DataFilterComponent
    {
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
                    {
                        return false;
                    }

                    return true;
                }
            }

            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                // Parameter 1: Data View
                DataViewGuid = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 0 ).AsGuidOrNull();

                // Parameter 2: Location Type
                LocationTypeGuid = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 1 ).AsGuidOrNull();
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

        #region Properties

        /// <summary>
        ///     Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        ///     The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return typeof(Model.Person).FullName; }
        }

        /// <summary>
        ///     Gets the section.
        /// </summary>
        /// <value>
        ///     The section.
        /// </value>
        public override string Section
        {
            get { return "Related Data Views"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        ///     The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "Location Data View";
        }

        /// <summary>
        ///     Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        ///     will set the description of the filter to whatever is returned by this property.  If including script, the
        ///     controls parent container can be referenced through a '$content' variable that is set by the control before
        ///     referencing this property.
        /// </summary>
        /// <value>
        ///     The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return @"
function() {
  var dataViewName = $('.rock-drop-down-list,select:first', $content).find(':selected').text();
  var locationType = $('.rock-drop-down-list,select:last', $content).find(':selected').text();
  var result = 'Location';
  if (locationType) {
     result = result + ' type ""' + locationType + "";
  }  
  result = result + ' is in filter: ' + dataViewName;
  return result;
}
";
        }

        /// <summary>
        ///     Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            var settings = new FilterSettings( selection );

            string result = "Connected to Location";

            if (!settings.IsValid)
            {
                return result;
            }

            using (var context = new RockContext())
            {
                var dataView = new DataViewService( context ).Get( settings.DataViewGuid.GetValueOrDefault() );

                string locationTypeName = null;

                if (settings.LocationTypeGuid.HasValue)
                {
                    locationTypeName = DefinedValueCache.Read( settings.LocationTypeGuid.Value, context ).Value;
                }

                result = string.Format( "Location {0} is in filter: {1}",
                                        ( locationTypeName != null ? "type \"" + locationTypeName + "\"" : string.Empty ),
                                        ( dataView != null ? dataView.ToString() : string.Empty ) );
            }

            return result;
        }

        private const string _CtlDataView = "ddlDataView";
        private const string _CtlLocationType = "ddlLocationType";

        /// <summary>
        ///     Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField parentControl )
        {
            // Define Control: Location Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = parentControl.GetChildControlInstanceName( _CtlDataView );
            ddlDataView.Label = "Connected to Locations";
            ddlDataView.Help = "A Data View that provides the list of Locations to which the Person may be connected.";

            parentControl.Controls.Add( ddlDataView );

            // Define Control: Location Type DropDown List
            var ddlLocationType = new RockDropDownList();
            ddlLocationType.ID = parentControl.GetChildControlInstanceName( _CtlLocationType );
            ddlLocationType.Label = "Address Type";
            ddlLocationType.Help = "Specifies the type of Address the filter will be applied to. If no value is selected, all of the Person's Addresses will be considered.";

            var familyLocations = GroupTypeCache.GetFamilyGroupType().LocationTypeValues.OrderBy( a => a.Order ).ThenBy( a => a.Value );

            foreach (var value in familyLocations)
            {
                ddlLocationType.Items.Add( new ListItem( value.Value, value.Guid.ToString() ) );
            }

            ddlLocationType.Items.Insert( 0, None.ListItem );

            parentControl.Controls.Add( ddlLocationType );

            // Populate the Data View Picker
            int entityTypeId = EntityTypeCache.Read( typeof(Location) ).Id;
            ddlDataView.EntityTypeId = entityTypeId;

            return new Control[] {ddlDataView, ddlLocationType};
        }

        /// <summary>
        ///     Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlLocationType = controls.GetByName<RockDropDownList>( _CtlLocationType );

            var settings = new FilterSettings();

            settings.LocationTypeGuid = ddlLocationType.SelectedValue.AsGuidOrNull();
            settings.DataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( ddlDataView.SelectedValue );

            return settings.ToSelectionString();
        }

        /// <summary>
        ///     Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlLocationType = controls.GetByName<RockDropDownList>( _CtlLocationType );

            var settings = new FilterSettings( selection );

            if (!settings.IsValid)
            {
                return;
            }

            ddlDataView.SelectedValue = DataComponentSettingsHelper.GetDataViewId( settings.DataViewGuid ).ToStringSafe();
            ddlLocationType.SelectedValue = settings.LocationTypeGuid.ToStringSafe();
        }

        /// <summary>
        ///     Gets the expression.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceInstance">The service instance.</param>
        /// <param name="parameterExpression">The parameter expression.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var settings = new FilterSettings( selection );

            var context = (RockContext)serviceInstance.Context;

            // Get the Location Data View that defines the set of candidates from which proximate Locations can be selected.
            var dataView = DataComponentSettingsHelper.GetDataViewForFilterComponent( settings.DataViewGuid, context );

            // Evaluate the Data View that defines the candidate Locations.
            var locationService = new LocationService( context );

            var locationQuery = locationService.Queryable();

            if (dataView != null)
            {
                var paramExpression = locationService.ParameterExpression;

                List<string> errorMessages;

                var whereExpression = dataView.GetExpression( locationService, paramExpression, out errorMessages );

                if (errorMessages.Any())
                {
                    throw new Exception( "Filter issue(s): " + errorMessages.AsDelimited( "; " ) );
                }

                locationQuery = locationQuery.Where( paramExpression, whereExpression, null );
            }

            // Get all the Family Groups that have a Location matching one of the candidate Locations.
            int familyGroupTypeId = GroupTypeCache.Read( SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() ).Id;

            var groupLocationsQuery = new GroupLocationService( context ).Queryable()
                                                                         .Where( gl => gl.Group.GroupTypeId == familyGroupTypeId && locationQuery.Any( l => l.Id == gl.LocationId ) );

            // If a Location Type is specified, apply the filter condition.
            if (settings.LocationTypeGuid.HasValue)
            {
                int groupLocationTypeId = DefinedValueCache.Read( settings.LocationTypeGuid.Value ).Id;

                groupLocationsQuery = groupLocationsQuery.Where( x => x.GroupLocationTypeValue.Id == groupLocationTypeId );
            }

            // Get all of the Group Members of the qualifying Families.
            var groupMemberServiceQry = new GroupMemberService( context ).Queryable()
                                                                         .Where( gm => groupLocationsQuery.Any( gl => gl.GroupId == gm.GroupId ) );

            // Get all of the People corresponding to the qualifying Group Members.
            var qry = new PersonService( context ).Queryable()
                                                  .Where( p => groupMemberServiceQry.Any( gm => gm.PersonId == p.Id ) );

            // Retrieve the Filter Expression.
            var extractedFilterExpression = FilterExpressionExtractor.Extract<Model.Person>( qry, parameterExpression, "p" );

            return extractedFilterExpression;
        }

        #endregion
    }
}