using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE
{
    public class EFCoreOpenTransactionRepository : AbstractEFCoreRepostiory<string, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public EFCoreOpenTransactionRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task InsertOrUpdateTransactionAsync(OpenTransaction transaction) => await InsertOrUpdateAsync(transaction);

        public async Task<OpenTransaction> RemoveTransactionAsync(string cbReceiptReference) => await RemoveAsync(cbReceiptReference);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference) != null;

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
