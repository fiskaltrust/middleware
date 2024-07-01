using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    [Collection("AzureTableStorageCollection")]
    public class AzureTableStorageOpenTransactionRepositoryTests : AbstractOpenTransactionRepositoryTests
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageOpenTransactionRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override Task<IPersistentTransactionRepository<OpenTransaction>> CreateReadOnlyRepository(IEnumerable<OpenTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<OpenTransaction>> CreateRepository(IEnumerable<OpenTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureTableStorageOpenTransactionRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(AzureTableStorageOpenTransactionRepository.TABLE_NAME);
    }
}
