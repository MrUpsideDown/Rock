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

namespace Rock.Reporting.DataFilter.GroupMembers
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Retrieve the group memberships involving a set of people in a Data View." )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "Contains People" )]
    public class ContainsPeopleFilter : DataFilterComponent
    {
        #region Settings

        /// <summary>
        ///     Settings for the Data Filter Component.
        /// </summary>
        private class FilterSettings : SettingsStringBase
        {
            public Guid? PersonDataViewGuid;

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
                    if ( !PersonDataViewGuid.HasValue )
                    {
                        return false;
                    }

                    return true;
                }
            }

            protected override void OnSetParameters( int version, IReadOnlyList<string> parameters )
            {
                // Parameter 1: Person Data View
                PersonDataViewGuid = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 0 ).AsGuidOrNull();
            }

            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( PersonDataViewGuid.ToStringSafe() );

                return settings;
            }
        }

        #endregion

        #region Properties

        public override string AppliesToEntityType
        {
            get { return typeof( Rock.Model.GroupMember ).FullName; }
        }

        public override string Section
        {
            get { return "Additional Filters"; }
        }

        #endregion

        #region Public Methods

        public override string GetTitle( Type entityType )
        {
            return "Contains People";
        }

        public override string GetClientFormatSelection( Type entityType )
        {
            return @"
function ()
{    
    var dataViewName = $('.rock-drop-down-list,select:first', $content).find(':selected').text();

    result = 'Members matching Person filter';
    result += ' ""' + dataViewName + '""';
    return result; 
}
";
        }

        public override string FormatSelection( Type entityType, string selection )
        {
            var settings = new FilterSettings( selection );

            string result = GetTitle( null );

            if ( !settings.IsValid )
            {
                return result;
            }

            using ( var context = new RockContext() )
            {
                var dataView = new DataViewService( context ).Get( settings.PersonDataViewGuid.GetValueOrDefault() );

                result = string.Format( "Members matching Person filter \"{0}\"", ( dataView != null ? dataView.ToString() : string.Empty ) );
            }

            return result;
        }

        private const string _CtlDataView = "ddlDataView";

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField parentControl )
        {
            // Define Control: Person Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = parentControl.GetChildControlInstanceName( _CtlDataView );
            ddlDataView.Label = "Represents People from this Data View";
            ddlDataView.Help = "A Person Data View that represents the set of possible Group Members.";
            parentControl.Controls.Add( ddlDataView );

            // Populate the Data View Picker
            ddlDataView.EntityTypeId = EntityTypeCache.Read( typeof( Rock.Model.Person ) ).Id;

            return new Control[] { ddlDataView };
        }

        public override string GetSelection( Type entityType, Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );

            var settings = new FilterSettings();

            settings.PersonDataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( ddlDataView.SelectedValue );

            return settings.ToSelectionString();
        }

        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {            
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );

            var settings = new FilterSettings( selection );

            if ( !settings.IsValid )
            {
                return;
            }

            ddlDataView.SelectedValue = DataComponentSettingsHelper.GetDataViewId( settings.PersonDataViewGuid ).ToStringSafe();
        }

        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var settings = new FilterSettings( selection );

            var context = (RockContext)serviceInstance.Context;

            //
            // Define Candidate People.
            //

            var dataView = DataComponentSettingsHelper.GetDataViewForFilterComponent( settings.PersonDataViewGuid, context );

            var personService = new PersonService( context );

            var personQuery = personService.Queryable();

            if ( dataView != null )
            {
                personQuery = DataComponentSettingsHelper.FilterByDataView( personQuery, dataView, personService );
            }

            var personKeys = personQuery.Select( x => x.Id );

            //
            // Construct the Query to return the list of Group Members matching the filter conditions.
            //            
            var groupMemberQuery = new GroupMemberService( context ).Queryable();

            groupMemberQuery = groupMemberQuery.Where( gm => personKeys.Contains( gm.PersonId ) );

            var result = FilterExpressionExtractor.Extract<Rock.Model.GroupMember>( groupMemberQuery, parameterExpression, "gm" );

            return result;
        }

        #endregion
    }
}