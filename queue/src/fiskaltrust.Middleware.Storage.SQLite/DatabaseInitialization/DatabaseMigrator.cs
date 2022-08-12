using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.Extensions;
using fiskaltrust.Middleware.Storage.SQLite.TypeHandlers;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization
{
    public class DatabaseMigrator
    {
        private const string MIGRATION_DIR = "Migrations";
        private const string MIGRATIONS_CONFIG_KEY = "migrationDirectory";

        private readonly string _connectionString;
        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly Dictionary<string, object> _configuration;
        private readonly ILogger<IMiddlewareBootstrapper> _logger;

        public DatabaseMigrator(ISqliteConnectionFactory connectionFactory, string path, Dictionary<string, object> configuration, ILogger<IMiddlewareBootstrapper> logger)
        {
            _connectionString = connectionFactory.BuildConnectionString(path);
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _logger = logger;

            SqlMapper.RemoveTypeMap(typeof(DateTime?));
            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            SqlMapper.AddTypeHandler(typeof(Guid), new GuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(Guid?), new NullableGuidTypeHandler());
            SqlMapper.AddTypeHandler(typeof(DateTime), new SQLiteDateTimeTypeHandler());
            SqlMapper.AddTypeHandler(typeof(DateTime?), new SQLiteNullableDateTimeTypeHandler());
        }

        public async Task MigrateAsync()
        {
            var migrationDir = _configuration.TryGetValue(MIGRATIONS_CONFIG_KEY, out var value)
                ? value.ToString()
                : Path.Combine(typeof(DatabaseMigrator).Assembly.GetDirectoryPath(), MIGRATION_DIR);

            var migrations = Directory.GetFiles(migrationDir, "*.sqlite3").OrderBy(x => x);

            _logger.LogDebug($"Found {migrations.Count()} migration files.");

            using (var connection = _connectionFactory.GetNewConnection(_connectionString))
            {
                var currentVersion = await GetCurrentVersionAsync(connection).ConfigureAwait(false);
                var notAppliedMigrations = migrations.Where(x => string.Compare(Path.GetFileNameWithoutExtension(x), currentVersion, true) > 0);

                if (notAppliedMigrations.Any())
                {
                    _logger.LogInformation($"{notAppliedMigrations.Count()} pending database updates were detected. Updating database now.");
                }
                foreach (var migrationScript in notAppliedMigrations)
                {
                    _logger.LogDebug($"Updating database with migration script {migrationScript}..");
                    await connection.ExecuteAsync(File.ReadAllText(migrationScript)).ConfigureAwait(false);
                    await SetCurrentVersionAsync(connection, Path.GetFileNameWithoutExtension(migrationScript)).ConfigureAwait(false);
                    _logger.LogDebug($"Applying the migration script was successful. Set current version to {Path.GetFileNameWithoutExtension(migrationScript)}.");
                }
            }
        }

        public async Task SetWALMode()
        {
            if (_configuration.TryGetValue("EnableWAL", out var value) && value != null && bool.TryParse(value?.ToString(), out var walEnabled))
            {
                if (walEnabled)
                {
                    using (var connection = _connectionFactory.GetNewConnection(_connectionString))
                    {
                        await connection.ExecuteAsync("PRAGMA journal_mode=WAL;").ConfigureAwait(false);
                        _logger.LogDebug($"SQLite WAL mode enabled.");
                    }
                }
                else
                {
                    using (var connection = _connectionFactory.GetNewConnection(_connectionString))
                    {
                        await connection.ExecuteAsync("PRAGMA journal_mode=DELETE;").ConfigureAwait(false);
                        _logger.LogDebug($"SQLite WAL mode disabled.");
                    }
                }
            }
        }

        private async Task<string> GetCurrentVersionAsync(IDbConnection connection)
        {
            var tableCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master AS TABLES WHERE TYPE = 'table' and name = 'ftDatabaseSchema'").ConfigureAwait(false);
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
                await connection.ExecuteAsync("UPDATE ftDatabaseSchema SET CurrentVersion = @version", new { version }).ConfigureAwait(false);
            }
        }
    }
}
