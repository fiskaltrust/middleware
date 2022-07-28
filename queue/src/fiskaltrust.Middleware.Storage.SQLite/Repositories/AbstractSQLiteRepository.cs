using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public abstract class AbstractSQLiteRepository<TKey, T> : IMiddlewareRepository<T> where T : new()
    {
        private readonly string _connectionString;

        private readonly IDbConnection _dbConnection;
        protected IDbConnection DbConnection
        {
            get
            {
                if(_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }
                return _dbConnection;
            }
        }

        public AbstractSQLiteRepository(ISqliteConnectionFactory connectionFactory, string path, bool read = false)
        {
            if (read)
            {

                _connectionString = $"Data Source={path};Version=3;Read Only=True;";
            }
            else
            {
                _connectionString = connectionFactory.BuildConnectionString(path);
            }

            _dbConnection = connectionFactory.GetNewConnection(_connectionString);
        }

        protected abstract TKey GetIdForEntity(T entity);

        public abstract void EntityUpdated(T entity);

        public abstract Task<T> GetAsync(TKey id);

        public abstract Task<IEnumerable<T>> GetAsync();

        public virtual async IAsyncEnumerable<T> GetByTimeStampRangeAsync(long fromInclusive, long toInclusive)
        {
            await foreach (var entry in DbConnection.Query<T>($"SELECT * FROM {typeof(T).Name} WHERE TimeStamp >= @from AND TimeStamp <= @to  ORDER BY TimeStamp", new { from = fromInclusive, to = toInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }

        public virtual async IAsyncEnumerable<T> GetEntriesOnOrAfterTimeStampAsync(long fromInclusive, int? take = null)
        {
            var query = $"SELECT * FROM {typeof(T).Name} WHERE TimeStamp >= @from ORDER BY TimeStamp";
            if (take.HasValue)
            {
                query += $" LIMIT {take}";
            }
            await foreach (var entry in DbConnection.Query<T>(query, new { from = fromInclusive }, buffered: false).ToAsyncEnumerable().ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }
}
