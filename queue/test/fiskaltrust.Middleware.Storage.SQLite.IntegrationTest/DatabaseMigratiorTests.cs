using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.SQLite.IntegrationTest
{
    public class DatabaseMigratiorTests
    {
        [Fact]
        public async Task PerformMigrations_ShouldMigrateDatabase()
        {
            const string path = "exampledb.sqlite";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var databaseMigrator = new DatabaseMigrator(new SqliteConnectionFactory(), path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databaseMigrator.MigrateAsync();

            File.Delete(path);
        }

        [Fact]
        public async Task PerformMigrations_ShouldUpdateDatabase_IfAPreviousVersionExists()
        {
            const string path = "Data/001_Init.sqlite";

            var connectionFactory = new SqliteConnectionFactory();
            var databaseMigrator = new DatabaseMigrator(connectionFactory, path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databaseMigrator.MigrateAsync();

            using (var connection = connectionFactory.GetConnection(connectionFactory.BuildConnectionString(path)))
            {
                var currentVersion = await connection.ExecuteScalarAsync<string>("SELECT CurrentVersion FROM ftDatabaseSchema LIMIT 1");
                currentVersion.Should().NotBeNullOrEmpty();
                string.Compare(currentVersion, "001_Init", true).Should().BePositive();

                var tables = await connection.QueryAsync<string>("SELECT name FROM sqlite_master AS TABLES WHERE TYPE = 'table'");
                tables.Should().Contain("FailedFinishTransaction");
                tables.Should().Contain("FailedStartTransaction");
                tables.Should().Contain("OpenTransaction");
            }

            File.Delete(path);
        }

        [Fact]
        public async Task PerformMigrations_SetWALModeON_WALModeON()
        {

            const string path = "waldb.sqlite";
      
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (File.Exists("waldb.sqlite-shm"))
            {
                File.Delete("waldb.sqlite-shm");
            }
            if (File.Exists("waldb.sqlite-wal"))
            {
                File.Delete("waldb.sqlite-wal");
            }
            var connectionFactory = new SqliteConnectionFactory();
            var config = new Dictionary<string, object>
            {
                { "WAL", "ON" }
            };

            var databaseMigrator = new DatabaseMigrator(connectionFactory, path, config, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databaseMigrator.MigrateAsync();
            await databaseMigrator.SetWALMode();
            var queueItemRepo = new SQLiteQueueItemRepository(connectionFactory, path);
            await queueItemRepo.InsertOrUpdateAsync(new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = Guid.NewGuid(),
                ftQueueRow = 1,
                ftQueueTimeout = 5,
                ftQueueMoment = DateTime.Now,
                cbReceiptMoment = DateTime.Now,
                TimeStamp = DateTime.Now.Ticks
            });
            File.Exists("waldb.sqlite-wal").Should().BeTrue();
        }

        [Fact]
        public async Task PerformMigrations_SetWALModeOFF_WALModeFF()
        {

            const string path = "waldb.sqlite";

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            if (File.Exists("waldb.sqlite-shm"))
            {
                File.Delete("waldb.sqlite-shm");
            }
            if (File.Exists("waldb.sqlite-wal"))
            {
                File.Delete("waldb.sqlite-wal");
            }
            var connectionFactory = new SqliteConnectionFactory();
            var config = new Dictionary<string, object>
            {
                { "WAL", "OFF" }
            };

            var databaseMigrator = new DatabaseMigrator(connectionFactory, path, config, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            await databaseMigrator.MigrateAsync();
            await databaseMigrator.SetWALMode();
            var queueItemRepo = new SQLiteQueueItemRepository(connectionFactory, path);
            await queueItemRepo.InsertOrUpdateAsync(new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = Guid.NewGuid(),
                ftQueueRow = 1,
                ftQueueTimeout = 5,
                ftQueueMoment = DateTime.Now,
                cbReceiptMoment = DateTime.Now,
                TimeStamp = DateTime.Now.Ticks
            });
            File.Exists("waldb.sqlite-wal").Should().BeFalse();

        }
    }
}
