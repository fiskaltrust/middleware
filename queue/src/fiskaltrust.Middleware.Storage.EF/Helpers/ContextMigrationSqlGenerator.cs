using System.Data.Common;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace fiskaltrust.Middleware.Storage.EF.Helpers
{
    public class ContextMigrationSqlGenerator : SqlServerMigrationSqlGenerator
    {
        private readonly string _schema;
        private readonly string _connection;

        public ContextMigrationSqlGenerator(string connectionString, string schemaString)
        {
            _schema = schemaString;
            _connection = connectionString;
        }

        protected override string Name(string name)
        {
            var p = name.IndexOf('.');
            if (p > 0)
            {
                name = name.Substring(p + 1);
            }
            return $"[{_schema}].[{name}]";
        }

        protected override DbConnection CreateConnection()
        {
            var sqlConnection = new SqlConnection(_connection);
            sqlConnection.Open();

            using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = @$"IF NOT EXISTS (
SELECT schema_name
FROM information_schema.schemata
WHERE schema_name = '{_schema}')
BEGIN
EXEC sp_executesql N'CREATE SCHEMA [{_schema}]'
END";
                command.ExecuteNonQuery();
            }

            return sqlConnection;
        }
    }
}