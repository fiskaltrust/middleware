using System;
using System.Data;

namespace fiskaltrust.Middleware.Storage.SQLite.Connection
{
    public sealed class SqliteConnectionFactory : ISqliteConnectionFactory, IDisposable
    {
        private bool _disposed = false;

#if NETSTANDARD || NET6_0_OR_GREATER
        private Microsoft.Data.Sqlite.SqliteConnection _sqliteConnection;
#else
        private System.Data.SQLite.SQLiteConnection _sqliteConnection;
#endif

        public string BuildConnectionString(string path)
        {
#if NETSTANDARD || NET6_0_OR_GREATER
            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = path,
                Pooling = false
            };
#else
            var builder = new System.Data.SQLite.SQLiteConnectionStringBuilder
            {
                DataSource = path,
                Version = 3
            };
#endif
            return builder.ConnectionString;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for methods")]
        public IDbConnection GetConnection(string connectionString)
        {
#if NETSTANDARD || NET6_0_OR_GREATER
            if (_sqliteConnection == null)
            {
                _sqliteConnection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            }
            return _sqliteConnection;
#else
            if (_sqliteConnection == null)
            {
                _sqliteConnection = new System.Data.SQLite.SQLiteConnection(connectionString);
            }
            return _sqliteConnection;
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for methods")]
        public IDbConnection GetNewConnection(string connectionString)
        {
#if NETSTANDARD || NET6_0_OR_GREATER
            return new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
#else
            return new System.Data.SQLite.SQLiteConnection(connectionString);
#endif
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
#if NETSTANDARD || NET6_0_OR_GREATER
                    if (_sqliteConnection != null)
                    {
                        _sqliteConnection.Dispose();
                    }
#else
                    if (_sqliteConnection != null)
                    {
                        _sqliteConnection.Dispose();
                    }
#endif
                }
                _disposed = true;
            }
        }
    }
}
