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

namespace Rock.Web.UI.Controls
{
    /// <summary>
    /// Represents a value extracted from a Data Source that can be rendered in a variety of formats.
    /// </summary>
    public interface IFormattedDataValue
    {
        /// <summary>
        /// Gets or sets the value that was extracted from the original data source.
        /// </summary>
        /// <value>
        /// The source value.
        /// </value>
        string SourceValue { get; set; }

        /// <summary>
        /// Gets or sets a representation of the value that is suitable for using to sequence the data.
        /// </summary>
        /// <value>
        /// The sort value.
        /// </value>
        string SortValue { get; set; }

        /// <summary>
        /// Gets or sets a representation of the value that is suitable for export to a file.
        /// </summary>
        /// <value>
        /// The export value.
        /// </value>
        string ExportValue { get; set; }

        /// <summary>
        /// Gets or sets a representation of the value that is suitable for display in a HTML client.
        /// This value may contain HTML markup.
        /// </summary>
        /// <value>
        /// The display value.
        /// </value>
        string DisplayValue { get; set; }

        string ToString();
    }

    /// <summary>
    /// Represents a value extracted from a Data Source that can be rendered in a variety of formats.
    /// </summary>
    public class FormattedDataValue : IFormattedDataValue
    {
        private string _DisplayValue;
        private string _ExportValue;
        private string _SortValue;

        public FormattedDataValue()
        {
        }

        public FormattedDataValue( string sourceValue )
        {
            SourceValue = sourceValue;
        }

        public string SourceValue { get; set; }

        public string SortValue
        {
            get { return _SortValue ?? SourceValue; }
            set { _SortValue = value; }
        }

        public virtual string DisplayValue
        {
            get { return _DisplayValue ?? SourceValue; }
            set { _DisplayValue = value; }
        }

        public string ExportValue
        {
            get { return _ExportValue ?? SourceValue; }
            set { _ExportValue = value; }
        }

        public override string ToString()
        {
            return DisplayValue ?? SourceValue;
        }
    }

    /// <summary>
    /// A FormattedDataValue that is rendered as a sortable HTML Link.
    /// </summary>
    public class HtmlLinkDataValue : FormattedDataValue
    {
        private string _DisplayValue;
        private string _Url;

        public override string DisplayValue
        {
            get
            {
                if (_DisplayValue == null)
                {
                    // Create the display value when it is first accessed.
                    _DisplayValue = string.Format( "<!--{0}--><a href='{1}'>{2}</a>", SortValue, Url, SourceValue );
                }
                return _DisplayValue;
            }
            set { _DisplayValue = value; }
        }

        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;

                // Force the DisplayValue to be refreshed next time it is accessed.
                _DisplayValue = null;
            }
        }
    }
}