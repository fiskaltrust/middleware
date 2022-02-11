using System.Data;

namespace fiskaltrust.Middleware.Storage.SQLite
{
    public interface ISqliteConnectionFactory
    {
        IDbConnection GetConnection(string connectionString);
        string BuildConnectionString(string path);
        IDbConnection GetNewConnection(string connectionString);
    }
}
