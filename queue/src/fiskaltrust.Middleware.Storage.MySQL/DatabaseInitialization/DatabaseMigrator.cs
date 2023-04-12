using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.MySQL.Extensions;
using fiskaltrust.Middleware.Storage.MySQL.TypeHandlers;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace fiskaltrust.Middleware.Storage.MySQL.DatabaseInitialization
{
    public class DatabaseMigrator
    {
        private const string MIGRATION_DIR = "Migrations";

        private readonly string _serverConnectionString;
        private readonly string _databaseConnectionString;
        //QueueId is used as dbName
        private readonly string _dbName;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;

        public DatabaseMigrator(string serverConnectionString, uint timeoutSec, Guid queueId, ILogger<IMiddlewareBootstrapper> logger)
        {
            _dbName = queueId.ToString().Replace("-", string.Empty);
            _serverConnectionString = serverConnectionString;
            var builder = new MySqlConnectionStringBuilder
            {
                ConnectionString = _serverConnectionString,
                Database = _dbName,
                DefaultCommandTimeout = timeoutSec,
                AllowUserVariables = true
            };
            _databaseConnectionString = builder.ConnectionString;
            _logger = logger;
            SqlMapper.RemoveTypeMap(typeof(DateTime?));
            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(Guid?), new NullableGuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(DateTime), new MySQLDateTimeTypeHandler());
            SqlMapper.AddTypeHandler(typeof(DateTime?), new MySQLNullableDateTimeTypeHandler());
        }

        public async Task<string> MigrateAsync()
        {
            var parentPath = typeof(DatabaseMigrator).Assembly.GetDirectoryPath();
            var migrations = Directory.GetFiles(Path.Combine(parentPath, MIGRATION_DIR), "*.mysql").OrderBy(x => x);


            _logger.LogDebug($"Found {migrations.Count()} migration files.");
            using (var connection = new MySqlConnection(_serverConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync($"CREATE DATABASE IF NOT EXISTS `{_dbName}`; USE `{_dbName}`;").ConfigureAwait(false);
            }

            using (var connection = new MySqlConnection(_databaseConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                var currentVersion = await GetCurrentVersionAsync(connection, _dbName).ConfigureAwait(false);
                var notAppliedMigrations = migrations.Where(x => string.Compare(Path.GetFileNameWithoutExtension(x), currentVersion, true) > 0);

                if (notAppliedMigrations.Any())
                {
                    _logger.LogInformation($"{notAppliedMigrations.Count()} pending database updates were detected. Updating database now.");
                }
                foreach (var migrationScript in notAppliedMigrations)
                {
                    var text = File.ReadAllText(migrationScript);
                    if (text.Substring(0, 14).Equals("--Queue needed"))
                    {
                        text = text.Replace("QueueNeeded",$"'{_dbName}'");
                        text = text.Remove(0, 15);
                    }
                    _logger.LogDebug($"Updating database with migration script {migrationScript}..");
                    await connection.ExecuteAsync(text).ConfigureAwait(false);
                    await SetCurrentVersionAsync(connection, Path.GetFileNameWithoutExtension(migrationScript)).ConfigureAwait(false);
                    _logger.LogDebug($"Applying the migration script was successful. Set current version to {Path.GetFileNameWithoutExtension(migrationScript)}.");
                }
            }
            return _dbName;
        }

        private async Task<string> GetCurrentVersionAsync(IDbConnection connection, string dbName)
        {
            var tableCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='{dbName}' AND TABLE_NAME='ftDatabaseSchema' ").ConfigureAwait(false);
            return tableCount == 0 ? null : await connection.ExecuteScalarAsync<string>("SELECT CurrentVersion FROM ftDatabaseSchema LIMIT 1").ConfigureAwait(false);
        }

        private async Task SetCurrentVersionAsync(IDbConnection connection, string version)
        {
            var rowCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ftDatabaseSchema").ConfigureAwait(false);
            if (rowCount == 0)
            {
                await connection.ExecuteAsync("INSERT INTO ftDatabaseSchema (CurrentVersion) VALUES (@version)", new { version }).ConfigureAwait(false);
            }
            else
            {
                await connection.ExecuteAsync("SET SQL_SAFE_UPDATES = 0;UPDATE ftDatabaseSchema SET CurrentVersion = @version;SET SQL_SAFE_UPDATES = 1;", new { version }).ConfigureAwait(false);
            }
        }
    }
}
