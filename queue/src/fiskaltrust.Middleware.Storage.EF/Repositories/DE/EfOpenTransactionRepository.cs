using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE
{
    public class EfOpenTransactionRepository : AbstractEFRepostiory<string, OpenTransaction>, IPersistentTransactionRepository<OpenTransaction>
    {
        public EfOpenTransactionRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task InsertOrUpdateTransactionAsync(OpenTransaction transaction) => await InsertOrUpdateAsync(transaction).ConfigureAwait(false);

        public async Task<OpenTransaction> RemoveTransactionAsync(string cbReceiptReference) => await RemoveAsync(cbReceiptReference).ConfigureAwait(false);

        public async Task<bool> ExistsAsync(string cbReceiptReference) => await GetAsync(cbReceiptReference).ConfigureAwait(false) != null;

        protected override void EntityUpdated(OpenTransaction entity) { }

        protected override string GetIdForEntity(OpenTransaction entity) => entity.cbReceiptReference;
    }
}
