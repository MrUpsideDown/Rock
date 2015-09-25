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

namespace Rock.Data
{
    /// <summary>
    /// Custom attribute used to decorate a model property and determine its reporting behavior.
    /// </summary>
    /// TODO: Include properties to consolidate the functionality of other Reporting Attributes: HideFromReportingAttribute, IncludeForReportingAttribute, PreviewableAttribute?
    [AttributeUsage(AttributeTargets.Property )]
    public class ReportingFieldAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the Field Type which should be used to filter the values in this field.
        /// This setting can be used to override the Field Type that would otherwise be inferred from the System Type of the Property.
        /// </summary>
        /// <value>
        /// The Field Type GUID.
        /// </value>
        public string FilterFieldTypeGuid { get; set; }
    }
}