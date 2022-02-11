using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE
{
    public class EFCoreFailedStartTransactionRepository : AbstractEFCoreRepostiory<string, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public EFCoreFailedStartTransactionRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction) => await InsertOrUpdateAsync(transaction);

        public async Task<FailedStartTransaction> RemoveTransactionAsync(string cbReceiptReference) => await RemoveAsync(cbReceiptReference);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference) != null;

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
