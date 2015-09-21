﻿// <copyright>
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
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using System.Linq;
using Rock.Data;

namespace Rock
{
    /// <summary>
    /// Helper class that will output EF SQL calls to the Debug Output Window
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// 
        /// </summary>
        private class DebugLoggingDbCommandInterceptor : DbCommandInterceptor
        {
            /// <summary>
            /// Gets or sets the rock context to show the SQL Output for.  Leave null to show SQL for all rockContexts
            /// </summary>
            /// <value>
            /// The rock context.
            /// </value>
            public RockContext RockContext { get; set; }

            /// <summary>
            /// </summary>
            /// <param name="command"></param>
            /// <param name="interceptionContext"></param>
            /// <inheritdoc />
            public override void ReaderExecuting( System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext )
            {
                if ( RockContext != null && !interceptionContext.DbContexts.Any( a => a == RockContext ) )
                {
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine( "\n" );

                StackTrace st = new StackTrace( 1, true );
                var frames = st.GetFrames().Where( a => a.GetFileName() != null );
                System.Diagnostics.Debug.WriteLine( string.Format( "/*\n{0}*/", frames.ToList().AsDelimited("") ) );
                
                System.Diagnostics.Debug.WriteLine( "BEGIN\n" );

                var declares = command.Parameters.OfType<System.Data.SqlClient.SqlParameter>()
                    .Select( p => 
                    {
                        if ( p.SqlDbType == System.Data.SqlDbType.NVarChar )
                        {
                            return string.Format( "@{0} {1}({2}) = '{3}'", p.ParameterName, p.SqlDbType, p.Size, p.SqlValue );
                        }
                        else
                        {
                            return string.Format( "@{0} {1} = '{2}'", p.ParameterName, p.SqlDbType, p.SqlValue );
                        }
                    }).ToList().AsDelimited( ",\n" );

                if ( !string.IsNullOrEmpty( declares ) )
                {
                    System.Diagnostics.Debug.WriteLine( "DECLARE\n" + declares + "\n\n" );
                }

                System.Diagnostics.Debug.WriteLine( command.CommandText );

                System.Diagnostics.Debug.WriteLine( "\nEND\nGO\n\n" );
            }
        }

        /// <summary>
        /// The _debug logging database command interceptor
        /// </summary>
        private static DebugLoggingDbCommandInterceptor _debugLoggingDbCommandInterceptor = new DebugLoggingDbCommandInterceptor();

        /// <summary>
        /// Starts logging all EF SQL Calls to the Debug Output Window as T-SQL Blocks
        /// </summary>
        /// <param name="rockContext">The rock context to limit the output to.  Leave blank to show output for all rockContexts.</param>
        public static void SQLLoggingStart( RockContext rockContext = null)
        {
            SQLLoggingStop();
            _debugLoggingDbCommandInterceptor.RockContext = rockContext;
            DbInterception.Add( _debugLoggingDbCommandInterceptor );
        }

        /// <summary>
        /// Stops logging all EF SQL Calls to the Debug Output Window
        /// </summary>
        public static void SQLLoggingStop()
        {
            DbInterception.Remove( _debugLoggingDbCommandInterceptor );
        }
    }
}