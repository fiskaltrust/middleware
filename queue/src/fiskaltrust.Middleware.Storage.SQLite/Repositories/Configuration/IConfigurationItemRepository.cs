using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration
{
    public interface IConfigurationItemRepository<T>
    {
        Task InsertOrUpdateAsync(T entity);
    }
}
