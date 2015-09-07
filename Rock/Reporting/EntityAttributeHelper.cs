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
using System.Linq;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Reporting
{
    /// <summary>
    ///     A collection of helper methods for working with Entity Attributes.
    /// </summary>
    public static class EntityAttributeHelper
    {
        /// <summary>
        /// Parse a data field name to retrieve the Entity Attribute from which the content is sourced.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static AttributeCache GetAttributeFromFieldName( string fieldName )
        {
            // AttributeFields are named in format "Attribute_{attributeId}_{columnIndex}". We need the attributeId portion
            if (string.IsNullOrWhiteSpace( fieldName )
                || !fieldName.StartsWith( "Attribute_" ))
            {
                return null;
            }

            string[] nameParts = fieldName.Split( '_' );

            if (nameParts.Count() > 1)
            {
                string attributeIdPortion = nameParts[1];
                int attributeID = attributeIdPortion.AsInteger();

                if (attributeID > 0)
                {
                    return AttributeCache.Read( attributeID );
                    //var cell = e.Row.Cells[i];
                    //string cellValue = HttpUtility.HtmlDecode( cell.Text ).Trim();
                    //cell.Text = attr.FieldType.Field.FormatValue( cell, cellValue, attr.QualifierValues, true );
                }
            }

            return null;
        }

        public static AttributeField GetAttributeFieldForEntityAttribute( AttributeCache attribute )
        {
            AttributeField boundField = new AttributeField();

            boundField.DataField = attribute.Key;
            boundField.HeaderText = attribute.Name;
            boundField.SortExpression = string.Empty;

            //var attributeCache = Rock.Web.Cache.AttributeCache.Read( attribute.Id );

            //if ( attributeCache != null )
            //          {
            boundField.ItemStyle.HorizontalAlign = attribute.FieldType.Field.AlignValue;
            //        }

            return boundField;
        }

        public static AttributeField GetAttributeFieldForEntityAttribute( Rock.Model.Attribute attribute )
        {
            AttributeField boundField = new AttributeField();

            boundField.DataField = attribute.Key;
            boundField.HeaderText = attribute.Name;
            boundField.SortExpression = string.Empty;

            var attributeCache = Rock.Web.Cache.AttributeCache.Read( attribute.Id );
            
            //if ( attributeCache != null )
              //          {
            boundField.ItemStyle.HorizontalAlign = attributeCache.FieldType.Field.AlignValue;
                //        }

            return boundField;
        }
    }
}