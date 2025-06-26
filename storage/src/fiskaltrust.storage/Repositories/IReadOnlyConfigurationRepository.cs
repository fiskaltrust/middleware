using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyConfigurationRepository
    {
        Task<IEnumerable<ftCashBox>> GetCashBoxListAsync();
        Task<ftCashBox> GetCashBoxAsync(Guid cashBoxId);

        Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync();
        Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid signaturCreationUnitATId);

        Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync();
        Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid signaturCreationUnitDEId);

        Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync();
        Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId);

        Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync();
        Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid signaturCreationUnitFRId);

        Task<IEnumerable<ftSignaturCreationUnitIT>> GetSignaturCreationUnitITListAsync();
        Task<ftSignaturCreationUnitIT> GetSignaturCreationUnitITAsync(Guid signaturCreationUnitITId);

        Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync();
        Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitDEId);

        Task<IEnumerable<ftQueue>> GetQueueListAsync();
        Task<ftQueue> GetQueueAsync(Guid queueId);

        Task<IEnumerable<ftQueueAT>> GetQueueATListAsync();
        Task<ftQueueAT> GetQueueATAsync(Guid queueATId);

        Task<IEnumerable<ftQueueDE>> GetQueueDEListAsync();
        Task<ftQueueDE> GetQueueDEAsync(Guid queueDEId);

        Task<IEnumerable<ftQueueES>> GetQueueESListAsync();
        Task<ftQueueES> GetQueueESAsync(Guid queueESId);

        Task<IEnumerable<ftQueueFR>> GetQueueFRListAsync();
        Task<ftQueueFR> GetQueueFRAsync(Guid queueFRId);

        Task<IEnumerable<ftQueueIT>> GetQueueITListAsync();
        Task<ftQueueIT> GetQueueITAsync(Guid queueITId);

        Task<IEnumerable<ftQueueME>> GetQueueMEListAsync();
        Task<ftQueueME> GetQueueMEAsync(Guid queueMEId);
    }
}