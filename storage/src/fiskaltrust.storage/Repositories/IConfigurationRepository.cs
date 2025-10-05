using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IConfigurationRepository : IReadOnlyConfigurationRepository
    {
        Task InsertOrUpdateCashBoxAsync(ftCashBox cashBox);
        Task InsertOrUpdateQueueAsync(ftQueue queue);
        Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu);
        Task InsertOrUpdateSignaturCreationUnitBEAsync(ftSignaturCreationUnitBE scu);
        Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu);
        Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDK scu);
        Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu);
        Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu);
        Task InsertOrUpdateSignaturCreationUnitITAsync(ftSignaturCreationUnitIT scu);
        Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu);
        Task InsertOrUpdateQueueATAsync(ftQueueAT queue);
        Task InsertOrUpdateQueueBEAsync(ftQueueBE queue);
        Task InsertOrUpdateQueueDEAsync(ftQueueDE queue);
        Task InsertOrUpdateQueueDKAsync(ftQueueDK queue);
        Task InsertOrUpdateQueueESAsync(ftQueueES queue);
        Task InsertOrUpdateQueueEUAsync(ftQueueEU queue);
        Task InsertOrUpdateQueueFRAsync(ftQueueFR queue);
        Task InsertOrUpdateQueueITAsync(ftQueueIT queue);
        Task InsertOrUpdateQueueMEAsync(ftQueueME queue);
    }
}