﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT;
using fiskaltrust.storage.V0;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public class AzureJournalATRepositoryTests : AbstractJournalATRepositoryTests, IClassFixture<AzureStorageFixture>
    {
        private readonly AzureStorageFixture _fixture;

        public AzureJournalATRepositoryTests(AzureStorageFixture fixture) => _fixture = fixture;

        public override async Task<IReadOnlyJournalATRepository> CreateReadOnlyRepository(IEnumerable<ftJournalAT> entries) => await CreateRepository(entries);

        public override async Task<IJournalATRepository> CreateRepository(IEnumerable<ftJournalAT> entries)
        {
            var azureJournalATRepository = new AzureJournalATRepository(new QueueConfiguration { QueueId = _fixture.QueueId }, new TableServiceClient(Constants.AzureStorageConnectionString));
            foreach (var entry in entries)
            {
                await azureJournalATRepository.InsertAsync(entry);
            }

            return azureJournalATRepository;
        }

        public override void DisposeDatabase() => _fixture.CleanTable(nameof(ftJournalAT));
    }
}