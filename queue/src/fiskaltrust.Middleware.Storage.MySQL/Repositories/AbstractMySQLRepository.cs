using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories
{
    public abstract class AbstractMySQLRepository<TKey, T> : IMiddlewareRepository<T> where T : new()
    {
        protected string ConnectionString { get; }

        public AbstractMySQLRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected abstract TKey GetIdForEntity(T entity);

        public abstract void EntityUpdated(T entity);

        public abstract Task<T> GetAsync(TKey id);

        public abstract Task<IEnumerable<T>> GetAsync();

        public async IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<T>($"SELECT * FROM {typeof(T).Name} WHERE TimeStamp >= @from AND TimeStamp <= @to  ORDER BY TimeStamp", new { from = fromInclusive, to = toInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }

        public async IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var query = $"SELECT * FROM {typeof(T).Name} WHERE TimeStamp >= @from ORDER BY TimeStamp";
            if (take.HasValue)
            {
                query += $" LIMIT {take}";
            }
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await foreach (var entry in connection.Query<T>(query, new { from = fromInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
                {
                    yield return entry;
                }
            }
        }
    }
}
