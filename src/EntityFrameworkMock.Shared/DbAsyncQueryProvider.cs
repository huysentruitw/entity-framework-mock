﻿using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EntityFrameworkMock
{
    // From https://msdn.microsoft.com/en-us/library/dn314429(v=vs.113).aspx
    public class DbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public DbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression m:
                {
                    var resultType = m.Method.ReturnType;
                    var genericElement = resultType.GetGenericArguments()[0];
                    var queryType = typeof(DbAsyncEnumerable<>).MakeGenericType(genericElement);
                    return (IQueryable)Activator.CreateInstance(queryType, expression);
                }
            }

            return new DbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new DbAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute(expression));

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => Task.FromResult(Execute<TResult>(expression));
    }
}
