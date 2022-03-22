using System.Data;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteDatabaseCleanUp : IDatabaseCleanUp
    {
        private readonly IDbConnection _dbConnection;

        public SQLiteDatabaseCleanUp(ISqliteConnectionFactory connectionFactory, string path)
        {
            var connectionString = connectionFactory.BuildConnectionString(path);
            _dbConnection = connectionFactory.GetConnection(connectionString);
        }

        private IDbConnection DbConnection
        {
            get
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }
                return _dbConnection;
            }
        }
        public async Task Vacuum()
        {
            _ = await DbConnection.ExecuteAsync("VACUUM;").ConfigureAwait(false);
        }
    }
}
