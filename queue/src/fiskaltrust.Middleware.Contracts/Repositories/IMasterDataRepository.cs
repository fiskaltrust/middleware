using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Repositories
{
    public interface IMasterDataRepository<T>
    {
        Task CreateAsync(T entity);
        Task<IEnumerable<T>> GetAsync();
        Task ClearAsync();
    }
}
