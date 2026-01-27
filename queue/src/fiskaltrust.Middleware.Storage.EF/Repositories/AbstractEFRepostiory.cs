using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Storage.EF.Repositories
{
    public abstract class AbstractEFRepostiory<TKey, T> : IMiddlewareRepository<T>
        where T : class
        where TKey : IEquatable<TKey>
    {
        protected MiddlewareDbContext DbContext { get; }

        public AbstractEFRepostiory(MiddlewareDbContext dbContext)
        {
            DbContext = dbContext;
            DbContext.Database.CommandTimeout = dbContext.CommandTimeoutSeconds;
        }

        protected abstract TKey GetIdForEntity(T entity);

        protected abstract void EntityUpdated(T entity);

        public virtual IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive) => DbContext.Set<T>().ToAsyncEnumerable();

        public virtual IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null) => DbContext.Set<T>().ToAsyncEnumerable();

        public virtual async Task<IEnumerable<T>> GetAsync() => await Task.FromResult(DbContext.Set<T>().AsEnumerable()).ConfigureAwait(false);

        public virtual async Task<T> GetAsync(TKey id) => await Task.FromResult(DbContext.Set<T>().Find(id)).ConfigureAwait(false);

        public virtual async Task InsertAsync(T entity)
        {
            var id = GetIdForEntity(entity);
            if (await GetAsync(id).ConfigureAwait(false) != null)
            {
                throw new Exception($"Entity with id {id} already exists");
            }
            EntityUpdated(entity);
            DbContext.Set<T>().Add(entity);
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task InsertOrUpdateAsync(T entity)
        {
            var id = GetIdForEntity(entity);
            var entityWithId = await GetAsync(id).ConfigureAwait(false);
            if (entityWithId == null)
            {
                EntityUpdated(entity);
                DbContext.Set<T>().Add(entity);
            }
            else
            {
                var local = DbContext.Set<T>().Local.SingleOrDefault(x => EqualityComparer<TKey>.Default.Equals(GetIdForEntity(x), GetIdForEntity(entity)) && x != entity);
                if (local != null)
                {
                    DbContext.Entry(local).State = EntityState.Detached;
                }

                EntityUpdated(entity);
                DbContext.Set<T>().Attach(entity);
                DbContext.Entry(entity).State = EntityState.Modified;
            }

            DbContext.SaveChanges();
        }

        public async Task RemoveAll()
        {
            DbContext.Set<T>().RemoveRange(DbContext.Set<T>());
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<T> RemoveAsync(TKey id)
        {
            var entity = await GetAsync(id).ConfigureAwait(false);
            if (entity != null)
            {
                DbContext.Set<T>().Remove(entity);
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return entity;
        }
    }
}
