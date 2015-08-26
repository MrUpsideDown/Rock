using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Rock.Utility
{
    /// <summary>
    /// Extension methods to simplify the process of constructing Linq predicates.
    /// A predicate is an expression that is either True or False for a given value of T.
    /// </summary>
    public static class LinqPredicateBuilder
    {
        /// <summary>
        /// Begins a predicate chain.
        /// </summary>
        /// <typeparam name="T">The Type to which the expression applies.</typeparam>
        /// <param name="value">Default return value if the chain is ended early</param>
        /// <returns>A lambda expression stub.</returns>
        public static Expression<Func<T, bool>> Begin<T>( bool value = false )
        {
            // Return a True or False lambda expression according to the supplied value.
            // This expression will be discarded when subsequent predicates are added.
            if ( value )
            {
                return parameter => true;
            }

            return parameter => false;
        }

        /// <summary>
        /// Combines the supplied predicates using a logical AND operation.
        /// </summary>
        /// <typeparam name="T">The Type to which the expression applies.</typeparam>
        /// <param name="left">The left expression.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>The combined predicate expression.</returns>
        public static Expression<Func<T, bool>> And<T>( this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right )
        {
            return CombineLambdas( left, right, ExpressionType.AndAlso );
        }

        /// <summary>
        /// Combines the supplied predicates using a logical OR operation.
        /// </summary>
        /// <typeparam name="T">The Type to which the expression applies.</typeparam>
        /// <param name="left">The left expression.</param>
        /// <param name="right">The right expression.</param>
        /// <returns>The combined predicate expression.</returns>
        public static Expression<Func<T, bool>> Or<T>( this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right )
        {
            return CombineLambdas( left, right, ExpressionType.OrElse );
        }

        /// <summary>
        /// Negates the specified predicate.
        /// </summary>
        /// <typeparam name="T">The Type to which the expression applies.</typeparam>
        /// <param name="expression">The predicate expression.</param>
        /// <returns>A negated predicate.</returns>
        public static Expression<Func<T, bool>> Not<T>( this Expression<Func<T, bool>> expression )
        {
            var negated = Expression.Not( expression.Body );

            return Expression.Lambda<Func<T, bool>>( negated, expression.Parameters );
        }

        private static Expression<Func<T, bool>> CombineLambdas<T>( this Expression<Func<T, bool>> left,
                                                                    Expression<Func<T, bool>> right, ExpressionType expressionType )
        {
            // If the existing expression is a constant, it was created with Begin<T>() and it should be removed.
            if ( IsExpressionBodyConstant( left ) )
            {
                return ( right );
            }

            var p = left.Parameters[0];

            var visitor = new SubstituteParameterVisitor();
            visitor.SubstitutionMap[right.Parameters[0]] = p;

            var body = Expression.MakeBinary( expressionType, left.Body, visitor.Visit( right.Body ) );
        
            return Expression.Lambda<Func<T, bool>>( body, p );
        }

        private static bool IsExpressionBodyConstant<T>( Expression<Func<T, bool>> left )
        {
            return left.Body.NodeType == ExpressionType.Constant;
        }

        /// <summary>
        /// An Expression Visitor that replaces one parameter with another.
        /// </summary>
        internal class SubstituteParameterVisitor : ExpressionVisitor
        {
            public Dictionary<Expression, Expression> SubstitutionMap = new Dictionary<Expression, Expression>();

            protected override Expression VisitParameter( ParameterExpression node )
            {
                Expression newValue;
                if ( SubstitutionMap.TryGetValue( node, out newValue ) )
                {
                    return newValue;
                }
                return node;
            }
        }
    }
}