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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web.UI.WebControls;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace Rock.Reporting.DataSelect.GroupMember
{
    /// <summary>
    /// Report Field for Group Member Person.
    /// </summary>
    [Description( "Show the name of the person as a navigable link to the person detail page" )]
    [Export( typeof( DataSelectComponent ) )]
    [ExportMetadata( "ComponentName", "Select Person Name" )]
    [BooleanField( "Show As Link", "", true )]
    [CustomRadioListField( "Display Order", "", "0^FirstName LastName,1^LastName&#44; FirstName", true, "0" )]
    public class PersonLinkSelect : DataSelectComponent
    {
        /// <summary>
        /// Gets the name of the entity type. Filter should be an empty string
        /// if it applies to all entities
        /// </summary>
        /// <value>
        /// The name of the entity type.
        /// </value>
        public override string AppliesToEntityType
        {
            get
            {
                return typeof( Rock.Model.GroupMember ).FullName;
            }
        }

        /// <summary>
        /// The PropertyName of the property in the anonymous class returned by the SelectExpression
        /// </summary>
        /// <value>
        /// The name of the column property.
        /// </value>
        public override string ColumnPropertyName
        {
            get
            {
                return "Person Name";
            }
        }

        /// <summary>
        /// Gets the section that this will appear in in the Field Selector
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get
            {
                return "Common";
            }
        }

        /// <summary>
        /// Gets the grid field.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override System.Web.UI.WebControls.DataControlField GetGridField( Type entityType, string selection )
        {
            var result = new BoundField();

            // Disable encoding of field content because the value contains markup.
            result.HtmlEncode = false;

            return result;
        }

        public override Type ColumnFieldType
        {
            get { return typeof( FormattedDataValue ); }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="entityIdProperty">The entity identifier property.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override System.Linq.Expressions.Expression GetExpression( Data.RockContext context, System.Linq.Expressions.MemberExpression entityIdProperty, string selection )
        {
            bool showAsLink = this.GetAttributeValueFromSelection( "ShowAsLink", selection ).AsBooleanOrNull() ?? false;
            int displayOrder = this.GetAttributeValueFromSelection( "DisplayOrder", selection ).AsIntegerOrNull() ?? 0;

            var memberQuery = new GroupMemberService( context ).Queryable();

            IQueryable<FormattedDataValue> personLinkQuery;

            if ( showAsLink )
            {
                if ( displayOrder == 0 )
                {
                    personLinkQuery = memberQuery.Select( gm => new HtmlLinkDataValue { SourceValue = gm.Person.NickName + " " + gm.Person.LastName, Url = "/person/" + gm.PersonId.ToString() } );
                }
                else
                {
                    personLinkQuery = memberQuery.Select( gm => new HtmlLinkDataValue { SourceValue = gm.Person.LastName + ", " + gm.Person.NickName, Url = "/person/" + gm.PersonId.ToString() } );
                }
            }
            else
            {
                if ( displayOrder == 0 )
                {
                    personLinkQuery = memberQuery.Select( gm => new FormattedDataValue { SourceValue = gm.Person.LastName + ", " + gm.Person.NickName } );
                }
                else
                {
                    personLinkQuery = memberQuery.Select( gm => new FormattedDataValue { SourceValue = gm.Person.LastName + ", " + gm.Person.NickName } );
                }
            }

            var exp = SelectExpressionExtractor.Extract( personLinkQuery, entityIdProperty, "gm" );

            return exp;
        }
    }
}
