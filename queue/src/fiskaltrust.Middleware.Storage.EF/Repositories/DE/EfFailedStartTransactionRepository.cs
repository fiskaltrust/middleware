using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE
{
    public class EfFailedStartTransactionRepository : AbstractEFRepostiory<string, FailedStartTransaction>, IPersistentTransactionRepository<FailedStartTransaction>
    {
        public EfFailedStartTransactionRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task InsertOrUpdateTransactionAsync(FailedStartTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<FailedStartTransaction> RemoveTransactionAsync(string cbReceiptReference) => await RemoveAsync(cbReceiptReference).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(FailedStartTransaction entity) { }

        protected override string GetIdForEntity(FailedStartTransaction entity) => entity.cbReceiptReference;
    }
}
