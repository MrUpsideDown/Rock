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
using System.Web.UI;
using Rock.Web.Cache;

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// Control for selecting a defined value
    /// </summary>
    [ToolboxData("<{0}:DefinedValueField runat=server></{0}:DefinedValueField>")]
    public class DefinedValueField : RockBoundField
    {
        /// <summary>
        /// Formats the specified field value for a cell in the <see cref="T:System.Web.UI.WebControls.BoundField" /> object.
        /// </summary>
        /// <param name="dataValue">The field value to format.</param>
        /// <param name="encode">true to encode the value; otherwise, false.</param>
        /// <returns>
        /// The field value converted to the format specified by <see cref="P:System.Web.UI.WebControls.BoundField.DataFormatString" />.
        /// </returns>
        protected override string FormatDataValue(object dataValue, bool encode)
        {
            if (dataValue == null)
            {
                return string.Empty;
            }
            
            var dataType = dataValue.GetType();

            if ( dataType == typeof( int? ) || dataType == typeof( int ) )
            {
                // Attempt to parse the value as a single Defined Value Id.
                int definedValueId;

                if ( dataType == typeof( int ) )
                {
                    definedValueId = (int)dataValue;
                }
                else
                {
                    definedValueId = (int?)dataValue ?? 0;
                }

                dataValue = Rock.Web.Cache.DefinedValueCache.Read( definedValueId ).Value;
            }
            else if ( dataType == typeof( string ) )
            {
                // Attempt to parse the value as a list of Defined Value Guids.
                // If a value is not a Guid or cannot be matched to a Defined Value, the raw value will be shown.
                var guids = dataValue.ToString().Split( ',' );

                var definedValues = new List<string>();

                foreach ( var guidString in guids )
                {
                    Guid definedValueGuid;

                    bool isGuid = Guid.TryParse( guidString, out definedValueGuid );
                    bool addRaw = true;

                    if ( isGuid )
                    {
                        var definedValue = DefinedValueCache.Read( definedValueGuid );

                        if ( definedValue != null )
                        {
                            definedValues.Add( definedValue.Value );
                            addRaw = false;
                        }
                    }

                    if ( addRaw )
                    {
                        definedValues.Add( guidString );
                    }
                }

                dataValue = definedValues.AsDelimited( ", " );
            }

            return base.FormatDataValue(dataValue, encode);
        }
    }
}
