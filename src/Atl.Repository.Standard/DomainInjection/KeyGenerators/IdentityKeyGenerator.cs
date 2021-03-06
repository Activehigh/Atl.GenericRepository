﻿using System;
using System.Linq.Expressions;
using Atl.Repository.Standard.DomainInjection.Contracts;
using Atl.Repository.Standard.Domains.Contracts;

namespace Atl.Repository.Standard.DomainInjection.KeyGenerators
{
    /// <summary>
    /// A identity generator calss, however the values are generated from database so calling the individual methods does not
    /// generate a new identity key
    /// </summary>
    /// <seealso cref="int" />
    public class IdentityKeyGenerator : IKeyGenerator<int>
    {
        internal class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return base.VisitParameter(_parameter);
            }

            internal ParameterReplacer(ParameterExpression parameter)
            {
                _parameter = parameter;
            }
        }
        /// <summary>
        /// Generates this instance.
        /// </summary>
        /// <returns></returns>
        public int Generate(IDomain<int> obj)
        {
            return 0;
        }

        /// <summary>
        /// Doeses the require new key.
        /// </summary>
        /// <param name="currentKey">The current key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool DoesRequireNewKey(int currentKey)
        {
            //identity keys are generated by database
            return false;
        }

        /// <summary>
        /// Equals the specified left.
        /// </summary>
        /// <typeparam name="TDomain">The type of the domain.</typeparam>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public Expression<Func<TDomain, bool>> Equal<TDomain>(Expression<Func<TDomain, int>> left, int right) where TDomain : IDomain<int>
        {
            var member = left.Body as MemberExpression;
            var boolFuncType = typeof(Func<,>).MakeGenericType(typeof(TDomain), typeof(bool));
            var invokedExp = Expression.Invoke((Expression<Func<int, bool>>)(a => a == right), member);
            var searchExp = Expression.Lambda(boolFuncType, invokedExp, left.Parameters);
            return Expression.Lambda<Func<TDomain, bool>>(searchExp.Body, left.Parameters);
        }
    }
}
