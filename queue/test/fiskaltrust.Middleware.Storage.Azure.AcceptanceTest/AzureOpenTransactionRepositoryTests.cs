using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;
using fiskaltrust.storage.V0.MasterData;
using Xunit;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureOpenTransactionRepositoryTests : AbstractOpenTransactionRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureOpenTransactionRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;
        
        public override Task<IPersistentTransactionRepository<OpenTransaction>> CreateReadOnlyRepository(IEnumerable<OpenTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<OpenTransaction>> CreateRepository(IEnumerable<OpenTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureOpenTransactionRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(OpenTransaction));
    }
}
