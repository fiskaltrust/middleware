using System.Threading.Tasks;
namespace fiskaltrust.Middleware.Storage.ES;

public interface IConfigurationRepository : IReadOnlyConfigurationRepository
{
    Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu);

    Task InsertOrUpdateQueueESAsync(ftQueueES queue);

}