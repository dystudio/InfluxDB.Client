﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Reflection;
//using System.Threading.Tasks;
//using Vibrant.InfluxDB.Client.Linq;

//namespace Vibrant.InfluxDB.Client.Visitors
//{
//   internal class InfluxQueryInfoGenerator<TInfluxRow> : ExpressionVisitor
//      where TInfluxRow : new()
//   {
//      private InfluxQueryInfo<TInfluxRow> _info;
//      private RowProjection _currentProjection;

//      internal InfluxQueryInfoGenerator()
//      {

//      }

//      internal InfluxQueryInfo<TInfluxRow> GetInfo( Expression expression, string db, string measurementName )
//      {
//         _info = new InfluxQueryInfo<TInfluxRow>( db, measurementName );
//         Visit( expression );
//         return _info;
//      }

//      private static Expression StripQuotes( Expression e )
//      {
//         while( e.NodeType == ExpressionType.Quote )
//         {
//            e = ( (UnaryExpression)e ).Operand;
//         }
//         return e;
//      }

//      protected override Expression VisitMethodCall( MethodCallExpression node )
//      {
//         if( node.Method.DeclaringType == typeof( Queryable ) )
//         {
//            // Visit the SOURCE itself (the object the method was called on)
//            // source.MethodName( expression )
//            //  -> source is node.Arguments[ 0 ]
//            //  -> expression is node.Arguments[ 1 ]

//            if( node.Method.Name == "Where" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store the Body of the lambda (representing part of the Where clause)
//               _info.WhereClauses.Add( new WhereClause( lambda.Body, _currentProjection ) );

//               // we do not visit the body itself, we will visit that later to perform query creation
//            }
//            else if( node.Method.Name == "Select" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store information about the projection and which columns were selected

//               // a chain of the lambdas that were selected represents the projection itself
//               _currentProjection = new RowProjection( lambda, _currentProjection );

//               // populate the Bindings of the current projection by visiting the body
//               Visit( lambda.Body );

//               // update the select clause
//               _info.SelectClause = new SelectClause( _currentProjection );
//            }
//            else if( node.Method.Name == "OrderBy" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store the Body of the lambda (representing part of the Where clause)
//               _info.OrderByClauses.Add( new OrderByClause( true, lambda.Body, _currentProjection ) );

//               // we do not visit the body itself, we will visit that later to perform query creation
//            }
//            else if( node.Method.Name == "ThenBy" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store the Body of the lambda (representing part of the Where clause)
//               _info.OrderByClauses.Add( new OrderByClause( true, lambda.Body, _currentProjection ) );

//               // we do not visit the body itself, we will visit that later to perform query creation
//            }
//            else if( node.Method.Name == "OrderByDescending" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store the Body of the lambda (representing part of the Where clause)
//               _info.OrderByClauses.Add( new OrderByClause( false, lambda.Body, _currentProjection ) );

//               // we do not visit the body itself, we will visit that later to perform query creation
//            }
//            else if( node.Method.Name == "ThenByDescending" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var lambda = (LambdaExpression)StripQuotes( node.Arguments[ 1 ] );

//               // store the Body of the lambda (representing part of the Where clause)
//               _info.OrderByClauses.Add( new OrderByClause( false, lambda.Body, _currentProjection ) );

//               // we do not visit the body itself, we will visit that later to perform query creation
//            }
//            else if( node.Method.Name == "Take" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var expression = (ConstantExpression)StripQuotes( node.Arguments[ 1 ] );

//               _info.Take = (int)expression.Value;
//            }
//            else if( node.Method.Name == "Skip" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var expression = (ConstantExpression)StripQuotes( node.Arguments[ 1 ] );

//               _info.Skip = (int)expression.Value;
//            }
//            else
//            {
//               throw new NotSupportedException( $"The method '{node.Method.Name}' is not supported." );
//            }
//         }
//         else if( node.Method.DeclaringType == typeof( InfluxQueryableExtensions ) )
//         {
//            if( node.Method.Name == "GroupByTime" )
//            {
//               Visit( node.Arguments[ 0 ] );

//               var expression = (ConstantExpression)StripQuotes( node.Arguments[ 1 ] );

//               _info.GroupByTime = (TimeSpan)expression.Value;
//            }
//         }

//         return node;
//      }

//      protected override MemberAssignment VisitMemberAssignment( MemberAssignment node )
//      {
//         var targetMember = node.Member;
//         var sourceExpression = node.Expression;
//         var targetToLookForOrOriginalSource = ParameterMemberLocator.Locate( sourceExpression );

//         // determine in the inner column binding, if possible
//         ColumnBinding innerBinding = null;
//         ColumnBinding newBinding = null;
//         if( _currentProjection.InnerProjection != null )
//         {
//            innerBinding = _currentProjection.InnerProjection.Bindings.First( x => x.TargetMember == targetToLookForOrOriginalSource );
//            newBinding = new ColumnBinding( sourceExpression, targetMember, innerBinding );
//         }
//         else
//         {
//            newBinding = new ColumnBinding( sourceExpression, targetMember, targetToLookForOrOriginalSource );
//         }

//         _currentProjection.Bindings.Add( newBinding );

//         return node;
//      }

//      protected override Expression VisitNew( NewExpression node )
//      {
//         for( int i = 0 ; i < node.Arguments.Count ; i++ )
//         {
//            var targetMember = node.Members[ i ];
//            var sourceExpression = node.Arguments[ i ];
//            var targetToLookForOrOriginalSource = ParameterMemberLocator.Locate( sourceExpression );

//            // determine in the inner column binding, if possible
//            ColumnBinding innerBinding = null;
//            ColumnBinding newBinding = null;
//            if( _currentProjection.InnerProjection != null )
//            {
//               innerBinding = _currentProjection.InnerProjection.Bindings.First( x => x.TargetMember == targetToLookForOrOriginalSource );
//               newBinding = new ColumnBinding( sourceExpression, targetMember, innerBinding );
//            }
//            else
//            {
//               newBinding = new ColumnBinding( sourceExpression, targetMember, targetToLookForOrOriginalSource );
//            }

//            _currentProjection.Bindings.Add( newBinding );
//         }

//         return node;
//      }
//   }
//}
