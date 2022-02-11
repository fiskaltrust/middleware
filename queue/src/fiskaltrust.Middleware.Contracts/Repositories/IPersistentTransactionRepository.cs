using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Contracts.Data
{
    public interface IPersistentTransactionRepository<T> where T : TseTransaction
    {
        Task InsertOrUpdateTransactionAsync(T transaction);
        Task<T> RemoveAsync(string cbReceiptReference);
        Task<T> GetAsync(string cbReceiptReference);
        Task<IEnumerable<T>> GetAsync();
        Task<bool> ExistsAsync(string cbReceiptReference);
    }
}
