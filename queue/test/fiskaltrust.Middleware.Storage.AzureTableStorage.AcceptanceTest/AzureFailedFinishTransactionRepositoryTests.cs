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
    public class AzureFailedFinishTransactionRepositoryTests : AbstractFailedFinishTransactionRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureFailedFinishTransactionRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateReadOnlyRepository(IEnumerable<FailedFinishTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateRepository(IEnumerable<FailedFinishTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureFailedFinishTransactionRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(FailedFinishTransaction));
    }
}
