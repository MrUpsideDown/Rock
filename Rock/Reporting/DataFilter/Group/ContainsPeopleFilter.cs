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
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.Utilities;

namespace Rock.Reporting.DataFilter.Group
{
    /// <summary>
    ///     A Data Filter to select Groups based on the number of group members that also exist in a Person Data View.
    /// </summary>
    [Description( "Filter groups based on the number of group members that also exist in a filtered set of people." )]
    [Export( typeof(DataFilterComponent) )]
    [ExportMetadata( "ComponentName", "Contains People" )]
    public class ContainsPeopleFilter : DataFilterComponent
    {
        #region Settings

        /// <summary>
        ///     Settings for the Data Filter Component.
        /// </summary>
        private class FilterSettings : SettingsStringBase
        {
            public int PersonCount;
            public ComparisonType PersonCountComparison = ComparisonType.GreaterThan;
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
                    if (!PersonDataViewGuid.HasValue)
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

                // Parameter 2: Person Count Comparison
                PersonCountComparison = DataComponentSettingsHelper.GetParameterAsEnum( parameters, 1, ComparisonType.GreaterThan );

                // Parameter 3: Person Count
                PersonCount = DataComponentSettingsHelper.GetParameterOrEmpty( parameters, 2 ).AsInteger();
            }

            protected override IEnumerable<string> OnGetParameters()
            {
                var settings = new List<string>();

                settings.Add( PersonDataViewGuid.ToStringSafe() );
                settings.Add( PersonCountComparison.ToStringSafe() );
                settings.Add( PersonCount.ToStringSafe() );

                return settings;
            }
        }

        #endregion

        #region Properties

        public override string AppliesToEntityType
        {
            get { return typeof(Model.Group).FullName; }
        }

        public override string Section
        {
            get { return "Member Filters"; }
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
    var comparisonName = $('.js-filter-compare', $content).find(':selected').text();
    var comparisonCount = $('.js-member-count', $content).val();

    result = 'Members matching Person filter';
    result += ' ""' + dataViewName + '""';
    result += ' ' + comparisonName;
    result += ' ' + comparisonCount;
    return result; 
}
";
        }

        public override string FormatSelection( Type entityType, string selection )
        {
            var settings = new FilterSettings( selection );

            string result = GetTitle( null );

            if (!settings.IsValid)
            {
                return result;
            }

            using (var context = new RockContext())
            {
                var dataView = new DataViewService( context ).Get( settings.PersonDataViewGuid.GetValueOrDefault() );

                result = string.Format( "Members matching Person filter \"{0}\" is {1} {2}",
                                        ( dataView != null ? dataView.ToString() : string.Empty ),
                                        settings.PersonCountComparison.ConvertToString(),
                                        settings.PersonCount );
            }

            return result;
        }

        private const string _CtlDataView = "ddlDataView";
        private const string _CtlComparison = "ddlComparison";
        private const string _CtlMemberCount = "nbMemberCount";

        private const ComparisonType CountComparisonTypesSpecifier =
            ComparisonType.EqualTo |
            ComparisonType.NotEqualTo |
            ComparisonType.GreaterThan |
            ComparisonType.GreaterThanOrEqualTo |
            ComparisonType.LessThan |
            ComparisonType.LessThanOrEqualTo;

        public override Control[] CreateChildControls( Type entityType, FilterField parentControl )
        {
            // Define Control: Person Data View Picker
            var ddlDataView = new DataViewPicker();
            ddlDataView.ID = parentControl.GetChildControlInstanceName( _CtlDataView );
            ddlDataView.Label = "Contains People from this Data View";
            ddlDataView.Help = "A Person Data View that provides the set of possible Group Members.";
            parentControl.Controls.Add( ddlDataView );

            var ddlCompare = ComparisonHelper.ComparisonControl( CountComparisonTypesSpecifier );
            ddlCompare.Label = "where the number of matching Group Members is";
            ddlCompare.ID = parentControl.GetChildControlInstanceName( _CtlComparison );
            ddlCompare.AddCssClass( "js-filter-compare" );
            parentControl.Controls.Add( ddlCompare );

            var nbCount = new NumberBox();
            nbCount.Label = "&nbsp;";
            nbCount.ID = parentControl.GetChildControlInstanceName( _CtlMemberCount );
            nbCount.AddCssClass( "js-filter-control js-member-count" );
            nbCount.FieldName = "Member Count";
            parentControl.Controls.Add( nbCount );

            // Populate the Data View Picker
            ddlDataView.EntityTypeId = EntityTypeCache.Read( typeof(Model.Person) ).Id;

            return new Control[] {ddlDataView, ddlCompare, nbCount};
        }

        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlCompare = controls.GetByName<RockDropDownList>( _CtlComparison );
            var nbValue = controls.GetByName<NumberBox>( _CtlMemberCount );

            ddlDataView.RenderControl( writer );

            // Comparison Row
            writer.AddAttribute( "class", "row field-criteria" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            // Comparison Type
            writer.AddAttribute( "class", "col-md-4" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            ddlCompare.RenderControl( writer );
            writer.RenderEndTag();

            //ComparisonType comparisonType = (ComparisonType)( ddlCompare.SelectedValue.AsInteger() );
            //nbValue.Style[HtmlTextWriterStyle.Display] = ( comparisonType == ComparisonType.IsBlank || comparisonType == ComparisonType.IsNotBlank ) ? "none" : string.Empty;

            // Comparison Value
            writer.AddAttribute( "class", "col-md-8" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            nbValue.RenderControl( writer );
            writer.RenderEndTag();

            writer.RenderEndTag(); // row

            RegisterFilterCompareChangeScript( filterControl );
        }

        public override string GetSelection( Type entityType, Control[] controls )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlCompare = controls.GetByName<RockDropDownList>( _CtlComparison );
            var nbValue = controls.GetByName<NumberBox>( _CtlMemberCount );

            var settings = new FilterSettings();

            settings.PersonDataViewGuid = DataComponentSettingsHelper.GetDataViewGuid( ddlDataView.SelectedValue );
            settings.PersonCountComparison = ddlCompare.SelectedValueAsEnum<ComparisonType>( ComparisonType.GreaterThan );
            settings.PersonCount = nbValue.Text.AsInteger();

            return settings.ToSelectionString();
        }

        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            var ddlDataView = controls.GetByName<DataViewPicker>( _CtlDataView );
            var ddlCompare = controls.GetByName<RockDropDownList>( _CtlComparison );
            var nbValue = controls.GetByName<NumberBox>( _CtlMemberCount );

            var settings = new FilterSettings( selection );

            if (!settings.IsValid)
            {
                return;
            }

            ddlDataView.SelectedValue = DataComponentSettingsHelper.GetDataViewId( settings.PersonDataViewGuid ).ToStringSafe();
            ddlCompare.SelectedValue = settings.PersonCountComparison.ConvertToInt().ToString();
            nbValue.Text = settings.PersonCount.ToString();
        }

        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            var settings = new FilterSettings( selection );

            var context = (RockContext)serviceInstance.Context;

            //
            // Define Candidate People.
            //

            // Get the Person Data View that defines the set of candidates from which matching Group Members can be selected.
            var dataView = DataComponentSettingsHelper.GetDataViewForFilterComponent( settings.PersonDataViewGuid, context );

            var personService = new PersonService( context );

            var personQuery = personService.Queryable();

            if (dataView != null)
            {
                personQuery = DataComponentSettingsHelper.FilterByDataView( personQuery, dataView, personService );
            }

            var personKeys = personQuery.Select( x => x.Id );

            //
            // Construct the Query to return the list of Groups matching the filter conditions.
            //            
            var comparisonType = settings.PersonCountComparison;
            int memberCountValue = settings.PersonCount;

            var memberCountQuery = new GroupService( context ).Queryable();

            var memberCountEqualQuery = memberCountQuery.Where( g => g.Members.Count( gm => personKeys.Contains( gm.PersonId ) ) == memberCountValue );

            var compareEqualExpression = FilterExpressionExtractor.Extract<Model.Group>( memberCountEqualQuery, parameterExpression, "g" ) as BinaryExpression;
            var result = FilterExpressionExtractor.AlterComparisonType( comparisonType, compareEqualExpression, 0 );

            return result;
        }

        #endregion
    }
}