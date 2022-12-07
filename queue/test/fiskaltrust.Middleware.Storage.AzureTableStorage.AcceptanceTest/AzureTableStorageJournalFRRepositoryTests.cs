﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public class AzureTableStorageJournalFRRepositoryTests : AbstractJournalFRRepositoryTests, IClassFixture<AzureTableStorageFixture>
    {
        private readonly AzureTableStorageFixture _fixture;

        public AzureTableStorageJournalFRRepositoryTests(AzureTableStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalFRRepository> CreateReadOnlyRepository(IEnumerable<ftJournalFR> entries) => await CreateRepository(entries);

        public override async Task<IJournalFRRepository> CreateRepository(IEnumerable<ftJournalFR> entries)
        {
            var azureJournalFRRepository = new AzureTableStorageJournalFRRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalFRRepository.InsertAsync(entry);
            }

            return azureJournalFRRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftJournalFR));
    }
}
