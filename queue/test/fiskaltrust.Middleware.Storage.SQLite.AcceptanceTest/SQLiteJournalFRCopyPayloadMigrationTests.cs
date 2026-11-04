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
    /// Migration 016 re-keys ftJournalFRCopyPayload on QueueItemId (market-fr#235). The migrator runs
    /// a script and records its version in two steps, so 016 must be safe to run again after it was
    /// interrupted.
    /// </summary>
    public class SQLiteJournalFRCopyPayloadMigrationTests : IDisposable
    {
        private const string BeforeMigration016 = "015_QueueFRV2Chains";

        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Guid.NewGuid().ToString());
        private readonly SqliteConnectionFactory _connectionFactory = new SqliteConnectionFactory();

        private Task MigrateAsync() => new DatabaseMigrator(_connectionFactory, 30 * 60, _path, new Dictionary<string, object>(), Mock.Of<ILogger<IMiddlewareBootstrapper>>()).MigrateAsync();

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
            await MigrateAsync();
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
            await MigrateAsync();
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

        public void Dispose()
        {
            _connectionFactory.Dispose();
            if (File.Exists(_path))
            {
                FileHelpers.WaitUntilFileIsAccessible(_path);
                File.Delete(_path);
            }
        }
    }
}
