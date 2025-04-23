using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.Middleware.Storage.EU;
namespace fiskaltrust.Middleware.Storage;

public interface IConfigurationRepository : IReadOnlyConfigurationRepository
{
    public Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu);

    public Task InsertOrUpdateQueueESAsync(ftQueueES queue);
    public Task InsertOrUpdateQueueEUAsync(ftQueueEU queue);

}