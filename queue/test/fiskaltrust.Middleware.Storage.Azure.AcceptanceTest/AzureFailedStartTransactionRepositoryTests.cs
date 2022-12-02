using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;
using Xunit;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    //[Collection(nameof(AzureStorageFixture))]
    public class AzureFailedStartTransactionRepositoryTests : AbstractFailedStartTransactionRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureFailedStartTransactionRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateReadOnlyRepository(IEnumerable<FailedStartTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateRepository(IEnumerable<FailedStartTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureFailedStartTransactionRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }
    }
}
