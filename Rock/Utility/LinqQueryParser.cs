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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Rock.Utility
{
    /// <summary>
    /// Extension methods to simplify the process of parsing and manipulating Linq Queries.
    /// </summary>
    public static class LinqQueryParser
    {
        public enum QueryClauseTypeSpecifier
        {
            Select = 0,
            Where = 1,
            OrderBy = 2
        }

        /// <summary>
        /// Removes expressions from a Linq query that implement the specified type of query operation.
        /// </summary>
        /// <typeparam name="T">The Type returned by the Linq query.</typeparam>
        /// <param name="qry">The Linq query to be modified.</param>
        /// <param name="clause">The modified Linq query.</param>
        /// <returns></returns>
        public static IQueryable<T> RemoveClause<T>( this IQueryable<T> qry, QueryClauseTypeSpecifier clause )
        {
            var methodNames = new List<string>();

            switch (clause)
            {
                case QueryClauseTypeSpecifier.Select:
                {
                    methodNames.Add( "Select" );
                }
                    break;
                case QueryClauseTypeSpecifier.Where:
                {
                    methodNames.Add( "Where" );
                }
                    break;
                case QueryClauseTypeSpecifier.OrderBy:
                {
                    methodNames.Add( "OrderBy" );
                    methodNames.Add( "OrderByDescending" );
                    methodNames.Add( "ThenBy" );
                    methodNames.Add( "ThenByDescending" );
                }
                    break;
            }

            return RemoveMethods( qry, methodNames );
        }

        private static IQueryable<T> RemoveMethods<T>( IQueryable<T> qry, IEnumerable<string> methodNames )
        {
            var methodRemover = new MethodRemover( methodNames );

            var queryDelegate = Expression.Lambda<Func<IQueryable<T>>>( methodRemover.Visit( qry.Expression ) ).Compile();

            var query = queryDelegate.Invoke();

            return query;
        }

        private class MethodRemover : ExpressionVisitor
        {
            private readonly List<string> _MethodNames = new List<string>();

            public MethodRemover( string methodName )
            {
                _MethodNames.Add( methodName );
            }

            public MethodRemover( IEnumerable<string> methodNames )
            {
                _MethodNames.AddRange( methodNames );
            }

            protected override Expression VisitMethodCall( MethodCallExpression node )
            {
                // If this is not an IQueryable or IEnumerable expression, ignore it.
                if (node.Method.DeclaringType != typeof(Enumerable)
                    && node.Method.DeclaringType != typeof(Queryable))
                {
                    return base.VisitMethodCall( node );
                }

                if (_MethodNames.Contains( node.Method.Name ))
                {
                    // Eliminate the method call from the expression tree by returning the object of the call instead.
                    return Visit( node.Arguments[0] );
                }

                return base.VisitMethodCall( node );
            }
        }
    }
}