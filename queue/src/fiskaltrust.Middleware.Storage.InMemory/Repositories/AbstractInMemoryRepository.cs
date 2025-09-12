using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public abstract class AbstractInMemoryRepository<TKey, T> : IMiddlewareRepository<T> where TKey : IEquatable<TKey>
    {
        protected ConcurrentDictionary<TKey, T> Data { get; }
        public AbstractInMemoryRepository() { }

        public AbstractInMemoryRepository(IEnumerable<T> seed) => Data = new ConcurrentDictionary<TKey, T>(seed?.ToDictionary(x => GetIdForEntity(x), x => x));

        protected abstract TKey GetIdForEntity(T entity);

        protected abstract void EntityUpdated(T entity);

        public virtual IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => Data.Values.ToAsyncEnumerable();

        public virtual IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null) => Data.Values.ToAsyncEnumerable();

        public virtual async Task<IEnumerable<T>> GetAsync() => await Task.FromResult(Data.Values.AsEnumerable()).ConfigureAwait(false);

        public virtual async Task<T> GetAsync(TKey id) => await Task.FromResult(Data.Values.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(GetIdForEntity(x), id))).ConfigureAwait(false);

        public virtual async Task InsertAsync(T entity)
        {
            var entityId = GetIdForEntity(entity);
            EntityUpdated(entity);
            if (!Data.TryAdd(entityId, entity))
            {
                throw new Exception("The key already exists");
            }

            await Task.Yield();
        }

        public async Task InsertOrUpdateAsync(T entity)
        {
            var entityId = GetIdForEntity(entity);
            EntityUpdated(entity);
            Data[entityId] = entity;
            await Task.Yield();
        }
    }
}