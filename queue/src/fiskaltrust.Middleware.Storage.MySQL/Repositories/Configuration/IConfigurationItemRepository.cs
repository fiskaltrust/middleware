using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public interface IConfigurationItemRepository<T>
    {
        Task InsertOrUpdateAsync(T entity);
    }
}
