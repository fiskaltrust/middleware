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
    public class AzureTableStorageFailedStartTransactionRepositoryTests : AbstractFailedStartTransactionRepositoryTests, IClassFixture<AzureTableStorageFixture>
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageFailedStartTransactionRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateReadOnlyRepository(IEnumerable<FailedStartTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateRepository(IEnumerable<FailedStartTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureTableStorageFailedStartTransactionRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(FailedStartTransaction));
    }
}
