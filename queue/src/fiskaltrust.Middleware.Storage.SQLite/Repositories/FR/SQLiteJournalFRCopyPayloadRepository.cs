﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using fiskaltrust.Middleware.Contracts.Repositories.FR.TempSpace;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.FR
{
    public class SQLiteJournalFRCopyPayloadRepository : AbstractSQLiteRepository<Guid, ftJournalFRCopyPayload>
    {
        public SQLiteJournalFRCopyPayloadRepository(ISqliteConnectionFactory connectionFactory, string path) : base(connectionFactory, path) { }

        public override void EntityUpdated(ftJournalFRCopyPayload entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftJournalFRCopyPayload> GetAsync(Guid id) => 
            await DbConnection.QueryFirstOrDefaultAsync<ftJournalFRCopyPayload>(
                "SELECT * FROM ftJournalFRCopyPayload WHERE QueueItemId = @Id", 
                new { Id = id }
            ).ConfigureAwait(false);

        public override async Task<IEnumerable<ftJournalFRCopyPayload>> GetAsync() => 
            await DbConnection.QueryAsync<ftJournalFRCopyPayload>("SELECT * FROM ftJournalFRCopyPayload").ConfigureAwait(false);

        public async Task<int> JournalFRGetCountOfCopies(string cbPreviousReceiptReference) =>
            await DbConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ftJournalFRCopyPayload WHERE CopiedReceiptReference = @Reference", 
                new { Reference = cbPreviousReceiptReference }
            ).ConfigureAwait(false);

        public async Task InsertAsync(ftJournalFRCopyPayload payload)
        {
            if (await GetAsync(GetIdForEntity(payload)).ConfigureAwait(false) != null)
            {
                throw new Exception("Entity with the same ID already exists.");
            }

            EntityUpdated(payload);
            await DbConnection.ExecuteAsync(
                "INSERT INTO ftJournalFRCopyPayload (QueueId, CashBoxIdentification, Siret, ReceiptId, ReceiptMoment, QueueItemId, CopiedReceiptReference, CertificateSerialNumber) " +
                "Values (@QueueId, @CashBoxIdentification, @Siret, @ReceiptId, @ReceiptMoment, @QueueItemId, @CopiedReceiptReference, @CertificateSerialNumber);", 
                payload
            ).ConfigureAwait(false);
        }

        public async Task<bool> HasJournalFRCopyPayloads() =>
            await DbConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ftJournalFRCopyPayload").ConfigureAwait(false) > 0;

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;
    }
}
