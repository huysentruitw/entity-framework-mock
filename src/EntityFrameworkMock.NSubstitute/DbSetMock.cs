/*
 * Copyright 2017-2018 Paul Michaels, Wouter Huysentruit
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;

namespace EntityFrameworkMock.NSubstitute
{
    public class DbSetMock<TEntity> : IDbSetMock
        where TEntity : class
    {
        private readonly DbSetBackingStore<TEntity> _store;

        public DbSet<TEntity> Object { get; }

        public DbSetMock(IEnumerable<TEntity> initialEntities, Func<TEntity, KeyContext, object> keyFactory, bool asyncQuerySupport = true)
        {
            _store = new DbSetBackingStore<TEntity>(initialEntities, keyFactory);

            var data = _store.GetDataAsQueryable();
            Object = Substitute.For<DbSet<TEntity>, IQueryable<TEntity>, IDbAsyncEnumerable<TEntity>>();

            ((IQueryable<TEntity>)Object).Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            Object.AsQueryable().Provider.Returns(asyncQuerySupport ? new DbAsyncQueryProvider<TEntity>(data.Provider) : data.Provider);
            Object.AsQueryable().Expression.Returns(data.Expression);
            Object.AsQueryable().ElementType.Returns(data.ElementType);
            ((IQueryable<TEntity>)Object).GetEnumerator().Returns(_ => data.GetEnumerator());
            ((IEnumerable)Object).GetEnumerator().Returns(_ => data.GetEnumerator());

            if (asyncQuerySupport)
            {
                ((IDbAsyncEnumerable<TEntity>)Object).GetAsyncEnumerator().Returns(_ => new DbAsyncEnumerator<TEntity>(data.GetEnumerator()));
            }

            Object.AsNoTracking().Returns(Object);
            Object.Include(Arg.Any<string>()).Returns(Object);
            Object.AsNoTracking().Include(Arg.Any<string>()).Returns(Object);

            Object.When(a => a.Add(Arg.Any<TEntity>())).Do(x => _store.Add(x.ArgAt<TEntity>(0)));
            Object.When(a => a.AddRange(Arg.Any<IEnumerable<TEntity>>())).Do(x => _store.Add(x.ArgAt<IEnumerable<TEntity>>(0)));
            Object.When(a => a.Remove(Arg.Any<TEntity>())).Do(x => _store.Remove(x.ArgAt<TEntity>(0)));
            Object.When(a => a.RemoveRange(Arg.Any<IEnumerable<TEntity>>())).Do(x => _store.Remove(x.ArgAt<IEnumerable<TEntity>>(0)));

            Object.Find(Arg.Any<object[]>()).Returns(info => _store.Find(info.Args()[0] as object[]));
            Object.FindAsync(Arg.Any<CancellationToken>(), Arg.Any<object[]>()).Returns(info => Task.FromResult(_store.Find(info.Args()[1] as object[])));

            _store.UpdateSnapshot();
        }

        public event EventHandler<SavedChangesEventArgs<TEntity>> SavedChanges;

        int IDbSetMock.SaveChanges()
        {
            var changes = _store.ApplyChanges();
            SavedChanges?.Invoke(this, new SavedChangesEventArgs<TEntity> { UpdatedEntities = _store.GetUpdatedEntities() });
            _store.UpdateSnapshot();
            return changes;
        }
    }
}
