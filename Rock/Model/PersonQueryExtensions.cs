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
using System.Linq;
using System.Linq.Expressions;
using Rock.Utility;
using Rock.Web.Cache;

namespace Rock.Model
{
    /// <summary>
    /// Extension methods for Person Query objects.
    /// </summary>
    public static class PersonQueryExtensions
    {
        /// <summary>
        /// Determines whether the specified record represents a Business entity.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="isBusiness">if set to <c>true</c> will only return records that represent a Business.</param>
        /// <returns></returns>
        public static IQueryable<Person> IsBusiness( this IQueryable<Person> query, bool isBusiness = true )
        {
            int recordTypeBusinessId = DefinedValueCache.Read( SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;

            if (isBusiness)
            {
                return query.Where( p => p.RecordTypeValueId.HasValue && p.RecordTypeValueId.Value == recordTypeBusinessId );
            }
            
            return query.Where( p => !p.RecordTypeValueId.HasValue || p.RecordTypeValueId.Value != recordTypeBusinessId );
        }

        /// <summary>
        /// Filters a Person Query by Deceased status.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="isDeceased">if set to <c>true</c> will only return records that where the Person is deceased.</param>
        /// <returns></returns>
        public static IQueryable<Person> IsDeceased( this IQueryable<Person> query, bool isDeceased = true )
        {
            query = query.Where( p => p.RecordTypeValueId.HasValue );

            if ( isDeceased )
            {
                return query.Where( p => p.IsDeceased.HasValue && p.IsDeceased.Value );
            }

            return query.Where( p => !p.IsDeceased.HasValue || !p.IsDeceased.Value );
        }

        public enum PersonNameFilterSpecifier
        {
            FirstLastNames = 0,
            FirstName = 1,
            LastName = 2
        }

        /// <summary>
        /// Filters a Person Query by Name, searching for specified terms in the Person Name Fields.
        /// The search algorithm is as follows:
        /// All supplied terms must match at least once in any of the fields.
        /// A term is considered to be matched if the field starts with the exact term, or contains one of its acceptable variations.
        /// </summary>
        /// <param name="query">The IQueryable to which the filter will be applied.</param>
        /// <param name="searchTerms">The search terms.</param>
        /// <param name="filterType">the type of filter to apply.</param>
        /// <returns></returns>
        public static IQueryable<Person> FilterByName( this IQueryable<Person> query, IEnumerable<string> searchTerms, PersonNameFilterSpecifier filterType = PersonNameFilterSpecifier.FirstLastNames )
        {
            var terms = searchTerms.ToList();

            Expression<Func<Person, bool>> whereClause = null;

            switch (filterType)
            {
                case PersonNameFilterSpecifier.FirstLastNames:
                    whereClause = GetFilterExpressionForMatchAllNames( terms );                
                    break;
                case PersonNameFilterSpecifier.FirstName:
                    whereClause = GetFilterExpressionForFirstName( terms );
                    break;
                case PersonNameFilterSpecifier.LastName:
                    whereClause = GetFilterExpressionForLastName( terms );
                    break;
            }

            if (whereClause != null)
            {
                query = query.Where( whereClause );
            }

            return query;
        }

        /// <summary>
        /// Creates a filter expression to match one or more names in the common Person Name Fields.
        /// The search algorithm is as follows:
        /// All supplied terms must match in at least once in any of the fields.
        /// A term is considered to be matched if the field starts with the exact term, or contains one of its acceptable variations.
        /// </summary>
        /// <param name="searchTerms">The search terms.</param>
        /// <returns></returns>
        private static Expression<Func<Person, bool>> GetFilterExpressionForMatchAllNames( IEnumerable<string> searchTerms )
        {
            var whereClause = LinqPredicateBuilder.Begin<Person>();

            foreach (var searchTerm in searchTerms)
            {
                // Create variations of the search term that could be embedded in a name field.
                var embeddedTerms = new List<string>();

                // Multi-word Names: eg. Search for "Swee Lou" or "Swee Chin" matches "Swee Chin LOU".
                embeddedTerms.Add( " " + searchTerm );
                // Hyphenated Names: eg. Search for "Susan Smith" matches "Susan Huntington-Smith".
                embeddedTerms.Add( "-" + searchTerm );
                // Names with Parentheses: eg. Search for "Andrew ST" matches "Andrew (ST) Lim".
                embeddedTerms.Add( "(" + searchTerm );

                // Create a predicate to match this search term in any field.
                var termPredicate = LinqPredicateBuilder.Begin<Person>();

                var searchTermForClosure = searchTerm;

                termPredicate = termPredicate.Or( p => p.FirstName.StartsWith( searchTermForClosure ) )
                                             .Or( p => p.NickName.StartsWith( searchTermForClosure ) )
                                             .Or( p => p.LastName.StartsWith( searchTermForClosure ) );

                foreach (var embeddedTerm in embeddedTerms)
                {
                    var embeddedTermForClosure = embeddedTerm;

                    termPredicate = termPredicate.Or( p => p.FirstName.Contains( embeddedTermForClosure ) )
                                                 .Or( p => p.NickName.Contains( embeddedTermForClosure ) )
                                                 .Or( p => p.LastName.Contains( embeddedTermForClosure ) );
                }

                // Add this to the Where clause using a logical AND to ensure that every search term is matched.
                whereClause = whereClause.And( termPredicate );
            }

            return whereClause;
        }

        private static Expression<Func<Person, bool>> GetFilterExpressionForFirstName( IEnumerable<string> searchTerms )
        {
            var whereClause = LinqPredicateBuilder.Begin<Person>();

            foreach (var searchTerm in searchTerms)
            {
                // Copy iterated variable for use with LINQ expression.
                string thisSearchTerm = searchTerm;

                whereClause = whereClause.Or( p => p.FirstName.StartsWith( thisSearchTerm ) || p.NickName.StartsWith( thisSearchTerm ) );
            }

            return whereClause;
        }

        private static Expression<Func<Person, bool>> GetFilterExpressionForLastName( IEnumerable<string> searchTerms )
        {
            var whereClause = LinqPredicateBuilder.Begin<Person>();

            foreach (var searchTerm in searchTerms)
            {
                // Copy iterated variable for use with LINQ expression.
                string thisSearchTerm = searchTerm;

                whereClause = whereClause.Or( p => p.LastName.StartsWith( thisSearchTerm ) );
            }

            return whereClause;
        }
    }
}