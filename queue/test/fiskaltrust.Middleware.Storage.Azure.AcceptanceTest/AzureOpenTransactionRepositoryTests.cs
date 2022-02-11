using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories.DE;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureOpenTransactionRepositoryTests : AbstractOpenTransactionRepositoryTests
    {
        public override Task<IPersistentTransactionRepository<OpenTransaction>> CreateReadOnlyRepository(IEnumerable<OpenTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<OpenTransaction>> CreateRepository(IEnumerable<OpenTransaction> entries)
        {
            var azureReceiptJournalRepository = new AzureOpenTransactionRepository(Guid.NewGuid(), Constants.AzureStorageConnectionString);
            foreach (var entry in entries)
            {
                await azureReceiptJournalRepository.InsertAsync(entry);
            }

            return azureReceiptJournalRepository;
        }
    }
}
