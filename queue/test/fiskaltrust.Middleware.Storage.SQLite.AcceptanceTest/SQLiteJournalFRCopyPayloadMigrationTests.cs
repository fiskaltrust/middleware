using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Storage.SQLite.Connection;
using fiskaltrust.Middleware.Storage.SQLite.DatabaseInitialization;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.FR;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.SQLite.AcceptanceTest
{
    /// <summary>
    /// Migration 016 re-keys ftJournalFRCopyPayload on QueueItemId (market-fr#235). It must be safe to
    /// run again after an interruption, and so must the QueueFR v2 migrations around it: each records
    /// its version in its own transaction, so one that committed is not replayed.
    /// </summary>
    public class SQLiteJournalFRCopyPayloadMigrationTests : IDisposable
    {
        private const string BeforeMigration016 = "015_QueueFRV2Chains";
        private const string Migration016 = "016_ftJournalFRCopyPayloadKey";

        private static readonly string MigrationDir = Path.Combine(AppContext.BaseDirectory, "Migrations");

        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private readonly string _partialMigrationDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly SqliteConnectionFactory _connectionFactory = new SqliteConnectionFactory();

        private Task MigrateAsync() => MigrateAsync(new Dictionary<string, object>());

        /// <summary>Migrates as a queue whose release shipped the scripts up to <paramref name="lastScript"/> would.</summary>
        private Task MigrateUpToAsync(string lastScript)
        {
            Directory.CreateDirectory(_partialMigrationDir);
            foreach (var script in Directory.GetFiles(MigrationDir, "*.sqlite3").Where(x => string.Compare(Path.GetFileNameWithoutExtension(x), lastScript, StringComparison.OrdinalIgnoreCase) <= 0))
            {
                File.Copy(script, Path.Combine(_partialMigrationDir, Path.GetFileName(script)), true);
            }
            return MigrateAsync(new Dictionary<string, object> { ["migrationDirectory"] = _partialMigrationDir });
        }

        private Task MigrateAsync(Dictionary<string, object> configuration) => new DatabaseMigrator(_connectionFactory, 30 * 60, _path, configuration, Mock.Of<ILogger<IMiddlewareBootstrapper>>()).MigrateAsync();

        private Task<string> CurrentVersionAsync() => _connectionFactory.GetConnection(_connectionFactory.BuildConnectionString(_path)).ExecuteScalarAsync<string>("SELECT CurrentVersion FROM ftDatabaseSchema LIMIT 1");

        private Task<int> ExecuteAsync(string sql, object param = null) => _connectionFactory.GetConnection(_connectionFactory.BuildConnectionString(_path)).ExecuteAsync(sql, param);

        private static ftJournalFRCopyPayload Copy(Guid queueId, string reference = "R-1") => new ftJournalFRCopyPayload
        {
            QueueId = queueId,
            QueueItemId = Guid.NewGuid(),
            CashBoxIdentification = "cashbox",
            Siret = "",
            ReceiptId = "",
            ReceiptMoment = DateTime.UtcNow,
            CopiedReceiptReference = reference,
            CertificateSerialNumber = "",
        };

        [Fact]
        public async Task AnInterruptedRun_ThatLeftItsTemporaryTable_RunsAgain()
        {
            await MigrateUpToAsync(Migration016);
            // Back to the 012 table with one v1.2 copy, plus what a run stopped after its CREATE TABLE leaves behind.
            var v12Copy = Guid.NewGuid();
            await ExecuteAsync("DROP TABLE ftJournalFRCopyPayload;");
            await ExecuteAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migrations", "012_ftJournalFRCopyPayload.sqlite3")));
            await ExecuteAsync("INSERT INTO ftJournalFRCopyPayload (QueueId, CashBoxIdentification, Siret, ReceiptId, ReceiptMoment, QueueItemId, CopiedReceiptReference, CertificateSerialNumber, TimeStamp) " +
                               "VALUES (@QueueId, 'cashbox', 'siret', 'ft1#D1', 0, @QueueItemId, 'R-1', 'csn', 1);", new { QueueId = Guid.NewGuid().ToString(), QueueItemId = v12Copy.ToString() });
            await ExecuteAsync("CREATE TABLE ftJournalFRCopyPayload_016 (QueueItemId NVARCHAR(36));");
            await ExecuteAsync("UPDATE ftDatabaseSchema SET CurrentVersion = @Version;", new { Version = BeforeMigration016 });

            await MigrateAsync();

            var repository = new SQLiteJournalFRCopyPayloadRepository(_connectionFactory, _path);
            var kept = await repository.GetAsync(v12Copy);
            kept.ReceiptId.Should().Be("ft1#D1");
            kept.QueueRow.Should().BeNull("a v1.2 row has no queue row");
            var queueId = Guid.NewGuid();
            await repository.InsertAsync(Copy(queueId));
            await repository.InsertAsync(Copy(queueId));
            (await repository.GetCopiesAsync("R-1")).Should().HaveCount(3);
        }

        [Fact]
        public async Task ARunWhoseVersionWasNotRecorded_RunsAgainAndKeepsTheRows()
        {
            await MigrateUpToAsync(Migration016);
            var repository = new SQLiteJournalFRCopyPayloadRepository(_connectionFactory, _path);
            var queueId = Guid.NewGuid();
            var copies = new[] { Copy(queueId), Copy(queueId) };
            foreach (var copy in copies)
            {
                await repository.InsertAsync(copy);
            }
            await ExecuteAsync("UPDATE ftDatabaseSchema SET CurrentVersion = @Version;", new { Version = BeforeMigration016 });

            await MigrateAsync();

            (await repository.GetCopiesAsync("R-1")).Select(x => x.QueueItemId).Should().BeEquivalentTo(copies.Select(x => x.QueueItemId));
        }

        [Theory]
        [InlineData("015_Poland", "015_QueueFRV2Chains")]
        [InlineData("015_QueueFRV2Chains", "016_ftJournalFRCopyPayloadKey")]
        [InlineData("016_ftJournalFRCopyPayloadKey", "017_QueueFRArchiveChain")]
        [InlineData("017_QueueFRArchiveChain", "018_QueueFRStateV2")]
        public async Task AScriptThatCommitted_BeforeTheMigratorRecordedIt_IsNotReplayed(string previous, string script)
        {
            await MigrateUpToAsync(previous);
            // The script ran and committed; the migrator stopped before its own version update.
            await ExecuteAsync(File.ReadAllText(Path.Combine(MigrationDir, script + ".sqlite3")));

            (await CurrentVersionAsync()).Should().Be(script);
            await MigrateAsync();

            (await CurrentVersionAsync()).Should().Be(Path.GetFileNameWithoutExtension(Directory.GetFiles(MigrationDir, "*.sqlite3").Where(x => !x.EndsWith(".legacy.sqlite3")).OrderBy(x => x).Last()));
        }

        public void Dispose()
        {
            if (Directory.Exists(_partialMigrationDir))
            {
                Directory.Delete(_partialMigrationDir, true);
            }
            _connectionFactory.Dispose();
            if (File.Exists(_path))
            {
                FileHelpers.WaitUntilFileIsAccessible(_path);
                File.Delete(_path);
            }
        }
    }
}
