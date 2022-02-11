using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE
{
    public class EfFailedFinishTransactionRepository : AbstractEFRepostiory<string, FailedFinishTransaction>, IPersistentTransactionRepository<FailedFinishTransaction>
    {
        public EfFailedFinishTransactionRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task InsertOrUpdateTransactionAsync(FailedFinishTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<FailedFinishTransaction> RemoveTransactionAsync(string cbReceiptReference) => await RemoveAsync(cbReceiptReference).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedFinishTransaction entity) { }

        protected override string GetIdForEntity(FailedFinishTransaction entity) => entity.cbReceiptReference;
    }
}
